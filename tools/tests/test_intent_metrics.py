import copy
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("intent_metrics", ROOT / "ai_training/evaluation/intent_metrics.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def target(client_id=1, x=3):
    return {"source_client_id": client_id, "position": [x, 1, 0]}


def record(case_id="case-1", action=0, selected_target=None, references=None):
    return {"schema": module.SCHEMA, "input_schema": module.INPUT_SCHEMA,
            "case_id": case_id, "scenario": "synthetic", "run_id": "unit-test", "policy_id": "synthetic-fixture",
            "split": "diagnostic", "seed": 11, "allowed_actions": [True, True, True, True, True, False, False],
            "action": action, "target": selected_target, "status": "accepted", "latency_ms": 5,
            "reference_outcomes": references}


class IntentMetricsTests(unittest.TestCase):
    def report(self, rows):
        return module.evaluate(rows, 0.05)["overall"]

    def test_matches_frozen_input_schema(self):
        contract = json.loads((ROOT / "ai_training/config/the_copy_draft2_contract.json").read_text())
        self.assertEqual(module.INPUT_SCHEMA, contract["inputSchema"])

    def test_empty_data_is_not_perfect_accuracy(self):
        result = self.report([])
        self.assertEqual(result["case_count"], 0)
        for key in ("acceptance_rate", "intent_accuracy", "target_accuracy", "selection_accuracy", "latency_mean_ms"):
            self.assertIsNone(result[key])

    def test_no_reference_does_not_invent_correctness(self):
        result = self.report([record()])
        self.assertEqual(result["accepted_count"], 1)
        self.assertEqual(result["reference_coverage"], 0)
        self.assertIsNone(result["selection_accuracy"])

    def test_correct_continue_has_no_target_metric(self):
        result = self.report([record(references=[{"action": 0, "target": None}])])
        self.assertEqual(result["selection_accuracy"], 1)
        self.assertIsNone(result["target_accuracy"])

    def test_wrong_id_does_not_pass_position_match(self):
        result = self.report([record(action=3, selected_target=target(0), references=[{"action": 3, "target": target(1)}])])
        self.assertEqual(result["intent_accuracy"], 1)
        self.assertEqual(result["target_accuracy"], 0)
        self.assertEqual(result["selection_accuracy"], 0)

    def test_host_zero_is_distinct_from_anonymous(self):
        result = self.report([record(action=1, selected_target=target(None), references=[{"action": 1, "target": target(0)}])])
        self.assertEqual(result["selection_accuracy"], 0)

    def test_anonymous_sound_is_valid(self):
        result = self.report([record(action=1, selected_target=target(None), references=[{"action": 1, "target": target(None)}])])
        self.assertEqual(result["accepted_reference_success_rate"], 1)

    def test_missing_visible_target_is_invalid_delivery(self):
        result = self.report([record(action=3, references=[{"action": 3, "target": target()}])])
        self.assertEqual(result["delivered_invalid_decision_count"], 1)
        self.assertEqual(result["target_accuracy"], 0)

    def test_position_tolerance(self):
        row = record(action=3, selected_target=target(x=0.05), references=[{"action": 3, "target": target(x=0)}])
        self.assertEqual(self.report([row])["target_accuracy"], 1)
        row["target"]["position"][0] = 0.051
        self.assertEqual(self.report([row])["target_accuracy"], 0)

    def test_multiple_explicit_references_without_priority_rule(self):
        row = record(action=4, selected_target=target(2), references=[{"action": 3, "target": target(1)}, {"action": 4, "target": target(2)}])
        self.assertEqual(self.report([row])["selection_accuracy"], 1)

    def test_masked_attempt_is_distinct_from_invalid_delivery(self):
        row = record(action=5)
        row.update(status="rejected", rejection_reason="masked")
        result = self.report([row])
        self.assertEqual(result["mask_violation_attempt_count"], 1)
        self.assertEqual(result["delivered_invalid_decision_count"], 0)
        row["status"] = "accepted"
        self.assertEqual(self.report([row])["delivered_invalid_decision_count"], 1)

    def test_rejected_correct_selection_is_not_delivered_success(self):
        row = record(action=3, selected_target=target(), references=[{"action": 3, "target": target()}])
        row.update(status="rejected", rejection_reason="stale")
        result = self.report([row])
        self.assertEqual(result["selection_accuracy"], 1)
        self.assertEqual(result["accepted_reference_success_rate"], 0)
        self.assertEqual(result["rejection_reasons"], {"stale": 1})

    def test_no_response_counts_as_failure_if_reference_exists(self):
        row = record(references=[{"action": 0, "target": None}])
        row.update(status="no_response", action=None, latency_ms=None)
        result = self.report([row])
        self.assertEqual(result["no_response_count"], 1)
        self.assertEqual(result["selection_accuracy"], 0)
        self.assertIsNone(result["latency_mean_ms"])

    def test_deliberate_cancellation_keeps_its_reason(self):
        row = record()
        row.update(status="no_response", action=None, latency_ms=None, rejection_reason="despawned")
        self.assertEqual(self.report([row])["no_response_reasons"], {"despawned": 1})

    def test_missing_latencies_do_not_become_zero(self):
        a, b = record(), record("case-2")
        b["latency_ms"] = None
        result = self.report([a, b])
        self.assertEqual(result["latency_sample_count"], 1)
        self.assertEqual(result["latency_mean_ms"], 5)

    def test_latency_quantiles_and_scenario_separation(self):
        a, b = record(), record("case-2")
        b.update(scenario="other", latency_ms=15)
        report = module.evaluate([a, b], 0.05)
        self.assertEqual(report["overall"]["latency_mean_ms"], 10)
        self.assertEqual(report["overall"]["latency_p95_nearest_rank_ms"], 15)
        self.assertEqual(report["by_scenario"]["other"]["case_count"], 1)

    def test_incomplete_reference_coverage_is_explicit(self):
        a = record(references=[{"action": 0, "target": None}])
        result = self.report([a, record("case-2")])
        self.assertEqual(result["reference_count"], 1)
        self.assertEqual(result["reference_coverage"], 0.5)

    def test_mixed_runs_policies_splits_or_seeds_are_rejected(self):
        for key, value in (("run_id", "other"), ("policy_id", "other"), ("split", "ood"), ("seed", 22)):
            with self.subTest(key=key):
                a, b = record(), record("case-2")
                b[key] = value
                with self.assertRaises(ValueError): self.report([a, b])

    def test_duplicate_cases_rejected(self):
        with self.assertRaises(ValueError): self.report([record(), record()])

    def test_invalid_masks_and_references_rejected(self):
        a = record()
        a["allowed_actions"][5] = True
        with self.assertRaises(ValueError): self.report([a])
        a = record(references=[{"action": 5, "target": None}])
        with self.assertRaises(ValueError): self.report([a])
        a = record(references=[])
        with self.assertRaises(ValueError): self.report([a])

    def test_nonfinite_and_boolean_data_rejected(self):
        for field, value in (("latency_ms", float("nan")), ("latency_ms", -1), ("action", True), ("seed", True), ("rejection_reason", [])):
            a = record()
            a[field] = value
            with self.assertRaises(ValueError): self.report([a])
        a = record(action=3, selected_target=target())
        a["target"]["position"][0] = float("inf")
        with self.assertRaises(ValueError): self.report([a])

    def test_out_of_range_action_is_counted_not_silently_dropped(self):
        result = self.report([record(action=999)])
        self.assertEqual(result["mask_violation_attempt_count"], 1)
        self.assertEqual(result["delivered_invalid_decision_count"], 1)

    def test_timeout_cannot_claim_response_latency(self):
        a = record()
        a.update(status="no_response", action=None)
        with self.assertRaises(ValueError): self.report([a])

    def test_no_reward_or_policy_quality_is_invented(self):
        report = module.evaluate([record()], 0.05)
        self.assertEqual(report["reward_status"], "not_defined")
        self.assertEqual(report["policy_quality_conclusion"], "not_assessed")
        self.assertNotIn("mean_reward", report["overall"])

    def test_input_not_mutated(self):
        a = record(references=[{"action": 0, "target": None}])
        before = copy.deepcopy(a)
        self.report([a])
        self.assertEqual(a, before)

    def test_cli_rejects_invalid_record_without_writing_success_report(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            records = folder / "invalid.jsonl"
            output = folder / "report.json"
            records.write_text("null\n", encoding="utf-8")
            result = subprocess.run([sys.executable, "-B", str(ROOT / "ai_training/evaluation/intent_metrics.py"),
                                     "--records", str(records), "--target-tolerance", "0.05", "--output", str(output)],
                                    capture_output=True, text=True, check=False)
            self.assertEqual(result.returncode, 2)
            self.assertFalse(output.exists())
            self.assertIn("Record must be an object", result.stderr)


if __name__ == "__main__":
    unittest.main()
