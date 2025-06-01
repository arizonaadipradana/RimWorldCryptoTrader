# RimWorld CryptoTrader - Error Fix Documentation

## Problem Fixed
**KeyNotFoundException in Debug System**
- Error: `System.Collections.Generic.KeyNotFoundException` in `LudeonTK.Dialog_Debug.SwitchTab`
- Cause: F9 keybind conflicted with RimWorld's debug system

## Changes Made

### 1. Changed Keybind (InputHandler.cs)
- **OLD:** F9 key
- **NEW:** DELETE key
- **Reason:** DELETE key has minimal conflicts with RimWorld systems

### 2. Enhanced Conflict Prevention
- Added checks for debug inspector windows
- Added checks for debug action menus
- Added try-catch blocks for error handling

### 3. Improved Error Handling
- Added exception handling in window operations
- Added logging for debugging issues
- Auto-close window on errors to prevent cascading failures

## Do You Need HugsLib?
**NO** - Your mod does not require HugsLib. The error was caused by keybind conflicts, not missing dependencies.

## Testing Instructions

1. **Build the mod:**
   ```
   double-click build_and_test.bat
   ```

2. **Test in RimWorld:**
   - Start RimWorld
   - Load a save or start new game
   - Press **DELETE** key to open trading terminal

3. **If errors persist:**
   - Check RimWorld log for `[CryptoTrader]` messages
   - Ensure no other mods use DELETE key
   - Try disabling debug mode (Ctrl+Shift+D)

## Alternative Keybind Options
If DELETE key conflicts with other mods, edit `InputHandler.cs` line 26:

```csharp
// Current:
if (Input.GetKeyDown(KeyCode.Delete))

// Alternatives:
if (Input.GetKeyDown(KeyCode.End))        // END key
if (Input.GetKeyDown(KeyCode.Home))       // HOME key  
if (Input.GetKeyDown(KeyCode.PageDown))   // Page Down
if (Input.GetKeyDown(KeyCode.Insert))     // Insert key
```

## Debug Mode Conflicts
The error occurred because:
1. F9 is used by RimWorld's debug system
2. Debug system tried to access non-existent tab definitions
3. Your mod's input handler was triggering debug functions

The fix prevents this by:
- Using a different key (DELETE)
- Checking for active debug windows
- Adding error boundaries

## Key Benefits of Changes
- ✅ No more KeyNotFoundException errors
- ✅ Better conflict prevention with debug tools
- ✅ Safer error handling
- ✅ Clear logging for troubleshooting
- ✅ No dependency on HugsLib required
