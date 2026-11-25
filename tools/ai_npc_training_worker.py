"""Дочірній клієнт supervisor: preflight, API без optimizer або явно погоджений PPO."""
import argparse
from importlib.metadata import version
import json
from pathlib import Path
import random
import sys
import time

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from ai_npc_training_supervisor import read, write, validate_prepared


def trainer_arguments(spec):
    folder = Path(spec['output'])
    return [str(folder / 'ppo.yaml'), '--run-id', spec['run_id'], '--results-dir', str(folder / 'trainer'),
            '--seed', str(spec['seed']), '--timeout-wait', '120']


def sdk_preflight(spec):
    from mlagents.trainers.learn import parse_command_line
    options = parse_command_line(trainer_arguments(spec))
    settings = options.behaviors['TheCopyDraft2']
    if (settings.trainer_type != 'ppo' or settings.max_steps != spec['planned_decisions'] + 1 or
            options.env_settings.env_path is not None or options.env_settings.num_envs != 1 or
            options.env_settings.max_lifetime_restarts != 0 or options.env_settings.seed != spec['seed'] or
            options.checkpoint_settings.resume or options.checkpoint_settings.force or
            options.checkpoint_settings.initialize_from is not None):
        raise ValueError('SDK options disagree with frozen proposal')
    result = {'status': 'pass', 'config_sha256': spec['config_sha256'], 'trainer_created': False,
              'optimization_steps': 0, 'sdk_options_parsed': True, 'python': sys.version,
              'versions': {name: version(name) for name in ('mlagents', 'mlagents-envs', 'torch', 'numpy', 'protobuf')},
              'editor_ppo_port': 5004, 'max_steps_safety_cap': settings.max_steps}
    write(Path(spec['output']) / 'sdk-preflight.json', result)
    return options


def api_client(spec):
    import numpy as np
    from mlagents_envs.environment import UnityEnvironment
    from mlagents_envs.base_env import ActionTuple
    from mlagents_envs.exception import UnityCommunicatorStoppedException
    from ai_training.cases.training_schedule import check_run_signal
    from ai_training.evaluation.expert_attention import validate_snapshot
    from verify_ai_npc_communicator import choose_allowed, check_terminal
    env = UnityEnvironment(file_name=None, base_port=spec['port'], seed=spec['seed'], timeout_wait=120)
    pending, completed = {}, []
    rng = random.Random(spec['seed'])
    try:
        env.reset()
        deadline = time.monotonic() + spec['timeout_seconds']
        while time.monotonic() < deadline:
            check_run_signal(spec['signal'])
            for name, shape in env.behavior_specs.items():
                if (len(shape.observation_specs) != 1 or shape.observation_specs[0].shape != (50,) or
                        list(shape.action_spec.discrete_branches) != [7] or shape.action_spec.continuous_size != 0):
                    raise ValueError('Unexpected worker observation/action spec')
                decisions, terminals = env.get_steps(name)
                for i, identity in enumerate(terminals.agent_id):
                    previous = pending.pop((name, int(identity)), None)
                    if previous is None: continue  # Terminal-only initial reset/teardown has no action/experience.
                    if not check_terminal(previous['observations'], previous['allowed_actions'], previous['action'],
                                          terminals.reward[i], bool(terminals.interrupted[i])):
                        raise RuntimeError('A prepared API case was interrupted')
                    completed.append(dict(previous, terminal_reward=float(terminals.reward[i])))
                actions = []
                for i, identity in enumerate(decisions.agent_id):
                    if decisions.action_mask is None or len(decisions.action_mask) != 1:
                        raise ValueError('Missing mask')
                    blocked = decisions.action_mask[0][i].tolist()
                    action = choose_allowed(blocked, rng)
                    frame, mask = decisions.obs[0][i].tolist(), [not v for v in blocked]
                    validate_snapshot(frame, mask)
                    key = (name, int(identity))
                    if key in pending: raise ValueError('Second action in a one-step episode')
                    pending[key] = {'observations': frame, 'allowed_actions': mask, 'action': action}
                    actions.append([action])
                if actions:
                    env.set_actions(name, ActionTuple(discrete=np.asarray(actions, dtype=np.int32)))
            try: env.step()
            except UnityCommunicatorStoppedException: break
        else: raise TimeoutError('API worker deadline exceeded')
        signal = check_run_signal(spec['signal'])
        if not signal or signal['state'] != 'Completed' or len(completed) != spec['planned_decisions'] or pending:
            raise ValueError('Incomplete API terminal transitions')
    finally:
        env.close()  # Release gRPC before supervisor waits for Editor shutdown.
    return {'mode': 'api-smoke', 'trainer_created': False, 'optimization_steps': 0,
            'planned_decisions': spec['planned_decisions'], 'terminal_transitions': completed}


def ppo_client(spec):
    if spec['port'] != 5004 or spec['diagnostic'] or spec['inject_technical_failure']:
        raise ValueError('Invalid PPO run configuration')
    from mlagents.trainers.learn import parse_command_line, run_cli
    run_cli(parse_command_line(trainer_arguments(spec)))
    status_path = Path(spec['output']) / 'trainer' / spec['run_id'] / 'run_logs/training_status.json'
    status = read(status_path)
    checkpoint = status['TheCopyDraft2']['final_checkpoint']
    if checkpoint['steps'] != spec['planned_decisions']:
        raise ValueError('Trainer steps disagree with finite Unity episode budget')
    run_root = status_path.parent.parent.resolve()
    artifacts = [Path(checkpoint['file_path']).resolve()] + [Path(p).resolve() for p in checkpoint['auxillary_file_paths']]
    for artifact in artifacts:
        artifact.relative_to(run_root)
        if not artifact.is_file(): raise ValueError('Missing trainer artifact: ' + str(artifact))
    if not any(p.suffix == '.onnx' for p in artifacts) or not any(p.suffix == '.pt' for p in artifacts):
        raise ValueError('Final ONNX and PyTorch checkpoint are required')
    return {'mode': 'ppo', 'trainer_created': True, 'optimization_steps': 'not_instrumented',
            'planned_decisions': spec['planned_decisions'], 'trainer_steps': checkpoint['steps'],
            'training_status': str(status_path), 'artifacts': [str(p) for p in artifacts]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--prepared', type=Path, required=True)
    parser.add_argument('--preflight', action='store_true')
    args = parser.parse_args()
    spec = validate_prepared(args.prepared)
    if args.preflight:
        sdk_preflight(spec)
        return
    # A worker may not bypass its parent's explicit start/gate.
    started = read(Path(spec['output']) / 'started.json')
    if started['mode'] != spec['mode']:
        raise ValueError('Supervisor start mode mismatch')
    result = api_client(spec) if spec['mode'] == 'api-smoke' else ppo_client(spec)
    write(Path(spec['output']) / 'worker-result.json', result)


if __name__ == '__main__': main()
