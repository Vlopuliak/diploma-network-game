"""Read-only draft-2 preflight. Does not install packages, train, or start Unity."""
import argparse
import hashlib
import importlib.metadata
import json
from pathlib import Path
import platform
import re
import sys


def read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def check_contract(contract, schema, width, action_count, behaviors):
    if (contract.get("scope") != "single-decision-sensory-intent" or
            contract.get("enabledIntentIndices") != [0, 1, 2, 3, 4] or
            contract.get("actionExecution") is not False):
        raise ValueError("Expected the approved single-decision scope without action execution")
    if contract["inputSchema"] != schema or contract["vectorObservationSize"] != width:
        raise ValueError("JSON contract disagrees with the C# encoder")
    if contract["discreteBranches"] != [action_count] or contract["stackedVectors"] != 1:
        raise ValueError("Expected one discrete branch and one vector stack")
    if contract["permanentlyMaskedActions"] != [5, 6]:
        raise ValueError("Both mechanism actions must remain masked")
    if contract["behaviorName"] not in behaviors:
        raise ValueError("Behavior name missing from PPO configuration")


def dependency_proposal(current, reference, root="com.unity.ml-agents"):
    """Compare only the dependency closure of ML-Agents, not the whole prototype."""
    visited = set()
    pending = [root]
    while pending:
        name = pending.pop()
        if name in visited:
            continue
        if name not in reference:
            raise ValueError("Reference lock lacks dependency: " + name)
        visited.add(name)
        pending.extend(reference[name].get("dependencies", {}))
    added, changed = {}, {}
    for name in sorted(visited):
        version = reference[name]["version"]
        if name not in current:
            added[name] = version
        elif current[name]["version"] != version:
            changed[name] = {"current": current[name]["version"], "reference": version}
    return {"added": added, "existing_version_changes": changed}


def check_model_signature(inputs, outputs, width, action_count):
    issues = []
    if set(inputs) != {"obs_0", "action_masks"}:
        issues.append("Expected only obs_0 and action_masks inputs")
    for name, expected in (("obs_0", width), ("action_masks", action_count)):
        shape = inputs.get(name, [])
        if len(shape) != 2 or shape[1] != expected:
            issues.append("{} must have shape [batch, {}], got {}".format(name, expected, shape))
    shape = outputs.get("discrete_actions", [])
    if len(shape) != 2 or shape[1] != 1:
        issues.append("Expected discrete_actions output [batch, 1]")
    return issues


def inspect_model(path, contract):
    import onnx
    model = onnx.load(str(path), load_external_data=False)
    onnx.checker.check_model(model)

    def shape(value):
        return [d.dim_value if d.HasField("dim_value") else d.dim_param
                for d in value.type.tensor_type.shape.dim]

    inputs = {v.name: shape(v) for v in model.graph.input}
    outputs = {v.name: shape(v) for v in model.graph.output}
    issues = check_model_signature(inputs, outputs, contract["vectorObservationSize"], contract["discreteBranches"][0])
    metadata = {entry.key: entry.value for entry in model.metadata_props}
    # Shape agreement alone never proves observation semantics or policy quality.
    return {
        "path": str(path.resolve()), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "onnx_checker": "pass", "inputs": inputs, "outputs": outputs,
        "signature_matches": not issues, "signature_issues": issues,
        "embedded_schema": metadata.get("the-copy.input-schema"),
        "embedded_schema_matches": metadata.get("the-copy.input-schema") == contract["inputSchema"],
        "model_purpose": metadata.get("the-copy.purpose"),
        "unity_inference": "not_run", "policy_quality": "not_evaluated",
    }


def validate_trainer(config, behavior):
    # The production CLI registers plugins before parsing trainer settings.
    from mlagents.plugins.trainer_type import register_trainer_plugins
    from mlagents.trainers.settings import RunOptions
    register_trainer_plugins()
    options = RunOptions.from_dict(config)
    settings = options.behaviors[behavior]
    if settings.trainer_type != "ppo":
        raise ValueError("Expected PPO trainer")
    return {"parser": "mlagents.trainers.settings.RunOptions", "status": "pass",
            "trainer_type": settings.trainer_type, "configured_max_steps": settings.max_steps,
            "training_steps_executed": 0}


def inspect_package(path, editor_version):
    package = read_json(path / "package.json")
    minimum = tuple(map(int, package["unity"].split(".")[:2]))
    actual = tuple(map(int, editor_version.split(".")[:2]))
    return {"path": str(path.resolve()), "name": package["name"], "version": package["version"],
            "minimum_unity": package["unity"], "minimum_unity_satisfied": actual >= minimum,
            "dependencies": package.get("dependencies", {})}


def run(args):
    import yaml
    repo = args.repo.resolve()
    contract = read_json(repo / "ai_training/config/the_copy_draft2_contract.json")
    config = yaml.safe_load((repo / "ai_training/config/the_copy_draft2_ppo.yaml").read_text(encoding="utf-8"))
    source = (repo / "Assets/Scripts/AI/TheCopy/CopyDecisionContractV2.cs").read_text(encoding="utf-8-sig")
    encoder = re.search(r'public static class CopyDecisionEncoderV2.*?SchemaId = "([^"]+)".*?ObservationCount = (\d+)', source, re.S)
    actions = re.search(r'ActionCount = (\d+)', source)
    snapshot = re.search(r'public readonly struct CopyDecisionSnapshotV2.*?SchemaId = "([^"]+)"', source, re.S)
    if encoder is None or actions is None:
        raise ValueError("Cannot locate C# encoder constants")
    check_contract(contract, encoder[1], int(encoder[2]), int(actions[1]), config["behaviors"])
    if snapshot is None or contract["snapshotSchema"] != snapshot[1]:
        raise ValueError("JSON snapshot schema disagrees with C#")
    editor = re.search(r'm_EditorVersion: (\S+)', (repo / "ProjectSettings/ProjectVersion.txt").read_text())[1]
    manifest = read_json(repo / "Packages/manifest.json")["dependencies"]
    lock = read_json(repo / "Packages/packages-lock.json")["dependencies"]
    packages = [inspect_package(args.mlagents_package, editor), inspect_package(args.inference_package, editor)]
    by_name = {p["name"]: p for p in packages}
    if set(by_name) != {"com.unity.ml-agents", "com.unity.ai.inference"}:
        raise ValueError("Provided package paths do not identify ML-Agents and Inference Engine")
    if by_name["com.unity.ml-agents"]["dependencies"]["com.unity.ai.inference"] != by_name["com.unity.ai.inference"]["version"]:
        raise ValueError("Inference version disagrees with ML-Agents package dependency")
    versions = {}
    for name in ("mlagents", "mlagents-envs", "torch", "numpy", "onnx", "protobuf"):
        versions[name] = importlib.metadata.version(name)
    report = {"contract": contract, "contract_check": "pass", "unity_editor": editor,
              "python": platform.python_version(), "python_packages": versions,
              "package_metadata": packages,
              "project_ml_agents_declared_in_lock": "com.unity.ml-agents" in lock,
              "project_direct_ml_agents": manifest.get("com.unity.ml-agents"),
              "trainer": validate_trainer(config, contract["behaviorName"]),
              "unity_agent_source_present": (repo / "Assets/Scripts/AI/TheCopy/CopyDecisionAgent.cs").is_file(),
              "unity_inference": "not_run_by_this_tool"}
    if args.reference_project:
        reference = read_json(args.reference_project / "Packages/packages-lock.json")["dependencies"]
        for p in packages:
            if reference[p["name"]]["version"] != p["version"]:
                raise ValueError("Reference lock and package metadata versions disagree")
        report["reference_dependency_proposal"] = dependency_proposal(lock, reference)
    if args.model:
        report["model"] = inspect_model(args.model, contract)
    blockers = []
    if not report["project_ml_agents_declared_in_lock"]:
        blockers.append("ML-Agents installation in team project requires approval")
    if any(not p["minimum_unity_satisfied"] for p in packages):
        blockers.append("Package minimum Unity version not satisfied")
    if not args.model or not report["model"]["signature_matches"]:
        blockers.append("No model with matching draft-2 signature")
    if args.model and report["model"]["model_purpose"] == "UNTRAINED_SMOKE_TEST_ONLY":
        blockers.append("Untrained smoke model is not a gameplay policy")
    blockers.append("This read-only preflight does not execute Unity inference or evaluate gameplay policy")
    report["blockers"] = blockers
    report["ready_for_game_inference"] = False
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--mlagents-package", type=Path, required=True)
    parser.add_argument("--inference-package", type=Path, required=True)
    parser.add_argument("--reference-project", type=Path)
    parser.add_argument("--model", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        report = run(args)
    except Exception as error:
        report = {"preflight_error": type(error).__name__ + ": " + str(error), "ready_for_game_inference": False}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    # Missing approval/model is explicitly not reported as readiness or successful inference.
    return 1 if "preflight_error" in report else 2 if report["blockers"] else 0


if __name__ == "__main__":
    sys.exit(main())
