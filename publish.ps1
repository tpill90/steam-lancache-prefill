Clear-Host

#region Powershell config

$ErrorActionPreference = "Stop"
# Checking to see if dependencies are installed
if(-Not(Get-Module -Name PSWriteColor))
{
    Install-Module PSWriteColor -Scope CurrentUser
}

#endregion

Push-Location $PSScriptRoot
$totalTimer = [System.Diagnostics.Stopwatch]::StartNew()

$csprojXml = [xml](gc SteamPrefill\SteamPrefill.csproj)

# The current version of the app
$version = "$($csprojXml.ChildNodes.PropertyGroup.Version)".Trim()
# The list of runtimes that will be targeted by the publish
$targetRuntimes = $csprojXml.ChildNodes.PropertyGroup.RuntimeIdentifiers[0].Split(';')

# Empty out the \obj folder, will guarantee a new build will be published each time
Remove-Item .\SteamPrefill\obj -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem publish -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Running dotnet build for each runtime, otherwise the publish step will fail when ran in parallel
Write-Host "Starting dotnet publish..." -ForegroundColor Yellow
foreach($runtime in $targetRuntimes)
{
    Write-Host ""
    Write-Host "Building " -NoNewline; Write-Host $runtime -ForegroundColor Cyan

    $publishDir = "publish/SteamPrefill-$version-$runtime"

    dotnet publish .\SteamPrefill\SteamPrefill.csproj `
        --nologo `
        -o $publishDir `
        -c Release `
        --runtime $runtime `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:PublishReadyToRun=true `
        /p:PublishTrimmed=true
        
    # Making sure that the compliation is successful before mmoving on to the next runtime
    if($LASTEXITCODE -ne 0)
    {
        Write-Host "\nBuild failed.  Skipping publish step until errors are resolved.." -ForegroundColor Red
        return
    }

    Compress-Archive -path $publishDir "$publishDir.zip"
}

# Writing out some metrics about the published files, for fun
foreach($runtime in $targetRuntimes)
{
    $publishName = "SteamPrefill-$version-$runtime"
    $folderSize = "{0:N2} MB" -f((Get-ChildItem publish/$publishName | Measure-Object -Property Length -sum).sum / 1Mb)
    $zipSize = "{0:N2} MB" -f((Get-ChildItem publish/$publishName.zip | Measure-Object -Property Length -sum).sum / 1Mb)

    Write-Color $runtime.PadRight(11), " - Publish: ", $folderSize, " Zip: ", $zipSize -Color Yellow, White, Cyan, White, Cyan
}

$totalTimer.Stop()
Write-Host "Publish took total time: " -NoNewline
Write-Host $totalTimer.Elapsed.ToString() -ForegroundColor Yellow

Pop-Location