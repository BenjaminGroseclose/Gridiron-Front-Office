# Before "watch: DevHost": port 5180 must be free, or held by the watcher's own DevHost (F5 re-runs this check while
# the watcher is already up). Anything else (an old-style debug launch, a stray dotnet run) has to be stopped first.
$listener = Get-NetTCPConnection -LocalPort 5180 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $listener) { exit 0 }

$app = Get-CimInstance Win32_Process -Filter "ProcessId = $($listener.OwningProcess)"
$parent = if ($app) { Get-CimInstance Win32_Process -Filter "ProcessId = $($app.ParentProcessId)" } else { $null }
if ($parent -and $parent.CommandLine -like '*DOTNET_WATCH=*') {
    Write-Host 'The watcher is already running on http://localhost:5180.'
    exit 0
}

Write-Host "Port 5180 is in use by $($app.Name) (pid $($app.ProcessId)), not by the watcher: stop that DevHost first (a debug session: Shift+F5), then run watch: DevHost again."
exit 1
