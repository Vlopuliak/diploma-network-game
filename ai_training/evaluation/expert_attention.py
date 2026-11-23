"""Погоджена offline-шкала експертної уваги; не ігровий Decision Maker і не trainer."""
import json
import math
from pathlib import Path

PROFILE_ID = "the-copy.expert-attention/1"
INPUT_SCHEMA = "the-copy.decision-input/sensory-intent-v1"
DEFAULT_PROFILE = Path(__file__).resolve().parents[1] / "config/sensory_intent_v1_expert_attention_reward.json"
# Зміна формул потребує нового profile_id, а не тихої зміни коефіцієнтів.
SUPPORTED_PROFILE = {
    "profile_id": PROFILE_ID, "input_schema": INPUT_SCHEMA,
    "observation_count": 50, "action_count": 7,
    "scope": "single-decision-surrogate-no-action-execution",
    "sound": "memory-strength-else-live-strength",
    "light": "perceived-strength-exact-aim-only",
    "player": "floor-plus-remaining-normalized-distance", "player_floor": 0.01,
    "continue": "one-if-no-sensory-action-else-zero",
    "normalization": "utility-divided-by-maximum-allowed-utility",
    "invalid_action_reward": -1, "technical_outcome_reward": None,
    "baselines": ["uniform-allowed", "first-allowed-sensory", "argmax-expert"],
}


def load_profile(path=DEFAULT_PROFILE):
    """Перевіряє, що файл справді описує підтримувану версію формул."""
    profile = json.loads(Path(path).read_text(encoding="utf-8"))
    if profile != SUPPORTED_PROFILE:
        raise ValueError("Unsupported expert-attention profile; version/formulas must match")
    return profile


def validate_snapshot(observations, allowed_actions):
    """Перевіряє діапазони кодера та узгодженість маски; не доводить походження з Unity."""
    if not isinstance(observations, (list, tuple)) or len(observations) != 50:
        raise ValueError("Expected 50 observations")
    signed = {3, 4, 5, 15, 16, 17, 26, 27, 28, 41, 42, 43, 46, 47, 48}
    flags = {0, 7, 8, 9, 10, 11, 12, 19, 20, 21, 22, 23, 24, 30, 31,
             32, 33, 34, 35, 36, 37, 38, 39, 40, 45}
    for index, value in enumerate(observations):
        try:
            valid = (type(value) in (int, float) and math.isfinite(value) and
                     (-1 if index in signed else 0) <= value <= 1)
        except OverflowError:
            valid = False
        if not valid or (index in flags and value not in (0, 1)):
            raise ValueError(f"Invalid observation at index {index}")
    for offset, end in ((0, 12), (12, 24), (24, 31), (40, 45), (45, 50)):
        if observations[offset] == 0 and any(observations[offset + 1:end]):
            raise ValueError(f"Absent sensor block {offset} must be zero")
    for offset in (0, 12):
        if observations[offset] and (observations[offset + 1] <= 0 or
                                     observations[offset + 2] <= 0 or
                                     sum(observations[offset + 7:offset + 11]) != 1):
            raise ValueError("Present sound needs positive strength/freshness and one category")
    if observations[24] and observations[25] <= 0:
        raise ValueError("Present light needs positive strength")
    for offset in (32, 34, 36, 38):
        if observations[offset] == 0 and observations[offset + 1] != 0:
            raise ValueError("Unknown context cannot be true")
    sound_offset = 12 if observations[12] else 0
    if observations[31] and not (observations[sound_offset] and
                                observations[sound_offset + 11] and observations[24]):
        raise ValueError("Same-source flag needs known sound and light sources")
    expected = [True, bool(observations[0] or observations[12]),
                bool(observations[24] and not observations[30]),
                bool(observations[40]), bool(observations[45]), False, False]
    if (not isinstance(allowed_actions, (list, tuple)) or len(allowed_actions) != 7 or
            any(type(value) is not bool for value in allowed_actions) or list(allowed_actions) != expected):
        raise ValueError("Mask does not match frozen sensory candidates")


def utilities(observations, allowed_actions):
    """Незмінна копія оцінок: недоступні дії — None, а не кандидат з нульовим балом."""
    validate_snapshot(observations, allowed_actions)
    values = [0.0 if any(allowed_actions[1:5]) else 1.0] + [None] * 6
    if allowed_actions[1]:
        values[1] = observations[13] if observations[12] else observations[1]
    if allowed_actions[2]:
        values[2] = observations[25]
    for action, distance in ((3, 44), (4, 49)):
        if allowed_actions[action]:
            values[action] = 0.01 + 0.99 * (1 - observations[distance])
    return tuple(values)


def _reward(values, action):
    if type(action) is not int or not 0 <= action < 7 or values[action] is None:
        return -1.0
    return values[action] / max(value for value in values if value is not None)


def reward(observations, allowed_actions, action):
    """Винагорода вибору на валідному знімку; помилкові дані викликають ValueError."""
    return _reward(utilities(observations, allowed_actions), action)


def delivered_reward(observations, allowed_actions, action, *, outcome):
    """Не змішує вибір із доставкою: technical outcomes не оцінюються як рішення.

    decision — отриманий дискретний вибір (також відхилений через маску).
    stale/cancelled/no_response — технічна невдача, навіть якщо callback містив індекс.
    Це явний offline API; він не переінтерпретовує JSONL status/reason автоматично.
    """
    values = utilities(observations, allowed_actions)
    if outcome in ("no_response", "stale", "cancelled"):
        return None
    if outcome != "decision":
        raise ValueError("Unknown delivery outcome")
    return _reward(values, action)


def baselines(observations, allowed_actions, *, rng):
    """Offline-порівняння; локальний random.Random передає викликач для відтворюваності.

    Uniform включає Continue. Argmax за рівності обирає найменший індекс;
    uniform_expected_reward — точне очікування, не оцінка за одним random-вибором.
    """
    values = utilities(observations, allowed_actions)
    allowed = [index for index, value in enumerate(values) if value is not None]
    rule = next((index for index in allowed if index > 0), 0)
    optimal = max(allowed, key=lambda index: values[index])
    return {
        "uniform_allowed_action": rng.choice(allowed),
        "uniform_expected_reward": sum(_reward(values, index) for index in allowed) / len(allowed),
        "first_allowed_sensory_action": rule,
        "first_allowed_sensory_reward": _reward(values, rule),
        "argmax_expert_action": optimal,
        "argmax_expert_reward": _reward(values, optimal),
    }
