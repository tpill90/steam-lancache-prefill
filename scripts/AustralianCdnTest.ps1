# TODO comment
$ErrorActionPreference = "Stop"
# TODO Set-StrictMode -Version 2
Import-Module PSWriteColor

$uri = "/depot/3527291/chunk/5e385330290f274474a065226bf6ccf0042a8e2d"

$allServers = @()
$jsonFiles = Get-ChildItem -Path "C:\Users\Tim\Dropbox\Programming\Lancache-Prefills\SteamPrefill\globalping-lancache\servers" -Filter *.json
foreach($file in $jsonFiles)
{
    $fileContent = Get-Content -Path $file.FullName -Raw
    $serversInFile = ConvertFrom-Json -InputObject $fileContent
    $allServers += $serversInFile.response.servers
}

Write-Host $allServers.Count
$filtered = $allServers | Where-Object { $_.bypass_proxies_of_type -ne $null } | Sort-Object { $_.cell_id }
Write-Color "Found ", $filtered.Count, " servers with bypass field" -Color White, Yellow, White

foreach ($cdn in $filtered)
{
    # Skip these ones they always time out
    if($cdn.cell_id -eq 8 -or $cdn.cell_id -eq 127)
    {
        continue
    }

    try
    {
        $headers = @{
        "user-agent" = "Valve/Steam HTTP Client 1.0";
        }

        $response = Invoke-WebRequest -Uri "http://$($cdn.host)$uri" -Method HEAD -UseBasicParsing -Headers $headers -TimeoutSec 1
        Write-Host "$($cdn.host) : $($response.StatusCode)"
    }
    catch
    {
        Write-Host "$($cdn.host) :  " -NoNewLine
        Write-Host "$($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    }
}