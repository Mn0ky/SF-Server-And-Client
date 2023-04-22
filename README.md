# SF-Server-And-Client
A custom, dedicated UDP socket server for Stick Fight: The Game using the networking library Lidgren.
This is a total experiment and WIP. It currently only supports--successfully--authenticated connection via the 
Steam Web API, joining, spawning in, and moving around.

## Installation

To play on a dedicated server, please download the latest client plugin from the releases section. This plugin is a 
BepInEx plugin and as such can be installed by simply dropping it in the ``BepInEx\plugins`` directory.<br>

**If you need a full tutorial on installing BepInEx and loading Stick Fight mods with it please see 
[this video](https://github.com/Mn0ky/QOL-Mod#installation-tutorial-video).**

## Compilation And Solution Specifics:
(*For those that want to compile and or modify the server/client for themselves*)

- The server project is built using .NET 7 and only has a single non-standard library dependency which is the networking
 library [Lidgren-network-gen3](https://github.com/lidgren/lidgren-network-gen3).
- The client project is built using .NET 3.5 as that is what the game runs on with its Unity version. The required dependencies 
to build this project on those specified in the solution's nuget.config and the project file. With the game installed, 
you can copy any necessary assemblies from the game directory into your project directory to finish covering all dependencies.