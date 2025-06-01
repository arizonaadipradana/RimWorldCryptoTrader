using RimWorld;
using RimWorldCryptoTrader.Models;
using System;
using System.Linq;
using Verse;

namespace RimWorldCryptoTrader.Services
{
    public static class TradingService
    {
        private const decimal SILVER_TO_USD_RATE = 1m; // 1 silver = 1 USD (adjust as needed)

        public static PlayerCryptoData GetPlayerData()
        {
            return Current.Game.GetComponent<PlayerCryptoData>();
        }

        public static int GetColonySilverCount()
        {
            var map = Find.CurrentMap;
            if (map == null) return 0;

            // Count silver in all stockpiles and inventory
            var silverDef = ThingDefOf.Silver;
            int totalSilver = 0;

            // Check all maps for silver
            foreach (var currentMap in Find.Maps)
            {
                // Count silver in storage and colonist inventory
                var silverItems = currentMap.listerThings.ThingsOfDef(silverDef);
                totalSilver += silverItems.Sum(thing => thing.stackCount);
            }

            return totalSilver;
        }

        public static bool DepositSilver(int silverAmount)
        {
            var playerData = GetPlayerData();
            var availableSilver = GetColonySilverCount();

            if (availableSilver < silverAmount)
            {
                Messages.Message($"Insufficient silver. Available: {availableSilver}, Required: {silverAmount}", 
                    MessageTypeDefOf.RejectInput);
                return false;
            }

            // Actually consume silver from the colony
            if (ConsumeSilverFromColony(silverAmount))
            {
                playerData.SilverDeposited += silverAmount * SILVER_TO_USD_RATE;
                Messages.Message($"Deposited {silverAmount} silver (${silverAmount * SILVER_TO_USD_RATE} USD) to crypto exchange.",
                    MessageTypeDefOf.PositiveEvent);
                return true;
            }
            else
            {
                Messages.Message("Failed to deduct silver from colony storage.", MessageTypeDefOf.RejectInput);
                return false;
            }
        }

        public static bool WithdrawSilver(decimal usdAmount)
        {
            var playerData = GetPlayerData();

            if (playerData.SilverDeposited < usdAmount)
            {
                Messages.Message("Insufficient deposited funds.", MessageTypeDefOf.RejectInput);
                return false;
            }

            var silverAmount = (int)(usdAmount / SILVER_TO_USD_RATE);
            playerData.SilverDeposited -= usdAmount;

            // Add silver back to colony
            SpawnSilverInColony(silverAmount);

            Messages.Message($"Withdrew ${usdAmount} USD ({silverAmount} silver) from crypto exchange.",
                MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public static bool BuyCrypto(string symbol, decimal usdAmount, decimal cryptoPrice)
        {
            var playerData = GetPlayerData();

            if (playerData.SilverDeposited < usdAmount)
            {
                Messages.Message("Insufficient deposited funds for purchase.", MessageTypeDefOf.RejectInput);
                return false;
            }

            var cryptoAmount = usdAmount / cryptoPrice;
            playerData.SilverDeposited -= usdAmount;
            
            var currentHolding = playerData.GetCryptoHolding(symbol);
            playerData.SetCryptoHolding(symbol, currentHolding + cryptoAmount);
            
            var currentInvested = playerData.GetTotalInvested(symbol);
            playerData.SetTotalInvested(symbol, currentInvested + usdAmount);

            // Add to tracked cryptos if not already there
            playerData.AddTrackedCrypto(symbol);

            // Record transaction
            playerData.Transactions.Add(new TradeTransaction
            {
                Type = "BUY",
                Symbol = symbol,
                Amount = cryptoAmount,
                Price = cryptoPrice,
                SilverUsed = usdAmount,
                Timestamp = DateTime.Now.ToString()
            });

            var buyDisplayName = GetCryptoDisplayName(symbol);
            Messages.Message($"Bought {cryptoAmount:F8} {buyDisplayName} for ${usdAmount:F2}", MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public static bool SellCrypto(string symbol, decimal cryptoAmount, decimal cryptoPrice)
        {
            var playerData = GetPlayerData();
            var currentHolding = playerData.GetCryptoHolding(symbol);

            if (currentHolding < cryptoAmount)
            {
                var cryptoDisplayName = GetCryptoDisplayName(symbol);
                Messages.Message($"Insufficient {cryptoDisplayName} holdings.", MessageTypeDefOf.RejectInput);
                return false;
            }

            var usdReceived = cryptoAmount * cryptoPrice;
            playerData.SetCryptoHolding(symbol, currentHolding - cryptoAmount);
            playerData.SilverDeposited += usdReceived;

            // Reduce invested amount proportionally
            var currentInvested = playerData.GetTotalInvested(symbol);
            var remainingHolding = currentHolding - cryptoAmount;
            if (remainingHolding > 0)
            {
                var sellRatio = cryptoAmount / currentHolding;
                playerData.SetTotalInvested(symbol, currentInvested * (1m - sellRatio));
            }
            else
            {
                playerData.SetTotalInvested(symbol, 0m);
            }

            // Record transaction
            playerData.Transactions.Add(new TradeTransaction
            {
                Type = "SELL",
                Symbol = symbol,
                Amount = cryptoAmount,
                Price = cryptoPrice,
                SilverUsed = -usdReceived,
                Timestamp = DateTime.Now.ToString()
            });

            var sellDisplayName = GetCryptoDisplayName(symbol);
            Messages.Message($"Sold {cryptoAmount:F8} {sellDisplayName} for ${usdReceived:F2}", MessageTypeDefOf.PositiveEvent);
            return true;
        }

        private static bool ConsumeSilverFromColony(int amount)
        {
            var silverDef = ThingDefOf.Silver;
            int remaining = amount;

            foreach (var map in Find.Maps)
            {
                if (remaining <= 0) break;

                var silverItems = map.listerThings.ThingsOfDef(silverDef).ToList();
                foreach (var thing in silverItems)
                {
                    if (remaining <= 0) break;

                    var toTake = Math.Min(remaining, thing.stackCount);
                    thing.SplitOff(toTake).Destroy();
                    remaining -= toTake;
                }
            }

            return remaining == 0;
        }

        private static void SpawnSilverInColony(int amount)
        {
            var map = Find.CurrentMap;
            if (map == null) return;

            var dropSpot = DropCellFinder.TradeDropSpot(map);
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = amount;
            
            GenPlace.TryPlaceThing(silver, dropSpot, map, ThingPlaceMode.Near);
        }

        public static string GetCryptoDisplayName(string symbol)
        {
            switch (symbol)
            {
                case "BTCUSDT":
                    return "BTC";
                case "ETHUSDT":
                    return "ETH";
                case "XLMUSDT":
                    return "XLM";
                case "LTCUSDT":
                    return "LTC";
                case "DOGEUSDT":
                    return "DOGE";
                case "ADAUSDT":
                    return "ADA";
                case "DOTUSDT":
                    return "DOT";
                case "LINKUSDT":
                    return "LINK";
                case "BNBUSDT":
                    return "BNB";
                case "SOLUSDT":
                    return "SOL";
                default:
                    return symbol.Replace("USDT", "");
            }
        }

        public static bool IsValidCryptoSymbol(string symbol)
        {
            // Basic validation - should end with USDT and have a valid base currency
            if (!symbol.EndsWith("USDT") || symbol.Length <= 4)
                return false;

            // Additional validation could be added here
            return true;
        }

        // Legacy methods for backward compatibility
        public static bool DepositGold(int goldAmount)
        {
            return DepositSilver(goldAmount);
        }

        public static bool WithdrawGold(decimal usdAmount)
        {
            return WithdrawSilver(usdAmount);
        }

        public static bool BuyBTC(decimal usdAmount, decimal btcPrice)
        {
            return BuyCrypto("BTCUSDT", usdAmount, btcPrice);
        }

        public static bool SellBTC(decimal btcAmount, decimal btcPrice)
        {
            return SellCrypto("BTCUSDT", btcAmount, btcPrice);
        }
    }
}
