"""Відтворювані рецепти сцени, не синтетичні ML-вектори і не тренування."""
import argparse
import hashlib
import json
from pathlib import Path
import random

CONFIG = Path(__file__).resolve().parents[1] / "config/sensory_intent_v1_dataset_design.json"
FAMILIES = ["quiet", "sound-live", "sound-memory", "light", "player-a", "player-b", "both-players", "competition"]


def load_design():
    design = json.loads(CONFIG.read_text(encoding="utf-8"))
    if (design.get("dataset_id") != "the-copy.attention-cases/1" or design.get("families") != FAMILIES or
            design.get("generation_seeds") != {"train": 1001, "id": 2001, "ood": 3001} or
            design.get("active_cases_per_family") != {"train": 64, "id": 32, "ood": 32} or
            design.get("quiet_cases_per_split") != 1 or
            design.get("status") != "approved-2026-11-23-recipes-only-no-training" or
            design.get("input_schema") != "the-copy.decision-input/sensory-intent-v1" or
            design.get("profile_id") != "the-copy.expert-attention/1" or
            design.get("protocol_id") != "the-copy.expert-attention-evaluation/1"):
        raise ValueError("Unsupported dataset design")
    return design


def generate_split(split):
    design = load_design()
    if split not in design["generation_seeds"]:
        raise ValueError("Unknown split")
    rng = random.Random(design["generation_seeds"][split])
    cases = []
    for family in FAMILIES:
        for index in range(1 if family == "quiet" else design["active_cases_per_family"][split]):
            ood = split == "ood"
            # Active parameters only: unused nuisance fields do not hide duplicate recipes.
            recipe = {"case_id": f"{split}/{family}/{index:03d}", "scenario": family}
            if family in {"sound-live", "sound-memory", "competition", "player-b", "both-players"}:
                recipe["player_b_distance"] = round(rng.uniform(12, 17) if ood else rng.uniform(3, 10), 6)
                recipe["player_b_angle"] = round(rng.uniform(15, 40), 6)
            if family in {"player-a", "both-players"}:
                recipe["player_a_distance"] = round(rng.uniform(12, 17) if ood else rng.uniform(3, 10), 6)
                recipe["player_a_angle"] = round(rng.uniform(-40, -15), 6)
            if family in {"sound-live", "sound-memory", "competition"}:
                bounds = ((0.05, 0.15) if rng.randrange(2) == 0 else (0.90, 1.0)) if ood else (0.25, 0.80)
                recipe["sound_loudness"] = round(rng.uniform(*bounds), 6)
            if family == "sound-memory":
                recipe["delay_fraction"] = round(rng.uniform(0.70, 0.90) if ood else rng.uniform(0.20, 0.50), 6)
            if family in {"light", "competition"}:
                # Light position owns host placement here; no independent contradictory A distance.
                recipe["light_range_fraction"] = round(rng.uniform(0.75, 0.94) if ood else rng.uniform(0.20, 0.55), 6)
                recipe["player_a_angle"] = round(rng.uniform(-40, -15), 6)
            cases.append(recipe)
    return {"schema": "the-copy.attention-recipes/1", "dataset_id": design["dataset_id"],
            "split": split, "generation_seed": design["generation_seeds"][split],
            "input_schema": design["input_schema"], "profile_id": design["profile_id"],
            "protocol_id": design["protocol_id"], "cases": cases}


def fingerprint(recipe):
    return hashlib.sha256(json.dumps({k: v for k, v in recipe.items() if k != "case_id"},
                                    sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def audit_splits(splits):
    seen, duplicate_quiet = {}, 0
    for split, bundle in splits.items():
        for recipe in bundle["cases"]:
            key = fingerprint(recipe)
            if key in seen:
                if recipe["scenario"] != "quiet":
                    raise ValueError("Duplicate active recipe: " + recipe["case_id"])
                duplicate_quiet += 1
            seen[key] = split
    return {"active_recipe_duplicates": 0, "declared_quiet_repetitions": duplicate_quiet,
            "captured_vector_duplicates": "not_checked_until_sensor_replay",
            "counts": {split: len(bundle["cases"]) for split, bundle in splits.items()},
            "policy_evaluation": "not_run", "training_steps": 0}


def export(output):
    output = Path(output).resolve()
    if "Logs" not in output.parts:
        raise ValueError("Generated artifacts must stay under an ignored Logs directory")
    bundles = {split: generate_split(split) for split in ("train", "id", "ood")}
    audit = audit_splits(bundles)
    # Never overwrite a frozen set; caller must use a new output directory.
    output.mkdir(parents=True, exist_ok=False)
    hashes = {}
    for split, bundle in bundles.items():
        manifest = {"schema": "the-copy.attention-case-manifest/1", "dataset_id": bundle["dataset_id"],
                    "split": split, "cases": [{"case_id": c["case_id"], "scenario": c["scenario"]} for c in bundle["cases"]]}
        for name, value in ((f"{split}-recipes.json", bundle), (f"{split}-manifest.json", manifest)):
            data = (json.dumps(value, sort_keys=True, indent=2) + "\n").encode()
            (output / name).write_bytes(data)
            hashes[name] = hashlib.sha256(data).hexdigest()
    audit["artifact_sha256"] = hashes
    audit["design_sha256"] = hashlib.sha256(CONFIG.read_bytes()).hexdigest()
    (output / "generation-audit.json").write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
    return audit


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        audit = export(args.output)
    except (ValueError, OSError) as error:
        parser.error(str(error))
    print(json.dumps(audit, indent=2))


if __name__ == "__main__":
    main()
