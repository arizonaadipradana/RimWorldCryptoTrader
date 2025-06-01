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
            
            // Apply settings to config after loading
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                ApplySettings();
            }
        }
        
        public static void ApplySettings()
        {
            // Update the main config with settings values
            CryptoTraderConfig.SilverToUsdRate = silverToUsdRate;
            CryptoTraderConfig.MinimumSilverToTrade = minimumSilverToTrade;
            CryptoTraderConfig.MaxSingleDepositSilver = maxSingleDepositSilver;
            
            if (enableDebugLogging)
            {
                Log.Message($"[CryptoTrader] Settings applied - Rate: {silverToUsdRate}, Min: {minimumSilverToTrade}, Max: {maxSingleDepositSilver}");
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
            
            ApplySettings();
        }
    }
}
