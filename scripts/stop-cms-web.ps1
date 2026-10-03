# Stops dotnet processes that lock CMS.Web build output (run/debug leftovers).
$ErrorActionPreference = 'SilentlyContinue'

$stopped = 0
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
    Where-Object { $_.CommandLine -like '*CMS.Web*' } |
    ForEach-Object {
        Write-Host "Stopping dotnet PID $($_.ProcessId)"
        Stop-Process -Id $_.ProcessId -Force
        $stopped++
    }

# Debugger can also hold DLL locks after a debug session.
Get-Process -Name 'clrdbg' -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Stopping clrdbg PID $($_.Id)"
    Stop-Process -Id $_.Id -Force
    $stopped++
}

if ($stopped -eq 0) {
    Write-Host 'No CMS.Web dotnet/clrdbg process found.'
} else {
    Write-Host "Stopped $stopped process(es). Waiting for file handles..."
    Start-Sleep -Seconds 1
}
