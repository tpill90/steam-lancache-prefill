#Requires -RunAsAdministrator

$targetInterfaceDesc = "Mellanox"
$lancacheIp = "192.168.1.222"


$ErrorActionPreference = "Stop"
Clear-Host


$ethernetInterface = @(Get-NetAdapter) | Where-Object { $_.InterfaceDescription -like "*$targetInterfaceDesc*" }

if(((Resolve-DnsName "lancache.steamcontent.com").IP4Address -eq "$lancacheIp"))
{
    Write-Host "Setting DNS resolver to : " -NoNewline
    Write-Host "8.8.8.8"  -ForegroundColor Yellow
    $ethernetInterface | Set-DnsClientServerAddress -ServerAddresses ("8.8.8.8")
}
else
{
    Write-Host "Resetting DNS resolution to default" -ForegroundColor Yellow
    $ethernetInterface | Set-DnsClientServerAddress -ResetServerAddresses
}


Write-Host 
Write-Host

Resolve-DnsName "lancache.steamcontent.com" | Format-Table -Property Name,IP4Address


Clear-DnsClientCache
ipconfig /flushdns
