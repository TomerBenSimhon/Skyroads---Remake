# SkyRoads — Remake

A 3D platformer, remaking the classic SkyRoads. Solo project — all code and mechanics built by me.

**Engine:** Unity 6000.3.2f1
**Role:** Solo developer

[Portfolio](https://tomerbensimhon.github.io)

## Overview

One or two sentences on the game and what you were going for.

## Systems

### Player Controller
Smooth, fluid 3D movement carrying over the same design goals as my 2D controller work — variable jump height and adaptive gravity — adapted for grid-based 3D platforms.

Assets/Player/Scripts/PlayerController.cs

### Modular Platform System
An interface-driven set of platform types: **boost**, **refuel**, **slippery**, and **death**. Each type implements a shared behavior contract, so adding a new platform type doesn't require touching the player controller.

Assets/Level Assets/Special Platforms/Scripts

### Custom Editor Tools
- **Prefab Painter** — a grid-based placement tool, effectively a 3D equivalent of Unity's 2D tilemap, built because the level's platforms follow a fixed grid.
- **Effect System + Global Event script** — a reusable component any object can attach camera, sound, and particle effects to, triggered off shared game events.

Assets/Editor/PrefabBrush
Assets/Effects

## Built with

Unity 6000.3.2f1 · C#
