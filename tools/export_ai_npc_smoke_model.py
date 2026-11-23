"""Export an untrained, seeded ML-Agents policy for wiring tests only; no optimizer or environment."""
import argparse
import hashlib
import json
from pathlib import Path
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--include-negative-fixture", action="store_true")
    args = parser.parse_args()
    if "Logs" not in args.output.resolve().parts:
        raise ValueError("Smoke model output must stay in an ignored Logs directory")
    from mlagents.torch_utils import torch
    from mlagents.torch_utils.torch import set_torch_config
    from mlagents.trainers.settings import NetworkSettings, TorchSettings
    from mlagents.trainers.policy.torch_policy import TorchPolicy
    from mlagents.trainers.torch_entities.networks import SimpleActor
    from mlagents.trainers.torch_entities.model_serialization import ModelSerializer
    from mlagents_envs.base_env import ActionSpec, BehaviorSpec, DimensionProperty, ObservationSpec, ObservationType
    import onnx

    set_torch_config(TorchSettings(device="cpu"))
    torch.set_default_device("cpu")
    contract_path = Path(__file__).resolve().parents[1] / "ai_training/config/the_copy_draft2_contract.json"
    contract = json.loads(contract_path.read_text(encoding="utf-8"))

    def export(path, width, schema, purpose):
        torch.manual_seed(11)
        spec = BehaviorSpec([ObservationSpec((width,), (DimensionProperty.NONE,), ObservationType.DEFAULT, "vector")],
                            ActionSpec.create_discrete(tuple(contract["discreteBranches"])))
        policy = TorchPolicy(11, spec, NetworkSettings(hidden_units=128, num_layers=2, normalize=False),
                             SimpleActor, {"conditional_sigma": False, "tanh_squash": False})
        policy.actor.eval()
        path.parent.mkdir(parents=True, exist_ok=True)
        ModelSerializer(policy).export_policy_model(str(path.with_suffix("")))
        model = onnx.load(str(path))
        onnx.helper.set_model_props(model, {"the-copy.input-schema": schema, "the-copy.purpose": purpose,
                                          "the-copy.training-steps": "0"})
        onnx.checker.check_model(model)
        onnx.save(model, str(path))
        report = {"purpose": purpose, "input_schema": schema, "seed": 11, "training_steps": 0,
                  "observations": width, "discrete_branches": contract["discreteBranches"], "hidden_layers": [128, 128],
                  "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
        path.with_suffix(".json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(report, indent=2))

    export(args.output, contract["vectorObservationSize"], contract["inputSchema"], "UNTRAINED_SMOKE_TEST_ONLY")
    if args.include_negative_fixture:
        export(args.output.with_name(args.output.stem + "-wrong-width.onnx"), 27,
               "incompatible-27-input-test", "NEGATIVE_WIDTH_FIXTURE_ONLY")


if __name__ == "__main__":
    sys.exit(main())
