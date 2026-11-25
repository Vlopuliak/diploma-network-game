"""Local ML-Agents communicator/reset smoke with uniform allowed actions; no trainer or optimizer."""
import argparse
from importlib.metadata import version
import json
import math
from pathlib import Path
import random
import re
import shutil
import subprocess
import sys
import time
import uuid

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from ai_training.evaluation import expert_attention
from ai_training.cases.training_schedule import build_schedule, check_run_signal, validate_capture, digest, read_run_signal


def choose_allowed(mask, rng):
    if len(mask) != 7 or any(type(v) is not bool for v in mask) or mask[0] or not mask[5] or not mask[6]:
        raise ValueError("Expected ML-Agents disabled-action mask with Continue on and 5/6 off")
    return rng.choice([i for i, blocked in enumerate(mask) if not blocked])


def check_terminal(observations, allowed, action, reward, interrupted):
    if interrupted:
        return False
    if not math.isfinite(float(reward)):
        raise ValueError("Non-finite terminal reward")
    expected = expert_attention.reward(observations, allowed, action)
    if abs(float(reward) - expected) > 1e-6:
        raise ValueError("Python terminal reward differs from frozen-frame expert criterion")
    return True


def run(args):
    import numpy as np
    from mlagents_envs.environment import UnityEnvironment
    from mlagents_envs.base_env import ActionTuple
    from mlagents_envs.exception import UnityCommunicatorStoppedException

    workspace, plan, output = args.workspace.resolve(), args.plan.resolve(), args.output.resolve()
    workspace.relative_to(ROOT / "Logs" / "AI-NPC")
    plan.relative_to(workspace / "Logs" / "AI-NPC")
    output.relative_to(ROOT / "Logs" / "AI-NPC")
    if not (workspace / "Assets").is_dir() or not args.unity.is_file():
        raise ValueError("Use the prepared disposable Unity workspace and installed Editor")
    output.mkdir(parents=True, exist_ok=False)
    bootstrap = args.bootstrap
    seed = args.seed if bootstrap else 2026
    signal_path = None
    schedule = None
    log = output / "unity.log"
    command = [str(args.unity), "-batchmode", "-nographics", "-projectPath", str(workspace),
               "--mlagents-port", str(args.port), "-p305Disposable", "-p305WebSockets", "-p306Perception",
               "-p403PlayerSight", "-p403DecisionInputV2", "-p406CommunicatorSmoke", "-p406CoordinatorPlan", str(plan),
               "-executeMethod", "TheCopy.Tests.P305AcousticVerification.Run", "-logFile", str(log)]
    if bootstrap:
        schedule = build_schedule(plan, args.epochs, seed, args.shuffle_seed, args.diagnostic)
        inputs = workspace / "Logs/AI-NPC" / ("p406-bootstrap-inputs-" + uuid.uuid4().hex)
        inputs.mkdir()
        schedule_path = inputs / "schedule.json"
        schedule_text = json.dumps(schedule, indent=2, allow_nan=False) + "\n"
        schedule_path.write_text(schedule_text, encoding="utf-8")
        (output / "schedule.json").write_text(schedule_text, encoding="utf-8")
        signal_path = inputs / "signal.json"
        command.remove("-p406CommunicatorSmoke")
        command += ["-p406TrainingBootstrap", "-p406TrainingSchedule", str(schedule_path), "-p406RunSignal", str(signal_path)]
        if args.diagnostic:
            command += ["-p406BootstrapDiagnostic"]
        if args.inject_technical_failure:
            command += ["-p406InjectTechnicalFailure"]
    sources = {}
    for relative in ("Assets/Scripts/AI/TheCopy/CopyAttentionRecipeCoordinator.cs",
                     "Assets/Scripts/AI/TheCopy/CopyAttentionTrainingAgent.cs", "Assets/Scripts/AI/TheCopy/CopyAttentionReward.cs",
                     "Assets/Scripts/AI/TheCopy/CopyHearing.cs", "Assets/Tests/AI/P406CoordinatorVerification.cs",
                     "Assets/Tests/AI/P406TrainingVerification.cs", "Assets/Tests/AI/P306PerceptionVerification.cs"):
        sources[relative] = digest(workspace / relative)
        if sources[relative] != digest(ROOT / relative):
            raise ValueError("Disposable source differs from checkout: " + relative)
    (output / "launch.json").write_text(json.dumps({"command": command, "seed": seed,
        "python": sys.version, "trainer_created": False, "optimization_steps": 0,
        "mlagents_envs": version("mlagents-envs"), "numpy": version("numpy"), "source_sha256": sources,
        "signal_path": str(signal_path) if signal_path else None,
        "tool_sha256": digest(Path(__file__)),
        "schedule_tool_sha256": digest(ROOT / "ai_training/cases/training_schedule.py")}, indent=2) + "\n", encoding="utf-8")
    startup = None
    if sys.platform == "win32":
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
    process = subprocess.Popen(command, startupinfo=startup)
    env = None
    pending, completed = {}, []
    rng = random.Random(seed)
    failure = None
    try:
        env = UnityEnvironment(file_name=None, base_port=args.port, seed=seed, timeout_wait=100)
        env.reset()
        deadline = time.monotonic() + args.timeout
        while time.monotonic() < deadline:
            if signal_path:
                check_run_signal(signal_path)
            for name, spec in env.behavior_specs.items():
                if (len(spec.observation_specs) != 1 or spec.observation_specs[0].shape != (50,) or
                        list(spec.action_spec.discrete_branches) != [7] or spec.action_spec.continuous_size != 0):
                    raise ValueError("Communicator observation/action spec mismatch")
                decisions, terminals = env.get_steps(name)
                for i, agent_id in enumerate(terminals.agent_id):
                    key = (name, int(agent_id))
                    previous = pending.pop(key, None)
                    if previous is None:
                        continue  # Initialization/teardown without an observed prepared decision.
                    if check_terminal(previous["observations"], previous["allowed_actions"], previous["action"],
                                      terminals.reward[i], bool(terminals.interrupted[i])):
                        completed.append(dict(previous, terminal_reward=float(terminals.reward[i]), episode_id=int(agent_id)))
                actions = []
                for i, agent_id in enumerate(decisions.agent_id):
                    if decisions.action_mask is None or len(decisions.action_mask) != 1:
                        raise ValueError("Missing seven-action mask")
                    blocked = decisions.action_mask[0][i].tolist()
                    action = choose_allowed(blocked, rng)
                    observations = decisions.obs[0][i].tolist()
                    allowed = [not blocked_action for blocked_action in blocked]
                    expert_attention.validate_snapshot(observations, allowed)
                    key = (name, int(agent_id))
                    if key in pending:
                        raise ValueError("A one-step episode requested a second action before termination")
                    pending[key] = {"observations": observations, "allowed_actions": allowed, "action": action}
                    actions.append([action])
                if actions:
                    env.set_actions(name, ActionTuple(discrete=np.asarray(actions, dtype=np.int32)))
            try:
                env.step()
            except UnityCommunicatorStoppedException:
                break
        else:
            raise TimeoutError("Communicator smoke exceeded its bounded deadline")
        # Release Python's gRPC server before waiting for Editor shutdown; otherwise each side can wait on the other.
        env.close()
        env = None
        exit_code = process.wait(timeout=30)
        signal = check_run_signal(signal_path) if signal_path else None
        log_text = log.read_text(encoding="utf-8", errors="replace")
        marker = "training bootstrap without optimizer" if bootstrap else "communicator smoke"
        if exit_code != 0 or "[P4-06 COORDINATOR] COMPLETE: " + marker not in log_text or "[P3-05] FAIL" in log_text:
            raise ValueError("Unity communicator fixture did not finish successfully")
        match = re.search(r"\[P4-06 COORDINATOR\] RECORDS_DIR=(.+)", log_text)
        folder = Path(match.group(1).strip()).resolve()
        folder.relative_to(workspace / "Logs" / "AI-NPC")
        records = [json.loads(line) for line in (folder / "complete/records.jsonl").read_text(encoding="utf-8").splitlines()]
        expected_count = len(schedule["cases"]) if bootstrap else 8
        if len(records) != expected_count:
            raise ValueError("Unexpected scheduled diagnostic/train case count")
        if bootstrap and len(completed) != expected_count:
            raise ValueError("Bootstrap produced off-manifest rewarded terminal transitions")
        # Match actual Unity request frames/actions to Python terminal transitions, not just reward counts.
        cursor = 0
        for row in records:
            while cursor < len(completed):
                candidate = completed[cursor]
                cursor += 1
                if (row["action"] == candidate["action"] and row["allowed_actions"] == candidate["allowed_actions"] and
                        np.allclose(row["observations"], candidate["observations"], rtol=0, atol=1e-6)):
                    if abs(row["assigned_reward"] - candidate["terminal_reward"]) > 1e-6:
                        raise ValueError("Recorded reward disagrees with Python terminal")
                    break
            else:
                raise ValueError("A coordinator case has no matching Python terminal transition")
        if bootstrap:
            if not signal or signal["state"] != "Completed":
                raise ValueError("Missing terminal bootstrap completion signal")
            validate_capture(schedule, folder / "complete", expected_count)
            if digest(output / "schedule.json") != json.loads((folder / "complete/run-context.json").read_text(encoding="utf-8"))["schedule_sha256"]:
                raise ValueError("Unity did not execute the declared schedule bytes")
            from validate_ai_npc_coordinator import audit_case_run
            audit_case_run(folder / "complete")
        shutil.copytree(folder, output / "coordinator-capture")
        result = {"status": "pass", "purpose": "TRAINING_BOOTSTRAP_NO_OPTIMIZER" if bootstrap else "COMMUNICATOR_API_SMOKE_NOT_POLICY_EVALUATION",
                  "matched_cases": expected_count, "policy": "uniform-allowed-random-api-test-not-neural", "policy_seed": seed,
                  "source_split": "train", "reported_split": "diagnostic" if not bootstrap or args.diagnostic else "train", "optimization_steps": 0,
                  "trainer_created": False, "ood_vectors_read": False, "unity_exit_code": exit_code,
                  "coordinator_records_directory": str(folder), "terminal_transitions": completed}
        if bootstrap:
            result.update(epochs=schedule["epochs"], shuffle_seed=schedule["shuffle_seed"], generation_seed=1001,
                          off_manifest_rewarded_episodes=0, schedule_sha256=digest(output / "schedule.json"))
        (output / "result.json").write_text(json.dumps(result, indent=2, allow_nan=False) + "\n", encoding="utf-8")
        return {key: value for key, value in result.items() if key != "terminal_transitions"}
    except Exception as error:
        failure = error
        signal = read_run_signal(signal_path) if signal_path else None
        (output / "failure.json").write_text(json.dumps({"status": "failed", "valid_run": False, "error": str(error),
            "unity_signal": signal, "completed_terminal_count": len(completed), "trainer_created": False,
            "optimization_steps": 0}, indent=2), encoding="utf-8")
        raise
    finally:
        if env is not None:
            try: env.close()
            except Exception:
                if failure is None: raise
        if process.poll() is None:
            process.terminate()
            try: process.wait(timeout=10)
            except subprocess.TimeoutExpired: process.kill(); process.wait()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", type=Path, required=True)
    parser.add_argument("--workspace", type=Path, required=True)
    parser.add_argument("--plan", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--port", type=int, default=5026)
    parser.add_argument("--bootstrap", action="store_true", help="Validate repeat/shuffle bootstrap, never train")
    parser.add_argument("--diagnostic", action="store_true", help="Bootstrap uses first train recipe per family")
    parser.add_argument("--epochs", type=int, default=2)
    parser.add_argument("--seed", type=int, default=11)
    parser.add_argument("--shuffle-seed", type=int, default=7011)
    parser.add_argument("--timeout", type=int, default=180)
    parser.add_argument("--inject-technical-failure", action="store_true")
    args = parser.parse_args()
    if not args.bootstrap and (args.diagnostic or args.inject_technical_failure):
        parser.error("Bootstrap-only flags require --bootstrap")
    if not 1 <= args.timeout <= 3600:
        parser.error("Timeout must be in [1, 3600] seconds")
    try:
        result = run(args)
    except Exception as error:
        parser.exit(1, str(error) + "\n")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
