using HarmonyLib;
using RimWorldCryptoTrader.Core;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimWorldCryptoTrader.Core
{
    public class CryptoTraderMod : Mod
    {
        public static CryptoTraderSettings settings;

        public CryptoTraderMod(ModContentPack content) : base(content)
        {
            // Initialize settings
            settings = GetSettings<CryptoTraderSettings>();
            CryptoTraderSettings.ApplySettings();

            var harmony = new Harmony("rimworld.cryptotrader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("[CryptoTrader] Mod loaded successfully. Check mod settings to customize behavior.");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // Title
            Text.Font = GameFont.Medium;
            listing.Label("Crypto Trader Settings");
            Text.Font = GameFont.Small;
            listing.Gap();

            // Key Binding Settings
            listing.Label("Hotkey Configuration:");
            listing.Gap(6f);

            var keyRect = listing.GetRect(30f);
            var keyLabelRect = new Rect(keyRect.x, keyRect.y, 150f, 30f);
            var keyButtonRect = new Rect(keyRect.x + 160f, keyRect.y, 120f, 30f);
            var resetKeyRect = new Rect(keyRect.x + 290f, keyRect.y, 80f, 30f);

            Widgets.Label(keyLabelRect, "Toggle Trading Window:");
            
            // Key binding button
            string buttonText = CryptoTraderSettings.waitingForKeyInput ? "Press any key..." : CryptoTraderSettings.keyInputLabel;
            GUI.color = CryptoTraderSettings.waitingForKeyInput ? Color.yellow : Color.white;
            
            if (Widgets.ButtonText(keyButtonRect, buttonText))
            {
                CryptoTraderSettings.waitingForKeyInput = true;
            }
            
            GUI.color = Color.white;

            // Reset key button
            if (Widgets.ButtonText(resetKeyRect, "Reset"))
            {
                CryptoTraderSettings.toggleKey = KeyCode.Delete;
                CryptoTraderSettings.keyInputLabel = "Delete";
                CryptoTraderSettings.waitingForKeyInput = false;
            }

            // Handle key input
            if (CryptoTraderSettings.waitingForKeyInput && Event.current.type == EventType.KeyDown)
            {
                var pressedKey = Event.current.keyCode;
                
                // Ignore modifier keys and some problematic keys
                if (pressedKey != KeyCode.None && 
                    pressedKey != KeyCode.LeftShift && pressedKey != KeyCode.RightShift &&
                    pressedKey != KeyCode.LeftControl && pressedKey != KeyCode.RightControl &&
                    pressedKey != KeyCode.LeftAlt && pressedKey != KeyCode.RightAlt &&
                    pressedKey != KeyCode.LeftCommand && pressedKey != KeyCode.RightCommand)
                {
                    CryptoTraderSettings.toggleKey = pressedKey;
                    CryptoTraderSettings.keyInputLabel = CryptoTraderSettings.GetKeyDisplayName(pressedKey);
                    CryptoTraderSettings.waitingForKeyInput = false;
                    Event.current.Use(); // Consume the event
                }
            }

            GUI.color = Color.white;
            listing.Gap();

            // Conversion Rate Settings
            listing.Label("Conversion Rate Settings:");
            listing.Gap(6f);

            // Fixed precision slider for silver to USD rate
            listing.Label($"Silver to USD Rate: ${CryptoTraderSettings.silverToUsdRate:F1}");
            
            // Use a custom slider with proper step handling
            var newRate = listing.Slider(CryptoTraderSettings.silverToUsdRate, 1f, 100f);
            // Round to nearest 0.5 to avoid precision issues
            CryptoTraderSettings.silverToUsdRate = Mathf.Round(newRate * 2f) / 2f;
            
            listing.Gap(6f);

            var rateInfo = $"1 silver = ${CryptoTraderSettings.silverToUsdRate:F1} USD ({1000f / CryptoTraderSettings.silverToUsdRate:F0} silver for $1000)";
            GUI.color = Color.gray;
            listing.Label(rateInfo);
            GUI.color = Color.white;
            listing.Gap();

            listing.Label($"Minimum Silver to Trade: {CryptoTraderSettings.minimumSilverToTrade}");
            CryptoTraderSettings.minimumSilverToTrade = (int)listing.Slider(CryptoTraderSettings.minimumSilverToTrade, 1, 100);
            listing.Gap();

            listing.Label($"Maximum Single Deposit: {CryptoTraderSettings.maxSingleDepositSilver} silver");
            CryptoTraderSettings.maxSingleDepositSilver = (int)listing.Slider(CryptoTraderSettings.maxSingleDepositSilver, 100, 10000);
            listing.Gap();

            // UI Settings
            listing.Label("Interface Settings:");
            listing.Gap(6f);

            listing.CheckboxLabeled("Enable Real-time Updates", ref CryptoTraderSettings.enableRealTimeUpdates);
            
            if (CryptoTraderSettings.enableRealTimeUpdates)
            {
                listing.Label($"Update Interval: {CryptoTraderSettings.updateIntervalSeconds:F1} seconds");
                // Round to nearest 0.5 seconds for clean values
                var newInterval = listing.Slider(CryptoTraderSettings.updateIntervalSeconds, 1f, 30f);
                CryptoTraderSettings.updateIntervalSeconds = Mathf.Round(newInterval * 2f) / 2f;
            }
            
            listing.CheckboxLabeled("Show Detailed Prices", ref CryptoTraderSettings.showDetailedPrices);
            listing.CheckboxLabeled("Enable Debug Logging", ref CryptoTraderSettings.enableDebugLogging);
            listing.Gap();

            // Safety Settings
            listing.Label("Safety Settings:");
            listing.Gap(6f);

            listing.CheckboxLabeled("Confirm Large Transactions", ref CryptoTraderSettings.confirmLargeTransactions);
            
            if (CryptoTraderSettings.confirmLargeTransactions)
            {
                listing.Label($"Large Transaction Threshold: ${CryptoTraderSettings.largeTransactionThreshold:F0}");
                // Round to nearest 50 for clean values
                var newThreshold = listing.Slider(CryptoTraderSettings.largeTransactionThreshold, 100f, 5000f);
                CryptoTraderSettings.largeTransactionThreshold = Mathf.Round(newThreshold / 50f) * 50f;
            }
            
            listing.CheckboxLabeled("Enable Trading Limits", ref CryptoTraderSettings.enableTradingLimits);
            listing.Gap();

            // Preset buttons
            listing.Label("Quick Presets:");
            listing.Gap(6f);

            var buttonRect = listing.GetRect(30f);
            var buttonWidth = buttonRect.width / 4f - 5f;

            if (Widgets.ButtonText(new Rect(buttonRect.x, buttonRect.y, buttonWidth, 30f), "Conservative"))
            {
                CryptoTraderSettings.silverToUsdRate = 5f;
                CryptoTraderSettings.maxSingleDepositSilver = 500;
                CryptoTraderSettings.confirmLargeTransactions = true;
                CryptoTraderSettings.largeTransactionThreshold = 250f;
                CryptoTraderSettings.toggleKey = KeyCode.Delete;
                CryptoTraderSettings.keyInputLabel = "Delete";
            }

            if (Widgets.ButtonText(new Rect(buttonRect.x + buttonWidth + 5f, buttonRect.y, buttonWidth, 30f), "Balanced"))
            {
                CryptoTraderSettings.silverToUsdRate = 10f;
                CryptoTraderSettings.maxSingleDepositSilver = 1000;
                CryptoTraderSettings.confirmLargeTransactions = true;
                CryptoTraderSettings.largeTransactionThreshold = 500f;
                CryptoTraderSettings.toggleKey = KeyCode.Delete;
                CryptoTraderSettings.keyInputLabel = "Delete";
            }

            if (Widgets.ButtonText(new Rect(buttonRect.x + (buttonWidth + 5f) * 2, buttonRect.y, buttonWidth, 30f), "Generous"))
            {
                CryptoTraderSettings.silverToUsdRate = 20f;
                CryptoTraderSettings.maxSingleDepositSilver = 2000;
                CryptoTraderSettings.confirmLargeTransactions = false;
                CryptoTraderSettings.largeTransactionThreshold = 1000f;
                CryptoTraderSettings.toggleKey = KeyCode.F5;
                CryptoTraderSettings.keyInputLabel = "F5";
            }

            if (Widgets.ButtonText(new Rect(buttonRect.x + (buttonWidth + 5f) * 3, buttonRect.y, buttonWidth, 30f), "Reset All"))
            {
                CryptoTraderSettings.ResetToDefaults();
            }

            listing.Gap();

            // Instructions
            listing.Label("Changes are applied immediately and saved automatically.");
            GUI.color = Color.gray;
            listing.Label($"Current hotkey: {CryptoTraderSettings.keyInputLabel} (customizable above)");
            GUI.color = Color.white;

            listing.End();

            // Apply settings whenever they change
            CryptoTraderSettings.ApplySettings();
        }

        public override string SettingsCategory()
        {
            return "Crypto Trader";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            CryptoTraderSettings.ApplySettings();
        }
    }
}
