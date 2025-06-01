using RimWorld;
using RimWorldCryptoTrader.Models;
using RimWorldCryptoTrader.Core;
using System;
using System.Linq;
using Verse;

namespace RimWorldCryptoTrader.Services
{
    public static class TradingService
    {
        // Get current conversion rate from config
        private static float SILVER_TO_USD_RATE => CryptoTraderConfig.SilverToUsdRate;

        public static PlayerCryptoData GetPlayerData()
        {
            var playerData = Current.Game.GetComponent<PlayerCryptoData>();
            if (playerData == null)
            {
                playerData = new PlayerCryptoData(Current.Game);
                Current.Game.components.Add(playerData);
                CryptoTraderConfig.DebugLog("Initialized PlayerCryptoData component.");
            }
            return playerData;
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

            CryptoTraderConfig.DebugLog($"Colony silver count: {totalSilver}");
            return totalSilver;
        }

        public static bool DepositSilver(int silverAmount)
        {
            var playerData = GetPlayerData();
            var availableSilver = GetColonySilverCount();

            CryptoTraderConfig.DebugLog($"Attempting to deposit {silverAmount} silver. Available: {availableSilver}");

            if (availableSilver < silverAmount)
            {
                Messages.Message($"Insufficient silver. Available: {availableSilver:N0}, Required: {silverAmount:N0}", 
                    MessageTypeDefOf.RejectInput);
                return false;
            }
            
            // Safety check for large deposits
            if (silverAmount > CryptoTraderConfig.MaxSingleDepositSilver)
            {
                var usdValue = silverAmount * SILVER_TO_USD_RATE;
                Messages.Message($"Deposit amount too large! {silverAmount:N0} silver = ${usdValue:N0} USD. Maximum allowed: {CryptoTraderConfig.MaxSingleDepositSilver:N0} silver.", 
                    MessageTypeDefOf.RejectInput);
                return false;
            }
            
            // Minimum deposit check
            if (silverAmount < CryptoTraderConfig.MinimumSilverToTrade)
            {
                Messages.Message($"Minimum deposit: {CryptoTraderConfig.MinimumSilverToTrade} silver (${CryptoTraderConfig.MinimumSilverToTrade * SILVER_TO_USD_RATE} USD)", 
                    MessageTypeDefOf.RejectInput);
                return false;
            }

            var usdAmount = silverAmount * SILVER_TO_USD_RATE;

            // Check for large transaction confirmation
            if (CryptoTraderConfig.IsLargeTransaction(usdAmount))
            {
                var confirmMessage = $"Large deposit: {silverAmount:N0} silver (${usdAmount:N0} USD). Continue?";
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    confirmMessage,
                    () => ExecuteDeposit(silverAmount, usdAmount),
                    false,
                    "Confirm Large Deposit"
                ));
                return true; // Return true since we're handling it asynchronously
            }

            return ExecuteDeposit(silverAmount, usdAmount);
        }

        private static bool ExecuteDeposit(int silverAmount, float usdAmount)
        {
            var playerData = GetPlayerData();

            // Actually consume silver from the colony
            if (ConsumeSilverFromColony(silverAmount))
            {
                playerData.SilverDeposited += usdAmount;
                Messages.Message($"Deposited {silverAmount:N0} silver → ${usdAmount:N0} USD (rate: 1 silver = ${SILVER_TO_USD_RATE} USD) to crypto exchange.",
                    MessageTypeDefOf.PositiveEvent);
                CryptoTraderConfig.DebugLog($"Successfully deposited {silverAmount} silver for ${usdAmount} USD");
                return true;
            }
            else
            {
                Messages.Message("Failed to deduct silver from colony storage.", MessageTypeDefOf.RejectInput);
                return false;
            }
        }

        public static bool WithdrawSilver(float usdAmount)
        {
            var playerData = GetPlayerData();

            CryptoTraderConfig.DebugLog($"Attempting to withdraw ${usdAmount} USD. Available: ${playerData.SilverDeposited}");

            if (playerData.SilverDeposited < usdAmount)
            {
                Messages.Message("Insufficient deposited funds.", MessageTypeDefOf.RejectInput);
                return false;
            }

            // Check for large transaction confirmation
            if (CryptoTraderConfig.IsLargeTransaction(usdAmount))
            {
                var silverAmount = (int)(usdAmount / SILVER_TO_USD_RATE);
                var confirmMessage = $"Large withdrawal: ${usdAmount:N0} USD ({silverAmount:N0} silver). Continue?";
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    confirmMessage,
                    () => ExecuteWithdrawal(usdAmount),
                    false,
                    "Confirm Large Withdrawal"
                ));
                return true; // Return true since we're handling it asynchronously
            }

            return ExecuteWithdrawal(usdAmount);
        }

        private static bool ExecuteWithdrawal(float usdAmount)
        {
            var playerData = GetPlayerData();
            var silverAmount = (int)(usdAmount / SILVER_TO_USD_RATE);
            
            playerData.SilverDeposited -= usdAmount;

            // Add silver back to colony
            SpawnSilverInColony(silverAmount);

            Messages.Message($"Withdrew ${usdAmount:F0} USD → {silverAmount} silver (rate: 1 silver = ${SILVER_TO_USD_RATE} USD) from crypto exchange.",
                MessageTypeDefOf.PositiveEvent);
            CryptoTraderConfig.DebugLog($"Successfully withdrew ${usdAmount} USD for {silverAmount} silver");
            return true;
        }

        public static bool BuyCrypto(string symbol, float usdAmount, float cryptoPrice)
        {
            var playerData = GetPlayerData();

            CryptoTraderConfig.DebugLog($"Attempting to buy {symbol} for ${usdAmount} at price ${cryptoPrice}");

            if (playerData.SilverDeposited < usdAmount)
            {
                Messages.Message("Insufficient deposited funds for purchase.", MessageTypeDefOf.RejectInput);
                return false;
            }

            // Check for large transaction confirmation
            if (CryptoTraderConfig.IsLargeTransaction(usdAmount))
            {
                var cryptoAmount = usdAmount / cryptoPrice;
                var cryptoName = GetCryptoDisplayName(symbol);
                var confirmMessage = $"Large purchase: ${usdAmount:N0} USD worth of {cryptoName} ({cryptoAmount:F8} {cryptoName}). Continue?";
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    confirmMessage,
                    () => ExecuteBuy(symbol, usdAmount, cryptoPrice),
                    false,
                    "Confirm Large Purchase"
                ));
                return true; // Return true since we're handling it asynchronously
            }

            return ExecuteBuy(symbol, usdAmount, cryptoPrice);
        }

        private static bool ExecuteBuy(string symbol, float usdAmount, float cryptoPrice)
        {
            var playerData = GetPlayerData();
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
            CryptoTraderConfig.DebugLog($"Successfully bought {cryptoAmount:F8} {symbol} for ${usdAmount}");
            return true;
        }

        public static bool SellCrypto(string symbol, float cryptoAmount, float cryptoPrice)
        {
            var playerData = GetPlayerData();
            var currentHolding = playerData.GetCryptoHolding(symbol);

            CryptoTraderConfig.DebugLog($"Attempting to sell {cryptoAmount} {symbol} at price ${cryptoPrice}. Current holding: {currentHolding}");

            if (currentHolding < cryptoAmount)
            {
                var cryptoDisplayName = GetCryptoDisplayName(symbol);
                Messages.Message($"Insufficient {cryptoDisplayName} holdings.", MessageTypeDefOf.RejectInput);
                return false;
            }

            var usdReceived = cryptoAmount * cryptoPrice;

            // Check for large transaction confirmation
            if (CryptoTraderConfig.IsLargeTransaction(usdReceived))
            {
                var cryptoName = GetCryptoDisplayName(symbol);
                var confirmMessage = $"Large sale: {cryptoAmount:F8} {cryptoName} for ${usdReceived:N0} USD. Continue?";
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    confirmMessage,
                    () => ExecuteSell(symbol, cryptoAmount, cryptoPrice),
                    false,
                    "Confirm Large Sale"
                ));
                return true; // Return true since we're handling it asynchronously
            }

            return ExecuteSell(symbol, cryptoAmount, cryptoPrice);
        }

        private static bool ExecuteSell(string symbol, float cryptoAmount, float cryptoPrice)
        {
            var playerData = GetPlayerData();
            var currentHolding = playerData.GetCryptoHolding(symbol);
            var usdReceived = cryptoAmount * cryptoPrice;

            playerData.SetCryptoHolding(symbol, currentHolding - cryptoAmount);
            playerData.SilverDeposited += usdReceived;

            // Reduce invested amount proportionally
            var currentInvested = playerData.GetTotalInvested(symbol);
            var remainingHolding = currentHolding - cryptoAmount;
            if (remainingHolding > 0)
            {
                var sellRatio = cryptoAmount / currentHolding;
                playerData.SetTotalInvested(symbol, currentInvested * (1f - sellRatio));
            }
            else
            {
                playerData.SetTotalInvested(symbol, 0f);
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
            CryptoTraderConfig.DebugLog($"Successfully sold {cryptoAmount:F8} {symbol} for ${usdReceived}");
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
            
            CryptoTraderConfig.DebugLog($"Spawned {amount} silver at {dropSpot}");
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

        public static bool WithdrawGold(float usdAmount)
        {
            return WithdrawSilver(usdAmount);
        }

        public static bool BuyBTC(float usdAmount, float btcPrice)
        {
            return BuyCrypto("BTCUSDT", usdAmount, btcPrice);
        }

        public static bool SellBTC(float btcAmount, float btcPrice)
        {
            return SellCrypto("BTCUSDT", btcAmount, btcPrice);
        }
    }
}
