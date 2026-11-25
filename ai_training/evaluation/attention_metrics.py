"""Offline-метрики expert-attention з явним допуском і маніфестом випадків, без навчання."""
import argparse
import json
from pathlib import Path
import random
import statistics

try:
    from . import expert_attention as expert
    from . import intent_metrics as technical
except ImportError:  # Прямий CLI-запуск файлу.
    import expert_attention as expert
    import intent_metrics as technical

SCHEMA = "the-copy.attention-evaluation/1"
MANIFEST_SCHEMA = "the-copy.attention-case-manifest/1"
TECHNICAL_REASONS = {"authority_denied", "configuration_invalid", "perception_unavailable", "target_changed"}
PROTOCOL_PATH = Path(__file__).resolve().parents[1] / "config/sensory_intent_v1_evaluation_protocol.json"
SUPPORTED_PROTOCOL = {
    "protocol_id": "the-copy.expert-attention-evaluation/1",
    "status": "approved-2026-11-23-no-training-authorization",
    "profile_id": expert.PROFILE_ID, "near_optimal_regret": 0.05,
    "primary_metric": "macro.accepted_near_optimal_rate_of_planned",
    "minimum_by_split": {"id": 0.9, "ood": 0.8},
    "require_complete_manifest": True, "maximum_accepted_invalid_count": 0,
}


def load_protocol(path=PROTOCOL_PATH):
    protocol = json.loads(Path(path).read_text(encoding="utf-8"))
    if protocol != SUPPORTED_PROTOCOL:
        raise ValueError("Unsupported evaluation protocol; version/criteria must match")
    return protocol


def validate_manifest(manifest):
    if not isinstance(manifest, dict) or manifest.get("schema") != MANIFEST_SCHEMA:
        raise ValueError("Expected attention case manifest")
    if not isinstance(manifest.get("dataset_id"), str) or not manifest["dataset_id"].strip():
        raise ValueError("Manifest needs dataset_id")
    if manifest.get("split") not in {"train", "id", "ood", "diagnostic"}:
        raise ValueError("Invalid manifest split")
    cases = manifest.get("cases")
    if not isinstance(cases, list) or not cases:
        raise ValueError("Manifest needs nonempty cases")
    expected = {}
    for case in cases:
        if (not isinstance(case, dict) or set(case) != {"case_id", "scenario"} or
                any(not isinstance(case[key], str) or not case[key].strip() for key in ("case_id", "scenario"))):
            raise ValueError("Manifest case needs case_id and scenario")
        if case["case_id"] in expected:
            raise ValueError("Duplicate manifest case_id")
        expected[case["case_id"]] = case["scenario"]
    return expected


def validate_record(row, manifest, expected):
    if not isinstance(row, dict) or row.get("schema") != SCHEMA:
        raise ValueError("Expected attention evaluation record, not a legacy trace")
    if row.get("profile_id") != expert.PROFILE_ID or row.get("dataset_id") != manifest["dataset_id"]:
        raise ValueError("Wrong reward profile or dataset")
    # Повторне використання технічного контракту без зміни старого evaluator-а.
    legacy = dict(row, schema=technical.SCHEMA)
    technical.validate_record(legacy)
    if row["split"] != manifest["split"] or expected.get(row["case_id"]) != row["scenario"]:
        raise ValueError("Case scenario/split not in manifest")
    expert.validate_snapshot(row.get("observations"), row["allowed_actions"])
    outcome, status, reason = row.get("decision_outcome"), row["status"], row.get("rejection_reason")
    valid = (
        (outcome == "decision" and status == "accepted" and reason is None) or
        (outcome == "decision" and status == "rejected" and reason in {"masked_or_unavailable", "malformed"}) or
        (outcome == "stale" and status == "rejected" and reason == "stale") or
        (outcome == "technical_rejection" and status == "rejected" and reason in TECHNICAL_REASONS) or
        (outcome in {"cancelled", "no_response"} and status == "no_response")
    )
    if not valid:
        raise ValueError("Decision outcome contradicts status/reason")


def mean(values):
    return statistics.fmean(values) if values else None


def summarize(rows, expected_count, epsilon):
    scored = []
    outcomes = {}
    paired_random, paired_rule = [], []
    all_random, all_rule = [], []
    near_count = delivered_near = delivered_invalid = 0
    for row in rows:
        outcome = row["decision_outcome"]
        outcomes[outcome] = outcomes.get(outcome, 0) + 1
        comparison = expert.baselines(row["observations"], row["allowed_actions"], rng=random.Random(0))
        # Використовується точне очікування random, а не випадково виграшна реалізація.
        all_random.append(comparison["uniform_expected_reward"])
        all_rule.append(comparison["first_allowed_sensory_reward"])
        action = row["action"]
        valid_choice = (technical.integer(action) and 0 <= action < 7 and row["allowed_actions"][action])
        valid_payload = technical.valid_selection(action, row["target"])
        delivered_invalid += row["status"] == "accepted" and not (valid_choice and valid_payload)
        if outcome != "decision":
            continue
        score = expert.reward(row["observations"], row["allowed_actions"], action)
        scored.append(score)
        near = valid_choice and 1 - score <= epsilon + 1e-12
        near_count += near
        delivered_near += near and valid_payload and row["status"] == "accepted"
        paired_random.append(comparison["uniform_expected_reward"])
        paired_rule.append(comparison["first_allowed_sensory_reward"])
    count = len(rows)
    return {
        "planned_case_count": expected_count, "observed_case_count": count,
        "missing_case_count": expected_count - count,
        "scored_selection_count": len(scored), "technical_outcome_count": count - len(scored),
        "outcome_counts": dict(sorted(outcomes.items())),
        "selection_coverage_of_observed": technical.ratio(len(scored), count),
        "selection_mean_reward": mean(scored),
        "selection_mean_regret": mean([1 - score for score in scored]),
        "near_optimal_selection_rate": technical.ratio(near_count, len(scored)),
        # Всі заплановані випадки у знаменнику: technical/missing не зникають.
        "accepted_near_optimal_rate_of_planned": technical.ratio(delivered_near, expected_count),
        "accepted_invalid_count": delivered_invalid,
        "paired_uniform_expected_reward": mean(paired_random),
        "paired_mask_rule_reward": mean(paired_rule),
        "paired_argmax_reward": 1.0 if scored else None,
        "paired_gain_over_uniform": mean([score - base for score, base in zip(scored, paired_random)]),
        "paired_gain_over_mask_rule": mean([score - base for score, base in zip(scored, paired_rule)]),
        "observed_uniform_expected_reward": mean(all_random),
        "observed_mask_rule_reward": mean(all_rule),
    }


def evaluate(rows, manifest, *, near_optimal_regret):
    """Один run/policy/seed/split; не об’єднує seeds і не оголошує quality PASS."""
    if not technical.finite(near_optimal_regret) or not 0 <= near_optimal_regret < 1:
        raise ValueError("Explicit near-optimal regret tolerance must be in [0, 1)")
    expert.load_profile()
    protocol = load_protocol()
    expected = validate_manifest(manifest)
    rows = list(rows)
    seen, context = set(), None
    by_scenario = {scenario: [] for scenario in sorted(set(expected.values()))}
    for row in rows:
        validate_record(row, manifest, expected)
        identity = {key: row[key] for key in technical.CONTEXT}
        if context is not None and identity != context:
            raise ValueError("Do not pool runs, policies, splits or seeds")
        context = identity
        if row["case_id"] in seen:
            raise ValueError("Duplicate evaluation case_id")
        seen.add(row["case_id"])
        by_scenario[row["scenario"]].append(row)
    reports = {scenario: summarize(group, list(expected.values()).count(scenario), near_optimal_regret)
               for scenario, group in by_scenario.items()}
    macro = {}
    for metric in ("selection_mean_reward", "near_optimal_selection_rate", "accepted_near_optimal_rate_of_planned",
                   "paired_gain_over_uniform", "paired_gain_over_mask_rule"):
        values = [report[metric] for report in reports.values()]
        macro[metric] = mean(values) if all(value is not None for value in values) else None
        macro[metric + "_scenario_coverage"] = sum(value is not None for value in values) / len(values)
    overall = summarize(rows, len(expected), near_optimal_regret)
    complete = len(seen) == len(expected)
    checks = None
    threshold = protocol["minimum_by_split"].get(manifest["split"])
    if near_optimal_regret != protocol["near_optimal_regret"]:
        gate = "not_assessed_nonprotocol_tolerance"
    elif threshold is None:
        gate = "not_assessed_train_or_diagnostic_split"
    else:
        checks = {
            "complete_manifest": complete,
            "no_accepted_invalid": overall["accepted_invalid_count"] == 0,
            "macro_primary_threshold": macro["accepted_near_optimal_rate_of_planned"] >= threshold,
        }
        gate = "pass_on_supplied_records" if all(checks.values()) else "fail_on_supplied_records"
    return {
        "schema": "the-copy.attention-metrics/1", "profile_id": expert.PROFILE_ID,
        "input_schema": expert.INPUT_SCHEMA, "dataset_id": manifest["dataset_id"], "split": manifest["split"],
        "context": context, "near_optimal_regret": near_optimal_regret, "comparison_numeric_tolerance": 1e-12,
        "record_provenance": "supplied_vectors_not_independently_verified",
        "protocol_id": protocol["protocol_id"], "quality_gate": gate,
        "quality_checks": checks, "required_macro_primary_rate": threshold,
        "complete": complete, "missing_case_ids": sorted(set(expected) - seen),
        "overall": overall,
        "macro_by_scenario": macro, "by_scenario": reports,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--records", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--near-optimal-regret", type=float, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        rows = [json.loads(line) for line in args.records.read_text(encoding="utf-8").splitlines() if line.strip()]
        manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
        report = evaluate(rows, manifest, near_optimal_regret=args.near_optimal_regret)
    except (ValueError, TypeError, KeyError, OSError) as error:
        parser.error(str(error))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False))


if __name__ == "__main__":
    main()
