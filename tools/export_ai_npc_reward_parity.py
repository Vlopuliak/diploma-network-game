"""Export Python reward references for C# parity, from audited train/ID only; no training."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from ai_training.cases.validate_sensor_replay import validate
from ai_training.evaluation import expert_attention as expert


def export(plan, capture, output):
    output = Path(output).resolve()
    if "Logs" not in output.parts:
        raise ValueError("Parity fixtures must stay under ignored Logs")
    audit = validate(plan, capture)
    frames, hashes = [], {}
    for split in ("train", "id"):
        path = Path(capture) / f"{split}-inputs.jsonl"
        hashes[path.name] = hashlib.sha256(path.read_bytes()).hexdigest()
        for line in path.read_text(encoding="utf-8").splitlines():
            if not line.strip():
                continue
            row = json.loads(line)
            v, mask = row["observations"], row["allowed_actions"]
            frames.append({"case_id": row["case_id"], "observations": v, "allowed_actions": mask,
                           "expected_rewards": [expert.reward(v, mask, action) for action in range(7)]})
    result = {"schema": "the-copy.reward-parity/1", "profile_id": expert.PROFILE_ID,
              "input_schema": expert.INPUT_SCHEMA, "frame_count": len(frames), "frames": frames,
              "source_hashes": hashes, "training_steps": 0, "ood_vectors_read": False,
              "sensor_audit": audit}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, allow_nan=False, separators=(",", ":")) + "\n", encoding="utf-8")
    return {key: result[key] for key in ("schema", "frame_count", "source_hashes", "training_steps", "ood_vectors_read")}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--plan", type=Path, required=True)
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        result = export(args.plan, args.capture, args.output)
    except (ValueError, TypeError, KeyError, OSError) as error:
        parser.error(str(error))
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
