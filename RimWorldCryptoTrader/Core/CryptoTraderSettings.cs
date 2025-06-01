using UnityEngine;
using Verse;

namespace RimWorldCryptoTrader.Core
{
    public class CryptoTraderSettings : ModSettings
    {
        // Conversion rate settings
        public static float silverToUsdRate = 10f;
        public static int minimumSilverToTrade = 10;
        public static int maxSingleDepositSilver = 1000;
        
        // UI and behavior settings
        public static bool enableRealTimeUpdates = true;
        public static float updateIntervalSeconds = 5f;
        public static bool enableDebugLogging = false;
        public static bool showDetailedPrices = true;
        
        // Trading safety settings
        public static bool confirmLargeTransactions = true;
        public static float largeTransactionThreshold = 500f;
        public static bool enableTradingLimits = true;

        // Key binding settings
        public static KeyCode toggleKey = KeyCode.Delete;
        public static bool useCustomKey = true;
        
        // Internal flag to track if we're waiting for key input
        public static bool waitingForKeyInput = false;
        public static string keyInputLabel = "Delete";

        public override void ExposeData()
        {
            base.ExposeData();
            
            // Conversion settings
            Scribe_Values.Look(ref silverToUsdRate, "silverToUsdRate", 10f);
            Scribe_Values.Look(ref minimumSilverToTrade, "minimumSilverToTrade", 10);
            Scribe_Values.Look(ref maxSingleDepositSilver, "maxSingleDepositSilver", 1000);
            
            // UI settings
            Scribe_Values.Look(ref enableRealTimeUpdates, "enableRealTimeUpdates", true);
            Scribe_Values.Look(ref updateIntervalSeconds, "updateIntervalSeconds", 5f);
            Scribe_Values.Look(ref enableDebugLogging, "enableDebugLogging", false);
            Scribe_Values.Look(ref showDetailedPrices, "showDetailedPrices", true);
            
            // Safety settings
            Scribe_Values.Look(ref confirmLargeTransactions, "confirmLargeTransactions", true);
            Scribe_Values.Look(ref largeTransactionThreshold, "largeTransactionThreshold", 500f);
            Scribe_Values.Look(ref enableTradingLimits, "enableTradingLimits", true);
            
            // Key binding settings
            Scribe_Values.Look(ref useCustomKey, "useCustomKey", true);
            
            // Handle KeyCode serialization
            string keyCodeString = toggleKey.ToString();
            Scribe_Values.Look(ref keyCodeString, "toggleKey", "Delete");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (System.Enum.TryParse(keyCodeString, out KeyCode parsedKey))
                {
                    toggleKey = parsedKey;
                    keyInputLabel = GetKeyDisplayName(toggleKey);
                }
            }
            
            // Apply settings to config after loading
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                ApplySettings();
            }
        }
        
        public static void ApplySettings()
        {
            // Round the silver rate to avoid decimal precision issues
            silverToUsdRate = Mathf.Round(silverToUsdRate * 10f) / 10f; // Round to 1 decimal place
            
            // Update the main config with settings values
            CryptoTraderConfig.SilverToUsdRate = silverToUsdRate;
            CryptoTraderConfig.MinimumSilverToTrade = minimumSilverToTrade;
            CryptoTraderConfig.MaxSingleDepositSilver = maxSingleDepositSilver;
            
            if (enableDebugLogging)
            {
                Log.Message($"[CryptoTrader] Settings applied - Rate: {silverToUsdRate}, Min: {minimumSilverToTrade}, Max: {maxSingleDepositSilver}, Key: {toggleKey}");
            }
        }
        
        public static void ResetToDefaults()
        {
            silverToUsdRate = 10f;
            minimumSilverToTrade = 10;
            maxSingleDepositSilver = 1000;
            enableRealTimeUpdates = true;
            updateIntervalSeconds = 5f;
            enableDebugLogging = false;
            showDetailedPrices = true;
            confirmLargeTransactions = true;
            largeTransactionThreshold = 500f;
            enableTradingLimits = true;
            toggleKey = KeyCode.Delete;
            useCustomKey = true;
            keyInputLabel = "Delete";
            waitingForKeyInput = false;
            
            ApplySettings();
        }
        
        public static string GetKeyDisplayName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Delete: return "Delete";
                case KeyCode.F1: return "F1";
                case KeyCode.F2: return "F2";
                case KeyCode.F3: return "F3";
                case KeyCode.F4: return "F4";
                case KeyCode.F5: return "F5";
                case KeyCode.F6: return "F6";
                case KeyCode.F7: return "F7";
                case KeyCode.F8: return "F8";
                case KeyCode.F9: return "F9";
                case KeyCode.F10: return "F10";
                case KeyCode.F11: return "F11";
                case KeyCode.F12: return "F12";
                case KeyCode.Tab: return "Tab";
                case KeyCode.CapsLock: return "Caps Lock";
                case KeyCode.LeftShift: return "Left Shift";
                case KeyCode.RightShift: return "Right Shift";
                case KeyCode.LeftControl: return "Left Ctrl";
                case KeyCode.RightControl: return "Right Ctrl";
                case KeyCode.LeftAlt: return "Left Alt";
                case KeyCode.RightAlt: return "Right Alt";
                case KeyCode.Space: return "Space";
                case KeyCode.Return: return "Enter";
                case KeyCode.Escape: return "Escape";
                case KeyCode.Backspace: return "Backspace";
                case KeyCode.Insert: return "Insert";
                case KeyCode.Home: return "Home";
                case KeyCode.End: return "End";
                case KeyCode.PageUp: return "Page Up";
                case KeyCode.PageDown: return "Page Down";
                case KeyCode.UpArrow: return "Up Arrow";
                case KeyCode.DownArrow: return "Down Arrow";
                case KeyCode.LeftArrow: return "Left Arrow";
                case KeyCode.RightArrow: return "Right Arrow";
                case KeyCode.Keypad0: return "Numpad 0";
                case KeyCode.Keypad1: return "Numpad 1";
                case KeyCode.Keypad2: return "Numpad 2";
                case KeyCode.Keypad3: return "Numpad 3";
                case KeyCode.Keypad4: return "Numpad 4";
                case KeyCode.Keypad5: return "Numpad 5";
                case KeyCode.Keypad6: return "Numpad 6";
                case KeyCode.Keypad7: return "Numpad 7";
                case KeyCode.Keypad8: return "Numpad 8";
                case KeyCode.Keypad9: return "Numpad 9";
                case KeyCode.KeypadPlus: return "Numpad +";
                case KeyCode.KeypadMinus: return "Numpad -";
                case KeyCode.KeypadMultiply: return "Numpad *";
                case KeyCode.KeypadDivide: return "Numpad /";
                case KeyCode.KeypadEnter: return "Numpad Enter";
                case KeyCode.KeypadPeriod: return "Numpad .";
                default: return key.ToString();
            }
        }
    }
}
