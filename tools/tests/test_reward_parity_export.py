import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("reward_parity_export", ROOT / "tools/export_ai_npc_reward_parity.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class RewardParityExportTests(unittest.TestCase):
    def test_unsafe_output_rejected_before_any_input_read(self):
        with tempfile.TemporaryDirectory() as folder:
            output = Path(folder) / "fixture.json"
            with self.assertRaisesRegex(ValueError, "ignored Logs"):
                module.export("missing-plan", "missing-capture", output)
            self.assertFalse(output.exists())

    def test_cli_missing_audited_data_does_not_write_reference_results(self):
        with tempfile.TemporaryDirectory() as folder:
            output = Path(folder) / "Logs" / "fixture.json"
            result = subprocess.run([sys.executable, "-B", str(ROOT / "tools/export_ai_npc_reward_parity.py"),
                                     "--plan", str(Path(folder) / "missing"), "--capture", str(Path(folder) / "capture"),
                                     "--output", str(output)], capture_output=True, text=True)
            self.assertEqual(result.returncode, 2)
            self.assertFalse(output.exists())


if __name__ == "__main__":
    unittest.main()
