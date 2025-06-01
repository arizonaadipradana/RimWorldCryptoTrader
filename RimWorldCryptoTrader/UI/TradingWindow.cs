using RimWorldCryptoTrader.Models;
using RimWorldCryptoTrader.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldCryptoTrader.UI
{
    public class TradingWindow : Window
    {
        private CryptoPrice currentPrice;
        private List<CandleData> candleData = new List<CandleData>();
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 5f; // Update every 5 seconds

        private string depositAmount = "100";
        private string withdrawAmount = "100";
        private string buyAmount = "50";
        private string sellAmount = "0.001";

        public override Vector2 InitialSize => new Vector2(1000f, 700f);

        public TradingWindow()
        {
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = false;

            // Initial data fetch
            FetchDataAsync();
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                // Update data periodically
                if (Time.time - lastUpdateTime > UPDATE_INTERVAL)
                {
                    FetchDataAsync();
                    lastUpdateTime = Time.time;
                }
                
                DoWindowContentsInternal(inRect);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[CryptoTrader] Error in DoWindowContents: {ex.Message}");
                // Close window on error to prevent further issues
                this.Close();
            }
        }
        
        private void DoWindowContentsInternal(Rect inRect)
        {

            var playerData = TradingService.GetPlayerData();
            Text.Font = GameFont.Medium;

            // Title
            var titleRect = new Rect(0f, 0f, inRect.width, 40f);
            Widgets.Label(titleRect, "Bitcoin Trading Terminal");

            Text.Font = GameFont.Small;
            float yPos = 50f;

            // Current Price Section
            DrawPriceSection(inRect, ref yPos);

            // Chart Section (simplified for now)
            DrawChartSection(inRect, ref yPos);

            // Account Info Section
            DrawAccountSection(inRect, ref yPos, playerData);

            // Trading Section
            DrawTradingSection(inRect, ref yPos, playerData);

            // Transaction History
            DrawTransactionHistory(inRect, ref yPos, playerData);
        }

        private void DrawPriceSection(Rect inRect, ref float yPos)
        {
            var priceRect = new Rect(0f, yPos, inRect.width, 60f);
            Widgets.DrawBoxSolid(priceRect, Color.black);

            if (currentPrice != null)
            {
                Text.Font = GameFont.Medium;
                var priceText = $"BTC/USDT: {currentPrice.FormattedPrice}";
                var changeColor = currentPrice.ChangePercent24h >= 0 ? Color.green : Color.red;
                var changeText = $"24h: {currentPrice.ChangePercent24h:F2}%";

                Widgets.Label(new Rect(10f, yPos + 5f, 300f, 30f), priceText);
                GUI.color = changeColor;
                Widgets.Label(new Rect(10f, yPos + 30f, 300f, 25f), changeText);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
            else
            {
                Widgets.Label(new Rect(10f, yPos + 20f, 200f, 25f), "Loading price data...");
            }

            yPos += 70f;
        }

        private void DrawChartSection(Rect inRect, ref float yPos)
        {
            var chartRect = new Rect(0f, yPos, inRect.width, 200f);
            Widgets.DrawBoxSolid(chartRect, Color.grey);

            // Simple candlestick chart
            if (candleData.Count > 0)
            {
                DrawSimpleCandlestickChart(chartRect);
            }
            else
            {
                Widgets.Label(new Rect(chartRect.x + 10f, chartRect.y + 90f, 200f, 25f), "Loading chart data...");
            }

            yPos += 210f;
        }

        private void DrawSimpleCandlestickChart(Rect chartRect)
        {
            if (candleData.Count < 2) return;

            var minPrice = decimal.MaxValue;
            var maxPrice = decimal.MinValue;

            foreach (var candle in candleData)
            {
                if (candle.Low < minPrice) minPrice = candle.Low;
                if (candle.High > maxPrice) maxPrice = candle.High;
            }

            var priceRange = maxPrice - minPrice;
            if (priceRange == 0) return;

            var candleWidth = (chartRect.width - 20f) / candleData.Count;

            for (int i = 0; i < candleData.Count; i++)
            {
                var candle = candleData[i];
                var x = chartRect.x + 10f + (i * candleWidth);

                var highY = chartRect.y + 10f + (float)((maxPrice - candle.High) / priceRange) * (chartRect.height - 20f);
                var lowY = chartRect.y + 10f + (float)((maxPrice - candle.Low) / priceRange) * (chartRect.height - 20f);
                var openY = chartRect.y + 10f + (float)((maxPrice - candle.Open) / priceRange) * (chartRect.height - 20f);
                var closeY = chartRect.y + 10f + (float)((maxPrice - candle.Close) / priceRange) * (chartRect.height - 20f);

                // Draw wick
                Widgets.DrawLine(new Vector2(x + candleWidth / 2, highY), new Vector2(x + candleWidth / 2, lowY), Color.white, 1f);

                // Draw body
                var bodyColor = candle.Close >= candle.Open ? Color.green : Color.red;
                var bodyRect = new Rect(x + 1f, Math.Min(openY, closeY), candleWidth - 2f, Math.Abs(closeY - openY));
                Widgets.DrawBoxSolid(bodyRect, bodyColor);
            }
        }

        private void DrawAccountSection(Rect inRect, ref float yPos, PlayerCryptoData playerData)
        {
            Widgets.Label(new Rect(0f, yPos, 200f, 25f), "Account Information:");
            yPos += 30f;

            Widgets.Label(new Rect(20f, yPos, 300f, 25f), $"Deposited USD: ${playerData.GoldDeposited:F2}");
            yPos += 25f;

            Widgets.Label(new Rect(20f, yPos, 300f, 25f), $"BTC Holdings: {playerData.BTCHoldings:F8}");
            yPos += 25f;

            if (currentPrice != null)
            {
                var currentValue = playerData.CurrentValue(currentPrice.PriceUSDT);
                var profitLoss = playerData.ProfitLoss(currentPrice.PriceUSDT);
                var profitLossPercent = playerData.ProfitLossPercentage(currentPrice.PriceUSDT);

                Widgets.Label(new Rect(20f, yPos, 300f, 25f), $"Current Value: ${currentValue:F2}");
                yPos += 25f;

                GUI.color = profitLoss >= 0 ? Color.green : Color.red;
                Widgets.Label(new Rect(20f, yPos, 300f, 25f), $"P&L: ${profitLoss:F2} ({profitLossPercent:F2}%)");
                GUI.color = Color.white;
            }

            yPos += 35f;
        }

        private void DrawTradingSection(Rect inRect, ref float yPos, PlayerCryptoData playerData)
        {
            Widgets.Label(new Rect(0f, yPos, 200f, 25f), "Trading:");
            yPos += 30f;

            // Deposit/Withdraw
            Widgets.Label(new Rect(20f, yPos, 100f, 25f), "Deposit Gold:");
            depositAmount = Widgets.TextField(new Rect(130f, yPos, 80f, 25f), depositAmount);
            if (Widgets.ButtonText(new Rect(220f, yPos, 80f, 25f), "Deposit"))
            {
                if (int.TryParse(depositAmount, out int amount))
                {
                    TradingService.DepositGold(amount);
                }
            }
            yPos += 30f;

            Widgets.Label(new Rect(20f, yPos, 100f, 25f), "Withdraw USD:");
            withdrawAmount = Widgets.TextField(new Rect(130f, yPos, 80f, 25f), withdrawAmount);
            if (Widgets.ButtonText(new Rect(220f, yPos, 80f, 25f), "Withdraw"))
            {
                if (decimal.TryParse(withdrawAmount, out decimal amount))
                {
                    TradingService.WithdrawGold(amount);
                }
            }
            yPos += 40f;

            // Buy/Sell BTC
            if (currentPrice != null)
            {
                Widgets.Label(new Rect(20f, yPos, 100f, 25f), "Buy USD Amount:");
                buyAmount = Widgets.TextField(new Rect(130f, yPos, 80f, 25f), buyAmount);
                if (Widgets.ButtonText(new Rect(220f, yPos, 80f, 25f), "Buy BTC"))
                {
                    if (decimal.TryParse(buyAmount, out decimal amount))
                    {
                        TradingService.BuyBTC(amount, currentPrice.PriceUSDT);
                    }
                }
                yPos += 30f;

                Widgets.Label(new Rect(20f, yPos, 100f, 25f), "Sell BTC Amount:");
                sellAmount = Widgets.TextField(new Rect(130f, yPos, 80f, 25f), sellAmount);
                if (Widgets.ButtonText(new Rect(220f, yPos, 80f, 25f), "Sell BTC"))
                {
                    if (decimal.TryParse(sellAmount, out decimal amount))
                    {
                        TradingService.SellBTC(amount, currentPrice.PriceUSDT);
                    }
                }
            }

            yPos += 40f;
        }

        private void DrawTransactionHistory(Rect inRect, ref float yPos, PlayerCryptoData playerData)
        {
            Widgets.Label(new Rect(0f, yPos, 200f, 25f), "Recent Transactions:");
            yPos += 30f;

            var historyRect = new Rect(0f, yPos, inRect.width, inRect.height - yPos - 40f);
            Widgets.DrawBoxSolid(historyRect, Color.black);

            float transactionY = historyRect.y + 5f;
            var recentTransactions = playerData.Transactions.Count > 10 ?
                playerData.Transactions.GetRange(playerData.Transactions.Count - 10, 10) :
                playerData.Transactions;

            foreach (var transaction in recentTransactions)
            {
                var color = transaction.Type == "BUY" ? Color.green : Color.red;
                GUI.color = color;
                Widgets.Label(new Rect(historyRect.x + 5f, transactionY, historyRect.width - 10f, 20f),
                    $"{transaction.Type} {transaction.Amount:F8} BTC @ ${transaction.Price:F2} - {transaction.Timestamp}");
                GUI.color = Color.white;
                transactionY += 22f;
            }
        }

        private void FetchDataAsync()
        {
            try
            {
                // Fetch current price
                BinanceApiService.GetCurrentPriceAsync(price => {
                    currentPrice = price;
                });
                
                // Fetch candle data
                BinanceApiService.GetKlineDataAsync(candles => {
                    candleData = candles ?? new List<CandleData>();
                }, "1m", 50);
            }
            catch (Exception ex)
            {
                Log.Error($"[CryptoTrader] Error fetching data: {ex.Message}");
            }
        }
    }
}