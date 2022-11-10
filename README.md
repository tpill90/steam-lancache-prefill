# steam-lancache-prefill

[![](https://dcbadge.vercel.app/api/server/BKnBS4u?style=for-the-badge)](https://discord.com/invite/BKnBS4u)
[![view - Documentation](https://img.shields.io/badge/view-Documentation-green?style=for-the-badge)](https://tpill90.github.io/steam-lancache-prefill/)
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Y8Y5DWGZN)


Automatically fills a [Lancache](https://lancache.net/) with games from Steam, so that subsequent downloads for the same content will be served from the Lancache, improving speeds and reducing load on your internet connection.

<img src="docs/img/Overview.png" width="730" alt="Overview">

# Features
* Select apps to prefill through an interactive menu.  
* Supports login with Steam Guard, and Steam Guard Mobile Authenticator
* No installation required! A completely self-contained, portable application.
* Multi-platform support (Windows, Linux, MacOS, Arm64)
* High-performance!  Downloads are significantly faster than using Steam, and can easily reach 10gbit/s or more!
* Game downloads write no data to disk, so there is no need to have enough free space available.  This also means no unnecessary wear-and-tear to SSDs!
* Completely implemented from scratch, has no dependency on `SteamCMD`!
* No Steam API key required!  

# Table of contents
- [Initial Setup](#initial-setup)
- [Getting Started](#getting-started)
- [Frequently Asked Questions](#frequently-asked-questions)
- [Detailed Command Usage](#detailed-command-usage)
- [Updating](#updating)
- [Need Help?](#need-help)

# Initial Setup

> **Note**
> New to Linux?  See this detailed tutorial : [Install Guide For Linux Beginners](https://tpill90.github.io/steam-lancache-prefill/install-guides/Install-Guide-For-Linux-Beginners/)

1.  Download the latest version for your OS from the [Releases](https://github.com/tpill90/steam-lancache-prefill/releases) page.
2.  Unzip to a directory of your choice

> **Warning**
> Linux and macOS will require executable permissions to be granted with `chmod +x ./SteamPrefill` prior to running the app.

> **Note**
> Alpine Linux requires additional dependencies to be installed for the .NET runtime : [Alpine Linux Dependencies](https://docs.microsoft.com/en-us/dotnet/core/install/linux-alpine#dependencies)

## Optional Windows Setup

Configuring your terminal to use Unicode will result in a much nicer experience with **SteamPrefill**, for much nicer looking UI output.

<img src="docs/img/ConsoleWithUtf8.png" width="730" alt="Initial Prefill">

As the default console in Windows does not support UTF8, you should instead consider installing **Windows Terminal** from the [Microsoft App Store](https://apps.microsoft.com/store/detail/windows-terminal/9N0DX20HK701), or [Chocolatey](https://community.chocolatey.org/packages/microsoft-windows-terminal).

Once **Windows Terminal** has been installed you will still need to enable Unicode, as it is not enabled by default. Running the following command in Powershell will enable it if it hasn't already been enabled.
```powershell 
if(!(Test-Path $profile) -or !(gc $profile).Contains("OutputEncoding")) 
{ 
    ac $profile "[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()";  
    & $profile; 
}
```

# Getting Started

## Selecting what to prefill

> **Warning**
> This guide was written with Linux in mind.  If you are running **SteamPrefill** on Windows you will need to substitute `./SteamPrefill` with `.\SteamPrefill.exe` instead.

Prior to prefilling for the first time, you will have to decide which apps should be prefilled.  This will be done using an interactive menu, for selecting what to prefill from all of your currently owned apps. To display the interactive menu, run the following command
```powershell
./SteamPrefill select-apps
```

Once logged into Steam, all of your currently owned apps will be displayed for selection.  Navigating using the arrow keys, select any apps that you are interested in prefilling with **space**.  Once you are satisfied with your selections, save them with **enter**.

<img src="docs/img/Interactive-App-Selection.png" height="350" alt="Interactive app selection">

These selections will be saved permanently, and can be freely updated at any time by simply rerunning `select-apps` again at any time.

## Initial prefill

Now that a prefill app list has been created, we can now move onto our initial prefill run by using 
```powershell
./SteamPrefill prefill
```

The `prefill` command will automatically pickup the prefill app list, and begin downloading each app.  During the initial run, it is likely that the Lancache is empty, so download speeds should be expected to be around your internet line speed (in the below example, a 300mbit/s connection was used).  Once the prefill has completed, the Lancache should be fully ready to serve clients cached data.

<img src="docs/img/Initial-Prefill.png" width="720" alt="Initial Prefill">

## Updating previously prefilled apps

Updating any previously prefilled apps can be done by simply re-running the `prefill` command, which will use same prefill app list as before.

**SteamPrefill** keeps track of which version of each app was previously prefilled, and will only re-download if there is a newer version of the app available.  Any apps that are currently up to date, will simply be skipped.

<img src="docs/img/Prefill-Up-To-Date.png" width="720" alt="Prefilled app up to date">


However, if there is a newer version of an app that is available, then **SteamPrefill** will re-download the app.  Due to how Lancache works, this subsequent run should complete much faster than the initial prefill (example below used a 10gbit connection).
Any data that was previously downloaded, will be retrieved from the Lancache, while any new data from the update will be retrieved from the internet.

<img src="docs/img/Prefill-New-Version-Available.png" width="720" alt="Prefill run when app has an update">

# Frequently Asked Questions

## Can I run SteamPrefill on the Lancache server?

You certainly can!  All you need to do is download **SteamPrefill** onto the server, and run it as you reguarly would!

If everything works as expected, you should see a message saying it found the server at `127.0.0.1`

<img src="docs/img/AutoDns-Server.png" width="580" alt="Prefill running on Lancache Server">

Running from a Docker container on the Lancache server is also supported!  You should instead see a message saying the server was found at `172.17.0.1`

<img src="docs/img/AutoDns-Docker.png" width="580" alt="Prefill running on Lancache Server in Docker">

Running on the Lancache server itself can give you some advantages over running **SteamPrefill** on a client machine, primarily the speed at which you can prefill apps.  
Since there is no network transfer happening, the `prefill` should only be limited by disk I/O and CPU throughput.  
For example, using a **SK hynix Gold P31 2TB NVME** and running `prefill --force` on previously cached game yields the following performance 
<img src="docs/img/AutoDns-ServerPerf.png" width="830" alt="Prefill running on Lancache Server in Docker">

## Can SteamPrefill be ran on a schedule?

Yes it can!  Scheduled jobs can be easily setup on Linux using `crontab`, and can be flexibly configured to run on any schedule that you desire.  Jobs are configured by specifying an "expression" that describes the schedule to run on. 
Some examples of cron expressions:

<table>
<tr> <td> Schedule </td> <td> Cron Expression </td> </tr>
<tr> <td> Every day at 2am </td> <td> 

`0 2 * * * $PWD/SteamPrefill prefill`
</td> </tr>
<tr> <td> Every 4 hours </td> <td> 

`"0 */4 * * * $PWD/SteamPrefill prefill`
</td> </tr>
</table>

If the above examples don't cover your use case, [crontab.guru](https://crontab.guru/) is an online cron expression editor that can interactively edit cron expressions, and explain what they mean.

Once you have determined a cron expression, you can then create a job using the following:

> **Note**
> This command should be run in the same directory where **SteamPrefill** is installed

`job="cron expression here"; { crontab -l; echo "$job"; } | crontab -` 

After running the command, you can verify that the job was successfully created with `crontab -l`.  If the output matches below, then your job is correctly configured!

<img src="docs/img/crontab.png" width="550" alt="Crontab jobs">

## Can I fill my cache using previously installed Steam games?

Unfortunately it is not possible to fill a Lancache using games that have been installed with Steam.  The installed games are in a different format than what Lancache caches, as they are decrypted and unzipped from the raw request.  The decryption/unzip process is not reversible.  Thus, the only way to get games properly cached is to redownload them using either **SteamPrefill** or **Steam**

## How can I limit download speeds?

You may want to limit the download speed of **SteamPrefill** to prevent it from potentially saturating your entire connection,  causing other devices to suffer from massive latency and poor speeds.  This issue is known as bufferbloat, and more detailed information on the issue can be found here: [What is bufferbloat?](https://www.waveform.com/tools/bufferbloat)

**SteamPrefill** does not currently contain any functionality to limit its own download speed, and due to the way that downloads are implemented will likely never be able to throttle its own download speed.  Additionally, even if **SteamPrefill** was able to throttle itself, the same issue would persist with downloads through **Steam**.

One method to limit bandwidth would be to configure *Quality of Service (QOS)* on your router, limiting bandwidth to the Lancache server, or by prioritizing other network traffic.  A general overview of QOS can be found here : [Beginners guide to QOS](https://www.howtogeek.com/75660/the-beginners-guide-to-qos-on-your-router/)

For more brand specific guides (non-exhaustive), see :
- [Asus](https://www.asus.com/support/FAQ/1013333/)
- [Netgear](https://kb.netgear.com/25613/How-do-I-enable-Dynamic-QoS-on-my-Nighthawk-router)
- [Linksys](https://www.linksys.com/support-article?articleNum=137079)
- [TP-Link](https://www.tp-link.com/us/support/faq/557/)


# Detailed Command Usage

> **Note**
> Detailed command documentation has been moved to the wiki : [Detailed Command Usage](https://tpill90.github.io/steam-lancache-prefill/Detailed-Command-Usage/)

# Updating
**SteamPrefill** will automatically check for updates, and notify you when an update is available :

<img src="docs/img/UpdateAvailable.png" width="675" alt="Update available message">

### Automatically updating
- **Windows**
    - Run the `.\update.ps1` script in the executable directory
- **Linux**
    - **First time only** : Grant executable permissions to the update script with `chmod +x ./update.sh`
    - Run the `./update.sh` script in the executable directory

### Manually updating:
1.  Download the latest version for your OS from the [Releases](https://github.com/tpill90/steam-lancache-prefill/releases) page.
2.  Unzip to the directory where **SteamPrefill** is currently installed, overwriting the previous executable.
3.  Thats it!  You're all up to date!

# Need Help?
If you are running into any issues, feel free to open up a Github issue on this repository.

You can also find us at the [**LanCache.NET** Discord](https://discord.com/invite/BKnBS4u), in the `#steam-prefill` channel.

# Additional Documentation
* [Development Configuration](https://tpill90.github.io/steam-lancache-prefill/dev-guides/Compiling-from-source/)

# Acknowledgements
- [@dlrudie](https://github.com/dlrudie) for all your help with debugging and testing!

# External Links
|   Link    |  Description   |  
| ----------- | ------------ | 
| https://steamdb.info/ | Third Party database of Steam apps.  Has extensive metadata, as well as historical data.    |
| https://steamdb.info/faq/ | Useful info about SteamDB, as well as some technical info on how Steam works internally |
| https://steamapi.xpaw.me/ | List of all known public Steam Web Api endpoints    |
| https://steamdb.info/freepackages/ | A tool to automatically add free games/DLC to your account |

* https://store.steampowered.com/api/appdetails?appids=10
* https://wiki.teamfortress.com/wiki/User:RJackson/StorefrontAPI#Known_methods
* https://steamspy.com/api.php
* https://steamstat.us/
* https://steam.readthedocs.io/en/latest/intro.html
* https://partner.steamgames.com/doc/api/steam_api?language=english#enums
* https://partner.steamgames.com/doc/api/steam_api?language=english#EAppReleaseState
