"""Перевірка завершеного PPO-пілота, Adam state і ONNX; не оцінює ID/OOD та не запускає inference."""
import argparse
import json
import math
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from ai_training.cases.training_schedule import digest


def optimizer_steps(state):
    steps = []
    for parameter in state.get('state', {}).values():
        value = parameter.get('step')
        if hasattr(value, 'item'):
            value = value.item()
        if type(value) not in (int, float) or not math.isfinite(value) or value <= 0 or int(value) != value:
            raise ValueError('Adam state has no positive integral optimizer step')
        steps.append(int(value))
    if not steps:
        raise ValueError('No Adam parameter state: optimization not demonstrated')
    return {'parameter_states': len(steps), 'minimum_step': min(steps), 'maximum_step': max(steps),
            'uniform_step': steps[0] if len(set(steps)) == 1 else None}


def inspect(prepared):
    import torch
    from ai_npc_training_supervisor import validate_prepared, read
    from verify_ai_npc_ml import inspect_model
    from validate_ai_npc_coordinator import audit
    folder = Path(prepared).resolve()
    folder.relative_to(ROOT / 'Logs/AI-NPC')
    spec = validate_prepared(folder)
    result = read(folder / 'result.json')
    if spec['mode'] != 'ppo' or not result.get('valid_run') or result.get('status') != 'pass':
        raise ValueError('Only a completed valid PPO pilot can pass this inspection')
    worker = result['worker_result']
    if worker['trainer_steps'] != spec['planned_decisions']:
        raise ValueError('Trainer step budget mismatch')
    artifacts = [Path(path).resolve() for path in worker['artifacts']]
    for path in artifacts:
        path.relative_to(folder / 'trainer')
    checkpoint = next(path for path in artifacts if path.suffix == '.pt')
    model = next(path for path in artifacts if path.suffix == '.onnx')
    # Only this supervisor's freshly generated local checkpoint is accepted, never an arbitrary download.
    saved = torch.load(checkpoint, map_location='cpu', weights_only=False)
    step_report = optimizer_steps(saved['Optimizer:value_optimizer'])
    moment_nonzero, moment_max = 0, 0.0
    for parameter in saved['Optimizer:value_optimizer']['state'].values():
        for key in ('exp_avg', 'exp_avg_sq'):
            if not torch.is_tensor(parameter.get(key)) or not bool(torch.isfinite(parameter[key]).all()):
                raise ValueError('Adam moments are missing or non-finite')
        moment_nonzero += int(torch.count_nonzero(parameter['exp_avg']).item())
        moment_max = max(moment_max, float(parameter['exp_avg'].abs().max().item()))
    if moment_nonzero == 0:
        raise ValueError('No nonzero Adam first moment: gradient evidence missing')
    global_steps = saved['global_step']
    if len(global_steps) != 1 or int(next(iter(global_steps.values())).item()) != spec['planned_decisions']:
        raise ValueError('Checkpoint global_step differs from trainer/Unity budget')
    tensor_count, parameter_count = 0, 0
    for value in saved['Policy'].values():
        if not torch.is_tensor(value) or not bool(torch.isfinite(value).all()):
            raise ValueError('Policy state is malformed or non-finite')
        tensor_count += 1
        parameter_count += value.numel()
    contract = read(ROOT / 'ai_training/config/the_copy_draft2_contract.json')
    signature = inspect_model(model, contract)
    if not signature['signature_matches']:
        raise ValueError('Exported ONNX has an incompatible signature')
    capture = audit(folder / 'coordinator-capture', folder / 'schedule.json')
    return {'status': 'pass', 'purpose': 'COMPLETED_TRAINING_ARTIFACT_CHECK_NOT_POLICY_EVALUATION',
            'trainer_steps': worker['trainer_steps'], 'captured_decisions': capture['completed_cases'],
            'optimizer': step_report, 'policy_state_tensors': tensor_count, 'policy_state_elements': parameter_count,
            'adam_first_moment_nonzero_elements': moment_nonzero, 'adam_first_moment_max_abs': moment_max,
            'checkpoint_sha256': digest(checkpoint), 'onnx_sha256': digest(model), 'onnx': signature,
            'schema_provenance': 'sealed-training-input-contract-not-asserted-by-shape',
            'policy_quality': 'not_assessed', 'id_policy_evaluation': 'not_run', 'ood_vectors_read': False,
            'game_inference': 'not_run'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--prepared', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.resolve().relative_to(ROOT / 'Logs/AI-NPC')
    try:
        result = inspect(args.prepared)
    except Exception as error:
        parser.exit(1, str(error) + '\n')
    args.output.write_text(json.dumps(result, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k != 'onnx'}, indent=2))


if __name__ == '__main__': main()
