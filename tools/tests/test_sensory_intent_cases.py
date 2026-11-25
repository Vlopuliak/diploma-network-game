import copy
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
from ai_training.cases import sensory_intent_cases as module
from ai_training.cases import validate_sensor_replay as replay
from ai_training.evaluation import attention_metrics


class SensoryIntentCasesTests(unittest.TestCase):
    def test_approved_counts_and_family_balance(self):
        for split, count, active in (("train", 449, 64), ("id", 225, 32), ("ood", 225, 32)):
            bundle = module.generate_split(split)
            self.assertEqual(len(bundle["cases"]), count)
            self.assertEqual(sum(c["scenario"] == "quiet" for c in bundle["cases"]), 1)
            for family in module.FAMILIES[1:]:
                self.assertEqual(sum(c["scenario"] == family for c in bundle["cases"]), active)

    def test_generation_is_reproducible_and_not_a_vector_dataset(self):
        first = module.generate_split("train")
        self.assertEqual(first, module.generate_split("train"))
        self.assertEqual(first["generation_seed"], 1001)
        self.assertTrue(all("observations" not in case and "action" not in case for case in first["cases"]))

    def test_splits_have_disjoint_case_ids_and_active_recipes(self):
        bundles = {split: module.generate_split(split) for split in ("train", "id", "ood")}
        report = module.audit_splits(bundles)
        self.assertEqual(report["active_recipe_duplicates"], 0)
        self.assertEqual(report["declared_quiet_repetitions"], 2)
        ids = [case["case_id"] for b in bundles.values() for case in b["cases"]]
        self.assertEqual(len(ids), len(set(ids)))

    def test_duplicate_active_recipe_rejected(self):
        bundle = module.generate_split("train")
        duplicate = copy.deepcopy(bundle["cases"][1])
        duplicate["case_id"] = "another-id"
        bundle["cases"].append(duplicate)
        with self.assertRaises(ValueError):
            module.audit_splits({"train": bundle})

    def test_ood_has_predeclared_observable_shifts(self):
        for case in module.generate_split("ood")["cases"]:
            for key in ("player_a_distance", "player_b_distance"):
                if key in case:
                    self.assertTrue(12 <= case[key] <= 17)
            if "sound_loudness" in case:
                self.assertTrue(0.05 <= case["sound_loudness"] <= 0.15 or 0.90 <= case["sound_loudness"] <= 1)
            if "light_range_fraction" in case:
                self.assertTrue(0.75 <= case["light_range_fraction"] <= 0.95)
            if "delay_fraction" in case:
                self.assertTrue(0.70 <= case["delay_fraction"] <= 0.90)

    def test_train_id_ranges_share_domain_but_have_different_recipes(self):
        for split in ("train", "id"):
            for case in module.generate_split(split)["cases"]:
                for key in ("player_a_distance", "player_b_distance"):
                    if key in case:
                        self.assertTrue(3 <= case[key] <= 10)
                if "sound_loudness" in case:
                    self.assertTrue(0.25 <= case["sound_loudness"] <= 0.80)
                if "light_range_fraction" in case:
                    self.assertTrue(0.20 <= case["light_range_fraction"] <= 0.65)
                if "delay_fraction" in case:
                    self.assertTrue(0.20 <= case["delay_fraction"] <= 0.50)

    def test_frozen_export_hashes_and_manifests(self):
        with tempfile.TemporaryDirectory() as folder:
            output = Path(folder) / "Logs" / "dataset"
            result = module.export(output)
            for name, expected_hash in result["artifact_sha256"].items():
                self.assertEqual(hashlib.sha256((output / name).read_bytes()).hexdigest(), expected_hash)
            for split in ("train", "id", "ood"):
                manifest = json.loads((output / f"{split}-manifest.json").read_text())
                self.assertEqual(len(attention_metrics.validate_manifest(manifest)), result["counts"][split])
            with self.assertRaises(FileExistsError):
                module.export(output)

    def test_unsafe_destination_and_unknown_split_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                module.export(Path(folder) / "dataset")
        with self.assertRaises(ValueError):
            module.generate_split("test")

    def test_replay_rejects_missing_artifacts(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(OSError):
                replay.validate(folder, folder)

    def test_replay_rejects_modified_frozen_recipe_before_reading_capture(self):
        with tempfile.TemporaryDirectory() as folder:
            plan = Path(folder) / "Logs" / "plan"
            module.export(plan)
            (plan / "train-recipes.json").write_text("{}")
            with self.assertRaisesRegex(ValueError, "hash changed"):
                replay.validate(plan, Path(folder) / "no-capture")

    def test_sensor_row_checks_actual_state_and_prevents_fake_policy_data(self):
        bundle = module.generate_split("train")
        recipe = bundle["cases"][0]
        row = {key: bundle[key] for key in ("dataset_id", "split", "input_schema", "profile_id", "protocol_id")}
        row.update(schema="the-copy.attention-sensor-input/1", generation_seed=bundle["generation_seed"],
                   case_id=recipe["case_id"], scenario="quiet", observations=[0.0]*50,
                   allowed_actions=[True,False,False,False,False,False,False], capture_purpose="SENSOR_REPLAY_ONLY_NO_POLICY")
        replay.validate_row(row, recipe, bundle)
        for changes in ({"action": 0}, {"capture_purpose": "policy"}, {"generation_seed": 3}, {"observations": []}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                replay.validate_row(dict(row, **changes), recipe, bundle)


if __name__ == "__main__":
    unittest.main()
