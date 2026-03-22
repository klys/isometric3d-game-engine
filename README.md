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

Recommended Unity version: `6000.0.69f1` (Unity 6.0 LTS, released March 4, 2026).

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

## GitHub Actions Release Workflow

The repository now includes [`.github/workflows/unity-release.yml`](.github/workflows/unity-release.yml), which:

- runs on every push to `master`
- builds separate Unity players for Linux, Windows, and macOS
- uploads each build as a workflow artifact
- creates a GitHub Release for that push and attaches the three packaged builds

### Required Repository Secrets

Set the Unity license secrets in GitHub before enabling the workflow:

- Personal license:
- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`
- Professional license:
- `UNITY_SERIAL`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

For personal licenses, GameCI documents a one-time manual activation flow to obtain `UNITY_LICENSE`. For pro licenses, use your Unity serial plus account credentials.

Add secrets here:

1. Open your GitHub repository.
2. Go to `Settings`.
3. Open `Secrets and variables` > `Actions`.
4. Create the needed `UNITY_*` secrets.

### Important Assumption

This workflow is pinned to Unity `6000.0.69f1`. The repository also includes matching `ProjectSettings/ProjectVersion.txt` so local and CI builds stay aligned. If your Unity project lives in a subfolder later, update `projectPath`.

### About The Warnings You Saw

- `Project settings file not found`: this was the real blocker, caused by the repo not yet having Unity `ProjectSettings/` and `Packages/` files.
- `Missing Unity License File and no Serial was found`: this means GitHub Actions cannot activate Unity yet because the required `UNITY_*` secrets are still missing or incomplete.
- `Library folder does not exist`: this is normal on a first CI build and is only a cache warmup warning.
- `Node.js 20 actions are deprecated`: this is an ecosystem warning from GitHub Actions. The workflow now opts into Node 24 and uses `actions/checkout@v5` to reduce that risk.
