import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("preflight", Path(__file__).parents[1] / "verify_ai_npc_ml.py")
preflight = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preflight)


class PreflightTests(unittest.TestCase):
    def setUp(self):
        self.contract = {"inputSchema": "draft-2", "vectorObservationSize": 50, "stackedVectors": 1,
                         "scope": "single-decision-sensory-intent", "enabledIntentIndices": [0, 1, 2, 3, 4],
                         "actionExecution": False,
                         "discreteBranches": [7], "permanentlyMaskedActions": [5, 6], "behaviorName": "Copy"}

    def validate(self):
        preflight.check_contract(self.contract, "draft-2", 50, 7, {"Copy": {}})

    def test_matching_contract(self):
        self.validate()

    def test_old_width_rejected(self):
        self.contract["vectorObservationSize"] = 27
        with self.assertRaises(ValueError): self.validate()

    def test_wrong_schema_rejected(self):
        self.contract["inputSchema"] = "draft-1"
        with self.assertRaises(ValueError): self.validate()

    def test_mechanism_unmask_rejected(self):
        self.contract["permanentlyMaskedActions"] = []
        with self.assertRaises(ValueError): self.validate()

    def test_multiple_stacks_rejected(self):
        self.contract["stackedVectors"] = 2
        with self.assertRaises(ValueError): self.validate()

    def test_wrong_behavior_rejected(self):
        self.contract["behaviorName"] = "OldStage4"
        with self.assertRaises(ValueError): self.validate()

    def test_action_execution_scope_rejected(self):
        self.contract["actionExecution"] = True
        with self.assertRaises(ValueError): self.validate()

    def test_extra_enabled_intent_rejected(self):
        self.contract["enabledIntentIndices"] = [0, 1, 2, 3, 4, 5]
        with self.assertRaises(ValueError): self.validate()

    def test_signature_match(self):
        self.assertEqual(preflight.check_model_signature({"obs_0": ["batch", 50], "action_masks": ["batch", 7]},
                                                        {"discrete_actions": ["batch", 1]}, 50, 7), [])

    def test_old_signature_rejected(self):
        self.assertTrue(preflight.check_model_signature({"obs_0": ["batch", 27], "action_masks": ["batch", 7]},
                                                       {"discrete_actions": ["batch", 1]}, 50, 7))

    def test_unknown_width_rejected(self):
        self.assertTrue(preflight.check_model_signature({"obs_0": ["batch", "unknown"], "action_masks": ["batch", 7]},
                                                       {"discrete_actions": ["batch", 1]}, 50, 7))

    def test_missing_mask_or_output_rejected(self):
        self.assertTrue(preflight.check_model_signature({"obs_0": ["batch", 50]}, {}, 50, 7))

    def test_dependency_closure_excludes_unrelated_prototype_packages(self):
        reference = {"com.unity.ml-agents": {"version": "4.1.0", "dependencies": {"inference": "2.6.1"}},
                     "inference": {"version": "2.6.1", "dependencies": {"burst": "1.8"}},
                     "burst": {"version": "2.0"}, "unrelated": {"version": "99"}}
        current = {"burst": {"version": "2.0"}}
        result = preflight.dependency_proposal(current, reference)
        self.assertEqual(result["added"], {"com.unity.ml-agents": "4.1.0", "inference": "2.6.1"})
        self.assertEqual(result["existing_version_changes"], {})
        self.assertEqual(current, {"burst": {"version": "2.0"}})

    def test_dependency_change_is_reported(self):
        reference = {"com.unity.ml-agents": {"version": "4.1.0"}}
        current = {"com.unity.ml-agents": {"version": "3"}}
        result = preflight.dependency_proposal(current, reference)
        self.assertEqual(result["existing_version_changes"]["com.unity.ml-agents"]["current"], "3")

    def test_missing_reference_dependency_rejected(self):
        with self.assertRaises(ValueError): preflight.dependency_proposal({}, {})


if __name__ == "__main__":
    unittest.main()
