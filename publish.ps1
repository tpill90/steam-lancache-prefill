Set-Location $PSScriptRoot
$ErrorActionPreference = "Stop"

Remove-Item publish -Recurse -Force -ErrorAction SilentlyContinue

# Windows publish
foreach($runtime in @("win-x64"))
{
    Write-Host "Publishing $runtime" -ForegroundColor Cyan
    dotnet publish .\BattleNetPrefill\BattleNetPrefill.csproj `
    -o publish/BattleNetPrefill-$runtime `
    -c Release `
    --runtime $runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishReadyToRun=true `
    /p:PublishTrimmed=true

    Compress-Archive -path publish/BattleNetPrefill-$runtime publish/$runtime.zip

    $folderSize = "{0:N2} MB" -f((Get-ChildItem publish/BattleNetPrefill-$runtime | Measure-Object -Property Length -sum).sum / 1Mb)
    Write-Host "Published file size : " -NoNewline
    Write-Host -ForegroundColor Cyan $folderSize

    $zipSize = "{0:N2} MB" -f((Get-ChildItem publish/$runtime.zip | Measure-Object -Property Length -sum).sum / 1Mb)
    Write-Host "Published zip size : " -NoNewline
    Write-Host -ForegroundColor Cyan $zipSize
}

# Doing linux and osx separatly, they don't support ReadyToRun
foreach($runtime in @("linux-x64", "osx-x64"))
{
    Write-Host "\n\nPublishing $runtime" -ForegroundColor Cyan
    dotnet publish .\BattleNetPrefill\BattleNetPrefill.csproj `
    -o publish/BattleNetPrefill-$runtime `
    -c Release `
    --runtime $runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishTrimmed=true

    $folderSize = "{0:N2} MB" -f((Get-ChildItem publish/BattleNetPrefill-$runtime | Measure-Object -Property Length -sum).sum / 1Mb)
    Write-Host "Published file size : " -NoNewline
    Write-Host -ForegroundColor Cyan $folderSize

    Compress-Archive -path publish/BattleNetPrefill-$runtime publish/$runtime.zip
}