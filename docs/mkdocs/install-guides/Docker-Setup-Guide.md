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

<div id="player"></div>
<br>

<script src="../../assets/asciinema-player.min.js"></script>
<script>
AsciinemaPlayer.create('../casts/docker-pull.cast', document.getElementById('player'), {
    autoPlay: true,
    terminalFontSize: "14px",
    fit: 'none',
    terminalFontFamily: "'Cascadia Mono', monospace",
    rows: 14
});
</script>


At this point, you will be able to run any of the `COMMANDS` listed in the output by appending the desired command at the end, for example with `select-apps`:

```Bash
docker run -it --rm --net=host  \
  --volume ~/.config/steam-prefill:/Config \
  tpill90/steam-lancache-prefill:latest \
  select-apps
```

## Next Steps

If you are new to **SteamPrefill** and would like an introductory tutorial, see the [Getting Started](https://github.com/tpill90/steam-lancache-prefill#getting-started) guide. 

Answers to common issues and questions can be found at [Frequently Asked Questions](https://github.com/tpill90/steam-lancache-prefill#frequently-asked-questions).  

Alternatively, to see all of the features that **SteamPrefill** offers, take a look at [Detailed command usage](../../Detailed-Command-Usage)