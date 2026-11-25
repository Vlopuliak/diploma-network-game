import sys
from pathlib import Path
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from inspect_ai_npc_pilot import optimizer_steps


class PilotInspectionTests(unittest.TestCase):
    def test_positive_adam_steps_are_reported_separately_from_environment_steps(self):
        report = optimizer_steps({'state': {0:{'step':96}, 1:{'step':96.0}}})
        self.assertEqual(report['uniform_step'], 96)
        self.assertEqual(report['parameter_states'], 2)

    def test_nonuniform_steps_are_not_fabricated_as_one_update_count(self):
        report = optimizer_steps({'state': {0:{'step':32}, 1:{'step':96}}})
        self.assertIsNone(report['uniform_step'])
        self.assertEqual((report['minimum_step'],report['maximum_step']), (32,96))

    def test_empty_state_does_not_demonstrate_optimization(self):
        for state in ({}, {'state':{}}):
            with self.assertRaises(ValueError): optimizer_steps(state)

    def test_zero_nonfinite_fractional_and_missing_steps_are_rejected(self):
        for value in (None, 0, -1, True, 0.5, float('nan'), float('inf'), '96'):
            with self.subTest(value=value), self.assertRaises(ValueError):
                optimizer_steps({'state':{0:{'step':value}}})


if __name__ == '__main__': unittest.main()
