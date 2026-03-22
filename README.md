# Isometric Survival Builder Prototype

This repository now contains a self-bootstrapping Unity C# prototype for an isometric low-poly survival builder.

## Included Foundation

- Isometric orthographic camera with pan and zoom
- Endless-feeling procedural grid terrain with multiple biomes
- Selectable and movable humanoid figurines
- Collectibles on terrain: plants, flowers, rocks, minerals
- Shared inventory and click-to-place buildings
- Enemy wave system with ground, underground, flying, and water enemies
- Main menu, settings menu, pause menu, and game over flow

## Project Layout

- `Assets/Scripts/IsoSurvival/RuntimeBootstrap.cs`: creates the game automatically at runtime
- `Assets/Scripts/IsoSurvival/GameController.cs`: game state, camera, inventory, high-level orchestration
- `Assets/Scripts/IsoSurvival/ProceduralWorldSystem.cs`: biome generation, endless chunk loading, collectibles
- `Assets/Scripts/IsoSurvival/UnitsSystem.cs`: humanoid spawning, selection, movement, harvesting
- `Assets/Scripts/IsoSurvival/BuildingsSystem.cs`: build placement, towers, walls, houses
- `Assets/Scripts/IsoSurvival/WaveSystem.cs`: enemy spawning and attack behavior
- `Assets/Scripts/IsoSurvival/GameUiController.cs`: runtime UI for menu, HUD, pause, and game over

## Unity Setup

1. Open this folder as a Unity 3D project.
2. Let Unity import the scripts.
3. Open any scene, even an empty one.
4. Press Play.

The runtime bootstrapper creates the camera, light, UI, world, units, and gameplay systems automatically.

## Controls

- `Left Click`: select humanoids
- `Shift + Left Click`: add to selection
- `Right Click`: move selected humanoids
- `1`: queue house placement
- `2`: queue wall placement
- `3`: queue tower placement
- `Tab`: cancel build placement
- `WASD` or arrow keys: move camera
- `Mouse Wheel`: zoom
- `Esc`: pause

## Notes

- This is a gameplay foundation and vertical slice, not a finished production project.
- The project uses primitive meshes and runtime-generated UI so it can run without prefabs or authored scenes.
- If you want, the next step can be splitting this into authored scenes, prefabs, ScriptableObjects, and proper combat/pathfinding.
