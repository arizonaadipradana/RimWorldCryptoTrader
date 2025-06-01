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
        /// </summary>
        public static float SilverToUsdRate = 10f;
        
        /// <summary>
        /// Minimum silver required to start trading (safety check)
        /// </summary>
        public static int MinimumSilverToTrade = 10;
        
        /// <summary>
        /// Maximum single deposit amount in silver (prevents accidental huge deposits)
        /// </summary>
        public static int MaxSingleDepositSilver = 1000;
        
        /// <summary>
        /// Apply the conversion rate configuration
        /// Call this method to change rates on the fly
        /// </summary>
        public static void SetConversionRate(ConversionRatePreset preset)
        {
            switch (preset)
            {
                case ConversionRatePreset.Conservative:
                    SilverToUsdRate = 10f;
                    break;
                case ConversionRatePreset.Balanced:
                    SilverToUsdRate = 15f;
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
                    SilverToUsdRate = 15f;
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
    }
    
    public enum ConversionRatePreset
    {
        Original,      // 1 silver = $1 USD (original mod)
        Conservative,  // 1 silver = $10 USD  
        Balanced,      // 1 silver = $15 USD (recommended)
        Generous,      // 1 silver = $20 USD
        VeryGenerous   // 1 silver = $50 USD
    }
}
