# Headless Unity compile check - compiles the project in batch mode and reports any C# errors,
# with no human in the loop. The Unity EDITOR must be CLOSED first (it locks the project, so a
# second instance can't open it). Usage:  powershell -ExecutionPolicy Bypass -File Tools/compile-check.ps1
param(
    # Defaults to the repo root (this script lives in <repo>/Tools), so it works on any clone/PC.
    [string]$Project = (Split-Path $PSScriptRoot -Parent),
    [string]$Unity   = "C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe"
)

if (-not (Test-Path $Unity))   { Write-Output "ERROR: Unity.exe not found at $Unity"; exit 2 }
if (-not (Test-Path $Project)) { Write-Output "ERROR: project not found at $Project"; exit 2 }
if (Test-Path "$Project\Temp\UnityLockfile") {
    Write-Output "ABORT: the Unity Editor is OPEN (lockfile present). Close it, then re-run."
    exit 3
}

$log = Join-Path $env:TEMP "caveman-compile.log"
if (Test-Path $log) { Remove-Item $log -Force }

Write-Output "Compiling (batch mode) - this takes ~30s-2min..."
& $Unity -batchmode -quit -projectPath $Project -logFile $log | Out-Null
$code = $LASTEXITCODE

if (-not (Test-Path $log)) { Write-Output "ERROR: no log produced (exit $code)"; exit 2 }
$errs = Select-String -Path $log -Pattern "error CS\d+" | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique
if ($errs) {
    Write-Output "COMPILE FAILED ($($errs.Count) error(s)):"
    $errs | ForEach-Object { Write-Output "  $_" }
    exit 1
}
Write-Output "COMPILE CLEAN (Unity exit code $code)."
exit 0
