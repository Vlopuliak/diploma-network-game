param(
    [string] $UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe',
    [switch] $KeepWorkspace,
    [switch] $UseWebSockets,
    [switch] $VerifyPerception,
    [switch] $VerifyScene,
    [switch] $VerifyObservations,
    [switch] $VerifyDecisionInput
)

$ErrorActionPreference = 'Stop'
if ($VerifyDecisionInput) { $VerifyObservations = $true }
if ($VerifyPerception -and ($VerifyScene -or $VerifyObservations)) { throw 'Run scene and generated-prefab checks separately.' }
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$scratchRoot = Join-Path $repo 'Logs/AI-NPC'
$runId = [guid]::NewGuid().ToString('N').Substring(0, 8)
$scratch = Join-Path $scratchRoot ('p305-workspace-' + $runId)
$logPrefix = if ($VerifyDecisionInput) { 'P4-03-decision-input-' } elseif ($VerifyObservations) { 'P3-07-observations-' } elseif ($VerifyScene) { 'P3-06-scene-' } elseif ($VerifyPerception) { 'P3-06-perception-' } else { 'P3-05-remote-' }
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
    $argsLine = '-batchmode -nographics -projectPath "' + $scratch +
        '" -p305Disposable -executeMethod ' + $entryMethod + ' -logFile "' + $log + '"'
    if ($UseWebSockets) { $argsLine += ' -p305WebSockets' }
    if ($VerifyPerception) { $argsLine += ' -p306Perception' }
    if ($VerifyObservations) { $argsLine += ' -p307Observations' }
    if ($VerifyDecisionInput) { $argsLine += ' -p403DecisionInput' }
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $argsLine -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(240000)) { throw "Unity verification exceeded 240 seconds. See $log." }

    $markers = Select-String -LiteralPath $log -Pattern '\[P3-05\]|\[P3-06\]|\[P3-06 SCENE\]|\[P3-07\]|\[P4-03\]|error CS'
    $markers | ForEach-Object { Write-Output $_.Line }
    $completion = if ($VerifyScene -or $VerifyObservations) { '\[P3-06 SCENE\] COMPLETE' } else { '\[P3-05\] COMPLETE' }
    if ($process.ExitCode -ne 0 -or !($markers -match $completion) -or
        ($VerifyPerception -and !($markers -match '\[P3-06\] COMPLETE')) -or
        ($VerifyObservations -and !($markers -match '\[P3-07\] COMPLETE')) -or
        ($VerifyDecisionInput -and !($markers -match '\[P4-03\] COMPLETE'))) {
        throw "NPC verification failed (Unity exit $($process.ExitCode)). See $log."
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
