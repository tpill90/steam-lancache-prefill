# TODO comment
$ErrorActionPreference = "Stop"
# TODO Set-StrictMode -Version 2
Import-Module PSWriteColor

$uri = "/depot/3527291/chunk/5e385330290f274474a065226bf6ccf0042a8e2d"

$allServers = @()
$jsonFiles = Get-ChildItem -Path .\servers -Filter *.json
foreach($file in $jsonFiles)
{
    $fileContent = Get-Content -Path $file.FullName -Raw
    $serversInFile = ConvertFrom-Json -InputObject $fileContent
    $allServers += $serversInFile.response.servers
}

$filtered = $allServers | Where-Object { $_.bypass_proxies_of_type -ne $null } | Sort-Object { $_.cell_id }
Write-Color "Found ", $filtered.Count, " servers of ", $allServers.Count, " which have the 'bypass_proxies_of_type' field" -Color White, Yellow, White, Magenta, White

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
        #Write-Host "$($cdn.host.PadRight(35)) : $($response.StatusCode)"
    }
    catch
    {
        Write-Host "$($cdn.host.PadRight(35)) : " -NoNewLine
        Write-Host "$($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    }
}