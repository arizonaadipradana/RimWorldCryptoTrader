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

            // Conversion Rate Settings
            listing.Label("Conversion Rate Settings:");
            listing.Gap(6f);

            listing.Label($"Silver to USD Rate: {CryptoTraderSettings.silverToUsdRate:F1}");
            CryptoTraderSettings.silverToUsdRate = listing.Slider(CryptoTraderSettings.silverToUsdRate, 1f, 100f);
            listing.Gap(6f);

            var rateInfo = $"1 silver = ${CryptoTraderSettings.silverToUsdRate:F1} USD (${1000f / CryptoTraderSettings.silverToUsdRate:F0} silver for $1000)";
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
                CryptoTraderSettings.updateIntervalSeconds = listing.Slider(CryptoTraderSettings.updateIntervalSeconds, 1f, 30f);
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
                CryptoTraderSettings.largeTransactionThreshold = listing.Slider(CryptoTraderSettings.largeTransactionThreshold, 100f, 5000f);
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
            }

            if (Widgets.ButtonText(new Rect(buttonRect.x + buttonWidth + 5f, buttonRect.y, buttonWidth, 30f), "Balanced"))
            {
                CryptoTraderSettings.silverToUsdRate = 10f;
                CryptoTraderSettings.maxSingleDepositSilver = 1000;
                CryptoTraderSettings.confirmLargeTransactions = true;
                CryptoTraderSettings.largeTransactionThreshold = 500f;
            }

            if (Widgets.ButtonText(new Rect(buttonRect.x + (buttonWidth + 5f) * 2, buttonRect.y, buttonWidth, 30f), "Generous"))
            {
                CryptoTraderSettings.silverToUsdRate = 20f;
                CryptoTraderSettings.maxSingleDepositSilver = 2000;
                CryptoTraderSettings.confirmLargeTransactions = false;
                CryptoTraderSettings.largeTransactionThreshold = 1000f;
            }

            if (Widgets.ButtonText(new Rect(buttonRect.x + (buttonWidth + 5f) * 3, buttonRect.y, buttonWidth, 30f), "Reset"))
            {
                CryptoTraderSettings.ResetToDefaults();
            }

            listing.Gap();

            // Instructions
            listing.Label("Changes are applied immediately and saved automatically.");
            GUI.color = Color.gray;
            listing.Label("Access the trading terminal with the assigned hotkey (configurable in Controls).");
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
