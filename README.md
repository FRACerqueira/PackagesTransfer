# <img align="left" width="100" height="100" src="./docs/images/icon.png">Welcome to PackagesTransfer
[![Build](https://github.com/FRACerqueira/PackagesTransfer/workflows/Build/badge.svg)](https://github.com/FRACerqueira/PackagesTransfer/actions/workflows/build.yml)
[![Publish](https://github.com/FRACerqueira/PackagesTransfer/actions/workflows/publish/badge.svg)](https://github.com/FRACerqueira/PackagesTransfer/actions/workflows/publish.yml)
[![License](https://img.shields.io/github/license/FRACerqueira/PackagesTransfer)](https://github.com/FRACerqueira/PackagesTransfer/blob/master/LICENSE)

**Interactive command-line to transfer packages between source to target with protocols Nuget and Npm.**

## Snapshot

![](./docs/images/snapshot.png)

## Features
[**Top**](#welcome-to-packagestransfer)

- Runs cross-platform (Windows/Linux)

- Verbose log to file (Folder Logs)

- Two types of protocols
    - NUGET (buit-in protocol)
    - NPM (must have the corresponding client version installed on your machine)

- Three types of transfer:
    - Between Azure-devops
    - File System to Azure-devops
    - Azure-devops to File System

- Option to exclude packages by Upstream-Source

- Shows in advance the count of distinct and versioned packages by protocol type

- Pre-skip already existing packets at the destination

- When the origin is a file system, it allows up to 10 levels of search sub-folders

- Authentication with Azure-devops is performed with PAT (Personal Access Token)

- Display of a summary of successful and failed transfers

- All entries saved (except passwords) for the next run to be done in the "Next–Next–Finished" model

## Supported platforms
[**Top**](#welcome-to-packagestransfer)

- Windows
    - Command Prompt, PowerShell, Windows Terminal
- Linux (Ubuntu, etc)
    - Windows Terminal (WSL 2)

## Release Notes 
[**Top**](#welcome-to-packagestransfer)

**PackagesTransfer (V1.0.0)**

- GA access

## License
[**Top**](#welcome-to-packagestransfer)

This project is licensed under the [MIT License](https://github.com/FRACerqueira/PackagesTransfer/blob/master/LICENSE)