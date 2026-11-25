import importlib.util
from pathlib import Path
import random
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("communicator_smoke", ROOT / "tools/verify_ai_npc_communicator.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class CommunicatorSmokeTests(unittest.TestCase):
    def test_ml_agents_mask_true_means_blocked(self):
        rng = random.Random(2026)
        mask = [False, False, True, True, True, True, True]
        self.assertEqual({module.choose_allowed(mask, rng) for _ in range(100)}, {0, 1})

    def test_invalid_transport_masks_rejected(self):
        for mask in ([], [False]*7, [True]*7, [0,1,1,1,1,1,1]):
            with self.subTest(mask=mask), self.assertRaises(ValueError):
                module.choose_allowed(mask, random.Random(1))

    def test_terminal_reward_checked_on_original_decision(self):
        v, allowed = [0.0]*50, [True,False,False,False,False,False,False]
        self.assertTrue(module.check_terminal(v, allowed, 0, 1, False))
        for bad in (0, float("nan"), float("inf")):
            with self.subTest(bad=bad), self.assertRaises(ValueError):
                module.check_terminal(v, allowed, 0, bad, False)

    def test_interrupted_bootstrap_is_not_counted_as_rewarded_episode(self):
        self.assertFalse(module.check_terminal([0.0]*50, [True,False,False,False,False,False,False], 0, 0, True))


if __name__ == "__main__":
    unittest.main()
