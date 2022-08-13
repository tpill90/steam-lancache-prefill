$scriptBlock = {
    function PublishRuntime([string] $runtime, [string] $version)
    {
        # All double quotes need to be escaped (""") in this block in order to work correctly with Start-Process
        $publishDir = """publish/SteamPrefill-$version-$runtime"""
        
        Write-Host "Publishing $runtime" -ForegroundColor Cyan
        dotnet publish .\SteamPrefill\SteamPrefill.csproj `
        --no-restore `
        --no-build `
        -o $publishDir `
        -c Release `
        --runtime $runtime `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:PublishReadyToRun=true `
        /p:PublishTrimmed=true `
        --nologo

        if($LASTEXITCODE -ne 0)
        {
            Read-Host
        }

        Compress-Archive -path $publishDir """$publishDir.zip"""
    }
}

Clear-Host
Set-Location $PSScriptRoot
$ErrorActionPreference = "Stop"
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

$csprojXml = [xml](gc SteamPrefill\SteamPrefill.csproj)

# The current version of the app
$version = "$($csprojXml.ChildNodes.PropertyGroup.Version)".Trim()
# The list of runtimes that will be targeted by the publish
$targetRuntimes = $csprojXml.ChildNodes.PropertyGroup.RuntimeIdentifiers[0].Split(';')

# Empty out the \obj folder, will guarantee a new build will be published each time
Remove-Item .\SteamPrefill\obj -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem publish -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Running dotnet build for each runtime, otherwise the publish step will fail when ran in parallel
Write-Host "Starting dotnet build.." -ForegroundColor Yellow
foreach($runtime in $targetRuntimes)
{
    Write-Host ""
    Write-Host "Building " -NoNewline; Write-Host $runtime -ForegroundColor Cyan
    dotnet build .\SteamPrefill\SteamPrefill.csproj `
        -c Release `
        --runtime $runtime `
        --self-contained true `
        /p:PublishSingleFile=true `
        -v quiet `
        --nologo

    # Making sure that the compliation is successful before mmoving on to the publishing step
    if($LASTEXITCODE -ne 0)
    {
        Write-Host "\nBuild failed.  Skipping publish step until errors are resolved.." -ForegroundColor Red
        return
    }
}

$processes = @()
foreach($runtime in $targetRuntimes)
{
    $processes += Start-Process powershell.exe `
                            -ArgumentList "-command", "& {$scriptBlock PublishRuntime '$runtime' '$version'}" `
                            -PassThru
}

Write-Host "Waiting on publish to complete" -ForegroundColor Yellow
$processes | Wait-Process


$stopwatch.Stop()
Write-Host "Build took: " -NoNewline
Write-Host $stopwatch.Elapsed.ToString() -ForegroundColor Yellow