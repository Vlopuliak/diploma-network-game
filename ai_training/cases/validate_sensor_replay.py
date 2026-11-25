"""Перевіряє train/ID sensor-only replay. OOD-вектори й оцінки політики не читає."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from ai_training.cases import sensory_intent_cases as recipes
from ai_training.evaluation import attention_metrics, expert_attention


def validate_row(row, recipe, bundle):
    if not isinstance(row, dict) or row.get("schema") != "the-copy.attention-sensor-input/1":
        raise ValueError("Expected sensor-only record")
    for key in ("dataset_id", "split", "input_schema", "profile_id", "protocol_id"):
        if row.get(key) != bundle[key]:
            raise ValueError("Replay disagrees with recipe bundle: " + key)
    if (row.get("case_id") != recipe["case_id"] or row.get("scenario") != recipe["scenario"] or
            row.get("generation_seed") != bundle["generation_seed"] or
            row.get("capture_purpose") != "SENSOR_REPLAY_ONLY_NO_POLICY" or "action" in row):
        raise ValueError("Wrong case identity or sensor-only purpose")
    vector, mask = row.get("observations"), row.get("allowed_actions")
    expert_attention.validate_snapshot(vector, mask)
    family = recipe["scenario"]
    sound = family in {"sound-live", "sound-memory", "competition"}
    light = family in {"light", "competition"}
    a = family in {"player-a", "both-players", "competition"}
    b = family in {"player-b", "both-players", "competition"}
    expected = [int(sound and family != "sound-memory"), int(sound), int(light), int(a), int(b)]
    if [vector[i] for i in (0, 12, 24, 40, 45)] != expected or vector[30] != 0:
        raise ValueError("Captured sensor state does not realize the recipe")


def validate(plan, capture):
    plan, capture = Path(plan), Path(capture)
    audit = json.loads((plan / "generation-audit.json").read_text(encoding="utf-8"))
    counts, seen, quiet_repetitions = {}, {}, 0
    for split in ("train", "id"):
        recipe_path, manifest_path = plan / f"{split}-recipes.json", plan / f"{split}-manifest.json"
        for path in (recipe_path, manifest_path):
            if hashlib.sha256(path.read_bytes()).hexdigest() != audit["artifact_sha256"][path.name]:
                raise ValueError("Frozen recipe/manifest hash changed")
        bundle = json.loads(recipe_path.read_text(encoding="utf-8"))
        if bundle != recipes.generate_split(split):
            raise ValueError("Recipes are not reproducible from the approved design")
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        expected = attention_metrics.validate_manifest(manifest)
        cases = {case["case_id"]: case for case in bundle["cases"]}
        if expected != {key: recipe["scenario"] for key, recipe in cases.items()}:
            raise ValueError("Manifest disagrees with recipes")
        provenance = json.loads((capture / f"{split}-provenance.json").read_text(encoding="utf-8"))
        if (provenance["recipe_sha256"] != audit["artifact_sha256"][recipe_path.name] or
                provenance["model_requests"] != 0 or provenance["training_steps"] != 0 or provenance["ood_read"] is not False):
            raise ValueError("Replay provenance is not a sensor-only run of the frozen recipes")
        rows = [json.loads(line) for line in (capture / f"{split}-inputs.jsonl").read_text(encoding="utf-8").splitlines() if line.strip()]
        observed = set()
        for row in rows:
            case_id = row.get("case_id") if isinstance(row, dict) else None
            if case_id not in cases or case_id in observed:
                raise ValueError("Unknown or duplicate captured case")
            observed.add(case_id)
            validate_row(row, cases[case_id], bundle)
            # Float rounding tolerance is explicit; recipe duplicates are checked separately.
            vector_key = tuple(round(float(v), 6) for v in row["observations"])
            if vector_key in seen:
                if row["scenario"] != "quiet" or seen[vector_key] != "quiet":
                    raise ValueError("Duplicate active captured vector within/between train and ID")
                quiet_repetitions += 1
            seen[vector_key] = row["scenario"]
        if observed != set(cases) or provenance["captured_cases"] != len(rows):
            raise ValueError("Incomplete captured manifest")
        counts[split] = len(rows)
    return {"schema": "the-copy.sensor-replay-audit/1", "counts": counts,
            "active_vector_duplicates": 0, "declared_quiet_repetitions": quiet_repetitions,
            "vector_rounding_decimals": 6, "ood_vectors_read": False, "model_requests": 0,
            "training_steps": 0, "policy_quality": "not_evaluated"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--plan", type=Path, required=True)
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        report = validate(args.plan, args.capture)
    except (ValueError, KeyError, TypeError, OSError) as error:
        parser.error(str(error))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
