# status

## Overview

Lists all selected apps and their disk usage.

-----

## Example usage

Checking the `status` is as simple as running the following from the terminal:
```powershell
./{{prefillName}} status
```

### Customized the sorting

An advanced usage with customized sorting can be used as the following from the terminal:
```powershell
./{{prefillName}} status --sort-order descending --sort-column size
```

## Options

| Option          |     | Values                | Default       |     |
| --------------- | --- | --------------------- | ------------- | --- |
| --os            |     | windows, linux, macos | **windows**   | Specifies which operating system(s) chunks should be filtered for. |
| --no-ansi       |     |                       |               | Application output will be in plain text, rather than using the visually appealing colors and progress bars.  Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file. |
| --sort-order    |     | ascending, descending | **ascending** | Specifies which sorting should be used for the data. |
| --sort-column   |     | app, size             | **app**       | Specifies which column should be used for the sorting. |