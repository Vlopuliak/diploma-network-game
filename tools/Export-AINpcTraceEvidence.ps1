param(
    [Parameter(Mandatory)] [string] $UnityLog,
    [Parameter(Mandatory)] [string] $Workspace,
    [Parameter(Mandatory)] [string] $OutputDirectory,
    [string] $MlPython = 'python'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$marker = Select-String -LiteralPath $UnityLog -Pattern '\[P4-04 TRACE\] RECORDS_DIR=(.+)' | Select-Object -Last 1
if (!$marker) { throw 'Missing trace artifacts directory.' }
$traceDirectory = (Resolve-Path -LiteralPath $marker.Matches[0].Groups[1].Value.Trim()).Path
$expectedRoot = [IO.Path]::GetFullPath((Join-Path $Workspace 'Logs/AI-NPC')) + [IO.Path]::DirectorySeparatorChar
if (!$traceDirectory.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Trace path is outside the temporary project log directory.'
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach ($name in @('p404-smoke-records', 'p404-controlled-records', 'p404-pending-despawn')) {
    $records = Join-Path $OutputDirectory ($name + '.jsonl')
    Copy-Item -LiteralPath (Join-Path $traceDirectory ($name + '.jsonl')) -Destination $records
    & $MlPython -B (Join-Path $repo 'ai_training/evaluation/intent_metrics.py') --records $records --target-tolerance 0.05 --output (Join-Path $OutputDirectory ($name + '-metrics.json'))
    if ($LASTEXITCODE -ne 0) { throw ('Trace evaluator failed for ' + $name) }
}
Write-Output "Trace evidence: $OutputDirectory"
