# Frequently Asked Questions

## I have to login with my password? How do I know this is safe?

**SteamPrefill**, like Steam, will never save your password. Your password will only be temporarily used once during the initial login, and won't be save to disk anywhere. Upon login **SteamPrefill** will receive an "access token" that will be used on future logins, no password required. Since **SteamPrefill** is open source, you can validate that this is indeed how your password is being used in the [source code](https://github.com/tpill90/steam-lancache-prefill/blob/919ee58ead1458778b121933bbde02cc16d03837/SteamPrefill/Handlers/Steam/Steam3Session.cs#L106).

For extra account security, it is good practice to enable 2 Factor Authentication (2FA) for your account using **Steam Guard Mobile Authenticator**. The authenticator generates a code that you need to enter every time that you log on to your Steam account. The code changes every 30 seconds, can be used only once, and is unguessable. To get setup, see the guide [How to set up a Steam Guard Mobile Authenticator](https://help.steampowered.com/en/faqs/view/6891-E071-C9D9-0134)

---

## Can I run SteamPrefill on the Lancache server?

You certainly can! All you need to do is install **SteamPrefill** onto the server, and run it as you regularly would!

If everything works as expected, you should see a message saying it found the server at `127.0.0.1`

<img src="../images/svg/AutoDns-Server.svg" alt="Prefill running on Lancache Server">

Running from a Docker container on the Lancache server is also supported! You should instead see a message saying the server was found at `172.17.0.1`

<img src="../images/svg/AutoDns-Docker.svg" alt="Prefill running on Lancache Server in Docker">

Running on the Lancache server itself can give you some advantages over running **SteamPrefill** on a client machine, primarily the speed at which you can prefill apps.
Since there is no network transfer happening, the `prefill` should only be limited by disk I/O and CPU throughput.
For example, using a **SK hynix Gold P31 2TB NVME** and running `prefill --force` on previously cached game yields the following performance:
<img src="../images/svg/AutoDns-ServerPerf.svg" alt="Prefill running on Lancache Server in Docker">

---

## Can SteamPrefill be run on a schedule?

Yes it can! Scheduled jobs can be easily setup on Linux using `systemd` services, and can be flexibly configured to run on any schedule that you desire.
See [Configuring a Nightly Job](https://tpill90.github.io/steam-lancache-prefill/install-guides/Scheduled-Job/) for a guide on how to get setup with a schedule.

---

## Can I fill my cache using previously installed Steam games?

Unfortunately it is not possible to fill a Lancache using games that have been installed with Steam. The installed games are in a different format than what Lancache caches, as they are decrypted and unzipped from the raw request. The decryption/unzip process is not reversible. Thus, the only way to get games properly cached is to redownload them using either **SteamPrefill** or **Steam**

---

## Where does SteamPrefill store downloads?

**SteamPrefill** actually doesn't save anything at all!  It will simply download data from the Lancache as quickly as it can, without saving the data to disk.  The Lancache instance will be what is writing game downloads to disk as something is being downloaded through it, whether by using **Steam** or **SteamPrefill**.

---

## How do I pause my running downloads?

You can pause your downloads at any time by simply pressing `CTRL + C`, which will immediately terminate the application. This won't hurt anything at all, and **SteamPrefill** will pickup where it left off during the next `prefill` run.

---

## Is it possible to prefill apps I don't own?

While it would certainly be helpful (and cheaper!) to prefill apps that you don't own, it is unfortunately not possible. In order to download from the Steam network, Steam requires you to authenticate with your username and password. Steam keeps track of which apps you own, which is how **SteamPrefill** displays the list of available apps in `select-apps`. When **SteamPrefill** attempts to download any app (owned or unowned) the Steam network will validate that you do indeed own that app. If you do not own it, then the Steam network will simply refuse to let you download it.

---

## How can I limit download speeds?

You may want to limit the download speed of **SteamPrefill** to prevent it from potentially saturating your entire connection, causing other devices to suffer from massive latency and poor speeds. This issue is known as bufferbloat, and more detailed information on the issue can be found here: [What is bufferbloat?](https://waveform.com/tools/bufferbloat)

**SteamPrefill** does not currently contain any functionality to limit its own download speed, and due to the way that downloads are implemented will likely never be able to throttle its own download speed. Additionally, even if **SteamPrefill** was able to throttle itself, the same issue would persist with downloads through **Steam**.

One method to limit bandwidth would be to configure _Quality of Service (QOS)_ on your router, limiting bandwidth to the Lancache server, or by prioritizing other network traffic. A general overview of QOS can be found here : [Beginners guide to QOS](https://www.howtogeek.com/75660/the-beginners-guide-to-qos-on-your-router/)

For more brand specific guides (non-exhaustive), see :

- [Asus](https://asus.com/support/FAQ/1013333/)
- [Netgear](https://kb.netgear.com/25613/How-do-I-enable-Dynamic-QoS-on-my-Nighthawk-router)
- [Linksys](https://linksys.com/support-article?articleNum=50216)
- [TP-Link](https://tp-link.com/us/support/faq/557/)

---

## My logs have weird characters that make it hard to read. Is there any way to remove them?

Depending on the terminal that you are using, and what colors your system supports, you may see output similar to the following:

```text
[6:20:46 PM] Starting [38;5;80mCounter-Strike: Global Offensive[0m
[6:20:46 PM] Downloading [38;5;170m12.91 GiB[0m
```

One of the reasons you may be seeing this is that your terminal is misreporting what capabilities it supports, thus receiving output that it can't handle. To remove these characters from the log, simply use the flag `--no-ansi` which will remove all unsupported characters from the application's output.

---

## Can I use more than one Steam account at the same time?

Unfortunately SteamPrefill doesn't directly support multiple accounts as it was written to be a single user application. Fortunately there is however a fairly simple workaround. SteamPrefill is designed as a "self-contained" application, meaning that it keeps all of its configuration inside of the folder where it is installed.

In order to use two (or more) accounts at the same time, you should create a separate instance of SteamPrefill for each account. Suppose that you have two accounts that you would like to use, when correctly setup the folder structure should look similar to this:

<img src="../images/svg/Multi-User-File-Structure.svg" alt="Structure required for two Steam accounts">

After the multiple instances have been created, they can both be used as usual by logging in and running the prefill.
