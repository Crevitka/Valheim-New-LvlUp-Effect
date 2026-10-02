# New LvlUp Effect — source

Author: Crevitka. MIT License. Version 1.2.1.

## Build

On Windows, install a .NET SDK, Valheim and BepInExPack Valheim. Open PowerShell in this folder:

```powershell
./build-plugin.ps1 -ValheimDir 'D:\Games\Valheim'
```

The script uses `BepInEx/core` inside the selected game folder. Use `-BepInExDir` to select a different BepInEx directory. Without parameters it uses the standard Steam installation under Program Files (x86).

Output: `bin/Release/NewLvlUpEffect.dll`. Only your mod's DLL is distributed; game, Unity and BepInEx assemblies are reference dependencies and must come from your own installation.

## Implementation

`Plugin.cs` contains the BepInEx entry point, Harmony hooks and the runtime Unity UI. The plugin ID is `local.valheim.newlvlupeffect`; its configuration file is `BepInEx/config/local.valheim.newlvlupeffect.cfg`.

- Normal skill gains use `Player.OnSkillLevelup`.
- Console gains use a before/after snapshot around `Skills.CheatRaiseSkill`.
- The overlay uses an independent screen-space Canvas.
- Masks for glow and rings are generated at runtime; icons and fonts are read from the game.
- `newlvlupeffect_preview` animates 5 → 6 without changing skills.

Close F5 after previewing. In-game verification is required after modifying UI or game hooks. The code was developed with AI assistance.
