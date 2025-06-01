using HarmonyLib;
using RimWorldCryptoTrader.UI;
using UnityEngine;
using Verse;

namespace RimWorldCryptoTrader.Core
{
    [HarmonyPatch(typeof(Root), "Update")]
    public static class InputHandler
    {
        private static TradingWindow tradingWindow;
        private static KeyBindingDef toggleKeyBinding;
        private static bool keyBindingChecked = false;
        private static bool useKeyBindingDef = false;

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Current.Game == null) return;
            
            // Enhanced conflict prevention - check for any debug activities
            if (DebugViewSettings.writeGame || DebugViewSettings.drawPawnDebug) return;
            
            // Prevent conflicts when any debug window is open
            if (Find.WindowStack.Count > 0)
            {
                // Check if any debug-related windows are open by checking window types
                foreach (var window in Find.WindowStack.Windows)
                {
                    if (window.GetType().Name.Contains("Debug"))
                    {
                        return; // Skip input processing if debug windows are open
                    }
                }
            }
            
            // Don't process input if we're waiting for key input in settings
            if (CryptoTraderSettings.waitingForKeyInput)
            {
                return;
            }
            
            // Initialize keybinding system (only check once)
            if (!keyBindingChecked)
            {
                InitializeKeyBinding();
                keyBindingChecked = true;
            }
            
            bool shouldToggle = false;
            
            // Priority order:
            // 1. Custom key from mod settings (if useCustomKey is enabled)
            // 2. KeyBindingDef (if available)
            // 3. Fallback to Delete key
            
            if (CryptoTraderSettings.useCustomKey)
            {
                // Use custom key from mod settings
                shouldToggle = Input.GetKeyDown(CryptoTraderSettings.toggleKey);
                if (CryptoTraderSettings.enableDebugLogging && shouldToggle)
                {
                    Log.Message($"[CryptoTrader] Custom key triggered: {CryptoTraderSettings.toggleKey}");
                }
            }
            else if (useKeyBindingDef && toggleKeyBinding != null)
            {
                // Use KeyBindingDef system
                shouldToggle = toggleKeyBinding.JustPressed;
                if (CryptoTraderSettings.enableDebugLogging && shouldToggle)
                {
                    Log.Message("[CryptoTrader] KeyBindingDef triggered");
                }
            }
            else
            {
                // Fallback to hardcoded Delete key
                shouldToggle = Input.GetKeyDown(KeyCode.Delete);
                if (CryptoTraderSettings.enableDebugLogging && shouldToggle)
                {
                    Log.Message("[CryptoTrader] Fallback Delete key triggered");
                }
            }
            
            if (shouldToggle)
            {
                try
                {
                    if (tradingWindow == null || !Find.WindowStack.IsOpen(tradingWindow))
                    {
                        tradingWindow = new TradingWindow();
                        Find.WindowStack.Add(tradingWindow);
                        if (CryptoTraderSettings.enableDebugLogging)
                        {
                            Log.Message("[CryptoTrader] Trading window opened.");
                        }
                    }
                    else
                    {
                        Find.WindowStack.TryRemove(tradingWindow);
                        if (CryptoTraderSettings.enableDebugLogging)
                        {
                            Log.Message("[CryptoTrader] Trading window closed.");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[CryptoTrader] Error toggling trading window: {ex.Message}");
                }
            }
        }

        private static void InitializeKeyBinding()
        {
            try
            {
                toggleKeyBinding = DefDatabase<KeyBindingDef>.GetNamedSilentFail("CryptoTrader_ToggleWindow");
                
                if (toggleKeyBinding != null)
                {
                    useKeyBindingDef = true;
                    Log.Message($"[CryptoTrader] KeyBindingDef loaded successfully. Default key: {toggleKeyBinding.defaultKeyCodeA}");
                }
                else
                {
                    useKeyBindingDef = false;
                    if (CryptoTraderSettings.enableDebugLogging)
                    {
                        Log.Warning("[CryptoTrader] KeyBindingDef not found. Using custom key from mod settings or fallback Delete key.");
                    }
                }
                
                // Always inform about current key configuration
                if (CryptoTraderSettings.useCustomKey)
                {
                    Log.Message($"[CryptoTrader] Using custom key from mod settings: {CryptoTraderSettings.toggleKey}");
                }
                else
                {
                    Log.Message("[CryptoTrader] Using RimWorld's key binding system or fallback Delete key.");
                }
            }
            catch (System.Exception ex)
            {
                useKeyBindingDef = false;
                Log.Error($"[CryptoTrader] Error initializing keybinding: {ex.Message}. Using custom key from mod settings or fallback Delete key.");
            }
        }
        
        // Method to manually check keybinding status (for debugging)
        public static string GetKeyBindingStatus()
        {
            if (!keyBindingChecked)
            {
                return "Not initialized yet";
            }
            
            if (CryptoTraderSettings.useCustomKey)
            {
                return $"Using custom key: {CryptoTraderSettings.toggleKey}";
            }
            else if (useKeyBindingDef && toggleKeyBinding != null)
            {
                return $"Using KeyBindingDef: {toggleKeyBinding.defaultKeyCodeA}";
            }
            else
            {
                return "Using fallback Delete key";
            }
        }
        
        // Method to refresh keybinding when settings change
        public static void RefreshKeyBinding()
        {
            keyBindingChecked = false;
            if (CryptoTraderSettings.enableDebugLogging)
            {
                Log.Message("[CryptoTrader] Key binding refreshed due to settings change.");
            }
        }
    }
}
