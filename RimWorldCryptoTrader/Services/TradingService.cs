using RimWorld;
using RimWorldCryptoTrader.Models;
using System;
using Verse;

namespace RimWorldCryptoTrader.Services
{
    public static class TradingService
    {
        private const decimal GOLD_TO_USD_RATE = 1m; // 1 gold = 1 USD (adjust as needed)

        public static PlayerCryptoData GetPlayerData()
        {
            return Current.Game.GetComponent<PlayerCryptoData>();
        }

        public static bool DepositGold(int goldAmount)
        {
            var playerData = GetPlayerData();
            var map = Find.CurrentMap;

            if (map?.wealthWatcher?.WealthTotal < goldAmount)
            {
                Messages.Message("Insufficient colony wealth to deposit gold.", MessageTypeDefOf.RejectInput);
                return false;
            }

            // Deduct from colony wealth (simplified)
            map.wealthWatcher.ForceRecount();
            playerData.GoldDeposited += goldAmount * GOLD_TO_USD_RATE;

            Messages.Message($"Deposited {goldAmount} gold (${goldAmount * GOLD_TO_USD_RATE} USD) to crypto exchange.",
                MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public static bool WithdrawGold(decimal usdAmount)
        {
            var playerData = GetPlayerData();

            if (playerData.GoldDeposited < usdAmount)
            {
                Messages.Message("Insufficient deposited funds.", MessageTypeDefOf.RejectInput);
                return false;
            }

            var goldAmount = (int)(usdAmount / GOLD_TO_USD_RATE);
            playerData.GoldDeposited -= usdAmount;

            // Add gold back to colony (simplified - you might want to spawn actual items)
            Messages.Message($"Withdrew ${usdAmount} USD ({goldAmount} gold) from crypto exchange.",
                MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public static bool BuyBTC(decimal usdAmount, decimal btcPrice)
        {
            var playerData = GetPlayerData();

            if (playerData.GoldDeposited < usdAmount)
            {
                Messages.Message("Insufficient deposited funds for purchase.", MessageTypeDefOf.RejectInput);
                return false;
            }

            var btcAmount = usdAmount / btcPrice;
            playerData.GoldDeposited -= usdAmount;
            playerData.BTCHoldings += btcAmount;
            playerData.TotalInvested += usdAmount;

            // Record transaction
            playerData.Transactions.Add(new TradeTransaction
            {
                Type = "BUY",
                Amount = btcAmount,
                Price = btcPrice,
                GoldUsed = usdAmount,
                Timestamp = DateTime.Now.ToString()
            });

            Messages.Message($"Bought {btcAmount:F8} BTC for ${usdAmount:F2}", MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public static bool SellBTC(decimal btcAmount, decimal btcPrice)
        {
            var playerData = GetPlayerData();

            if (playerData.BTCHoldings < btcAmount)
            {
                Messages.Message("Insufficient BTC holdings.", MessageTypeDefOf.RejectInput);
                return false;
            }

            var usdReceived = btcAmount * btcPrice;
            playerData.BTCHoldings -= btcAmount;
            playerData.GoldDeposited += usdReceived;

            // Reduce invested amount proportionally
            var sellRatio = playerData.BTCHoldings > 0 ? btcAmount / (playerData.BTCHoldings + btcAmount) : 1m;
            playerData.TotalInvested *= (1m - sellRatio);

            // Record transaction
            playerData.Transactions.Add(new TradeTransaction
            {
                Type = "SELL",
                Amount = btcAmount,
                Price = btcPrice,
                GoldUsed = -usdReceived,
                Timestamp = DateTime.Now.ToString()
            });

            Messages.Message($"Sold {btcAmount:F8} BTC for ${usdReceived:F2}", MessageTypeDefOf.PositiveEvent);
            return true;
        }
    }
}