"""Підготовка й керування ізольованими Unity/worker процесами; PPO — лише після окремого погодження."""
import argparse
import json
import math
from pathlib import Path
import re
import shutil
import socket
import subprocess
import sys
import time
import uuid

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from ai_training.cases.training_schedule import build_schedule, check_run_signal, digest, validate_capture, read_run_signal

SOURCES = (
    'Assets/Scripts/AI/TheCopy/CopyAttentionRecipeCoordinator.cs',
    'Assets/Scripts/AI/TheCopy/CopyAttentionTrainingAgent.cs', 'Assets/Scripts/AI/TheCopy/CopyAttentionReward.cs',
    'Assets/Scripts/AI/TheCopy/CopyHearing.cs', 'Assets/Tests/AI/P406CoordinatorVerification.cs',
    'Assets/Tests/AI/P406TrainingVerification.cs', 'Assets/Tests/AI/P306PerceptionVerification.cs',
    'Assets/Scripts/AI/TheCopy/CopyDecisionContractV2.cs',
    'Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/ProjectVersion.txt')
TOOLS = ('tools/ai_npc_training_supervisor.py', 'tools/ai_npc_training_worker.py',
         'ai_training/cases/training_schedule.py', 'tools/verify_ai_npc_communicator.py')


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write(path, value):
    Path(path).write_text(json.dumps(value, indent=2, allow_nan=False) + '\n', encoding='utf-8')


def in_logs(path):
    path = Path(path).resolve()
    path.relative_to(ROOT / 'Logs/AI-NPC')
    return path


def prepare(workspace, recipe_plan, output, unity, python, epochs, seed, shuffle_seed, mode='api-smoke',
            diagnostic=False, port=5004, timeout=240, inject=False):
    import yaml
    from verify_ai_npc_ml import check_contract
    workspace, recipe_plan, output = map(in_logs, (workspace, recipe_plan, output))
    recipe_plan.relative_to(workspace / 'Logs/AI-NPC')
    if mode not in ('api-smoke', 'ppo') or type(timeout) is not int or not 10 <= timeout <= 7200:
        raise ValueError('Invalid mode or wall-clock budget')
    if type(port) is not int or not 1024 <= port <= 65535:
        raise ValueError('Invalid port')
    if mode == 'ppo' and (port != 5004 or diagnostic or inject):
        raise ValueError('PPO Editor requires port 5004, full train, and no fault injection')
    unity, python = Path(unity).resolve(), Path(python).resolve()
    if not unity.is_file() or not python.is_file() or not (workspace / 'Assets').is_dir():
        raise ValueError('Missing Editor, Python or disposable workspace')
    schedule = build_schedule(recipe_plan, epochs, seed, shuffle_seed, diagnostic)
    template = ROOT / 'ai_training/config/the_copy_draft2_ppo.yaml'
    config = yaml.safe_load(template.read_text(encoding='utf-8'))
    contract = read(ROOT / 'ai_training/config/the_copy_draft2_contract.json')
    source = (ROOT / 'Assets/Scripts/AI/TheCopy/CopyDecisionContractV2.cs').read_text(encoding='utf-8')
    encoder = re.search(r'public static class CopyDecisionEncoderV2.*?SchemaId = "([^"]+)".*?ObservationCount = (\d+)', source, re.S)
    if not encoder:
        raise ValueError('Missing encoder constants')
    check_contract(contract, encoder[1], int(encoder[2]), 7, config['behaviors'])
    if set(config['behaviors']) != {'TheCopyDraft2'}:
        raise ValueError('Only the approved behavior is allowed')
    behavior = config['behaviors']['TheCopyDraft2']
    if mode == 'ppo' and len(schedule['cases']) <= behavior['hyperparameters']['buffer_size']:
        raise ValueError('Pilot budget must exceed one PPO buffer; no zero-update nominal training')
    # Unity owns the exact finite episode budget; avoid trainer closing before the final terminal flush.
    behavior['max_steps'] = len(schedule['cases']) + 1
    config['engine_settings'] = {'time_scale': 1, 'target_frame_rate': 60}
    config['env_settings'] = {'num_envs': 1, 'num_areas': 1, 'max_lifetime_restarts': 0, 'timeout_wait': 120}
    hashes = {}
    for relative in SOURCES:
        hashes[relative] = digest(workspace / relative)
        if hashes[relative] != digest(ROOT / relative):
            raise ValueError('Disposable source differs: ' + relative)
    output.mkdir(parents=True, exist_ok=False)
    inputs = workspace / 'Logs/AI-NPC' / ('p406-supervisor-inputs-' + uuid.uuid4().hex)
    inputs.mkdir()
    write(output / 'schedule.json', schedule)
    shutil.copyfile(output / 'schedule.json', inputs / 'schedule.json')
    # JSON is valid YAML and accepted by the real ML-Agents YAML loader.
    write(output / 'ppo.yaml', config)
    spec = {'schema': 'the-copy.training-supervisor/1', 'mode': mode, 'workspace': str(workspace),
            'recipe_plan': str(recipe_plan), 'output': str(output), 'unity': str(unity), 'python': str(python),
            'signal': str(inputs / 'signal.json'), 'schedule': str(inputs / 'schedule.json'),
            'seed': seed, 'shuffle_seed': shuffle_seed, 'epochs': epochs, 'diagnostic': diagnostic,
            'planned_decisions': len(schedule['cases']), 'port': port, 'timeout_seconds': timeout,
            'inject_technical_failure': inject, 'run_id': 'TheCopy-' + uuid.uuid4().hex[:12],
            'source_sha256': hashes, 'config_sha256': digest(output / 'ppo.yaml'),
            'schedule_sha256': digest(output / 'schedule.json'), 'template_sha256': digest(template),
            'contract_sha256': digest(ROOT / 'ai_training/config/the_copy_draft2_contract.json'),
            'tool_sha256': {p: digest(ROOT / p) for p in TOOLS},
            'training_authorization': 'requires-separate-user-approval' if mode == 'ppo' else 'no-optimizer-api-test'}
    write(output / 'prepared.json', spec)
    (output / 'prepared.sha256').write_text(digest(output / 'prepared.json'), encoding='ascii')
    return spec


def validate_prepared(folder):
    folder = in_logs(folder)
    if digest(folder / 'prepared.json') != (folder / 'prepared.sha256').read_text(encoding='ascii'):
        raise ValueError('Prepared specification changed; prepare a fresh proposal')
    spec = read(folder / 'prepared.json')
    if spec['schema'] != 'the-copy.training-supervisor/1' or in_logs(spec['output']) != folder:
        raise ValueError('Unexpected prepared run')
    workspace = in_logs(spec['workspace'])
    in_logs(spec['recipe_plan']).relative_to(workspace / 'Logs/AI-NPC')
    for key in ('signal', 'schedule'):
        in_logs(spec[key]).relative_to(workspace / 'Logs/AI-NPC')
    if spec['mode'] not in ('api-smoke', 'ppo') or not 10 <= spec['timeout_seconds'] <= 7200:
        raise ValueError('Invalid prepared mode/budget')
    if spec['mode'] == 'ppo' and (spec['port'] != 5004 or spec['diagnostic'] or spec['inject_technical_failure']):
        raise ValueError('PPO requires full train and the Editor port 5004')
    if (digest(ROOT / 'ai_training/config/the_copy_draft2_ppo.yaml') != spec['template_sha256'] or
            digest(ROOT / 'ai_training/config/the_copy_draft2_contract.json') != spec['contract_sha256']):
        raise ValueError('Template or observation/action contract changed')
    for key, file in (('config_sha256', 'ppo.yaml'), ('schedule_sha256', 'schedule.json')):
        if digest(folder / file) != spec[key]:
            raise ValueError('Prepared artifact changed: ' + file)
    if digest(spec['schedule']) != spec['schedule_sha256']:
        raise ValueError('Sandbox schedule changed')
    schedule = build_schedule(spec['recipe_plan'], spec['epochs'], spec['seed'], spec['shuffle_seed'], spec['diagnostic'])
    if schedule != read(folder / 'schedule.json') or len(schedule['cases']) != spec['planned_decisions']:
        raise ValueError('Prepared schedule/source mismatch')
    for relative, expected in spec['source_sha256'].items():
        if relative not in SOURCES or digest(workspace / relative) != expected or digest(ROOT / relative) != expected:
            raise ValueError('Prepared source changed: ' + relative)
    if set(spec['source_sha256']) != set(SOURCES):
        raise ValueError('Incomplete source provenance')
    if set(spec['tool_sha256']) != set(TOOLS): raise ValueError('Incomplete tool provenance')
    for relative, expected in spec['tool_sha256'].items():
        if digest(ROOT / relative) != expected:
            raise ValueError('Prepared tool changed: ' + relative)
    return spec


def commands(spec):
    folder = Path(spec['output'])
    worker = [spec['python'], '-B', str(ROOT / 'tools/ai_npc_training_worker.py'), '--prepared', str(folder)]
    unity = [spec['unity'], '-batchmode', '-nographics', '-projectPath', spec['workspace'],
             '--mlagents-port', str(spec['port']), '-p305Disposable', '-p305WebSockets', '-p306Perception',
             '-p403PlayerSight', '-p403DecisionInputV2', '-p406TrainingBootstrap',
             '-p406CoordinatorPlan', spec['recipe_plan'], '-p406TrainingSchedule', spec['schedule'],
             '-p406RunSignal', spec['signal'], '-executeMethod', 'TheCopy.Tests.P305AcousticVerification.Run',
             '-logFile', str(folder / 'unity.log')]
    if spec['diagnostic']: unity += ['-p406BootstrapDiagnostic']
    if spec['inject_technical_failure']: unity += ['-p406InjectTechnicalFailure']
    if spec['mode'] == 'ppo': unity += ['-p406ExternalTrainer']
    return worker, unity


def stop_owned(process):
    if process is None or process.poll() is not None:
        return
    if sys.platform == 'win32':
        # PPO's SDK can own environment subprocesses. Restrict tree cleanup to this Popen PID.
        subprocess.run(['taskkill', '/PID', str(process.pid), '/T', '/F'], stdout=subprocess.PIPE,
                       stderr=subprocess.PIPE, timeout=10, **hidden())
    else:
        process.terminate()
    try: process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        process.kill(); process.wait(timeout=5)


def monitor(processes, signal, timeout, sleep=time.sleep, monotonic=time.monotonic):
    deadline = monotonic() + timeout
    while True:
        check_run_signal(signal)  # Check abort before accepting child exit codes.
        codes = [p.poll() for p in processes]
        if any(code is not None and code != 0 for code in codes):
            raise RuntimeError('Child process failed: ' + str(codes))
        if all(code is not None for code in codes):
            state = check_run_signal(signal)
            if not state or state['state'] != 'Completed':
                raise RuntimeError('Children exited without Unity completion')
            return codes
        if monotonic() > deadline:
            raise TimeoutError('Supervisor wall-clock budget exceeded')
        sleep(0.05)


def compare_api_records(rows, transitions):
    if len(rows) != len(transitions):
        raise ValueError('Worker terminal coverage differs from Unity records')
    for row, transition in zip(rows, transitions):
        if (row['action'] != transition['action'] or row['allowed_actions'] != transition['allowed_actions'] or
                len(row['observations']) != len(transition['observations']) or
                any(not math.isfinite(v) for v in row['observations'] + transition['observations'] +
                    [row['assigned_reward'], transition['terminal_reward']]) or
                any(abs(a-b) > 1e-6 for a,b in zip(row['observations'], transition['observations'])) or
                abs(row['assigned_reward']-transition['terminal_reward']) > 1e-6):
            raise ValueError('Worker terminal frame/action/reward differs from Unity record')


def preflight(folder):
    spec = validate_prepared(folder)
    worker, _ = commands(spec)
    subprocess.run(worker + ['--preflight'], check=True, timeout=120, **hidden())
    audit = read(Path(spec['output']) / 'sdk-preflight.json')
    if audit.get('config_sha256') != spec['config_sha256'] or audit.get('status') != 'pass':
        raise ValueError('SDK preflight did not accept the frozen config')
    return audit


def hidden():
    if sys.platform != 'win32': return {}
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    return {'startupinfo': startup}


def execute(folder, execute_ppo=False):
    spec = validate_prepared(folder)
    if spec['mode'] == 'ppo' and not execute_ppo:
        raise PermissionError('PPO requires prior user approval and explicit --execute-ppo; no process started')
    folder = Path(spec['output'])
    if (folder / 'started.json').exists() or Path(spec['signal']).exists():
        raise ValueError('Runs are single-use; prepare a new output, never resume/overwrite')
    preflight(folder)
    with socket.socket() as probe:
        probe.bind(('127.0.0.1', spec['port']))
    worker_command, unity_command = commands(spec)
    write(folder / 'started.json', {'worker': worker_command, 'unity': unity_command,
                                  'mode': spec['mode'], 'wall_clock_budget': spec['timeout_seconds'],
                                  'operator_execute_ppo_flag': execute_ppo})
    worker = unity = None
    result = None
    with (folder / 'worker.log').open('w', encoding='utf-8') as worker_log:
        try:
            worker = subprocess.Popen(worker_command, stdout=worker_log, stderr=subprocess.STDOUT, **hidden())
            unity = subprocess.Popen(unity_command, **hidden())
            write(folder / 'owned-processes.json', {'worker_pid': worker.pid, 'unity_pid': unity.pid})
            codes = monitor([worker, unity], spec['signal'], spec['timeout_seconds'])
            text = (folder / 'unity.log').read_text(encoding='utf-8', errors='replace')
            match = re.search(r'\[P4-06 COORDINATOR\] RECORDS_DIR=(.+)', text)
            if not match or '[P3-05] FAIL' in text or 'error CS' in text:
                raise ValueError('Unity verification did not finish cleanly')
            capture = in_logs(match.group(1).strip())
            capture.relative_to(Path(spec['workspace']) / 'Logs/AI-NPC')
            shutil.copytree(capture, folder / 'coordinator-capture')
            validate_capture(read(folder / 'schedule.json'), folder / 'coordinator-capture/complete', spec['planned_decisions'])
            from validate_ai_npc_coordinator import audit
            audit_result = audit(folder / 'coordinator-capture', folder / 'schedule.json')
            write(folder / 'capture-audit.json', audit_result)
            worker_result = read(folder / 'worker-result.json')
            if worker_result['planned_decisions'] != spec['planned_decisions'] or worker_result['mode'] != spec['mode']:
                raise ValueError('Worker budget mismatch')
            if spec['mode'] == 'api-smoke':
                rows = [json.loads(line) for line in (folder / 'coordinator-capture/complete/records.jsonl').read_text(encoding='utf-8').splitlines()]
                compare_api_records(rows, worker_result['terminal_transitions'])
            result = {'status': 'pass', 'valid_run': True, 'mode': spec['mode'], 'child_exit_codes': codes,
                      'completed_cases': spec['planned_decisions'], 'policy_quality': 'not_assessed',
                      'trainer_created': spec['mode'] == 'ppo',
                      'optimization_steps': 0 if spec['mode'] == 'api-smoke' else 'not_instrumented',
                      'worker_result': worker_result}
        except BaseException as error:
            result = {'status': 'failed', 'valid_run': False, 'mode': spec['mode'],
                      'error': type(error).__name__ + ': ' + str(error), 'artifact_policy': 'discard-run-do-not-select-or-resume',
                      'trainer_created': spec['mode'] == 'ppo',
                      'optimization_steps': 0 if spec['mode'] == 'api-smoke' else 'unknown-aborted-run'}
            signal = read_run_signal(spec['signal'])
            if signal is not None: result['unity_signal'] = signal
            raise
        finally:
            stop_owned(unity); stop_owned(worker)
            if result is not None:
                result['owned_processes_stopped'] = all(p is None or p.poll() is not None for p in (unity, worker))
                if result['status'] == 'failed' and (folder / 'unity.log').exists():
                    try:
                        text = (folder / 'unity.log').read_text(encoding='utf-8', errors='replace')
                        match = re.search(r'\[P4-06 COORDINATOR\] RECORDS_DIR=(.+)', text)
                        if match:
                            partial = in_logs(match.group(1).strip())
                            partial.relative_to(Path(spec['workspace']) / 'Logs/AI-NPC')
                            if partial.is_dir(): shutil.copytree(partial, folder / 'partial-capture')
                    except (OSError, ValueError) as copy_error:
                        result['partial_capture_error'] = str(copy_error)
                write(folder / 'result.json', result)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='action', required=True)
    prepare_parser = sub.add_parser('prepare')
    for key in ('workspace', 'recipe-plan', 'output', 'unity', 'python'):
        prepare_parser.add_argument('--' + key, type=Path, required=True)
    prepare_parser.add_argument('--mode', choices=('api-smoke', 'ppo'), default='api-smoke')
    prepare_parser.add_argument('--epochs', type=int, default=2)
    prepare_parser.add_argument('--seed', type=int, default=11)
    prepare_parser.add_argument('--shuffle-seed', type=int, default=7011)
    prepare_parser.add_argument('--port', type=int, default=5004)
    prepare_parser.add_argument('--timeout', type=int, default=240)
    prepare_parser.add_argument('--diagnostic', action='store_true')
    prepare_parser.add_argument('--inject-technical-failure', action='store_true')
    for action in ('preflight', 'run'):
        p = sub.add_parser(action); p.add_argument('--prepared', type=Path, required=True)
        if action == 'run': p.add_argument('--execute-ppo', action='store_true')
    args = parser.parse_args()
    try:
        if args.action == 'prepare':
            result = prepare(args.workspace, args.recipe_plan, args.output, args.unity, args.python, args.epochs,
                             args.seed, args.shuffle_seed, args.mode, args.diagnostic, args.port, args.timeout,
                             args.inject_technical_failure)
            print(json.dumps({k: result[k] for k in ('output', 'mode', 'planned_decisions', 'training_authorization')}, indent=2))
        elif args.action == 'preflight': print(json.dumps(preflight(args.prepared), indent=2))
        else: print(json.dumps({k:v for k,v in execute(args.prepared, args.execute_ppo).items() if k != 'worker_result'}, indent=2))
    except Exception as error:
        parser.exit(1, str(error) + '\n')


if __name__ == '__main__': main()
