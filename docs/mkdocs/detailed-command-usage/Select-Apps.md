# select-apps

<div data-cli-player="../casts/select-apps.cast" data-rows=30></div>
<br>

## Overview

<!-- TODO Write this at some point -->

-----

## Status

Lists all selected apps and their download size.  Please note that the download size is going to be smaller than the final install size since Steam compresses game files for download.

<div data-cli-player="../casts/select-apps-status.cast" data-rows=18></div>
<br>

-----

<!-- TODO give this another pass -->

### Example usage

Checking the `status` is as simple as running the following from the terminal:

```powershell
./{{prefill_name}} select-apps status
```

#### Customized the sorting

An advanced usage with customized sorting can be used as the following from the terminal:

```powershell
./{{prefill_name}} select-apps status --sort-order descending --sort-by size
```

-----

### Options

| Option       |     | Values                | Default       |                                                                                                                                                                                                                            |
| ------------ | --- | --------------------- | ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| --sort-order |     | ascending, descending | **ascending** | Specifies which order the data should be sorted.                                                                                                                                                                           |
| --sort-by    |     | app, size             | **app**       | Specifies which column should be used for the sorting.                                                                                                                                                                     |
| --os         |     | windows, linux, macos | **windows**   | Specifies which operating system(s) chunks should be filtered for.                                                                                                                                                         |
| --no-ansi    |     |                       |               | Application output will be in plain text, rather than using the visually appealing colors and progress bars. Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file. |
