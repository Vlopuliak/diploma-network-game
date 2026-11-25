param(
    [Parameter(Mandatory)] [string] $UnityLog,
    [Parameter(Mandatory)] [string] $Workspace,
    [Parameter(Mandatory)] [string] $OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$logText = Get-Content -LiteralPath $UnityLog -Raw
$match = [regex]::Match($logText, '\[P4-06 ADAPTER\] PASS_COUNT=(\d+)')
if (!$match.Success -or $logText -notmatch '\[P4-06 ADAPTER\] COMPLETE' -or $logText -match 'error CS\d+|\[P3-05\] FAIL') {
    throw 'Training adapter verification did not complete successfully.'
}
$fixturePath = Join-Path $Workspace 'Logs/AI-NPC/P406RewardParity.json'
$fixture = Get-Content -LiteralPath $fixturePath -Raw | ConvertFrom-Json
if ($fixture.profile_id -ne 'the-copy.expert-attention/1' -or $fixture.frame_count -ne 674 -or
    $fixture.training_steps -ne 0 -or $fixture.ood_vectors_read -ne $false) { throw 'Unexpected parity fixture.' }
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Use a new evidence directory.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
Copy-Item -LiteralPath $fixturePath -Destination (Join-Path $OutputDirectory 'reward-parity.json')
$logSnapshot = Join-Path $OutputDirectory 'unity-log.txt'
[IO.File]::WriteAllText($logSnapshot, $logText, [Text.UTF8Encoding]::new($false))
$sources = [ordered]@{}
foreach ($path in @('Assets/Scripts/AI/TheCopy/CopyAttentionReward.cs',
    'Assets/Scripts/AI/TheCopy/CopyAttentionTrainingAgent.cs', 'Assets/Tests/AI/P406TrainingVerification.cs',
    'Assets/Tests/AI/P306PerceptionVerification.cs', 'Assets/Scripts/AI/TheCopy/CopyDecisionInputV2.cs',
    'Assets/Scripts/AI/TheCopy/CopyDecisionContractV2.cs')) {
    $sources[$path] = (Get-FileHash -LiteralPath (Join-Path $Workspace $path) -Algorithm SHA256).Hash
}
$adapterPasses = [int]$match.Groups[1].Value
$regressionPasses = ([regex]::Matches($logText, '\[P[^\]]+\] PASS ')).Count
[ordered]@{
    purpose = 'CONTROLLED_ADAPTER_API_TESTS_NOT_PPO'
    adapter_passes = $adapterPasses
    parity_checks = ([int]$fixture.frame_count * 7)
    lifecycle_and_negative_checks = ($adapterPasses - [int]$fixture.frame_count * 7)
    regression_diagnostic_passes = $regressionPasses
    optimization_steps_executed = 0
    connected_trainer_tested = $false
    ood_vectors_read = $false
    policy_quality = 'not_evaluated'
    repo_head = (& git -C $repo rev-parse HEAD)
    repo_branch = (& git -C $repo branch --show-current)
    unity_log_snapshot_sha256 = (Get-FileHash -LiteralPath $logSnapshot -Algorithm SHA256).Hash
    fixture_sha256 = (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash
    source_hashes = $sources
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'verification.json') -Encoding utf8
Write-Output "Training adapter evidence: $OutputDirectory"
