# RimWorld Assembly References

If the build fails with missing RimWorld assemblies, copy the following files from your RimWorld installation to this References folder:

## Required Files:
- `Assembly-CSharp.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`

## Common RimWorld Installation Paths:
- **Steam (Default)**: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\`
- **Steam (Custom Drive)**: `D:\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\`
- **GOG**: `C:\GOG Games\RimWorld\RimWorldWin64_Data\Managed\`
- **Epic Games**: `C:\Program Files\Epic Games\RimWorld\RimWorldWin64_Data\Managed\`

## How to Copy Files:
1. Navigate to your RimWorld installation folder
2. Go to the `RimWorldWin64_Data\Managed\` subdirectory
3. Copy the three required DLL files to this References folder
4. Run the build again

## Finding Your RimWorld Installation:
If you can't find RimWorld, try these methods:
- **Steam**: Right-click RimWorld in your library → Properties → Local Files → Browse Local Files
- **GOG**: Open GOG Galaxy → Games → RimWorld → Manage Installation → Show Folder
- **Epic**: Open Epic Games Launcher → Library → RimWorld → Settings (gear icon) → Manage → Browse Local Files
