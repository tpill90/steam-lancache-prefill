---
title: Test
---

<!-- This is a test page used to make sure that all the syntax highlighting doesnt look bad --->

## Bash

```powershell
./{{prefill_name}} prefill --top --os linux --verbose
choco install dotnet-sdk --version=8.0.100
git clone --recurse-submodules -j8 https://github.com/tpill90/steam-lancache-prefill.git
git submodule update --init --recursive
```

## Ini

```ini
[Unit]
Description={{prefill_name}}

# Set this to the directory where {{prefill_name}} is installed.
# Example : /home/tim/{{prefill_name}}
WorkingDirectory=
```

## Powershell
```powershell
if(!(Test-Path $profile))
{
    New-Item -Path $profile -Type File -Force
}
if(!(gc $profile).Contains("OutputEncoding"))
{
    ac $profile "[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()";
    & $profile;
}
```


## Project Layout

    mkdocs.yml    # Mkdocs root configuration file.
    mkdocs/
        index.md  # The documentation homepage.
        assets/   # Contains custom Javascript and CSS used on the docs site
        custom_theme/
    	img/
    	img/svg/   # The .ansi files in the parent directory will be rendered here.
        ...       # Other markdown pages, images and other files.

