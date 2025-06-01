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
            
            // Initialize keybinding system (only check once)
            if (!keyBindingChecked)
            {
                InitializeKeyBinding();
                keyBindingChecked = true;
            }
            
            bool shouldToggle = false;
            
            // Try to use KeyBindingDef first, fallback to hardcoded key
            if (useKeyBindingDef && toggleKeyBinding != null)
            {
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
                    Log.Warning("[CryptoTrader] KeyBindingDef not found. Using fallback Delete key. This is normal during development or if KeyBindingDefs.xml is not in the correct location.");
                    Log.Message("[CryptoTrader] Trading terminal can be opened with the Delete key.");
                }
            }
            catch (System.Exception ex)
            {
                useKeyBindingDef = false;
                Log.Error($"[CryptoTrader] Error initializing keybinding: {ex.Message}. Using fallback Delete key.");
            }
        }
        
        // Method to manually check keybinding status (for debugging)
        public static string GetKeyBindingStatus()
        {
            if (!keyBindingChecked)
            {
                return "Not initialized yet";
            }
            
            if (useKeyBindingDef && toggleKeyBinding != null)
            {
                return $"Using KeyBindingDef: {toggleKeyBinding.defaultKeyCodeA}";
            }
            else
            {
                return "Using fallback Delete key";
            }
        }
    }
}
