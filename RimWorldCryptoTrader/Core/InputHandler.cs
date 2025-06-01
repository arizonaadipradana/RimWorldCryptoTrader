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
            
            // Changed to Delete key to avoid conflicts with debug system
            // Delete key is rarely used by other systems and safe for mod keybinds
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                try
                {
                    if (tradingWindow == null || !Find.WindowStack.IsOpen(tradingWindow))
                    {
                        tradingWindow = new TradingWindow();
                        Find.WindowStack.Add(tradingWindow);
                    }
                    else
                    {
                        Find.WindowStack.TryRemove(tradingWindow);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[CryptoTrader] Error toggling trading window: {ex.Message}");
                }
            }
        }
    }
}