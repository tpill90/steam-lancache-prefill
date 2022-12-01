# Docker Setup Guide

This guide is intended for getting started with the **SteamPrefill** Docker image [(DockerHub)](https://hub.docker.com/r/tpill90/steam-lancache-prefill/tags), and to become familiarized with how to interact with it.  This guide does not intend to be a detailed guide on how to use **SteamPrefill** itself, which can be found in the [Getting Started](https://github.com/tpill90/steam-lancache-prefill#getting-started) guide.

!!! Note
    Docker is not strictly required to run **SteamPrefill**,  as it is a completely self-contained portable app.

## Basic Usage Via Command Line

To download and run the latest version of the container, open up a terminal and run the following command:

```bash
docker run -it --rm --net=host \
  --volume ~/.config/SteamPrefill:/Config \
  tpill90/steam-lancache-prefill:latest 
```

This command is the same as running **SteamPrefill** from the command line with no options, and if successful should produce the following output:

![docker no command](../images/install-guides/docker-no-command.png)

At this point, you will be able to run any of the `COMMANDS` listed in the output by appending the desired command at the end, for example with `select-apps`:

```Bash
docker run -it --rm --net=host  \
  --volume ~/.config/steam-prefill:/Config \
  tpill90/steam-lancache-prefill:latest \
  select-apps
```

### Next Steps

To get familiarized with how to use **SteamPrefill**, see the [Getting Started](https://github.com/tpill90/steam-lancache-prefill#getting-started) guide.  Answers to common issues and questions can be found at [Frequently Asked Questions](https://github.com/tpill90/steam-lancache-prefill#frequently-asked-questions).  For more detailed documentation on commands and option flags, see [Detailed Command Usage](https://tpill90.github.io/steam-lancache-prefill/Detailed-Command-Usage/).


## Using Unraid

Prior to installing SteamPrefill via docker you should ensure you have a working Lancache caching and Lancache DNS server installed on your system.

!!! note
    These steps assume that you are running Lancache caching server as a docker container on a custom network configured for your own subnet.
    
![Unraid Lancache Setup](../images/install-guides/unraid-docker-typical-setup.png){: style="width:530px"}

### Installing SteamPrefill Docker Image

Again from your Unraid terminal you will download and run the **SteamPrefill** Docker image.

!!! note
    ```--add-host=lancache.steamcontent.com:XXX.XXX.XXX.XXX``` is required in order to tell the container the IP Address of your Lancache server running on your custom network.  If not specified, **SteamPrefill** will be unable to locate the Lancache server, and will be unable to prefill.
    
Next you will run the following command to setup the container, and start configuring which apps to prefill.

```bash
docker run -it --rm \
  --net=br0 \
  --add-host=lancache.steamcontent.com:XXX.XXX.XXX.XXX  \
  --volume ~/.config/SteamPrefill:/Config \
  tpill90/steam-lancache-prefill:latest \
  select-apps
```

To get familiarized with how to use **SteamPrefill**, see the [Getting Started](https://github.com/tpill90/steam-lancache-prefill#getting-started) guide.

### Setting up a schedule

The [User Scripts Community App](https://forums.unraid.net/topic/48286-plugin-ca-user-scripts/) can be used to create and configure custom scheduled jobs on Unraid.  To begin, ensure that the app is installed from Unraid's *App* tab, if it isn't already installed.

![Unraid Community Apps](../images/install-guides/unraid-userscript-community-app.png){: style="height:300px"}

After installing **User Scripts**, click Unraid's *Plugins* tab, then the **User Scripts** icon to open up the settings for scheduled jobs.
![User Scripts Settings button](../images/install-guides/unraid-userscripts-button.png){: style="width:830px"}

Next, add a new script by clicking *ADD NEW SCRIPT*, and give it the name `Steam Prefill`.  After it has been created, click the orange gear next to `Steam Prefill`, and select *EDIT SCRIPT*.
Adding the following command will configure the scheduled job to run the `prefill` command every time it is configured.  Be sure sure to change `XXX.XXX.XXX.XXX` to your Lancache server IP.

!!! note
    This command is almost identical to the one we used previously, except for `--it` being omitted.  This will make the container run non-interactively, as required by **User Scripts**

```bash
#!/bin/bash
docker rm -f SteamPrefill &>/dev/null && echo 'Removed old container from previous run';
docker run --rm --name SteamPrefill \
  --net=br0 \
  --add-host=lancache.steamcontent.com:XXX.XXX.XXX.XXX  \
  --volume ~/.config/SteamPrefill:/Config \
  tpill90/steam-lancache-prefill:latest \
  prefill
```

After saving changes, the final step will be to decide on a schedule, and configure that schedule.  Change the schedule drop down from *Schedule Disabled*, to *Custom*, which will allow you to specify your desired schedule.  Jobs are configured by specifying a *Cron expression* that describes the schedule to run on.

Some examples of cron expressions:

| Schedule | Cron Expression |
| --- | --- |
| Every day at 2am | `0 2 * * *` |
| Every 4 hours | `0 */4 * * *` |

If the above examples don't cover your use case, [crontab.guru](https://crontab.guru/) is an online cron expression editor that can interactively edit cron expressions, and explain what they mean.

Finally after entering a cron expression, click *APPLY* to save your cron expression.  You should now be all set to have **SteamPrefill** run on a schedule!

### Next Steps

 Answers to common issues and questions can be found at [Frequently Asked Questions](https://github.com/tpill90/steam-lancache-prefill#frequently-asked-questions).  
 For more detailed documentation on commands and option flags, see [Detailed Command Usage](https://tpill90.github.io/steam-lancache-prefill/Detailed-Command-Usage/).