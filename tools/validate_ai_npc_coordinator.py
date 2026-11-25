"""Audit coordinator manifest/frame/reward/failure artifacts; no policy quality inference."""
import argparse
import json
import math
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from ai_training.evaluation import attention_metrics, expert_attention
from ai_training.cases.training_schedule import validate_capture, digest


def audit_case_run(folder):
    folder = Path(folder)
    summary = json.loads((folder / "summary.json").read_text(encoding="utf-8"))
    manifest = json.loads((folder / "manifest.json").read_text(encoding="utf-8"))
    context = json.loads((folder / "run-context.json").read_text(encoding="utf-8"))
    path = folder / "records.jsonl"
    rows = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()] if path.exists() else []
    metrics = attention_metrics.evaluate(rows, manifest, near_optimal_regret=0.05)
    if summary["recorded_cases"] != len(rows) or summary["planned_cases"] != len(manifest["cases"]):
        raise ValueError("Summary disagrees with recorded/planned cases")
    if context["source_split"] != "train" or context["ood_vectors_read"] is not False:
        raise ValueError("Coordinator must not open ID/OOD for training")
    for row in rows:
        expected = expert_attention.reward(row["observations"], row["allowed_actions"], row["action"])
        if any(type(row[key]) not in (int, float) or not math.isfinite(row[key]) or abs(row[key] - expected) > 1e-6
               for key in ("assigned_reward", "cumulative_reward")):
            raise ValueError("Recorded reward is not the single prepared decision reward")
        if row["seed"] != context["policy_seed"] or row["generation_seed"] != 1001:
            raise ValueError("Policy and generation seed metadata disagree")
    if summary["state"] == "Completed":
        if (not summary["valid_run"] or not metrics["complete"] or summary["prepared_cases"] != len(rows) or
                metrics["overall"]["accepted_invalid_count"] != 0):
            raise ValueError("Completed run is incomplete or has invalid accepted actions")
    elif summary["state"] == "Aborted":
        if summary["valid_run"] or not summary["abort_reason"] or summary["prepared_cases"] > summary["recorded_cases"] + 1:
            raise ValueError("Aborted run must be invalid and stop after at most the current case")
    else:
        raise ValueError("Unfinished coordinator run")
    return summary, context, metrics


def audit(folder, schedule_path=None):
    folder = Path(folder)
    summary, context, metrics = audit_case_run(folder / "complete")
    expected = 8 if context["selection"] == "first-per-family-diagnostic-not-a-research-subset" else 449
    if schedule_path is not None:
        schedule = json.loads(Path(schedule_path).read_text(encoding="utf-8"))
        if context.get("schedule_sha256") != digest(schedule_path):
            raise ValueError("Declared schedule hash disagrees with Unity run")
        expected = validate_capture(schedule, folder / "complete", summary["recorded_cases"])
    elif context["selection"] == "scheduled-train":
        raise ValueError("Scheduled run requires its --schedule evidence")
    if summary["state"] != "Completed" or summary["recorded_cases"] != expected:
        raise ValueError("Wrong completed coordinator case count")
    failures = {}
    if context["mode"] == "controlled-heuristic-api-test":
        for name, prepared in (("technical-failure", 1), ("capture-failure", 0)):
            failed, _, _ = audit_case_run(folder / name)
            if failed["state"] != "Aborted" or failed["prepared_cases"] != prepared or failed["recorded_cases"] != 0:
                raise ValueError("Controlled failure was not stopped at the first bad case")
            failures[name] = failed["abort_reason"]
    return {"status": "pass", "completed_cases": expected, "mode": context["mode"], "failure_checks": failures,
            "single_decision_rewards_verified": True, "ood_vectors_read": False,
            "policy_quality": "not_assessed", "metrics": metrics}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--schedule", type=Path)
    args = parser.parse_args()
    try:
        result = audit(args.capture, args.schedule)
    except (ValueError, TypeError, KeyError, OSError) as error:
        parser.error(str(error))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    print(json.dumps({k: v for k, v in result.items() if k != "metrics"}, indent=2))


if __name__ == "__main__":
    main()
