# Docker Setup Guide

This guide is intended for getting started with the **{{prefill_name}}** Docker image [(DockerHub)](https://hub.docker.com/r/tpill90/{{repo_name}}/tags), and to become familiarized with how to interact with it.  This guide does not intend to be a detailed guide on how to use **{{prefill_name}}** itself, which can be found in the [Getting Started](https://github.com/tpill90/{{repo_name}}#getting-started) guide.

!!! Note
    Docker is not strictly required to run **{{prefill_name}}**,  as it is a completely self-contained portable app.

-----

## Basic Usage Via Command Line

To download and run the latest version of the container, open up a terminal and run the following command:

```bash
docker run -it --rm --net=host \
  --volume ~/.config/{{prefill_name}}:/Config \
  tpill90/{{repo_name}}:latest 
```

This command is the same as running **{{prefill_name}}** from the command line with no options, and if successful should produce the following output:

<div data-cli-player="../casts/docker-pull.cast"></div>
<br>

At this point, you will be able to run any of the `COMMANDS` listed in the output by appending the desired command at the end, for example with `select-apps`:

```Bash
docker run -it --rm --net=host  \
  --volume ~/.config/{{prefill_name}}:/Config \
  tpill90/{{repo_name}}:latest \
  select-apps
```

-----

## Next Steps

If you are new to **{{prefill_name}}** and would like an introductory tutorial, see the [Getting Started](https://github.com/tpill90/{{repo_name}}#getting-started) guide. 

Answers to common issues and questions can be found at [Frequently Asked Questions](https://github.com/tpill90/{{repo_name}}#frequently-asked-questions).  
