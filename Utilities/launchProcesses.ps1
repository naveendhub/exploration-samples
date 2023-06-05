param
(
    [int]$numberOfProcesses = 1,
    [string]$exeLocation = "C:\Workspace\Source\exploration-samples\Dotnet-samples\exploration-samples\exploration-samples\bin\Debug\exploration-samples.exe"

)


try {
    # Set strict mode, to prevent nasty debugging sessions
    Set-StrictMode -Version 5.1
    for ($i=1; $i -le $numberOfProcesses; $i++)
    {
        Start-Process $exeLocation
    }

}
catch {
    Write-Host "Launch Failed: $($PSItem.ToString())" -ForegroundColor Red
} finally {
    Set-StrictMode -Off
}
