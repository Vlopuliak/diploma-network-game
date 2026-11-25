"""Відтворюваний train-only розклад; не змінює frozen рецепти й не запускає PPO."""
import hashlib
import json
from pathlib import Path
import random
import os
import time

from ai_training.cases.sensory_intent_cases import FAMILIES

SCHEMA = "the-copy.attention-training-schedule/1"


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def build_schedule(plan, epochs, policy_seed, shuffle_seed, diagnostic=False):
    for name, value, minimum in (("epochs", epochs, 1), ("policy_seed", policy_seed, 0), ("shuffle_seed", shuffle_seed, 0)):
        if type(value) is not int or not minimum <= value <= 2147483647:
            raise ValueError("Invalid " + name)
    if epochs > 10000 or type(diagnostic) is not bool:
        raise ValueError("Invalid epoch budget or diagnostic flag")
    plan = Path(plan)
    audit = json.loads((plan / "generation-audit.json").read_text(encoding="utf-8"))
    for name in ("train-recipes.json", "train-manifest.json"):
        if digest(plan / name) != audit["artifact_sha256"][name]:
            raise ValueError("Frozen train artifact hash changed")
    bundle = json.loads((plan / "train-recipes.json").read_text(encoding="utf-8"))
    manifest = json.loads((plan / "train-manifest.json").read_text(encoding="utf-8"))
    cases = bundle["cases"]
    if (bundle["split"] != "train" or bundle["generation_seed"] != 1001 or len(cases) != 449 or
            bundle["dataset_id"] != "the-copy.attention-cases/1" or manifest["split"] != "train" or
            manifest["dataset_id"] != bundle["dataset_id"] or len({c["case_id"] for c in cases}) != 449 or
            manifest["cases"] != [{"case_id": c["case_id"], "scenario": c["scenario"]} for c in cases] or
            any(sum(c["scenario"] == f for c in cases) != (1 if f == "quiet" else 64) for f in FAMILIES)):
        raise ValueError("Wrong frozen train contract")
    selected = [next(c for c in cases if c["scenario"] == family) for family in FAMILIES] if diagnostic else cases
    rng = random.Random(shuffle_seed)
    entries = []
    for epoch in range(epochs):
        order = list(selected)
        rng.shuffle(order)
        entries.extend({"case_id": f"epoch/{epoch:04d}/{c['case_id']}", "source_case_id": c["case_id"],
                        "scenario": c["scenario"], "epoch": epoch} for c in order)
    return {"schema": SCHEMA, "source_split": "train", "generation_seed": 1001,
            "policy_seed": policy_seed, "shuffle_seed": shuffle_seed, "epochs": epochs,
            "selection": "first-per-family-diagnostic" if diagnostic else "all-frozen-train",
            "source_recipe_sha256": digest(plan / "train-recipes.json"),
            "source_manifest_sha256": digest(plan / "train-manifest.json"), "cases": entries}


def _read_signal_once(path):
    if os.name != 'nt':
        return json.loads(Path(path).read_text(encoding='utf-8'))
    # Share deletion as well as read/write: Unity replaces this file atomically on Windows.
    import ctypes
    from ctypes import wintypes
    import msvcrt
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    create = kernel.CreateFileW
    create.argtypes = (wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD, wintypes.LPVOID,
                       wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE)
    create.restype = wintypes.HANDLE
    handle = create(str(Path(path).resolve()), 0x80000000, 0x7, None, 3, 0x80, None)
    if handle == ctypes.c_void_p(-1).value:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        descriptor = msvcrt.open_osfhandle(handle, os.O_RDONLY)
    except BaseException:
        kernel.CloseHandle.argtypes = (wintypes.HANDLE,)
        kernel.CloseHandle(handle)
        raise
    with os.fdopen(descriptor, 'r', encoding='utf-8') as stream:
        return json.load(stream)


def read_run_signal(path):
    for attempt in range(25):
        try: return _read_signal_once(path)
        except FileNotFoundError: return None
        except PermissionError:
            if attempt == 24: raise
            time.sleep(0.002)


def check_run_signal(path):
    """Abort/refusal before another SDK action, even if the interrupted transition has reward=0."""
    signal = read_run_signal(path)
    if signal is None:
        return None
    if signal.get("state") not in ("Running", "Completed", "Aborted"):
        raise ValueError("Invalid Unity bootstrap signal")
    if signal["state"] == "Aborted":
        raise RuntimeError("Unity run aborted: " + str(signal.get("reason")))
    return signal


def validate_capture(schedule, folder, terminal_count):
    folder = Path(folder)
    context = json.loads((folder / "run-context.json").read_text(encoding="utf-8"))
    summary = json.loads((folder / "summary.json").read_text(encoding="utf-8"))
    manifest = json.loads((folder / "manifest.json").read_text(encoding="utf-8"))
    rows = [json.loads(line) for line in (folder / "records.jsonl").read_text(encoding="utf-8").splitlines()]
    expected = schedule["cases"]
    if (summary["state"] != "Completed" or not summary["valid_run"] or terminal_count != len(expected) or
            summary["recorded_cases"] != len(expected) or summary["prepared_cases"] != len(expected) or
            len(rows) != len(expected) or manifest["cases"] != [{"case_id": e["case_id"], "scenario": e["scenario"]} for e in expected] or
            any(context[k] != schedule[k] for k in ("source_recipe_sha256", "source_manifest_sha256",
                                                    "policy_seed", "shuffle_seed", "epochs"))):
        raise ValueError("Bootstrap coverage/context mismatch")
    for row, entry in zip(rows, expected):
        if any(row.get(key) != entry[key] for key in ("case_id", "source_case_id", "scenario", "epoch")):
            raise ValueError("Recorded recipe/order differs from scheduled epoch")
    return len(rows)
