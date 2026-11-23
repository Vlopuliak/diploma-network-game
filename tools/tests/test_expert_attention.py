"""Синтетичні перевірки формул; не результати політики чи гри."""
import importlib.util
import itertools
import json
from pathlib import Path
import random
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("expert_attention", ROOT / "ai_training/evaluation/expert_attention.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def snapshot(sound=None, memory=None, light=None, player_a=None, player_b=None, approximate=False):
    vector = [0.0] * 50
    for offset, strength in ((0, sound), (12, memory)):
        if strength is not None:
            vector[offset] = vector[offset + 2] = vector[offset + 7] = 1
            vector[offset + 1] = strength
    if light is not None:
        vector[24], vector[25], vector[30] = 1, light, int(approximate)
    for offset, distance in ((40, player_a), (45, player_b)):
        if distance is not None:
            vector[offset], vector[offset + 4] = 1, distance
    mask = [True, sound is not None or memory is not None,
            light is not None and not approximate, player_a is not None, player_b is not None, False, False]
    return vector, mask


class ExpertAttentionTests(unittest.TestCase):
    def test_profile_matches_frozen_contract(self):
        profile = module.load_profile()
        contract = json.loads((ROOT / "ai_training/config/the_copy_draft2_contract.json").read_text())
        self.assertEqual(profile["input_schema"], contract["inputSchema"])
        self.assertEqual(profile["observation_count"], contract["vectorObservationSize"])
        self.assertEqual(profile["action_count"], contract["discreteBranches"][0])

    def test_changed_formula_or_schema_requires_new_implementation(self):
        for key, value in (("player_floor", 0.02), ("input_schema", "wrong"), ("invalid_action_reward", 0)):
            with self.subTest(key=key), tempfile.TemporaryDirectory() as folder:
                profile = module.load_profile()
                profile[key] = value
                path = Path(folder) / "profile.json"
                path.write_text(json.dumps(profile))
                with self.assertRaises(ValueError):
                    module.load_profile(path)

    def test_quiet_only_continue(self):
        vector, mask = snapshot()
        self.assertEqual(module.utilities(vector, mask), (1, None, None, None, None, None, None))
        self.assertEqual(module.reward(vector, mask, 0), 1)
        self.assertEqual(module.reward(vector, mask, 1), -1)

    def test_invalid_or_unavailable_selection_is_minus_one(self):
        vector, mask = snapshot(sound=0.5)
        for action in (-1, 2, 3, 4, 5, 6, 7, True, 1.0, None, "1", []):
            with self.subTest(action=action):
                self.assertEqual(module.reward(vector, mask, action), -1)

    def test_known_competition_has_fractional_reward(self):
        vector, mask = snapshot(sound=0.2, light=0.8, player_a=1)
        self.assertEqual(module.reward(vector, mask, 0), 0)
        self.assertEqual(module.reward(vector, mask, 1), 0.25)
        self.assertEqual(module.reward(vector, mask, 2), 1)
        self.assertAlmostEqual(module.reward(vector, mask, 3), 0.0125)

    def test_memory_preferred_even_if_live_stronger(self):
        vector, mask = snapshot(sound=0.9, memory=0.2, light=0.8)
        self.assertEqual(module.reward(vector, mask, 1), 0.25)
        vector[1] = 0.1
        self.assertEqual(module.reward(vector, mask, 1), 0.25)

    def test_memory_freshness_not_multiplied_twice(self):
        vector, mask = snapshot(memory=0.2, light=0.8)
        vector[14] = 0.01
        self.assertEqual(module.reward(vector, mask, 1), 0.25)

    def test_approximate_light_is_excluded(self):
        vector, mask = snapshot(light=0.9, approximate=True)
        self.assertEqual(module.reward(vector, mask, 0), 1)
        self.assertIsNone(module.utilities(vector, mask)[2])
        mask[2] = True
        with self.assertRaises(ValueError):
            module.reward(vector, mask, 2)

    def test_player_distance_endpoints_and_floor(self):
        vector, mask = snapshot(player_a=0, player_b=1)
        self.assertEqual(module.reward(vector, mask, 3), 1)
        self.assertEqual(module.reward(vector, mask, 4), 0.01)

    def test_player_closeness_is_monotonic(self):
        rewards = []
        for distance in (1, 0.75, 0.5, 0.25, 0):
            vector, mask = snapshot(light=1, player_a=distance)
            rewards.append(module.reward(vector, mask, 3))
        self.assertEqual(rewards, sorted(set(rewards)))

    def test_ties_all_get_one_argmax_tie_is_stable(self):
        vector, mask = snapshot(sound=1, light=1, player_a=0, player_b=0)
        for action in range(1, 5):
            self.assertEqual(module.reward(vector, mask, action), 1)
        self.assertEqual(module.baselines(vector, mask, rng=random.Random(11))["argmax_expert_action"], 1)

    def test_context_and_identity_flags_do_not_change_expert_scale(self):
        vector, mask = snapshot(sound=0.2, light=0.8)
        original = module.utilities(vector, mask)
        for index in (11, 31, 32, 33, 34, 35, 36, 37, 38, 39):
            vector[index] = 1
        self.assertEqual(module.utilities(vector, mask), original)

    def test_bad_vector_is_data_error_even_for_invalid_action(self):
        for index, value in ((1, float("nan")), (1, float("inf")), (1, 10**1000),
                             (1, True), (1, "0.5"), (1, -0.1), (40, 0.5), (44, 1.1), (41, -1.1)):
            with self.subTest(index=index, value=str(value)[:20]):
                vector, mask = snapshot(sound=0.5)
                vector[index] = value
                with self.assertRaises(ValueError):
                    module.reward(vector, mask, 5)

    def test_wrong_shape_and_mask(self):
        for vector in (None, [], [0]*49, [0]*51):
            with self.assertRaises(ValueError):
                module.reward(vector, [True]*7, 0)
        vector, mask = snapshot(sound=0.5)
        for bad in (None, [], [1, True, False, False, False, False, False],
                    [True, False, False, False, False, False, False],
                    [True, True, False, False, False, True, False]):
            with self.assertRaises(ValueError):
                module.reward(vector, bad, 0)

    def test_absent_blocks_positive_strength_and_onehot(self):
        for index, value in ((1, 0), (2, 0), (7, 0), (8, 1), (13, 0.2), (25, 0.3), (43, 0.1)):
            vector, mask = snapshot(sound=0.5)
            vector[index] = value
            with self.subTest(index=index), self.assertRaises(ValueError):
                module.utilities(vector, mask)

    def test_unknown_context_and_false_identity_claim_rejected(self):
        for index in (31, 33, 35, 37, 39):
            vector, mask = snapshot()
            vector[index] = 1
            with self.subTest(index=index), self.assertRaises(ValueError):
                module.utilities(vector, mask)

    def test_technical_outcomes_do_not_get_reward(self):
        vector, mask = snapshot(sound=0.5)
        for outcome in ("stale", "cancelled", "no_response"):
            self.assertIsNone(module.delivered_reward(vector, mask, 5, outcome=outcome))
        self.assertEqual(module.delivered_reward(vector, mask, 5, outcome="decision"), -1)
        with self.assertRaises(ValueError):
            module.delivered_reward(vector, mask, 1, outcome="unknown")
        with self.assertRaises(ValueError):
            module.delivered_reward([], mask, None, outcome="no_response")

    def test_baselines_are_distinct_and_random_expectation_exact(self):
        vector, mask = snapshot(sound=0.2, light=0.8)
        result = module.baselines(vector, mask, rng=random.Random(11))
        self.assertEqual(result["first_allowed_sensory_action"], 1)
        self.assertEqual(result["first_allowed_sensory_reward"], 0.25)
        self.assertEqual(result["argmax_expert_action"], 2)
        self.assertEqual(result["argmax_expert_reward"], 1)
        self.assertAlmostEqual(result["uniform_expected_reward"], 1.25 / 3)

    def test_random_is_local_reproducible_and_includes_continue(self):
        vector, mask = snapshot(sound=0.5)
        rng1, rng2 = random.Random(11), random.Random(11)
        first = [module.baselines(vector, mask, rng=rng1)["uniform_allowed_action"] for _ in range(100)]
        second = [module.baselines(vector, mask, rng=rng2)["uniform_allowed_action"] for _ in range(100)]
        self.assertEqual(first, second)
        self.assertEqual(set(first), {0, 1})

    def test_all_structural_masks_have_optimal_baseline_and_bounded_rewards(self):
        for present in itertools.product((False, True), repeat=4):
            vector, mask = snapshot(sound=0.2 if present[0] else None,
                                    light=0.7 if present[1] else None,
                                    player_a=0.4 if present[2] else None,
                                    player_b=1 if present[3] else None)
            result = module.baselines(vector, mask, rng=random.Random(11))
            with self.subTest(present=present):
                self.assertEqual(result["argmax_expert_reward"], 1)
                for action, allowed in enumerate(mask):
                    score = module.reward(vector, mask, action)
                    self.assertTrue(0 <= score <= 1 if allowed else score == -1)
                self.assertTrue(mask[result["uniform_allowed_action"]])


if __name__ == "__main__":
    unittest.main()
