# Windows Setup Guide

## Installing {{prefill_name}}

1.  Navigate to the [Releases](https://github.com/tpill90/{{repo_name}}/releases) page on Github.
2.  Download the latest version for Windows.  The filename should look like `{{prefill_name}}-X.Y.Z-win-x64.zip`.
3.  Unzip to a directory of your choice.  This can be anywhere on your system.

-----

## Optional Windows Setup

Configuring your terminal to use Unicode will result in a much nicer experience with **{{prefill_name}}**, for much nicer looking UI output.

![UTF8 Comparison](images/ConsoleWithUtf8.png){: style="width:730px"}

As the default console in Windows does not support UTF8, you should instead consider installing **Windows Terminal** from the [Microsoft App Store](https://apps.microsoft.com/store/detail/windows-terminal/9N0DX20HK701), or [Chocolatey](https://community.chocolatey.org/packages/microsoft-windows-terminal).

Once **Windows Terminal** has been installed you will still need to enable Unicode, as it is not enabled by default. Running the following command in Powershell will enable it if it hasn't already been enabled.

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

-----

## Next Steps

If you are new to **{{prefill_name}}** and would like an introductory tutorial, see the [Getting Started](https://github.com/tpill90/{{repo_name}}#getting-started) guide. 

Answers to common issues and questions can be found at [Frequently Asked Questions](https://github.com/tpill90/{{repo_name}}#frequently-asked-questions).  
