# clear-cache

<div data-cli-player="../casts/clear-cache.cast" data-rows=6></div>
<br>

## Overview
Deletes temporary cached manifests stored in the `/Cache` directory.  
These files are cached in order to dramatically speed up future `prefill` runs (in some cases 3X faster).
For most users it isn't necessary to use this command, however it may be useful to free up some disk space if you are running low on storage.

These cached manifests will also build up over time, as newer versions of games are released, leaving unused manifests behind that will never be used again.

In the case that you would like to save disk space without having to constantly clear the manifest cache, 
running `prefill` with the `--nocache` flag specified will prevent the cached manifests from being written in the first place.

## Options

| Option      |     |      |
| ----------- | --- | ---  |
| --yes       | -y  | Skips the prompt asking to clear the cache, and immediately begins clearing the cache.     |