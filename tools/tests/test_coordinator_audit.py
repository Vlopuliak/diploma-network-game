import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("coordinator_audit", ROOT / "tools/validate_ai_npc_coordinator.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def write_case(folder):
    row = {"schema": module.attention_metrics.SCHEMA, "profile_id": module.expert_attention.PROFILE_ID,
           "input_schema": module.expert_attention.INPUT_SCHEMA, "dataset_id": "fixture", "split": "train",
           "run_id": "unit", "policy_id": "controlled", "seed": 0, "generation_seed": 1001,
           "case_id": "quiet", "scenario": "quiet", "observations": [0.0]*50,
           "allowed_actions": [True,False,False,False,False,False,False], "action": 0, "target": None,
           "status": "accepted", "rejection_reason": None, "decision_outcome": "decision",
           "latency_ms": None, "reference_outcomes": None, "assigned_reward": 1, "cumulative_reward": 1}
    summary = {"state": "Completed", "valid_run": True, "abort_reason": None,
               "planned_cases": 1, "prepared_cases": 1, "recorded_cases": 1}
    context = {"source_split": "train", "ood_vectors_read": False, "policy_seed": 0}
    manifest = {"schema": module.attention_metrics.MANIFEST_SCHEMA, "dataset_id": "fixture", "split": "train",
                "cases": [{"case_id": "quiet", "scenario": "quiet"}]}
    for name, data in (("summary.json", summary), ("run-context.json", context), ("manifest.json", manifest)):
        (folder / name).write_text(json.dumps(data), encoding="utf-8")
    (folder / "records.jsonl").write_text(json.dumps(row) + "\n", encoding="utf-8")
    return row, summary, context


class CoordinatorAuditTests(unittest.TestCase):
    def test_complete_one_step_run_is_checked_without_claiming_policy_quality(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            write_case(folder)
            summary, _, metrics = module.audit_case_run(folder)
            self.assertEqual(summary["state"], "Completed")
            self.assertTrue(metrics["complete"])
            self.assertEqual(metrics["quality_gate"], "not_assessed_train_or_diagnostic_split")

    def test_wrong_or_accumulated_reward_rejected(self):
        for key in ("assigned_reward", "cumulative_reward"):
            with self.subTest(key=key), tempfile.TemporaryDirectory() as directory:
                folder = Path(directory)
                row, _, _ = write_case(folder)
                row[key] = 2
                (folder / "records.jsonl").write_text(json.dumps(row), encoding="utf-8")
                with self.assertRaises(ValueError): module.audit_case_run(folder)

    def test_missing_case_cannot_be_completed_run(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            _, summary, _ = write_case(folder)
            (folder / "records.jsonl").write_text("")
            summary["recorded_cases"] = summary["prepared_cases"] = 0
            (folder / "summary.json").write_text(json.dumps(summary))
            with self.assertRaises(ValueError): module.audit_case_run(folder)

    def test_nonfinite_and_nonnumeric_rewards_rejected(self):
        for key in ("assigned_reward", "cumulative_reward"):
            for value in (float("nan"), float("inf"), None, True, "1"):
                with self.subTest(key=key, value=value), tempfile.TemporaryDirectory() as directory:
                    folder = Path(directory)
                    row, _, _ = write_case(folder)
                    row[key] = value
                    (folder / "records.jsonl").write_text(json.dumps(row), encoding="utf-8")
                    with self.assertRaises(ValueError): module.audit_case_run(folder)

    def test_policy_seed_is_not_silently_replaced_with_generation_seed(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            row, _, _ = write_case(folder)
            row["seed"] = 1001
            (folder / "records.jsonl").write_text(json.dumps(row))
            with self.assertRaises(ValueError): module.audit_case_run(folder)

    def test_training_source_cannot_be_id_or_ood(self):
        for split in ("id", "ood"):
            with self.subTest(split=split), tempfile.TemporaryDirectory() as directory:
                folder = Path(directory)
                _, _, context = write_case(folder)
                context["source_split"] = split
                (folder / "run-context.json").write_text(json.dumps(context))
                with self.assertRaises(ValueError): module.audit_case_run(folder)


if __name__ == "__main__":
    unittest.main()
