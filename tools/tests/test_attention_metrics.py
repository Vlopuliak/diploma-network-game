"""Контроль арифметики та знаменників на синтетичних даних, не оцінка NN."""
import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
from ai_training.evaluation import attention_metrics as module


def record(case="one", scenario="competition", action=2):
    vector = [0.0] * 50
    vector[0] = vector[2] = vector[7] = vector[24] = 1
    vector[1], vector[25] = 0.2, 0.8
    target = None if action == 0 else {"source_client_id": 1, "position": [3, 1, 0]}
    return {"schema": module.SCHEMA, "input_schema": module.expert.INPUT_SCHEMA,
            "profile_id": module.expert.PROFILE_ID, "dataset_id": "synthetic-only",
            "case_id": case, "scenario": scenario, "run_id": "unit-test", "policy_id": "synthetic-fixture",
            "split": "diagnostic", "seed": 11, "observations": vector,
            "allowed_actions": [True, True, True, False, False, False, False],
            "action": action, "target": target, "status": "accepted", "rejection_reason": None,
            "latency_ms": 5, "reference_outcomes": None, "decision_outcome": "decision"}


def manifest(*cases):
    return {"schema": module.MANIFEST_SCHEMA, "dataset_id": "synthetic-only", "split": "diagnostic",
            "cases": [{"case_id": case, "scenario": scenario} for case, scenario in cases]}


class AttentionMetricsTests(unittest.TestCase):
    def report(self, rows, planned=None, epsilon=0.05):
        if planned is None:
            planned = manifest(*[(r["case_id"], r["scenario"]) for r in rows])
        return module.evaluate(rows, planned, near_optimal_regret=epsilon)

    def test_known_reward_regret_and_paired_baselines(self):
        report = self.report([record(action=1)])["overall"]
        self.assertEqual(report["selection_mean_reward"], 0.25)
        self.assertEqual(report["selection_mean_regret"], 0.75)
        self.assertEqual(report["near_optimal_selection_rate"], 0)
        self.assertAlmostEqual(report["paired_uniform_expected_reward"], 1.25 / 3)
        self.assertAlmostEqual(report["paired_gain_over_uniform"], 0.25 - 1.25 / 3)
        self.assertEqual(report["paired_gain_over_mask_rule"], 0)
        self.assertEqual(report["paired_argmax_reward"], 1)

    def test_exact_ties_all_count_as_optimal(self):
        rows = [record(case=str(i), action=i) for i in (1, 2)]
        for row in rows:
            row["observations"][1] = 0.8
        self.assertEqual(self.report(rows, epsilon=0)["overall"]["near_optimal_selection_rate"], 1)

    def test_quiet_continue_and_baselines_all_score_one(self):
        row = record(scenario="quiet", action=0)
        row["observations"] = [0.0] * 50
        row["allowed_actions"] = [True, False, False, False, False, False, False]
        result = self.report([row], epsilon=0)["overall"]
        self.assertEqual(result["accepted_near_optimal_rate_of_planned"], 1)
        self.assertEqual(result["observed_uniform_expected_reward"], 1)
        self.assertEqual(result["paired_gain_over_uniform"], 0)

    def test_snapshot_mask_mismatch_is_data_error(self):
        row = record()
        row["allowed_actions"][1] = False
        with self.assertRaises(ValueError):
            self.report([row])

    def test_near_optimal_boundary_is_explicit(self):
        row = record(action=1)
        row["observations"][1], row["observations"][25] = 0.95, 1
        self.assertEqual(self.report([row], epsilon=0.05)["overall"]["near_optimal_selection_rate"], 1)
        self.assertEqual(self.report([row], epsilon=0.049)["overall"]["near_optimal_selection_rate"], 0)

    def test_bad_tolerance_rejected(self):
        for epsilon in (None, True, -0.1, 1, float("nan"), float("inf")):
            with self.subTest(epsilon=epsilon), self.assertRaises(ValueError):
                self.report([record()], epsilon=epsilon)

    def test_technical_failure_not_zero_reward_or_disappearing_case(self):
        row = record(case="stale")
        row.update(status="rejected", decision_outcome="stale", rejection_reason="stale")
        result = self.report([record(), row])["overall"]
        self.assertEqual(result["selection_mean_reward"], 1)
        self.assertEqual(result["technical_outcome_count"], 1)
        self.assertEqual(result["accepted_near_optimal_rate_of_planned"], 0.5)
        self.assertEqual(result["selection_coverage_of_observed"], 0.5)

    def test_missing_and_unobserved_scenario_not_hidden_by_macro(self):
        plan = manifest(("one", "competition"), ("missing", "quiet"))
        result = self.report([record()], plan)
        self.assertFalse(result["complete"])
        self.assertEqual(result["missing_case_ids"], ["missing"])
        self.assertEqual(result["overall"]["accepted_near_optimal_rate_of_planned"], 0.5)
        self.assertIsNone(result["macro_by_scenario"]["selection_mean_reward"])
        self.assertEqual(result["macro_by_scenario"]["selection_mean_reward_scenario_coverage"], 0.5)
        self.assertIsNone(result["by_scenario"]["quiet"]["selection_mean_reward"])

    def test_empty_records_with_plan_are_not_perfect(self):
        result = self.report([], manifest(("one", "quiet")))
        self.assertIsNone(result["context"])
        self.assertIsNone(result["overall"]["selection_mean_reward"])
        self.assertEqual(result["overall"]["accepted_near_optimal_rate_of_planned"], 0)
        self.assertEqual(result["overall"]["missing_case_count"], 1)

    def test_micro_and_macro_differ_for_unbalanced_scenarios(self):
        rows = [record("a", "good"), record("b", "good"), record("c", "bad", action=0)]
        result = self.report(rows)
        self.assertAlmostEqual(result["overall"]["selection_mean_reward"], 2 / 3)
        self.assertEqual(result["macro_by_scenario"]["selection_mean_reward"], 0.5)

    def test_invalid_initial_choice_minus_one_not_near_optimal(self):
        row = record(action=5)
        row.update(status="rejected", rejection_reason="masked_or_unavailable")
        result = self.report([row])["overall"]
        self.assertEqual(result["selection_mean_reward"], -1)
        self.assertEqual(result["selection_mean_regret"], 2)
        self.assertEqual(result["near_optimal_selection_rate"], 0)

    def test_accepted_invalid_payload_is_counted_and_cannot_be_delivery_success(self):
        row = record()
        row["target"] = None
        result = self.report([row])["overall"]
        self.assertEqual(result["selection_mean_reward"], 1)
        self.assertEqual(result["accepted_invalid_count"], 1)
        self.assertEqual(result["accepted_near_optimal_rate_of_planned"], 0)

    def test_rejected_good_choice_is_not_accepted_success(self):
        row = record()
        row.update(status="rejected", rejection_reason="masked_or_unavailable")
        result = self.report([row])["overall"]
        self.assertEqual(result["selection_mean_reward"], 1)
        self.assertEqual(result["accepted_near_optimal_rate_of_planned"], 0)

    def test_all_technical_outcomes_have_no_paired_selection_gain(self):
        for outcome, status, reason in (("no_response", "no_response", "timeout"),
                                        ("cancelled", "no_response", "despawned"),
                                        ("technical_rejection", "rejected", "target_changed")):
            row = record()
            row.update(status=status, rejection_reason=reason, decision_outcome=outcome)
            if status == "no_response":
                row.update(action=None, target=None, latency_ms=None)
            result = self.report([row])["overall"]
            self.assertIsNone(result["selection_mean_reward"])
            self.assertIsNone(result["paired_gain_over_uniform"])
            self.assertIsNotNone(result["observed_uniform_expected_reward"])
            self.assertEqual(result["accepted_near_optimal_rate_of_planned"], 0)

    def test_inconsistent_outcomes_rejected(self):
        for changes in ({"decision_outcome": "stale"}, {"decision_outcome": None},
                        {"rejection_reason": "stale"},
                        {"status": "rejected", "rejection_reason": "target_changed"}):
            row = record()
            row.update(changes)
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                self.report([row])

    def test_bad_data_never_gets_negative_reward_instead_of_error(self):
        for field, value in (("observations", None), ("profile_id", "wrong"),
                             ("dataset_id", "wrong"), ("schema", "the-copy.intent-evaluation/1")):
            row = record()
            row[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                self.report([row])

    def test_manifest_duplicate_extra_and_wrong_scenario_rejected(self):
        plans = [manifest(), manifest(("one", "quiet")), manifest(("other", "competition")),
                 manifest(("one", "competition"), ("one", "competition"))]
        for plan in plans:
            with self.subTest(plan=plan), self.assertRaises(ValueError):
                self.report([record()], plan)

    def test_mixed_context_or_duplicate_case_rejected(self):
        for field, value in (("run_id", "another"), ("policy_id", "random"), ("seed", 22), ("split", "ood")):
            a, b = record(), record("two")
            b[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                self.report([a, b])
        with self.assertRaises(ValueError):
            self.report([record(), record()], manifest(("one", "competition")))

    def test_input_not_mutated_and_no_quality_pass(self):
        row = record()
        original = copy.deepcopy(row)
        result = self.report([row])
        self.assertEqual(row, original)
        self.assertEqual(result["quality_gate"], "not_assessed_train_or_diagnostic_split")

    def test_protocol_gate_id_90_percent_and_ood_80_percent(self):
        rows = [record(str(i), action=2 if i < 8 else 0) for i in range(10)]
        plan = manifest(*[(r["case_id"], r["scenario"]) for r in rows])
        for split, expected in (("id", "fail_on_supplied_records"), ("ood", "pass_on_supplied_records")):
            for row in rows:
                row["split"] = split
            plan["split"] = split
            self.assertEqual(self.report(rows, plan)["quality_gate"], expected)
        rows[8]["action"], rows[8]["target"] = 2, {"source_client_id": 1, "position": [3, 1, 0]}
        plan["split"] = "id"
        for row in rows:
            row["split"] = "id"
        self.assertEqual(self.report(rows, plan)["quality_gate"], "pass_on_supplied_records")

    def test_protocol_gate_requires_complete_and_safe_records(self):
        row = record()
        row["split"] = "id"
        plan = manifest(("one", "competition"), ("missing", "competition"))
        plan["split"] = "id"
        result = self.report([row], plan)
        self.assertFalse(result["quality_checks"]["complete_manifest"])
        self.assertEqual(result["quality_gate"], "fail_on_supplied_records")
        plan["cases"].pop()
        row["target"] = None
        result = self.report([row], plan)
        self.assertFalse(result["quality_checks"]["no_accepted_invalid"])
        self.assertEqual(result["quality_gate"], "fail_on_supplied_records")

    def test_protocol_gate_uses_macro_not_easy_case_count(self):
        rows = [record(str(i), "easy") for i in range(9)] + [record("hard", "hard", action=0)]
        for row in rows:
            row["split"] = "id"
        plan = manifest(*[(r["case_id"], r["scenario"]) for r in rows])
        plan["split"] = "id"
        result = self.report(rows, plan)
        self.assertEqual(result["overall"]["accepted_near_optimal_rate_of_planned"], 0.9)
        self.assertEqual(result["macro_by_scenario"]["accepted_near_optimal_rate_of_planned"], 0.5)
        self.assertEqual(result["quality_gate"], "fail_on_supplied_records")

    def test_nonprotocol_tolerance_never_gets_quality_pass(self):
        row = record()
        row["split"] = "id"
        plan = manifest(("one", "competition"))
        plan["split"] = "id"
        result = self.report([row], plan, epsilon=0.99)
        self.assertEqual(result["quality_gate"], "not_assessed_nonprotocol_tolerance")
        self.assertIsNone(result["quality_checks"])

    def test_changed_protocol_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "protocol.json"
            protocol = module.load_protocol()
            protocol["minimum_by_split"]["id"] = 0.5
            path.write_text(json.dumps(protocol))
            with self.assertRaises(ValueError):
                module.load_protocol(path)

    def test_cli_positive_and_negative_do_not_invent_success(self):
        with tempfile.TemporaryDirectory() as folder:
            folder = Path(folder)
            records, plan, output = folder / "records.jsonl", folder / "manifest.json", folder / "report.json"
            records.write_text(json.dumps(record()) + "\n")
            plan.write_text(json.dumps(manifest(("one", "competition"))))
            args = [sys.executable, "-B", str(ROOT / "ai_training/evaluation/attention_metrics.py"),
                    "--records", str(records), "--manifest", str(plan), "--near-optimal-regret", "0.05"]
            result = subprocess.run(args + ["--output", str(output)], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(json.loads(output.read_text())["overall"]["selection_mean_reward"], 1)
            records.write_text("null\n")
            bad_output = folder / "bad.json"
            result = subprocess.run(args + ["--output", str(bad_output)], capture_output=True, text=True)
            self.assertEqual(result.returncode, 2)
            self.assertFalse(bad_output.exists())


if __name__ == "__main__":
    unittest.main()
