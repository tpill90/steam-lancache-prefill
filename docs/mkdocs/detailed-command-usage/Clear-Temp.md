# clear-temp

<div data-cli-player="../casts/clear-temp.cast" data-rows=6></div>
<br>

## Overview
Deletes temporary data created by {{prefill_name}}, such as saved manifests, to free up disk space.

These files are saved locally in order to dramatically speed up future `prefill` runs (in some cases 3X faster), at the expense of increase disk usage.  These manifests will also build up over time, as newer versions of games are released, leaving unused manifests behind that will never be used again.

For most users it isn't necessary to use this command, however it may be useful to free up some disk space if you are running low on storage, or to reset any temp data to assist with debugging {{prefill_name}}.



-----

## Options

| Option      |     |      |
| ----------- | --- | ---  |
| --yes       | -y  | When specified, will clear the temp files without prompting. |