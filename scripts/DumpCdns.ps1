# TODO comment
$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2

$allResults = @()
$maxCellId = 10

for ($i = 1; $i -le $maxCellId; $i++)
{
    Write-Host "Querying cell ID $i of $maxCellId"
    $response = Invoke-RestMethod -Uri "https://api.steampowered.com/IContentServerDirectoryService/GetServersForSteamPipe/v1/?cell_id=$i"

    $allResults += $response.response.servers
}

$allResults | ConvertTo-Json -Depth 5 | Out-File -FilePath "server_results.json"

$uniqueServers = $allResults | Sort-Object cell_id, host -Unique
$uniqueServers | ConvertTo-Json -Depth 5 | Out-File -FilePath "deduped_servers.json"