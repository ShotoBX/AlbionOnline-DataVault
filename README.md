AlbionOnline - DataVault
===================
[![GitHub release (with filter)](https://img.shields.io/github/v/release/ShotoBX/AlbionOnline-DataVault?style=for-the-badge&labelColor=1E2126&color=0C637F)](https://github.com/ShotoBX/AlbionOnline-DataVault/releases)
[![GitHub all releases downloads](https://img.shields.io/github/downloads/ShotoBX/AlbionOnline-DataVault/total?style=for-the-badge&labelColor=1E2126&color=EF476F)](https://github.com/ShotoBX/AlbionOnline-DataVault/releases)
[![Build + Tests Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/ShotoBX/AlbionOnline-DataVault/pr-build-and-unit-tests.yml?style=for-the-badge&label=%F0%9F%9B%A0%EF%B8%8F%20Build%20%2B%20Unit%20tests&labelColor=1E2126&color=09C3A5)](https://github.com/ShotoBX/AlbionOnline-DataVault/actions/workflows/pr-build-and-unit-tests.yml)
![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/ShotoBX/AlbionOnline-DataVault/latest?style=for-the-badge&labelColor=1E2126&color=09C3A5)
[![GitHub issues](https://img.shields.io/github/issues/ShotoBX/AlbionOnline-DataVault?style=for-the-badge&labelColor=1E2126&color=FBAF69)](https://github.com/ShotoBX/AlbionOnline-DataVault/issues)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg?style=for-the-badge&labelColor=1E2126)](LICENSE)

A tool to easily read auction house data with a loot logger, damage meter, dungeon tracker, dungeon entry timer, crafting calculator, map history and player information.

> **AlbionOnline - DataVault** is a rebranded, redesigned continuation of [**AlbionOnline-StatisticsAnalysis**](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis), originally created by **[Triky313 (Aaron Schultz)](https://github.com/Triky313)**. All credit for the original tool, its architecture and years of work goes to Triky313 and everyone listed in [Credits & Original Project](#credits--original-project) below. This fork keeps the GPLv3 license and builds on that foundation with a new visual design and additional features.

<p align="center" align='right'>
  <img src="https://user-images.githubusercontent.com/14247773/147143464-c36d0cba-dddb-4b34-bd2e-11e3f65e3289.png" data-canonical-src="https://user-images.githubusercontent.com/14247773/147143464-c36d0cba-dddb-4b34-bd2e-11e3f65e3289.png" width="400" height="400" />
</p>

*(Screenshot carried over from the original project - will be updated to reflect the new interface.)*

## Getting Started

### Prerequisites & Installation
- You need **Windows 10** or higher
- Install **.NET 10.0 Desktop Runtime** (v10.0.0 or higher) [here](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (To start the tool)

**Download AlbionOnline - DataVault**
[**Latest release**](https://github.com/ShotoBX/AlbionOnline-DataVault/releases/latest)

Unzip the `.zip` file (or run the installer `.exe`) and start `AlbionOnline-DataVault.exe` with a double click. You may not be able to see the `.exe` extension. Don't worry, usually it's the file with the icon.

![tool_dir](https://user-images.githubusercontent.com/14247773/170473306-4dcc629e-384e-41b2-ada8-657cabe1b472.png)

### Tracking via socket
If tracking is used via socket, the tool only needs to be started as an administrator for it to work fully.

### Tracking via Npcap
As an alternative there is the Npcap variant, for this Npcap must be installed, but the tool must no longer be started as administrator.
In addition, a filtering is available for VPN users that allows IP and port filtering.

https://npcap.com/ (Normally the latest version should work!)

## Is This Allowed
[Debate](https://forum.albiononline.com/index.php/Thread/124819-Regarding-3rd-Party-Software-and-Network-Traffic-aka-do-not-cheat-Update-16-45-U/) (Original link), [Clarified](https://forum.albiononline.com/index.php/Thread/153238-DPS-METER/#:~:text=As%20noted%20on%20the%20GitHub,to%20use%20it%20without%20concern.) (2023 short CM answer)

✅ Only monitors

✅ Does not modify our game client

✅ Does not track players that are not within the player's view

✅ Does not have an overlay to the game

## FAQ
### Which operating system is supported?
✅ Windows 10 and later

❌ Windows XP, Vista, 7 and 8 are not supported!

❌ Linux is currently not supported!

❌ Mac is currently not supported!

### Can I use the tool with Geforce Now
No, unfortunately this is not technically possible.

### Can I use the tool with ExitLag or VPN?
Yes, VPN or ExitLag can generally be used, if you use Npcap tracking

### How fast does my internet need to be?
An internet connection with at least 1M/bit (256KB/s) download rate.

### Can I use the tool even if the game is not started
Yes, but not all features are available.
It is only important that you set the game server from automatic to one of your choice in the settings. Otherwise the tool does not know for which server it should load data.

## WIKI
The original project's [Wiki](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis/wiki) covers most of the base functionality this fork also shares.

## Credits & Original Project

This project would not exist without **[Triky313 (Aaron Schultz)](https://github.com/Triky313)**, who has maintained [AlbionOnline-StatisticsAnalysis](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis) since June 2019, and everyone who contributed to it over the years. If you find this tool useful, please consider supporting the original creator directly:

[Patreon - Triky313](https://www.patreon.com/triky313)

<img src="https://user-images.githubusercontent.com/14247773/166248069-3211a206-b475-4e83-860b-e5c51b9554bf.png" data-canonical-src="https://www.patreon.com/triky313" width="40" height="40" />

[PayPal - Triky313](https://www.paypal.com/donate/?hosted_button_id=N6T3CWXYNGHKC)

<img src="https://user-images.githubusercontent.com/14247773/201472890-33a0ed70-7ef8-4804-aa84-46f0a84f3168.png" width="100" height="100" />

The original project also has its own [Discord community](https://discord.gg/Wv5RWehbrU) if you want to connect with that project directly.

### Original project contributors
<table>
<tr>
    <td align="center">
        <a href="https://github.com/Triky313">
            <img src="https://avatars.githubusercontent.com/u/14247773?v=4" width="50;" alt="Triky313"/>
            <br />
            <sub><b>Aaron Schultz</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/arabinos">
            <img src="https://avatars.githubusercontent.com/u/115917138?v=4" width="50;" alt="arabinos"/>
            <br />
            <sub><b>Marcin Wieczorek</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/bluenyx">
            <img src="https://avatars.githubusercontent.com/u/96876?v=4" width="50;" alt="bluenyx"/>
            <br />
            <sub><b>SeoheeKhang</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/isnullibi">
            <img src="https://avatars.githubusercontent.com/u/100205074?v=4" width="50;" alt="isnullibi"/>
            <br />
            <sub><b>Null</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/ewersonmssilva">
            <img src="https://avatars.githubusercontent.com/u/26557729?v=4" width="50;" alt="ewersonmssilva"/>
            <br />
            <sub><b>Ewerson</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/NuberuSH">
            <img src="https://avatars.githubusercontent.com/u/45773746?v=4" width="50;" alt="NuberuSH"/>
            <br />
            <sub><b>Dani Tallón</b></sub>
        </a>
    </td></tr>
<tr>
    <td align="center">
        <a href="https://github.com/kkkingim">
            <img src="https://avatars.githubusercontent.com/u/22095496?v=4" width="50;" alt="kkkingim"/>
            <br />
            <sub><b>Vagitus</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/Faeeth">
            <img src="https://avatars.githubusercontent.com/u/37340968?v=4" width="50;" alt="Faeeth"/>
            <br />
            <sub><b>Faeeth</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/Me1onSeed">
            <img src="https://avatars.githubusercontent.com/u/81557800?v=4" width="50;" alt="Me1onSeed"/>
            <br />
            <sub><b>瓜子</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/PurpleGale">
            <img src="https://avatars.githubusercontent.com/u/90148755?v=4" width="50;" alt="PurpleGale"/>
            <br />
            <sub><b>Null</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/taco0603">
            <img src="https://avatars.githubusercontent.com/u/19679024?v=4" width="50;" alt="taco0603"/>
            <br />
            <sub><b>Null</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/1027603857">
            <img src="https://avatars.githubusercontent.com/u/38471268?v=4" width="50;" alt="1027603857"/>
            <br />
            <sub><b>Null</b></sub>
        </a>
    </td></tr>
<tr>
    <td align="center">
        <a href="https://github.com/acelan">
            <img src="https://avatars.githubusercontent.com/u/71646?v=4" width="50;" alt="acelan"/>
            <br />
            <sub><b>AceLan Kao</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/ivanmaxlogiudice">
            <img src="https://avatars.githubusercontent.com/u/3275920?v=4" width="50;" alt="ivanmaxlogiudice"/>
            <br />
            <sub><b>Iván Máximiliano, Lo Giudice</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/devsurimlee">
            <img src="https://avatars.githubusercontent.com/u/53467957?v=4" width="50;" alt="devsurimlee"/>
            <br />
            <sub><b>Surim Lee</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/lx78WyY0J5">
            <img src="https://avatars.githubusercontent.com/u/84735589?v=4" width="50;" alt="lx78WyY0J5"/>
            <br />
            <sub><b>Null</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/rdayltx">
            <img src="https://avatars.githubusercontent.com/u/82792422?v=4" width="50;" alt="rdayltx"/>
            <br />
            <sub><b>DayLight</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/zenion">
            <img src="https://avatars.githubusercontent.com/u/4081449?v=4" width="50;" alt="zenion"/>
            <br />
            <sub><b>Josh Stout</b></sub>
        </a>
    </td></tr>
<tr>
    <td align="center">
        <a href="https://github.com/Kukkimonsuta">
            <img src="https://avatars.githubusercontent.com/u/737093?v=4" width="50;" alt="Kukkimonsuta"/>
            <br />
            <sub><b>Lukáš Novotný</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/Dibort">
            <img src="https://avatars.githubusercontent.com/u/7732931?v=4" width="50;" alt="Dibort"/>
            <br />
            <sub><b>Null</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/dyj0816">
            <img src="https://avatars.githubusercontent.com/u/40786887?v=4" width="50;" alt="dyj0816"/>
            <br />
            <sub><b>Redmeier</b></sub>
        </a>
    </td>
    <td align="center">
        <a href="https://github.com/mleen4">
            <img src="https://avatars.githubusercontent.com/u/63968148?v=4" width="50;" alt="mleen4"/>
            <br />
            <sub><b>Michael</b></sub>
        </a>
    </td></tr>
</table>

### This fork
Redesigned UI, multi-city crafting/refining profit optimizer, arbitrage finder, Discord webhook notifications, loot split calculator and more, maintained by [ShotoBX](https://github.com/ShotoBX).

## License
Licensed under [GPLv3](LICENSE), same as the original project.
