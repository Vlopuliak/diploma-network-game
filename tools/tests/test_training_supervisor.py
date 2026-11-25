import copy
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
import ai_npc_training_supervisor as supervisor
from test_training_schedule import fixture


class SupervisorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(dir=ROOT / 'Logs/AI-NPC')
        self.base = Path(self.temp.name)
        self.workspace = self.base / 'workspace'
        for relative in supervisor.SOURCES:
            target = self.workspace / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / relative, target)
        self.recipes = self.workspace / 'Logs/AI-NPC/train'
        self.recipes.mkdir(parents=True)
        fixture(self.recipes)
        self.output = self.base / 'prepared'

    def tearDown(self):
        self.temp.cleanup()

    def prepare(self, **kwargs):
        defaults = dict(epochs=2, seed=11, shuffle_seed=7011, diagnostic=True, port=5035)
        defaults.update(kwargs)
        return supervisor.prepare(self.workspace, self.recipes, self.output, sys.executable, sys.executable, **defaults)

    def test_prepared_smoke_has_separate_seed_budget_paths_and_immutable_inputs(self):
        spec = self.prepare()
        self.assertEqual(spec['planned_decisions'], 16)
        self.assertEqual(spec, supervisor.validate_prepared(self.output))
        self.assertEqual(supervisor.read(self.output / 'ppo.yaml')['behaviors']['TheCopyDraft2']['max_steps'], 17)
        self.assertEqual(supervisor.read(self.output / 'schedule.json')['generation_seed'], 1001)
        worker, unity = supervisor.commands(spec)
        self.assertNotIn('-p406ExternalTrainer', unity)
        self.assertIn('-p406RunSignal', unity)
        self.assertNotIn('shell', worker)

    def test_spec_change_is_rejected_before_processes(self):
        self.prepare()
        spec = supervisor.read(self.output / 'prepared.json')
        spec['mode'] = 'ppo'
        supervisor.write(self.output / 'prepared.json', spec)
        with self.assertRaisesRegex(ValueError, 'specification changed'):
            supervisor.validate_prepared(self.output)

    def test_config_change_is_rejected(self):
        self.prepare()
        with (self.output / 'ppo.yaml').open('a') as stream: stream.write(' ')
        with self.assertRaisesRegex(ValueError, 'artifact changed'):
            supervisor.validate_prepared(self.output)

    def test_source_drift_is_rejected(self):
        self.prepare()
        with (self.workspace / supervisor.SOURCES[0]).open('a') as stream: stream.write(' ')
        with self.assertRaisesRegex(ValueError, 'source changed'):
            supervisor.validate_prepared(self.output)

    def test_ppo_proposal_does_not_start_without_operator_gate(self):
        spec = self.prepare(mode='ppo', epochs=20, diagnostic=False, port=5004, timeout=3600)
        self.assertEqual(spec['planned_decisions'], 8980)
        with patch.object(supervisor, 'preflight') as preflight:
            with self.assertRaisesRegex(PermissionError, 'prior user approval'):
                supervisor.execute(self.output)
            preflight.assert_not_called()
        self.assertFalse((self.output / 'started.json').exists())
        self.assertIn('-p406ExternalTrainer', supervisor.commands(spec)[1])

    def test_ppo_rejects_diagnostic_wrong_port_or_zero_update_budget(self):
        for overrides in (dict(mode='ppo'), dict(mode='ppo', diagnostic=False, port=5035),
                          dict(mode='ppo', diagnostic=False, port=5004, epochs=2)):
            with self.subTest(overrides=overrides), self.assertRaises(ValueError):
                self.prepare(**overrides)

    def test_existing_run_cannot_be_overwritten_or_restarted(self):
        self.prepare()
        with self.assertRaises(FileExistsError): self.prepare()
        supervisor.write(self.output / 'started.json', {})
        with patch.object(supervisor, 'preflight') as preflight:
            with self.assertRaisesRegex(ValueError, 'single-use'): supervisor.execute(self.output)
            preflight.assert_not_called()

    def test_monitor_aborts_even_when_both_exit_codes_are_zero(self):
        signal = self.base / 'signal.json'
        supervisor.write(signal, {'state': 'Aborted', 'reason': 'technical_failure'})
        class Finished:
            def poll(self): return 0
        with self.assertRaisesRegex(RuntimeError, 'technical_failure'):
            supervisor.monitor([Finished(), Finished()], signal, 1)

    def test_zero_child_exits_without_completed_signal_are_not_success(self):
        class Finished:
            def poll(self): return 0
        with self.assertRaisesRegex(RuntimeError, 'without Unity completion'):
            supervisor.monitor([Finished(), Finished()], self.base / 'missing.json', 1)

    def test_monitor_accepts_only_completed_zero_exits(self):
        signal = self.base / 'signal.json'
        supervisor.write(signal, {'state': 'Completed'})
        class Finished:
            def poll(self): return 0
        self.assertEqual(supervisor.monitor([Finished(), Finished()], signal, 1), [0,0])

    def test_real_children_are_stopped_after_timeout(self):
        processes = [subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(30)'], **supervisor.hidden()) for _ in range(2)]
        try:
            with self.assertRaises(TimeoutError):
                supervisor.monitor(processes, self.base / 'missing.json', 0.1)
        finally:
            for process in processes: supervisor.stop_owned(process)
        self.assertTrue(all(process.poll() is not None for process in processes))

    @unittest.skipUnless(sys.platform == 'win32', 'Windows child-tree contract')
    def test_windows_cleanup_also_stops_owned_grandchild(self):
        pid_file = self.base / 'grandchild.txt'
        program = ('import subprocess,sys,time; from pathlib import Path; '
                   'p=subprocess.Popen([sys.executable,"-c","import time; time.sleep(30)"]); '
                   'Path(sys.argv[1]).write_text(str(p.pid)); time.sleep(30)')
        parent = subprocess.Popen([sys.executable, '-c', program, str(pid_file)], **supervisor.hidden())
        try:
            deadline = time.monotonic() + 5
            while not pid_file.exists() and time.monotonic() < deadline: time.sleep(0.01)
            self.assertTrue(pid_file.exists())
            identity = int(pid_file.read_text())
        finally:
            supervisor.stop_owned(parent)
        result = subprocess.run(['tasklist', '/FI', 'PID eq ' + str(identity), '/FO', 'CSV', '/NH'],
                                capture_output=True, text=True, **supervisor.hidden())
        self.assertNotIn('"' + str(identity) + '"', result.stdout)

    def test_worker_frame_action_and_reward_must_match_capture(self):
        row = {'action':0, 'allowed_actions':[True]+[False]*6, 'observations':[0.0]*50, 'assigned_reward':1.0}
        transition = {k:v for k,v in row.items() if k != 'assigned_reward'}
        transition['terminal_reward'] = 1.0
        supervisor.compare_api_records([row], [transition])
        for key, value in (('action',1), ('terminal_reward',float('nan')), ('observations',[0.5]*50)):
            changed = copy.deepcopy(transition); changed[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                supervisor.compare_api_records([row], [changed])
        with self.assertRaises(ValueError): supervisor.compare_api_records([row], [])


if __name__ == '__main__': unittest.main()
