# Docker Setup

TODO Write intro
TODO What do '--user=$UID' and  '--net=host' change?
TODO what do 'VOLUME /Config' and 'VOLUME /Cache' mean
TODO Breakdown of each flag

- [Docker hub images](https://hub.docker.com/r/tpill90/steam-lancache-prefill/tags)

## Usage

```bash
docker run -it --rm \
  -v  ~/.config/steamprefill:/Config \
  tpill90/steam-lancache-prefill:latest 
```


```bash
docker run -it --rm -v ${PWD}/Config:/Config tpill90/steam-lancache-prefill:issues-94-DockerDocumentation
```

For instructions on how to use SteamPrefill please read the [README on the GitHub project](https://github.com/tpill90/steam-lancache-prefill).

## Running without Docker-Compose

This will use the latest image from Docker hub.

## Unraid

[Unraid Guide](../Unraid-Docker-Install-Guide)

## Portainer

### Setup
1. Go to "Stacks" page in the menu
2. Click "+ Add stack"
3. Give it a name (e.g. "steam-prefill") and under "Build method" select "Repository" and paste `https://gitlab.com/kirbo/steam-prefill-docker` into "Repository URL"
4. Once you've done your own modifications, at the bottom of the page click "Deploy the stack"
5. Navigate to "Containers" page
6. Click on `steam-lancache-prefill-<name-you-gave-in-step-3>-1` (e.g. `steam-lancache-prefill-steam-prefill-1`)
7. Click "Duplicate/Edit"
8. Scroll at the bottom of the page, next to "Command" click the "Override" button and next to that button fill in `select-apps`
9. Underneath select the Console `Interactive & TTY`
10. Click the "Deploy the container" which is slightly above the Command you just modified
11. You should be automatically redirected to "Containers" page, so open the same container you did in step 6
12. Click the "Console"
13. Click the "Connect"
14. Type in `./SteamPrefill select-apps`
15. Log in into Steam using your Steam account
16. Select the games you want to prefill, using arrow up/down, Space to select the game and once you've selected all you want to prefill, press Enter (program will ask whether you want to prefill now or not, your choise)
17. Click "Disconnect"
18. Navigate back to the same page you did on step 6
19. Click "Duplicate/Edit"
20. Scroll at the bottom of the page, change the "Command" to be `prefill`
21. Underneath select the Console `None`
22. Click the "Deploy the container"
23. Done

### Schedule the prefill

To make the prefill scheduled:
1. You need to enable and setup Edge Computing (Settings -> Edge Computing -> "Enable Edge Compute features").
2. You will need to set up the Edge Agent.
3. Once those are done, go to Edge Jobs and click "Add Edge job"
4. Give it a name, e.g. "scheduled-steam-prefill" 
5. Select when do you want the prefill to be run, for me I chose "Advanced configuration" and fill in the input `0 0,2,4,6,8,10,12,14,16,18,20,22 * * *` so the Steam prefill will be run at every even hour
6. Under the "Web editor" add the same name you had in the bullets above on step 6, such as:
   ```
   docker start steam-lancache-prefill-steam-prefill-1
   ```
7. Select the Target environment
8. Click "Create edge job"
9. Done
