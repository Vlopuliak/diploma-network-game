param([Parameter(Mandatory)] [string] $Prepared)
$ErrorActionPreference = 'Stop'
$folder = (Resolve-Path -LiteralPath $Prepared).Path
$spec = Get-Content -LiteralPath (Join-Path $folder 'prepared.json') -Raw | ConvertFrom-Json
$resultPath = Join-Path $folder 'result.json'
$state = 'Running'
if (Test-Path -LiteralPath $resultPath) {
    $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    $state = $result.status
}
$count = 0
$log = Join-Path $folder 'unity.log'
if (Test-Path -LiteralPath $log) {
    $match = [regex]::Match((Get-Content -LiteralPath $log -Raw), '\[P4-06 COORDINATOR\] RECORDS_DIR=(.+)')
    if ($match.Success) {
        $capture = [IO.Path]::GetFullPath($match.Groups[1].Value.Trim())
        $allowed = [IO.Path]::GetFullPath((Join-Path $spec.workspace 'Logs/AI-NPC')) + [IO.Path]::DirectorySeparatorChar
        if (!$capture.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected capture location.' }
        $records = Join-Path $capture 'complete/records.jsonl'
        if (Test-Path -LiteralPath $records) {
            # A normal ReadLines/Get-Content handle can deny Unity's next AppendAllText write.
            $share = [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete
            $stream = [IO.FileStream]::new($records, [IO.FileMode]::Open, [IO.FileAccess]::Read, $share)
            $reader = [IO.StreamReader]::new($stream)
            try { $count = ([regex]::Matches($reader.ReadToEnd(), "`n")).Count }
            finally { $reader.Dispose(); $stream.Dispose() }
        }
    }
}
[ordered]@{
    state = $state
    recorded_cases = $count
    planned_cases = $spec.planned_decisions
    complete_train_passes = [math]::Floor($count / 449)
    seed = $spec.seed
    observed_utc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json
