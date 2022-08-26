$scriptBlock = {
    function PublishRuntime([string] $runtime, [string] $version, [string] $targetBranch, [string] $originalRepoPath)
    {
        # All double quotes need to be escaped (""") in this block in order to work correctly with Start-Process
        $repoName = """SteamPrefill-$runtime"""

        Write-Host "Starting publish for  " -NoNewline; Write-Host $runtime -ForegroundColor Cyan

        # Cloning
        Set-Location $env:TEMP
        if(-Not (Test-Path $repoName))
        {
            Write-Host "Cloning " -NoNewline; Write-Host $repoName -ForegroundColor Cyan
            git clone https://github.com/tpill90/steam-lancache-prefill.git $repoName
        }

        # Making sure we're on the right branch
        Set-Location $repoName
        $currentBranch = (git rev-parse --abbrev-ref HEAD)
        if(!($currentBranch -eq $targetBranch))
        {
            git checkout $targetBranch
            Write-Host "Switched to $targetBranch branch"
        }

        #Updating branch
        Write-Host "Pulling branch" -ForegroundColor Green
        git pull

        # Git clean
        Write-Host "Cleaning untracked files from previous build" -ForegroundColor Green
        git clean -fx -d

        $publishDir = """$originalRepoPath\publish\SteamPrefill-$version-$runtime"""
        Write-Host """Publishing $runtime""" -ForegroundColor Cyan
        dotnet publish .\SteamPrefill\SteamPrefill.csproj `
            --nologo `
            -o $publishDir `
            -c Release `
            --runtime $runtime `
            --self-contained true `
            /p:PublishSingleFile=true `
            /p:PublishReadyToRun=true `
            /p:PublishTrimmed=true

        if($LASTEXITCODE -ne 0)
        {
            Read-Host
        }

        Compress-Archive -path $publishDir """$publishDir.zip"""
    }
}

Clear-Host

#region Powershell config

$ErrorActionPreference = "Stop"
# Checking to see if dependencies are installed
if(-Not(Get-Module -Name PSWriteColor))
{
    Install-Module PSWriteColor -Scope CurrentUser
}

#endregion

Set-Location $PSScriptRoot
$totalTimer = [System.Diagnostics.Stopwatch]::StartNew()

# Clearing out previous build
Get-ChildItem publish -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

$csprojXml = [xml](gc SteamPrefill\SteamPrefill.csproj)

# The current version of the app
$version = "$($csprojXml.ChildNodes.PropertyGroup.Version)".Trim()
# The list of runtimes that will be targeted by the publish
$targetRuntimes = $csprojXml.ChildNodes.PropertyGroup.RuntimeIdentifiers[0].Split(';')
$currentBranch = (git rev-parse --abbrev-ref HEAD)
$repoPath = "$((Get-Location).Path)"

Write-Host "Starting dotnet publish..." -ForegroundColor Yellow
$processes = @()
foreach($runtime in $targetRuntimes)
{
    $processes += Start-Process powershell.exe `
                            -ArgumentList "-command", "& {$scriptBlock PublishRuntime '$runtime' '$version' '$currentBranch' '$repoPath'}" `
                            -PassThru
}

Write-Host "Waiting on publish to complete" -ForegroundColor Yellow
$processes | Wait-Process

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