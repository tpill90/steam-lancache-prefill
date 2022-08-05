
# steam-lancache-prefill

[![](https://dcbadge.vercel.app/api/server/BKnBS4u?style=flat-square)](https://discord.com/invite/BKnBS4u)

Automatically fills a [Lancache](https://lancache.net/) with games from Steam, so that subsequent downloads for the same content will be served from the Lancache, improving speeds and reducing load on your internet connection.

Inspired by the [lancache-autofill](https://github.com/zeropingheroes/lancache-autofill) project for Steam games.

---
 
## Features

* Select apps to prefill through an interactive menu.  
* Supports login with Steam Guard, and Steam Guard Mobile Authenticator
* No installation required! A completely self-contained, portable application.
* Multi-platform support (Windows, Linux, MacOS)
* High-performance!  Downloads are significantly faster than using Steam, and can easily reach 10gbit/s or more!
* Game install writes no data to disk, so there is no need to have enough free space available.  This also means no unnecessary wear-and-tear to SSDs!
* Completely implemented from scratch, has no dependency on `SteamCMD`!
* No Steam API key required!  

---

## Initial Setup
1.  Download the latest version for your OS from the [Releases](https://github.com/tpill90/steam-lancache-prefill/releases) page.
2.  Unzip to a directory of your choice
3.  (**Linux / OSX Only**)  Give the downloaded executable permissions to be run with `chmod +x .\SteamPrefill`
4.  (**Windows Only - Optional**)  Configure your terminal to use Unicode, for much nicer looking UI output.
    - <img src="https://raw.githubusercontent.com/tpill90/steam-lancache-prefill/master/docs/ConsoleWithUtf8.png" width="730" alt="Initial Prefill">
    - As the default console in Windows does not support UTF8, Windows Terminal should be installed from the [App Store](https://apps.microsoft.com/store/detail/windows-terminal/9N0DX20HK701), or [Chocolatey](https://community.chocolatey.org/packages/microsoft-windows-terminal).
    - Unicode on Windows is not enabled by default, however running the following will enable it if it hasn't already been enabled.
    - `if(!(Test-Path $profile) -or !(gc $profile).Contains("OutputEncoding")) { ac $profile "[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()";  & $profile; }`

---

## Getting Started

### Selecting what to prefill

Prior to prefilling for the first time, you will have to decide which apps should be prefilled.  This will be done using an interactive menu, for selecting what to prefill from all of your currently owned apps. To display the interactive menu, run the following command
```powershell
.\SteamPrefill.exe select-apps
```

Once logged into Steam, all of your currently owned apps will be displayed for selection.  Navigating using the arrow keys, select any apps that you are interested in prefilling with **space**.  Once you are satisfied with your selections, save them with **enter**.

<img src="https://raw.githubusercontent.com/tpill90/steam-lancache-prefill/master/docs/Interactive-App-Selection.png" height="300" alt="Interactive app selection">

These selections will be saved permanently, and can be freely updated at any time by simply rerunning `select-apps` again at any time.

### Initial prefill

Now that a prefill app list has been created, we can now move onto our initial prefill run by using 
```powershell
.\SteamPrefill.exe prefill
```

The `prefill` command will automatically pickup the prefill app list, and begin downloading each app.  During the initial run, it is likely that the Lancache is empty, so download speeds should be expected to be around your internet line speed (in the below example, a 1gbit connection was used).  Once the prefill has completed, the Lancache should be fully ready to serve clients cached data.

<img src="https://raw.githubusercontent.com/tpill90/steam-lancache-prefill/master/docs/Initial-Prefill.png" width="830" alt="Initial Prefill">

### Updating previously prefilled apps

Updating any previously prefilled apps can be done by simply re-running the `prefill` command, which will use same prefill app list as before.

**SteamPrefill** keeps track of which version of each app was previously prefilled, and will only re-download if there is a newer version of the app available.  Any apps that are currently up to date, will simply be skipped.

<img src="https://raw.githubusercontent.com/tpill90/steam-lancache-prefill/master/docs/Prefill-Up-To-Date.png" width="730" alt="Prefilled app up to date">


However, if there is a newer version of an app that is available, then **SteamPrefill** will re-download the app.  Due to how Lancache works, this subsequent run should complete much faster than the initial prefill (example below used a 10gbit connection).
Any data that was previously downloaded, will be retrieved from the Lancache, while any new data from the update will be retrieved from the internet.

<img src="https://raw.githubusercontent.com/tpill90/steam-lancache-prefill/master/docs/Prefill-New-Version-Available.png" width="830" alt="Prefill run when app has an update">

## Detailed Command Usage

### prefill

| Option |  | Example | |
| ------- | --- | --- | --- |
| --all   |     |     | Downloads all owned apps, useful for prefilling a completely empty cache.  |
| --force | -f  |     | By default, **SteamPrefill** will keep track of the most recently prefilled apps, and will only attempt to prefill if there it determines there a newer version available for download.  This default behavior will work best for most use cases, as no time will be wasted re-downloading files that have been previously prefilled.  <br/><br/> Running with the flag `--force` will override this behavior, and instead will always run the prefill, re-downloading all files for the specified product.  This flag may be useful for diagnostics, or benchmarking network performance.  |
| --dns-override | -d | 192.168.1.111 | Manually specifies the IP address for the Lancache server, in order to run the prefill on the Lancache server itself. <br/>  _**This is only required if running prefill on the Lancache server.**_  <br/>This should not be used in a typical setup where **SteamPrefill** is running on a client machine, e.g. a gaming machine that Steam is normally used on. <br/>| 

# Need Help?
If you are running into any issues, feel free to open up a Github issue on this repository.

You can also find us at the [**LanCache.NET** Discord](https://discord.com/invite/BKnBS4u), in the `#steam-prefill` channel.

# Other Docs
* [Development Configuration](/docs/Development.md)

# External Docs
* https://steamdb.info/
* https://steamstat.us/
* https://steam.readthedocs.io/en/latest/intro.html
* https://steamapi.xpaw.me/
* https://steamapi.xpaw.me/#ISteamDirectory/GetCMList
* https://partner.steamgames.com/doc/api/steam_api?language=english#enums
* https://partner.steamgames.com/doc/api/steam_api?language=english#EAppReleaseState
* https://help.steampowered.com/en/accountdata/SteamLoginHistory