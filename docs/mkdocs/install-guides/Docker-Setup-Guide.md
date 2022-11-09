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
    These steps assume that you are running Lancache caching server as a docker container on a custom network configured for your own subnet. Although could be adapted to suit your own network setup.
    
![create](../images/install-guides/lancachenet-typical-setup.png){: style="width:430px"}

### Installing SteamPrefill Docker Image

Again from your UNRAID terminal you will download and run the **SteamPrefill** Docker image.


!!! note
    ```--net=br0```
    (this is to connect the container to your custom bridge network)

    ```--add-host=lancache.steamcontent.com:192.168.2.140```
    (this is to tell the container the IP Address of your Lancache Server running on your custom network)
    
Next you will run the full command with your network parameters.    

```bash
docker run -it --rm \
  --net=br0 --add-host=lancache.steamcontent.com:192.168.2.140  \
  --volume ~/.config/SteamPrefill:/Config \
  tpill90/steam-lancache-prefill:latest 
```


### Next Steps

To get familiarized with how to use **SteamPrefill**, see the [Getting Started](https://github.com/tpill90/steam-lancache-prefill#getting-started) guide.  Answers to common issues and questions can be found at [Frequently Asked Questions](https://github.com/tpill90/steam-lancache-prefill#frequently-asked-questions).  For more detailed documentation on commands and option flags, see [Detailed Command Usage](https://tpill90.github.io/steam-lancache-prefill/Detailed-Command-Usage/).