Set-Location $PSScriptRoot
$ErrorActionPreference = "Stop"

Remove-Item publish -Recurse -Force -ErrorAction SilentlyContinue

$csprojXml = [xml](gc SteamPrefill\SteamPrefill.csproj)
$version = "$($csprojXml.ChildNodes.PropertyGroup.Version)".Trim()


# Windows publish
foreach($runtime in @("win-x64", "linux-x64", "osx-x64"))
{
    $publishDir = "publish/SteamPrefill-$version-$runtime"

    $readyToRun = $false
    if($runtime -eq "win-x64")
    {
        $readyToRun = $true
    }

    Write-Host "Publishing $runtime" -ForegroundColor Cyan
    dotnet publish .\SteamPrefill\SteamPrefill.csproj `
    -o $publishDir `
    -c Release `
    --runtime $runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishReadyToRun=$readyToRun `
    /p:PublishTrimmed=true

    Compress-Archive -path $publishDir "$publishDir.zip"

    $folderSize = "{0:N2} MB" -f((Get-ChildItem $publishDir | Measure-Object -Property Length -sum).sum / 1Mb)
    Write-Host "Published file size : " -NoNewline
    Write-Host -ForegroundColor Cyan $folderSize

    $zipSize = "{0:N2} MB" -f((Get-ChildItem "$publishDir.zip" | Measure-Object -Property Length -sum).sum / 1Mb)
    Write-Host "Published zip size : " -NoNewline
    Write-Host -ForegroundColor Cyan $zipSize
}
