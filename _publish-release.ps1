Push-Location $PSScriptRoot
clear

# Getting current version
[xml]$parsedCsproj = Get-Content .\SteamPrefill\SteamPrefill.csproj
$versionPrefix = $parsedCsproj.Project.PropertyGroup.VersionPrefix
$currentVersion = "$versionPrefix".Trim();
Write-Color "Current version: ", $currentVersion -Color White, Yellow

# Getting new version to use
$newVersion = Read-Host "Enter new version, with no leading 'v'.  Ex. '1.2.3'"
if($newVersion.Contains("v"))
{
    Write-Color $newVersion, " is not a valid version since it has a leading 'v'." -Color Yellow, Red
    return
}

# Updating csproj version
$currentContent = Get-Content -Path .\SteamPrefill\SteamPrefill.csproj -Raw
$currentContent = $currentContent.Replace('<VersionPrefix>' + $currentVersion, '<VersionPrefix>' + $newVersion)
Set-Content -Value $currentContent -Path .\SteamPrefill\SteamPrefill.csproj -NoNewline

# Committing + tag.  Pushing the tag is what creates the release.
git commit -a -m "v$newVersion"
git tag "v$newVersion"
git push origin master --tags

Pop-Location