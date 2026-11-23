"""Metrics for supplied sensory-intent-v1 decision records. No reward, policy, or training."""
import argparse
import json
import math
from pathlib import Path
import statistics

SCHEMA = "the-copy.intent-evaluation/1"
INPUT_SCHEMA = "the-copy.decision-input/sensory-intent-v1"
CONTEXT = ("run_id", "policy_id", "split", "seed")


def integer(value):
    return isinstance(value, int) and not isinstance(value, bool)


def finite(value):
    try:
        return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)
    except OverflowError:
        return False


def validate_target(target):
    if target is None:
        return
    if not isinstance(target, dict) or set(target) != {"source_client_id", "position"}:
        raise ValueError("Target needs source_client_id and position")
    client_id = target["source_client_id"]
    if client_id is not None and (not integer(client_id) or not 0 <= client_id < 2 ** 64):
        raise ValueError("Source ID must be uint64 or null")
    position = target["position"]
    if not isinstance(position, list) or len(position) != 3 or not all(finite(v) for v in position):
        raise ValueError("Position must contain three finite numbers")


def valid_selection(action, target):
    if not integer(action) or not 0 <= action <= 4:
        return False
    if action == 0:
        return target is None
    return target is not None and (action == 1 or target["source_client_id"] is not None)


def validate_record(row):
    if not isinstance(row, dict):
        raise ValueError("Record must be an object")
    if row.get("schema") != SCHEMA or row.get("input_schema") != INPUT_SCHEMA:
        raise ValueError("Record schema disagrees with sensory-intent-v1")
    for key in ("case_id", "scenario", "run_id", "policy_id"):
        if not isinstance(row.get(key), str) or not row[key].strip():
            raise ValueError("Missing nonempty " + key)
    if row.get("split") not in {"train", "id", "ood", "diagnostic"}:
        raise ValueError("Unknown data split")
    if not integer(row.get("seed")) or row["seed"] < 0:
        raise ValueError("Seed must be a nonnegative integer")
    mask = row.get("allowed_actions")
    if not isinstance(mask, list) or len(mask) != 7 or not all(type(v) is bool for v in mask):
        raise ValueError("Expected seven boolean availability flags")
    if not mask[0] or mask[5] or mask[6]:
        raise ValueError("Continue must be available and mechanism actions unavailable")
    status = row.get("status")
    if status not in {"accepted", "rejected", "no_response"}:
        raise ValueError("Unknown response status")
    reason = row.get("rejection_reason")
    if reason is not None and (not isinstance(reason, str) or not reason.strip()):
        raise ValueError("Response reason must be a nonempty string or null")
    if status == "rejected" and (not isinstance(row.get("rejection_reason"), str) or not row["rejection_reason"].strip()):
        raise ValueError("Rejected response needs an explicit reason")
    if "action" not in row or "target" not in row or "latency_ms" not in row:
        raise ValueError("Action, target and latency fields must be explicit")
    action, target, latency = row["action"], row["target"], row["latency_ms"]
    if action is not None and not integer(action):
        raise ValueError("Action must be an integer or null; out-of-range integers are scored as invalid")
    validate_target(target)
    if latency is not None and (not finite(latency) or latency < 0):
        raise ValueError("Latency must be nonnegative finite milliseconds or null")
    if status == "no_response" and (action is not None or target is not None or latency is not None):
        raise ValueError("No response must not invent a selection or a measured latency")
    references = row.get("reference_outcomes")
    if references is not None:
        if not isinstance(references, list) or not references:
            raise ValueError("Use null for no reference, not an empty list")
        for outcome in references:
            if not isinstance(outcome, dict) or set(outcome) != {"action", "target"}:
                raise ValueError("Reference outcome needs action and target")
            validate_target(outcome["target"])
            if not valid_selection(outcome["action"], outcome["target"]) or not mask[outcome["action"]]:
                raise ValueError("Reference outcome must be possible in the recorded frame")


def target_matches(actual, expected, tolerance):
    if actual is None or expected is None:
        return actual is None and expected is None
    return (actual["source_client_id"] == expected["source_client_id"] and
            math.dist(actual["position"], expected["position"]) <= tolerance)


def ratio(numerator, denominator):
    return numerator / denominator if denominator else None


def metrics(rows, tolerance):
    total = len(rows)
    accepted = rejected = no_response = attempts = delivered_invalid = 0
    ref_count = intent_correct = selection_correct = delivered_correct = target_count = target_correct = 0
    latency = []
    reasons = {}
    no_response_reasons = {}
    for row in rows:
        action, target, status = row["action"], row["target"], row["status"]
        responded = status != "no_response"
        mask_valid = integer(action) and 0 <= action < 7 and row["allowed_actions"][action]
        payload_valid = valid_selection(action, target)
        accepted += status == "accepted"
        rejected += status == "rejected"
        no_response += not responded
        attempts += responded and action is not None and not mask_valid
        delivered_invalid += status == "accepted" and not (mask_valid and payload_valid)
        if status == "rejected":
            reason = row["rejection_reason"]
            reasons[reason] = reasons.get(reason, 0) + 1
        if status == "no_response":
            reason = row.get("rejection_reason") or "unspecified"
            no_response_reasons[reason] = no_response_reasons.get(reason, 0) + 1
        if responded and row["latency_ms"] is not None:
            latency.append(row["latency_ms"])
        references = row.get("reference_outcomes")
        if references is None:
            continue
        ref_count += 1
        same_intent = [r for r in references if r["action"] == action]
        intent_correct += bool(same_intent)
        if same_intent and action != 0:
            target_count += 1
            target_correct += any(target_matches(target, r["target"], tolerance) for r in same_intent)
        correct = bool(mask_valid and payload_valid and any(
            r["action"] == action and target_matches(target, r["target"], tolerance) for r in references))
        selection_correct += correct
        delivered_correct += status == "accepted" and correct
    latency.sort()
    return {
        "case_count": total, "response_count": total - no_response,
        "accepted_count": accepted, "rejected_count": rejected, "no_response_count": no_response,
        "acceptance_rate": ratio(accepted, total), "no_response_rate": ratio(no_response, total),
        "mask_violation_attempt_count": attempts, "mask_violation_attempt_rate": ratio(attempts, total - no_response),
        "delivered_invalid_decision_count": delivered_invalid,
        "rejection_reasons": reasons,
        "no_response_reasons": no_response_reasons,
        "reference_count": ref_count, "reference_coverage": ratio(ref_count, total),
        "intent_accuracy": ratio(intent_correct, ref_count),
        "selection_accuracy": ratio(selection_correct, ref_count),
        "accepted_reference_success_rate": ratio(delivered_correct, ref_count),
        "target_comparison_count": target_count, "target_accuracy": ratio(target_correct, target_count),
        "latency_sample_count": len(latency),
        "latency_mean_ms": statistics.fmean(latency) if latency else None,
        "latency_median_ms": statistics.median(latency) if latency else None,
        "latency_p95_nearest_rank_ms": latency[math.ceil(0.95 * len(latency)) - 1] if latency else None,
    }


def evaluate(rows, target_tolerance):
    if not finite(target_tolerance) or target_tolerance < 0:
        raise ValueError("An explicit nonnegative target tolerance is required")
    rows = list(rows)
    context = None
    case_ids = set()
    scenarios = {}
    for row in rows:
        validate_record(row)
        identity = {key: row[key] for key in CONTEXT}
        if context is not None and identity != context:
            raise ValueError("Do not pool different runs, policies, splits or seeds in one report")
        context = identity
        if row["case_id"] in case_ids:
            raise ValueError("Duplicate case ID in run")
        case_ids.add(row["case_id"])
        scenarios.setdefault(row["scenario"], []).append(row)
    return {"schema": "the-copy.intent-metrics/1", "input_schema": INPUT_SCHEMA,
            "context": context, "target_tolerance_m": target_tolerance,
            "record_provenance": "supplied_records_not_independently_verified",
            "reward_status": "not_defined", "policy_quality_conclusion": "not_assessed",
            "overall": metrics(rows, target_tolerance),
            "by_scenario": {key: metrics(value, target_tolerance) for key, value in sorted(scenarios.items())}}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--records", type=Path, required=True)
    parser.add_argument("--target-tolerance", type=float, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        rows = [json.loads(line) for line in args.records.read_text(encoding="utf-8").splitlines() if line.strip()]
        result = evaluate(rows, args.target_tolerance)
    except (ValueError, TypeError, KeyError) as error:
        parser.error(str(error))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False))


if __name__ == "__main__":
    main()
