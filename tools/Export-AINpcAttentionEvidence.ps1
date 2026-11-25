param(
    [Parameter(Mandatory)] [string] $UnityLog,
    [Parameter(Mandatory)] [string] $Workspace,
    [Parameter(Mandatory)] [string] $OutputDirectory,
    [string] $MlPython = 'python'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$marker = Select-String -LiteralPath $UnityLog -Pattern '\[P4-05 CASES\] RECORDS_DIR=(.+)' | Select-Object -Last 1
if (!$marker) { throw 'Missing attention case artifacts directory.' }
$source = (Resolve-Path -LiteralPath $marker.Matches[0].Groups[1].Value.Trim()).Path
$expectedRoot = [IO.Path]::GetFullPath((Join-Path $Workspace 'Logs/AI-NPC')) + [IO.Path]::DirectorySeparatorChar
if (!$source.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Attention evidence is outside the temporary project log directory.'
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach ($name in @('manifest.json', 'records.jsonl', 'provenance.json')) {
    Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $OutputDirectory $name)
}
& $MlPython -B (Join-Path $repo 'ai_training/evaluation/attention_metrics.py') --records (Join-Path $OutputDirectory 'records.jsonl') --manifest (Join-Path $OutputDirectory 'manifest.json') --near-optimal-regret 0.05 --output (Join-Path $OutputDirectory 'metrics.json')
if ($LASTEXITCODE -ne 0) { throw 'Attention evaluator rejected the diagnostic records.' }
$report = Get-Content -LiteralPath (Join-Path $OutputDirectory 'metrics.json') -Raw | ConvertFrom-Json
if (!$report.complete -or $report.split -ne 'diagnostic' -or
    $report.overall.observed_case_count -ne 8 -or $report.overall.accepted_invalid_count -ne 0 -or
    $report.quality_gate -ne 'not_assessed_train_or_diagnostic_split') {
    throw 'Incomplete/unsafe diagnostic evidence or unexpected quality gate.'
}
$sourceHashes = [ordered]@{}
foreach ($path in @('Assets/Scripts/AI/TheCopy/CopyDecisionAgent.cs', 'Assets/Scripts/AI/TheCopy/CopyDecisionTrace.cs',
    'Assets/Scripts/AI/TheCopy/CopyDecisionContractV2.cs', 'Assets/Scripts/AI/TheCopy/CopyDecisionInputV2.cs',
    'Assets/Scripts/AI/TheCopy/CopyHearing.cs', 'Assets/Scripts/AI/TheCopy/CopyLightPerception.cs',
    'Assets/Scripts/AI/TheCopy/CopyPlayerPerception.cs', 'Assets/Tests/AI/P405AttentionCaseVerification.cs')) {
    $sourceHashes[$path] = (Get-FileHash -LiteralPath (Join-Path $Workspace $path) -Algorithm SHA256).Hash
}
$artifactHashes = [ordered]@{}
foreach ($name in @('manifest.json', 'records.jsonl', 'provenance.json', 'metrics.json')) {
    $artifactHashes[$name] = (Get-FileHash -LiteralPath (Join-Path $OutputDirectory $name) -Algorithm SHA256).Hash
}
# Scratch source hashes identify the code actually executed, even in a dirty checkout.
[ordered]@{
    repo_head = (& git -C $repo rev-parse HEAD)
    repo_branch = (& git -C $repo branch --show-current)
    repo_status = @(& git -C $repo status --porcelain=v1)
    source_hashes = $sourceHashes
    artifact_hashes = $artifactHashes
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'verification-context.json') -Encoding utf8
Write-Output "Attention evidence: $OutputDirectory"
