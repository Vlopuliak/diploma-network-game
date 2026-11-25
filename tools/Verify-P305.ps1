param(
    [string] $UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe',
    [string] $MlPython = 'python',
    [string] $RecipePlan,
    [string] $RewardPlan,
    [string] $RewardCapture,
    [string] $CoordinatorPlan,
    [switch] $KeepWorkspace,
    [switch] $UseWebSockets,
    [switch] $VerifyPerception,
    [switch] $VerifyScene,
    [switch] $VerifyObservations,
    [switch] $VerifyDecisionInput,
    [switch] $VerifyNpcInteraction,
    [switch] $VerifyPlayerSight,
    [switch] $VerifyDecisionInputV2,
    [switch] $VerifyAgentInference,
    [switch] $VerifyDecisionTrace,
    [switch] $VerifyAttentionCases,
    [switch] $VerifyTrainingAdapter,
    [switch] $VerifyTrainingCoordinator
)

$ErrorActionPreference = 'Stop'
if ($VerifyTrainingCoordinator) {
    $VerifyTrainingAdapter = $true
    if (!$CoordinatorPlan) { $CoordinatorPlan = $RewardPlan }
    if (!$CoordinatorPlan) { throw 'Coordinator verification requires a frozen -CoordinatorPlan or -RewardPlan.' }
    $CoordinatorPlan = (Resolve-Path -LiteralPath $CoordinatorPlan).Path
}
if ($VerifyTrainingAdapter) {
    if (!$RewardPlan -or !$RewardCapture) { throw 'Training adapter verification requires -RewardPlan and -RewardCapture from the audited P4-05 replay.' }
    $RewardPlan = (Resolve-Path -LiteralPath $RewardPlan).Path
    $RewardCapture = (Resolve-Path -LiteralPath $RewardCapture).Path
    $VerifyAttentionCases = $true
}
if ($RecipePlan) { $RecipePlan = (Resolve-Path -LiteralPath $RecipePlan).Path; $VerifyAttentionCases = $true }
if ($VerifyAttentionCases) { $VerifyDecisionTrace = $true }
if ($VerifyDecisionInput) { $VerifyObservations = $true }
if ($VerifyNpcInteraction) { $VerifyDecisionInput = $true; $VerifyObservations = $true }
if ($VerifyDecisionTrace) { $VerifyAgentInference = $true }
if ($VerifyAgentInference) { $VerifyDecisionInputV2 = $true }
if ($VerifyDecisionInputV2) { $VerifyPlayerSight = $true }
if ($VerifyPlayerSight) { $VerifyPerception = $true }
if ($VerifyPerception -and ($VerifyScene -or $VerifyObservations)) { throw 'Run scene and generated-prefab checks separately.' }
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$scratchRoot = Join-Path $repo 'Logs/AI-NPC'
$runId = [guid]::NewGuid().ToString('N').Substring(0, 8)
$scratch = Join-Path $scratchRoot ('p305-workspace-' + $runId)
$logPrefix = if ($VerifyTrainingAdapter) { 'P4-06-adapter-' } elseif ($VerifyAttentionCases) { 'P4-05-cases-' } elseif ($VerifyDecisionTrace) { 'P4-04-trace-' } elseif ($VerifyDecisionInput) { 'P4-03-decision-input-' } elseif ($VerifyObservations) { 'P3-07-observations-' } elseif ($VerifyScene) { 'P3-06-scene-' } elseif ($VerifyPerception) { 'P3-06-perception-' } else { 'P3-05-remote-' }
$log = Join-Path $scratchRoot ($logPrefix + $runId + '.log')
$process = $null

New-Item -ItemType Directory -Path $scratchRoot -Force | Out-Null
New-Item -ItemType Directory -Path $scratch | Out-Null
Write-Output "Workspace: $scratch"
Write-Output "Log: $log"

try {
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings', 'Library')) {
        $source = Join-Path $repo $folder
        if (!(Test-Path -LiteralPath $source -PathType Container)) { continue }

        # Ignore editor process IDs and live database locks from the source checkout.
        & robocopy $source (Join-Path $scratch $folder) /E /XJ /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XF ilpp.pid burst.pid ArtifactDB-lock SourceAssetDB-lock
        if ($LASTEXITCODE -ge 8) { throw "Copy failed for $folder (exit $LASTEXITCODE)." }
    }

    $entryMethod = if ($VerifyScene -or $VerifyObservations) { 'TheCopy.Tests.P306SceneVerification.Run' } else { 'TheCopy.Tests.P305AcousticVerification.Run' }
    if ($VerifyAgentInference) {
        $smokeModel = Join-Path $scratch 'Logs/AI-NPC/P305SmokeModel.onnx'
        & $MlPython -B (Join-Path $PSScriptRoot 'export_ai_npc_smoke_model.py') --output $smokeModel --include-negative-fixture
        if ($LASTEXITCODE -ne 0) { throw 'Untrained smoke model export failed; use the existing ML Python environment.' }
        Copy-Item -LiteralPath $smokeModel -Destination (Join-Path $scratch 'Assets/P305SmokeModel.onnx')
        $negativeModel = Join-Path (Split-Path $smokeModel) 'P305SmokeModel-wrong-width.onnx'
        Copy-Item -LiteralPath $negativeModel -Destination (Join-Path $scratch 'Assets/P305WrongWidthModel.onnx')
    }
    if ($RecipePlan) {
        $copiedPlan = Join-Path $scratch 'Logs/AI-NPC/P405RecipePlan'
        New-Item -ItemType Directory -Path $copiedPlan | Out-Null
        foreach ($split in @('train', 'id')) {
            foreach ($suffix in @('-recipes.json', '-manifest.json')) {
                $name = $split + $suffix
                Copy-Item -LiteralPath (Join-Path $RecipePlan $name) -Destination (Join-Path $copiedPlan $name)
            }
        }
    }
    if ($VerifyTrainingAdapter) {
        & $MlPython -B (Join-Path $PSScriptRoot 'export_ai_npc_reward_parity.py') --plan $RewardPlan --capture $RewardCapture --output (Join-Path $scratch 'Logs/AI-NPC/P406RewardParity.json')
        if ($LASTEXITCODE -ne 0) { throw 'Python reward parity fixture export failed.' }
    }
    if ($VerifyTrainingCoordinator) {
        $copiedCoordinatorPlan = Join-Path $scratch 'Logs/AI-NPC/P406CoordinatorPlan'
        New-Item -ItemType Directory -Path $copiedCoordinatorPlan | Out-Null
        foreach ($name in @('train-recipes.json', 'train-manifest.json', 'generation-audit.json')) {
            Copy-Item -LiteralPath (Join-Path $CoordinatorPlan $name) -Destination (Join-Path $copiedCoordinatorPlan $name)
        }
    }
    $argsLine = '-batchmode -nographics -projectPath "' + $scratch +
        '" -p305Disposable -executeMethod ' + $entryMethod + ' -logFile "' + $log + '"'
    if ($UseWebSockets) { $argsLine += ' -p305WebSockets' }
    if ($VerifyPerception) { $argsLine += ' -p306Perception' }
    if ($VerifyObservations) { $argsLine += ' -p307Observations' }
    if ($VerifyDecisionInput) { $argsLine += ' -p403DecisionInput' }
    if ($VerifyNpcInteraction) { $argsLine += ' -p403NpcInteraction' }
    if ($VerifyPlayerSight) { $argsLine += ' -p403PlayerSight' }
    if ($VerifyDecisionInputV2) { $argsLine += ' -p403DecisionInputV2' }
    if ($VerifyAgentInference) { $argsLine += ' -p403AgentInference' }
    if ($VerifyDecisionTrace) { $argsLine += ' -p404DecisionTrace' }
    if ($VerifyAttentionCases) { $argsLine += ' -p405AttentionCases' }
    if ($RecipePlan) { $argsLine += ' -p405RecipePlan "' + $copiedPlan + '"' }
    if ($VerifyTrainingAdapter) { $argsLine += ' -p406TrainingAdapter' }
    if ($VerifyTrainingCoordinator) { $argsLine += ' -p406Coordinator -p406CoordinatorPlan "' + $copiedCoordinatorPlan + '"' }
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $argsLine -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(240000)) { throw "Unity verification exceeded 240 seconds. See $log." }

    $markers = Select-String -LiteralPath $log -Pattern '\[P3-05\]|\[P3-06\]|\[P3-06 SCENE\]|\[P3-07\]|\[P4-03\]|\[P4-03 INTERACTION\]|\[P4-03 SIGHT\]|\[P4-03 V2\]|\[P4-03 AGENT\]|\[P4-04 TRACE\]|\[P4-05 CASES\]|\[P4-05 RECIPES\]|\[P4-06 ADAPTER\]|error CS'
    $markers | ForEach-Object { Write-Output $_.Line }
    $completion = if ($VerifyScene -or $VerifyObservations) { '\[P3-06 SCENE\] COMPLETE' } else { '\[P3-05\] COMPLETE' }
    if ($process.ExitCode -ne 0 -or !($markers -match $completion) -or
        ($VerifyPerception -and !($markers -match '\[P3-06\] COMPLETE')) -or
        ($VerifyObservations -and !($markers -match '\[P3-07\] COMPLETE')) -or
        ($VerifyDecisionInput -and !($markers -match '\[P4-03\] COMPLETE')) -or
        ($VerifyNpcInteraction -and !($markers -match '\[P4-03 INTERACTION\] COMPLETE')) -or
        ($VerifyPlayerSight -and !($markers -match '\[P4-03 SIGHT\] COMPLETE')) -or
        ($VerifyDecisionInputV2 -and !($markers -match '\[P4-03 V2\] COMPLETE')) -or
        ($VerifyAgentInference -and !($markers -match '\[P4-03 AGENT\] COMPLETE')) -or
        ($VerifyDecisionTrace -and !($markers -match '\[P4-04 TRACE\] COMPLETE')) -or
        ($VerifyAttentionCases -and !($markers -match '\[P4-05 CASES\] COMPLETE')) -or
        ($RecipePlan -and !($markers -match '\[P4-05 RECIPES\] COMPLETE')) -or
        ($VerifyTrainingAdapter -and !($markers -match '\[P4-06 ADAPTER\] COMPLETE')) -or
        ($VerifyTrainingCoordinator -and !(Select-String -LiteralPath $log -Quiet -Pattern '\[P4-06 COORDINATOR\] COMPLETE: controlled train coordinator'))) {
        throw "NPC verification failed (Unity exit $($process.ExitCode)). See $log."
    }
    if ($VerifyDecisionTrace) {
        $savedTraces = Join-Path $scratchRoot ('p404-records-' + $runId)
        & (Join-Path $PSScriptRoot 'Export-AINpcTraceEvidence.ps1') -UnityLog $log -Workspace $scratch -OutputDirectory $savedTraces -MlPython $MlPython
    }
    if ($VerifyAttentionCases) {
        $savedCases = Join-Path $scratchRoot ('p405-cases-' + $runId)
        & (Join-Path $PSScriptRoot 'Export-AINpcAttentionEvidence.ps1') -UnityLog $log -Workspace $scratch -OutputDirectory $savedCases -MlPython $MlPython
    }
    if ($RecipePlan) {
        $savedReplay = Join-Path $scratchRoot ('p405-replay-' + $runId)
        & (Join-Path $PSScriptRoot 'Export-AINpcRecipeEvidence.ps1') -UnityLog $log -Workspace $scratch -RecipePlan $RecipePlan -OutputDirectory $savedReplay -MlPython $MlPython
    }
    if ($VerifyTrainingAdapter) {
        $savedAdapter = Join-Path $scratchRoot ('p406-adapter-' + $runId)
        & (Join-Path $PSScriptRoot 'Export-AINpcTrainingEvidence.ps1') -UnityLog $log -Workspace $scratch -OutputDirectory $savedAdapter
    }
    if ($VerifyTrainingCoordinator) {
        $savedCoordinator = Join-Path $scratchRoot ('p406-coordinator-' + $runId)
        & (Join-Path $PSScriptRoot 'Export-AINpcCoordinatorEvidence.ps1') -UnityLog $log -Workspace $scratch -OutputDirectory $savedCoordinator -MlPython $MlPython
    }
}
finally {
    if ($process -and !$process.HasExited) { Stop-Process -Id $process.Id }
    if (!$KeepWorkspace -and (Test-Path -LiteralPath $scratch)) {
        $expectedParent = (Resolve-Path -LiteralPath $scratchRoot).Path
        $resolved = Get-Item -LiteralPath $scratch -Force
        if ($resolved.Parent.FullName -ne $expectedParent -or $resolved.Name -ne "p305-workspace-$runId" -or $resolved.LinkType) {
            throw "Unexpected temporary project path: $($resolved.FullName)"
        }
        Remove-Item -LiteralPath $resolved.FullName -Recurse -Force
    }
}
