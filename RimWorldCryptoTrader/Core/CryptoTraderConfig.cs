using System;
using Verse;

namespace RimWorldCryptoTrader.Core
{
    /// <summary>
    /// Configuration settings for the Crypto Trader mod
    /// </summary>
    public static class CryptoTraderConfig
    {
        /// <summary>
        /// Conversion rate from silver to USD for crypto trading
        /// This is now controlled by mod settings
        /// </summary>
        public static float SilverToUsdRate 
        { 
            get => CryptoTraderSettings.silverToUsdRate;
            set => CryptoTraderSettings.silverToUsdRate = value;
        }
        
        /// <summary>
        /// Minimum silver required to start trading (safety check)
        /// This is now controlled by mod settings
        /// </summary>
        public static int MinimumSilverToTrade 
        { 
            get => CryptoTraderSettings.minimumSilverToTrade;
            set => CryptoTraderSettings.minimumSilverToTrade = value;
        }
        
        /// <summary>
        /// Maximum single deposit amount in silver (prevents accidental huge deposits)
        /// This is now controlled by mod settings
        /// </summary>
        public static int MaxSingleDepositSilver 
        { 
            get => CryptoTraderSettings.maxSingleDepositSilver;
            set => CryptoTraderSettings.maxSingleDepositSilver = value;
        }

        /// <summary>
        /// Update interval for real-time price updates
        /// </summary>
        public static float UpdateIntervalSeconds => CryptoTraderSettings.updateIntervalSeconds;

        /// <summary>
        /// Whether to enable real-time updates
        /// </summary>
        public static bool EnableRealTimeUpdates => CryptoTraderSettings.enableRealTimeUpdates;

        /// <summary>
        /// Whether to show detailed price information
        /// </summary>
        public static bool ShowDetailedPrices => CryptoTraderSettings.showDetailedPrices;

        /// <summary>
        /// Whether to confirm large transactions
        /// </summary>
        public static bool ConfirmLargeTransactions => CryptoTraderSettings.confirmLargeTransactions;

        /// <summary>
        /// Threshold for what constitutes a "large" transaction
        /// </summary>
        public static float LargeTransactionThreshold => CryptoTraderSettings.largeTransactionThreshold;

        /// <summary>
        /// Whether trading limits are enabled
        /// </summary>
        public static bool EnableTradingLimits => CryptoTraderSettings.enableTradingLimits;

        /// <summary>
        /// Whether debug logging is enabled
        /// </summary>
        public static bool EnableDebugLogging => CryptoTraderSettings.enableDebugLogging;
        
        /// <summary>
        /// Apply the conversion rate configuration
        /// Call this method to change rates on the fly
        /// </summary>
        public static void SetConversionRate(ConversionRatePreset preset)
        {
            switch (preset)
            {
                case ConversionRatePreset.Conservative:
                    SilverToUsdRate = 5f;
                    break;
                case ConversionRatePreset.Balanced:
                    SilverToUsdRate = 10f;
                    break;
                case ConversionRatePreset.Generous:
                    SilverToUsdRate = 20f;
                    break;
                case ConversionRatePreset.VeryGenerous:
                    SilverToUsdRate = 50f;
                    break;
                case ConversionRatePreset.Original:
                    SilverToUsdRate = 1f;
                    break;
                default:
                    SilverToUsdRate = 10f;
                    break;
            }
            
            Log.Message($"[CryptoTrader] Conversion rate set to 1 silver = ${SilverToUsdRate} USD");
        }
        
        /// <summary>
        /// Get conversion rate information for display
        /// </summary>
        public static string GetConversionRateInfo()
        {
            var silverFor1000Usd = (int)(1000f / SilverToUsdRate);
            return $"Current rate: 1 silver = ${SilverToUsdRate} USD\n" +
                   $"To invest $1,000: {silverFor1000Usd} silver needed";
        }

        /// <summary>
        /// Check if a transaction amount is considered "large"
        /// </summary>
        public static bool IsLargeTransaction(float usdAmount)
        {
            return ConfirmLargeTransactions && usdAmount >= LargeTransactionThreshold;
        }

        /// <summary>
        /// Log debug message if debug logging is enabled
        /// </summary>
        public static void DebugLog(string message)
        {
            if (EnableDebugLogging)
            {
                Log.Message($"[CryptoTrader DEBUG] {message}");
            }
        }
    }
    
    public enum ConversionRatePreset
    {
        Original,      // 1 silver = $1 USD (original mod)
        Conservative,  // 1 silver = $5 USD  
        Balanced,      // 1 silver = $10 USD (recommended)
        Generous,      // 1 silver = $20 USD
        VeryGenerous   // 1 silver = $50 USD
    }
}
