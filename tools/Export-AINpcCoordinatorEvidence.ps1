param(
    [Parameter(Mandatory)] [string] $UnityLog,
    [Parameter(Mandatory)] [string] $Workspace,
    [Parameter(Mandatory)] [string] $OutputDirectory,
    [string] $MlPython = 'python'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$workspacePath = (Resolve-Path -LiteralPath $Workspace).Path
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
$allowedOutput = [IO.Path]::GetFullPath((Join-Path $repo 'Logs/AI-NPC')) + [IO.Path]::DirectorySeparatorChar
if (!$outputPath.StartsWith($allowedOutput, [StringComparison]::OrdinalIgnoreCase)) { throw 'Evidence must stay in repository Logs/AI-NPC.' }
$text = Get-Content -LiteralPath $UnityLog -Raw
$match = [regex]::Match($text, '\[P4-06 COORDINATOR\] RECORDS_DIR=(.+)')
if (!$match.Success -or $text -notmatch '\[P4-06 COORDINATOR\] COMPLETE' -or $text -match 'error CS\d+|\[P3-05\] FAIL') {
    throw 'Coordinator verification did not complete successfully.'
}
$source = [IO.Path]::GetFullPath($match.Groups[1].Value.Trim())
$allowedSource = [IO.Path]::GetFullPath((Join-Path $workspacePath 'Logs/AI-NPC')) + [IO.Path]::DirectorySeparatorChar
if (!$source.StartsWith($allowedSource, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected coordinator capture path.' }
if (Test-Path -LiteralPath $outputPath) { throw 'Use a new evidence directory.' }
New-Item -ItemType Directory -Path $outputPath | Out-Null
foreach ($name in @('complete', 'technical-failure', 'capture-failure')) {
    $capture = Join-Path $source $name
    if (!(Test-Path -LiteralPath $capture -PathType Container)) { continue }
    $destination = Join-Path $outputPath $name
    New-Item -ItemType Directory -Path $destination | Out-Null
    # Copy only declared evidence files, never arbitrary captures or Unity caches.
    foreach ($file in @('manifest.json', 'run-context.json', 'records.jsonl', 'summary.json')) {
        $path = Join-Path $capture $file
        if (Test-Path -LiteralPath $path -PathType Leaf) { Copy-Item -LiteralPath $path -Destination (Join-Path $destination $file) }
    }
}
Copy-Item -LiteralPath $UnityLog -Destination (Join-Path $outputPath 'unity-log.txt')
& $MlPython -B (Join-Path $PSScriptRoot 'validate_ai_npc_coordinator.py') --capture $outputPath --output (Join-Path $outputPath 'audit.json')
if ($LASTEXITCODE -ne 0) { throw 'Copied coordinator evidence failed audit.' }
$sources = [ordered]@{}
foreach ($path in @('Assets/Scripts/AI/TheCopy/CopyAttentionRecipeCoordinator.cs',
    'Assets/Scripts/AI/TheCopy/CopyAttentionTrainingAgent.cs', 'Assets/Scripts/AI/TheCopy/CopyAttentionReward.cs',
    'Assets/Scripts/AI/TheCopy/CopyHearing.cs', 'Assets/Tests/AI/P406CoordinatorVerification.cs',
    'Assets/Tests/AI/P406TrainingVerification.cs', 'Assets/Tests/AI/P306PerceptionVerification.cs')) {
    $sources[$path] = (Get-FileHash -LiteralPath (Join-Path $workspacePath $path) -Algorithm SHA256).Hash
    if ($sources[$path] -ne (Get-FileHash -LiteralPath (Join-Path $repo $path) -Algorithm SHA256).Hash) {
        throw "Executed source differs from checkout: $path"
    }
}
$hashes = [ordered]@{}
Get-ChildItem -LiteralPath $outputPath -File -Recurse | ForEach-Object {
    $hashes[$_.FullName.Substring($outputPath.Length + 1)] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
}
[ordered]@{
    purpose = 'COORDINATOR_API_VERIFICATION_NOT_PPO'
    optimization_steps = 0
    policy_quality = 'not_assessed'
    repo_head = (& git -C $repo rev-parse HEAD)
    repo_branch = (& git -C $repo branch --show-current)
    source_hashes = $sources
    evidence_hashes = $hashes
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputPath 'provenance.json') -Encoding utf8
Write-Output "Coordinator evidence: $outputPath"
