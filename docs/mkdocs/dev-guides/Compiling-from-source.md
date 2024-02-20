# Compiling From Source

## Installing Prerequisites

Only the .NET 8 SDK is required to compile the project.  The following instructions will be using Chocolatey as a package manager for Windows, which makes installing software much easier as it can be done by a single command.  Chocolatey can be installed using the [Chocolatey install guide](https://chocolatey.org/install#individual) if it isn't already installed.

### dotnet SDK

```powershell
choco install dotnet-sdk --version=8.0.100
```

### Git

Additionally, if Git has not already been installed it can be installed using the following:

```powershell
choco install git.install
```

-----

## Cloning the repository

Prior to doing any work on the project a copy of the code must first be cloned from Github.  The following will clone the repo as well as all of the required submodules:

```powershell
git clone --recurse-submodules -j8 https://github.com/tpill90/{{repo_name}}.git
```


Alternatively if you've already cloned the repository without the submodules, use this command to include the submodules:
```
git submodule update --init --recursive
```

-----

## Compiling

The project can be compiled by running the following in the repository root (the directory with the .sln file).  This will generate an .exe that can be run locally.  Subsequent `dotnet build` commands will perform incremental compilation.

```powershell
dotnet build
```

-----

## Running the project

!!! note
    These steps assume that the working directory is `/{{prefill_name}}`.  All commands assume that they can find `{{prefill_name}}.csproj` in the working dir.

Typically, for development you will want to run the project in `Debug` mode.  This mode will run dramatically slower than `Release`, however it will leave useful debugging information in the compiled assembly.  Running the following will detect and compile any changes, and then execute the project:
```powershell
dotnet run
```

The above is analogous to running `./{{prefill_name}}.exe` without any parameters.  To instead supply parameters :
```powershell
dotnet run -- prefill --all
```

Alternatively, to run the project at full speed with all compilation optimizations enabled, add the additional `--configuration Release` flag:
```powershell
dotnet run --configuration Release
```

-----

## Running Unit Tests

To compile and run all tests in the entire repo, run the following command:
```powershell
dotnet test
```

-----

## Where should I get started in the codebase?

A good place to start poking around the project would be the [CliCommands folder](https://github.com/tpill90/{{repo_name}}/tree/master/{{prefill_name}}/CliCommands).  This folder contains the implementations for each of the individual commands that can be run, such as `prefill` or `select-apps`.  