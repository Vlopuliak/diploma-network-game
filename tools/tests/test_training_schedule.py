import json
from pathlib import Path
import tempfile
import unittest
import threading
import time
from unittest.mock import patch
import ai_training.cases.training_schedule as scheduler

from ai_training.cases.sensory_intent_cases import generate_split
from ai_training.cases.training_schedule import build_schedule, check_run_signal, digest, validate_capture


def fixture(folder):
    bundle = generate_split("train")
    manifest = {"schema": "the-copy.attention-case-manifest/1", "dataset_id": bundle["dataset_id"], "split": "train",
                "cases": [{"case_id": c["case_id"], "scenario": c["scenario"]} for c in bundle["cases"]]}
    for name, data in (("train-recipes.json", bundle), ("train-manifest.json", manifest)):
        (folder / name).write_text(json.dumps(data), encoding="utf-8")
    audit = {"artifact_sha256": {name: digest(folder / name) for name in ("train-recipes.json", "train-manifest.json")}}
    (folder / "generation-audit.json").write_text(json.dumps(audit), encoding="utf-8")
    return manifest


class TrainingScheduleTests(unittest.TestCase):
    def test_transient_signal_permission_is_retried_but_not_reported_as_success(self):
        with patch.object(scheduler, '_read_signal_once', side_effect=[PermissionError(), {'state':'Aborted', 'reason':'fault'}]):
            with self.assertRaisesRegex(RuntimeError, 'fault'): check_run_signal('signal.json')
        with patch.object(scheduler, '_read_signal_once', side_effect=PermissionError()):
            with self.assertRaises(PermissionError): scheduler.read_run_signal('signal.json')

    def test_signal_reader_allows_concurrent_atomic_replacement(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'signal.json'
            path.write_text(json.dumps({'state':'Running'}))
            errors = []
            def writer():
                try:
                    for index in range(100):
                        temp = path.with_suffix('.tmp')
                        temp.write_text(json.dumps({'state':'Running', 'index':index}))
                        for attempt in range(25):
                            try:
                                temp.replace(path)
                                break
                            except PermissionError:
                                if attempt == 24: raise
                                time.sleep(0.002)
                except Exception as error: errors.append(error)
            thread = threading.Thread(target=writer)
            thread.start()
            reads = 0
            try:
                while thread.is_alive():
                    signal = scheduler.read_run_signal(path)
                    self.assertEqual(signal['state'], 'Running')
                    reads += 1
            finally: thread.join(timeout=5)
            self.assertFalse(errors)
            self.assertGreater(reads, 0)

    def test_full_epochs_cover_each_frozen_case_once_with_distinct_occurrence_ids(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            manifest = fixture(folder)
            schedule = build_schedule(folder, 2, 11, 7011)
            self.assertEqual(len(schedule["cases"]), 898)
            self.assertEqual(len({c["case_id"] for c in schedule["cases"]}), 898)
            for epoch in range(2):
                entries = schedule["cases"][epoch * 449:(epoch + 1) * 449]
                self.assertEqual({c["source_case_id"] for c in entries}, {c["case_id"] for c in manifest["cases"]})
                self.assertTrue(all(c["epoch"] == epoch for c in entries))

    def test_shuffle_is_deterministic_and_independent_of_generation_and_policy_seeds(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            fixture(folder)
            first = build_schedule(folder, 2, 11, 7011)
            self.assertEqual(first, build_schedule(folder, 2, 11, 7011))
            self.assertEqual(first["cases"], build_schedule(folder, 2, 22, 7011)["cases"])
            self.assertNotEqual(first["cases"], build_schedule(folder, 2, 11, 7022)["cases"])
            self.assertEqual(first["generation_seed"], 1001)
            self.assertNotEqual([c["source_case_id"] for c in first["cases"][:449]],
                                [c["source_case_id"] for c in first["cases"][449:]])

    def test_diagnostic_repeats_are_explicit_not_research_split(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            fixture(folder)
            schedule = build_schedule(folder, 2, 11, 7011, True)
            self.assertEqual(len(schedule["cases"]), 16)
            self.assertEqual(schedule["selection"], "first-per-family-diagnostic")
            self.assertEqual(len({c["source_case_id"] for c in schedule["cases"]}), 8)

    def test_id_and_ood_are_not_opened_and_frozen_train_files_unchanged(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            fixture(folder)
            before = digest(folder / "train-recipes.json")
            # Invalid files would throw if opened; neither is an input to schedule generation.
            (folder / "id-recipes.json").write_text("not JSON")
            (folder / "ood-recipes.json").write_text("not JSON")
            build_schedule(folder, 1, 11, 7011)
            self.assertEqual(digest(folder / "train-recipes.json"), before)

    def test_tampered_train_hash_and_invalid_budget_seeds_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            fixture(folder)
            for epochs, seed, shuffle in ((0, 11, 1), (10001, 11, 1), (1, -1, 1), (1, 11, True), (True, 11, 1)):
                with self.subTest(epochs=epochs, seed=seed, shuffle=shuffle), self.assertRaises(ValueError):
                    build_schedule(folder, epochs, seed, shuffle)
            (folder / "train-recipes.json").write_text("{}")
            with self.assertRaises(ValueError): build_schedule(folder, 2, 11, 7011)

    def test_abort_signal_stops_python_instead_of_returning_zero_reward(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "signal.json"
            self.assertIsNone(check_run_signal(path))
            path.write_text(json.dumps({"state": "Running"}))
            self.assertEqual(check_run_signal(path)["state"], "Running")
            path.write_text(json.dumps({"state": "Aborted", "reason": "technical_failure"}))
            with self.assertRaisesRegex(RuntimeError, "technical_failure"): check_run_signal(path)

    def test_unknown_signal_is_not_success(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "signal.json"
            path.write_text(json.dumps({"state": "unknown"}))
            with self.assertRaises(ValueError): check_run_signal(path)

    def test_capture_must_match_epoch_order_coverage_and_seed_metadata(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            fixture(folder)
            schedule = build_schedule(folder, 2, 11, 7011, True)
            summary = {"state": "Completed", "valid_run": True, "recorded_cases": 16, "prepared_cases": 16}
            (folder / "summary.json").write_text(json.dumps(summary))
            (folder / "run-context.json").write_text(json.dumps(schedule))
            (folder / "manifest.json").write_text(json.dumps({"cases": [{"case_id": e["case_id"], "scenario": e["scenario"]} for e in schedule["cases"]]}))
            records = "\n".join(json.dumps(c) for c in schedule["cases"])
            (folder / "records.jsonl").write_text(records)
            self.assertEqual(validate_capture(schedule, folder, 16), 16)
            with self.assertRaises(ValueError): validate_capture(schedule, folder, 15)
            reversed_records = "\n".join(json.dumps(c) for c in reversed(schedule["cases"]))
            (folder / "records.jsonl").write_text(reversed_records)
            with self.assertRaises(ValueError): validate_capture(schedule, folder, 16)


if __name__ == "__main__":
    unittest.main()
