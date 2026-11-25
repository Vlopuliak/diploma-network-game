param(
    [Parameter(Mandatory)] [string] $UnityLog,
    [Parameter(Mandatory)] [string] $Workspace,
    [Parameter(Mandatory)] [string] $RecipePlan,
    [Parameter(Mandatory)] [string] $OutputDirectory,
    [string] $MlPython = 'python'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$marker = Select-String -LiteralPath $UnityLog -Pattern '\[P4-05 RECIPES\] RECORDS_DIR=(.+)' | Select-Object -Last 1
if (!$marker) { throw 'Missing sensor replay evidence directory.' }
$source = (Resolve-Path -LiteralPath $marker.Matches[0].Groups[1].Value.Trim()).Path
$expectedRoot = [IO.Path]::GetFullPath((Join-Path $Workspace 'Logs/AI-NPC')) + [IO.Path]::DirectorySeparatorChar
if (!$source.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Replay path outside disposable Logs.' }
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Use a new evidence directory; do not overwrite a frozen replay.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
foreach ($split in @('train', 'id')) {
    foreach ($suffix in @('-recipes.json', '-manifest.json', '-inputs.jsonl', '-provenance.json')) {
        $name = $split + $suffix
        Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $OutputDirectory $name)
    }
}
& $MlPython -B (Join-Path $repo 'ai_training/cases/validate_sensor_replay.py') --plan $RecipePlan --capture $OutputDirectory --output (Join-Path $OutputDirectory 'sensor-audit.json')
if ($LASTEXITCODE -ne 0) { throw 'Sensor replay did not pass frozen-recipe validation.' }
$hashes = [ordered]@{}
foreach ($file in Get-ChildItem -LiteralPath $OutputDirectory -File) {
    $hashes[$file.Name] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
}
$sources = [ordered]@{}
foreach ($path in @('Assets/Tests/AI/P405RecipeVerification.cs', 'Assets/Scripts/AI/TheCopy/CopyDecisionInputV2.cs',
    'Assets/Scripts/AI/TheCopy/CopyDecisionContractV2.cs', 'Assets/Scripts/AI/TheCopy/CopyDecisionContract.cs',
    'Assets/Scripts/AI/TheCopy/CopyHearing.cs', 'Assets/Scripts/AI/TheCopy/CopyLightPerception.cs',
    'Assets/Scripts/AI/TheCopy/CopyPlayerPerception.cs')) {
    $sources[$path] = (Get-FileHash -LiteralPath (Join-Path $Workspace $path) -Algorithm SHA256).Hash
}
[ordered]@{ repo_head = (& git -C $repo rev-parse HEAD); source_hashes = $sources; artifact_hashes = $hashes } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'verification-context.json') -Encoding utf8
Write-Output "Sensor replay evidence: $OutputDirectory"
