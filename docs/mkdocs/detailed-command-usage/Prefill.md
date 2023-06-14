# prefill

<div data-cli-player="../casts/prefill.cast" data-rows=13></div>
<br>

## Overview

Automatically fills a Lancache with games from {{ gaming_service_name }} so that subsequent downloads will be served from the Lancache, improving speeds and reducing load on your internet connection.

Keeps track of which games have been previously downloaded, and will only download games that have updates.  

-----

## Example usage

!!! Note
    This command will automatically include any apps that have been selected using `select-apps`, regardless of any additional optional flags specified.

Initiating a `prefill` run is as simple as running the following from the terminal:
```powershell
./{{prefillName}} prefill
```

At the beginning of a `prefill` run, **{{prefillName}}** will check to see which apps have new updates since the last `prefill` run, as well as checking to see if any apps have never been successfully prefilled.  If `prefill` detects that there are any apps that need to be downloaded, it will begin doing so.  If there are no apps that need to be downloaded, then the `prefill` run will simply finish immediately.  


### Prefilling your entire Steam library

Depending on the size of your library, and which apps you want to prefill, it may be easier to simply prefill the entire library instead.  This will also automatically include any new games you may have purchased, without having to use `select-apps` to select the newly purchased game.

```powershell
./{{prefillName}} prefill --all
```

### Ensuring your cache is fully primed

Suppose that you have an event coming up, and you want to be 100% certain that your Lancache is prefilled.  Normally running `prefill` will ensure that you have the latest update data primed, however you may want to have complete certainty that it is.  Adding the `--force` flag will make **{{prefillName}}** re-download every app, ignoring the fact that they may have already been up to date from a previous run.  Because **{{prefillName}}** will be re-downloading every app again from start to finish, any data that may be missing will be filled in again.

```powershell
./{{prefillName}} prefill --force
```

### Combining multiple flags

It is possible to combine multiple flags together in a single command, rather than having to use them separately one at a time.  For example, the following command will prefill the most popular games on Steam, only download the Linux version, and display more detailed log output:

```powershell
./{{prefillName}} prefill --top --os linux --verbose
```

-----

## Options

| Option      |     | Values                | Default     |     |
| ----------- | --- | --------------------- | ----------- | --- |
| --os        |     | windows, linux, macos | **windows** | Specifies which operating system(s) games should be downloaded for.  Typically, almost all games support Windows, however there are increasingly more games that have Linux specific game files.  In some cases, the Linux game files may be as large as the Windows version. |
| --all       |     |                       |             | Downloads all owned apps, useful for prefilling a completely empty cache.  |
| --recent    |     |                       |             | Adds any games played within the last 2 weeks to the download queue.  |
| --top       |     | 1-100                 | **50**      | Downloads the most popular games by player count, over the last 2 weeks.  |
| --force     | -f  |                       |             | By default, **{{prefillName}}** will keep track of the most recently prefilled apps, and will only attempt to prefill if there it determines there a newer version available for download.  This default behavior will work best for most use cases, as no time will be wasted re-downloading files that have been previously prefilled.  <br/><br/> Running with the flag `--force` will override this behavior, and instead will always run the prefill, re-downloading all files for the selected apps.  This flag may be useful for diagnostics, or benchmarking network performance.  |
| --nocache   |     |                       |             | Skips using locally cached manifests.  Normally **{{prefillName}}** will cache copies of manifests on disk, in order to dramatically speed up future runs. However, in some scenarios this disk cache can potentially take up a non-trivial amount of storage (300mb), which may not be ideal for all use cases.  |
| --verbose   |     |                       |             | Produces more detailed log output.  By default, games that are already up to date will not be displayed at all.  Specifying this option will make it so that all games, even ones up to date, will be logged.  |
| --unit      |     | bits, bytes           | **bits**    | Specifies which unit to use to display download speed.   |
| --no-ansi   |     |                       |             | Application output will be in plain text, rather than using the visually appealing colors and progress bars.  Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file. |
