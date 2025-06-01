using RimWorldCryptoTrader.Models;
using RimWorldCryptoTrader.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldCryptoTrader.UI
{
    public class TradingWindow : Window
    {
        private enum Tab
        {
            Trading,
            Portfolio,
            AddCrypto
        }

        private Tab currentTab = Tab.Trading;
        private Dictionary<string, CryptoPrice> currentPrices = new Dictionary<string, CryptoPrice>();
        private Dictionary<string, List<CandleData>> candleData = new Dictionary<string, List<CandleData>>();
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 10f; // Update every 10 seconds

        // Trading tab variables
        private string selectedCrypto = "BTCUSDT";
        private string depositAmount = "100";
        private string withdrawAmount = "100";
        private string buyAmount = "50";
        private string sellAmount = "0.001";

        // Add crypto tab variables
        private string newCryptoSymbol = "";
        private string feedbackMessage = "";
        private float feedbackMessageTime = 0f;

        public override Vector2 InitialSize => new Vector2(1200f, 800f);

        public TradingWindow()
        {
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = false;

            // Initial data fetch
            FetchAllDataAsync();
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                // Update data periodically
                if (Time.time - lastUpdateTime > UPDATE_INTERVAL)
                {
                    FetchAllDataAsync();
                    lastUpdateTime = Time.time;
                }
                
                DoWindowContentsInternal(inRect);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[CryptoTrader] Error in DoWindowContents: {ex.Message}");
                this.Close();
            }
        }
        
        private void DoWindowContentsInternal(Rect inRect)
        {
            var playerData = TradingService.GetPlayerData();
            Text.Font = GameFont.Medium;

            // Title
            var titleRect = new Rect(0f, 0f, inRect.width, 40f);
            Widgets.Label(titleRect, "Crypto Trading Terminal");

            Text.Font = GameFont.Small;

            // Tab buttons
            DrawTabButtons(inRect);

            // Main content area
            var contentRect = new Rect(0f, 80f, inRect.width, inRect.height - 80f);
            
            switch (currentTab)
            {
                case Tab.Trading:
                    DrawTradingTab(contentRect, playerData);
                    break;
                case Tab.Portfolio:
                    DrawPortfolioTab(contentRect, playerData);
                    break;
                case Tab.AddCrypto:
                    DrawAddCryptoTab(contentRect, playerData);
                    break;
            }

            // Show feedback message if any
            if (Time.time - feedbackMessageTime < 3f && !string.IsNullOrEmpty(feedbackMessage))
            {
                var feedbackRect = new Rect(inRect.width - 300f, inRect.height - 50f, 290f, 30f);
                Widgets.DrawBoxSolid(feedbackRect, Color.black);
                Widgets.Label(feedbackRect, feedbackMessage);
            }
        }

        private void DrawTabButtons(Rect inRect)
        {
            var tabWidth = 120f;
            var tabHeight = 30f;
            var yPos = 50f;

            // Trading tab
            var tradingTabRect = new Rect(0f, yPos, tabWidth, tabHeight);
            if (currentTab == Tab.Trading)
                Widgets.DrawBoxSolid(tradingTabRect, Color.grey);
            if (Widgets.ButtonText(tradingTabRect, "Trading"))
                currentTab = Tab.Trading;

            // Portfolio tab
            var portfolioTabRect = new Rect(tabWidth + 5f, yPos, tabWidth, tabHeight);
            if (currentTab == Tab.Portfolio)
                Widgets.DrawBoxSolid(portfolioTabRect, Color.grey);
            if (Widgets.ButtonText(portfolioTabRect, "Portfolio"))
                currentTab = Tab.Portfolio;

            // Add Crypto tab
            var addCryptoTabRect = new Rect((tabWidth + 5f) * 2, yPos, tabWidth, tabHeight);
            if (currentTab == Tab.AddCrypto)
                Widgets.DrawBoxSolid(addCryptoTabRect, Color.grey);
            if (Widgets.ButtonText(addCryptoTabRect, "Add Crypto"))
                currentTab = Tab.AddCrypto;
        }

        private void DrawTradingTab(Rect contentRect, PlayerCryptoData playerData)
        {
            float yPos = 10f;

            // Account summary
            DrawAccountSummary(contentRect, ref yPos, playerData);

            // Crypto selector
            DrawCryptoSelector(contentRect, ref yPos, playerData);

            // Current price section
            DrawCurrentPriceSection(contentRect, ref yPos);

            // Chart section
            DrawChartSection(contentRect, ref yPos);

            // Trading controls
            DrawTradingControls(contentRect, ref yPos, playerData);
        }

        private void DrawAccountSummary(Rect contentRect, ref float yPos, PlayerCryptoData playerData)
        {
            Widgets.Label(new Rect(750f, yPos, 200f, 25f), "Account Summary:");
            yPos += 30f;

            // Colony silver count
            var colonySilver = TradingService.GetColonySilverCount();
            Widgets.Label(new Rect(770f, yPos, 300f, 25f), $"Colony Silver: {colonySilver:N0}");
            yPos += 25f;

            Widgets.Label(new Rect(770f, yPos, 300f, 25f), $"Deposited USD: ${playerData.SilverDeposited:F2}");
            yPos += 25f;

            var totalPortfolioValue = playerData.GetTotalPortfolioValue(currentPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PriceUSDT));
            Widgets.Label(new Rect(770f, yPos, 300f, 25f), $"Total Portfolio: ${totalPortfolioValue:F2}");
            yPos += 25f;

            var totalPL = playerData.GetTotalPortfolioProfitLoss(currentPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PriceUSDT));
            GUI.color = totalPL >= 0 ? Color.green : Color.red;
            Widgets.Label(new Rect(770f, yPos, 300f, 25f), $"Total P&L: ${totalPL:F2}");
            GUI.color = Color.white;

            yPos += 40f;
        }

        private void DrawCryptoSelector(Rect contentRect, ref float yPos, PlayerCryptoData playerData)
        {
            Widgets.Label(new Rect(0f, yPos, 200f, 25f), "Select Cryptocurrency:");
            yPos += 30f;

            var dropdownRect = new Rect(20f, yPos, 200f, 30f);
            if (Widgets.ButtonText(dropdownRect, TradingService.GetCryptoDisplayName(selectedCrypto)))
            {
                var options = new List<FloatMenuOption>();
                foreach (var crypto in playerData.TrackedCryptos)
                {
                    var displayName = TradingService.GetCryptoDisplayName(crypto);
                    options.Add(new FloatMenuOption(displayName, () => selectedCrypto = crypto));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            yPos += 40f;
        }

        private void DrawCurrentPriceSection(Rect contentRect, ref float yPos)
        {
            var priceRect = new Rect(0f, yPos, contentRect.width, 60f);
            Widgets.DrawBoxSolid(priceRect, Color.black);

            if (currentPrices.ContainsKey(selectedCrypto))
            {
                var price = currentPrices[selectedCrypto];
                Text.Font = GameFont.Medium;
                var cryptoName = TradingService.GetCryptoDisplayName(selectedCrypto);
                var priceText = $"{cryptoName}/USDT: {price.FormattedPrice}";
                var changeColor = price.ChangePercent24h >= 0 ? Color.green : Color.red;
                var changeText = $"24h: {price.ChangePercent24h:F2}%";

                Widgets.Label(new Rect(10f, yPos + 5f, 400f, 30f), priceText);
                GUI.color = changeColor;
                Widgets.Label(new Rect(10f, yPos + 30f, 400f, 25f), changeText);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
            else
            {
                Widgets.Label(new Rect(10f, yPos + 20f, 200f, 25f), "Loading price data...");
            }

            yPos += 70f;
        }

        private void DrawChartSection(Rect contentRect, ref float yPos)
        {
            var chartRect = new Rect(0f, yPos, contentRect.width, 180f);
            Widgets.DrawBoxSolid(chartRect, Color.grey);

            if (candleData.ContainsKey(selectedCrypto) && candleData[selectedCrypto].Count > 0)
            {
                DrawSimpleCandlestickChart(chartRect, candleData[selectedCrypto]);
            }
            else
            {
                Widgets.Label(new Rect(chartRect.x + 10f, chartRect.y + 80f, 200f, 25f), "Loading chart data...");
            }

            yPos += 190f;
        }

        private void DrawTradingControls(Rect contentRect, ref float yPos, PlayerCryptoData playerData)
        {
            Widgets.Label(new Rect(0f, yPos, 200f, 25f), "Trading Controls:");
            yPos += 30f;

            // Deposit/Withdraw Silver
            Widgets.Label(new Rect(20f, yPos, 120f, 25f), "Deposit Silver:");
            depositAmount = Widgets.TextField(new Rect(150f, yPos, 80f, 25f), depositAmount);
            if (Widgets.ButtonText(new Rect(240f, yPos, 80f, 25f), "Deposit"))
            {
                if (int.TryParse(depositAmount, out int amount))
                {
                    TradingService.DepositSilver(amount);
                }
            }
            yPos += 30f;

            Widgets.Label(new Rect(20f, yPos, 120f, 25f), "Withdraw USD:");
            withdrawAmount = Widgets.TextField(new Rect(150f, yPos, 80f, 25f), withdrawAmount);
            if (Widgets.ButtonText(new Rect(240f, yPos, 80f, 25f), "Withdraw"))
            {
                if (float.TryParse(withdrawAmount, out float amount))
                {
                    TradingService.WithdrawSilver(amount);
                }
            }
            yPos += 40f;

            // Buy/Sell Crypto
            if (currentPrices.ContainsKey(selectedCrypto))
            {
                var cryptoName = TradingService.GetCryptoDisplayName(selectedCrypto);
                var price = currentPrices[selectedCrypto];

                Widgets.Label(new Rect(20f, yPos, 120f, 25f), "Buy USD Amount:");
                buyAmount = Widgets.TextField(new Rect(150f, yPos, 80f, 25f), buyAmount);
                if (Widgets.ButtonText(new Rect(240f, yPos, 100f, 25f), $"Buy {cryptoName}"))
                {
                    if (float.TryParse(buyAmount, out float amount))
                    {
                        TradingService.BuyCrypto(selectedCrypto, amount, price.PriceUSDT);
                    }
                }
                yPos += 30f;

                Widgets.Label(new Rect(20f, yPos, 120f, 25f), $"Sell {cryptoName}:");
                sellAmount = Widgets.TextField(new Rect(150f, yPos, 80f, 25f), sellAmount);
                if (Widgets.ButtonText(new Rect(240f, yPos, 100f, 25f), $"Sell {cryptoName}"))
                {
                    if (float.TryParse(sellAmount, out float amount))
                    {
                        TradingService.SellCrypto(selectedCrypto, amount, price.PriceUSDT);
                    }
                }
            }
        }

        private void DrawPortfolioTab(Rect contentRect, PlayerCryptoData playerData)
        {
            float yPos = 10f;

            // Portfolio summary
            Widgets.Label(new Rect(750f, yPos, 200f, 30f), "Portfolio Overview:");
            yPos += 40f;

            var totalValue = playerData.GetTotalPortfolioValue(currentPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PriceUSDT));
            var totalPL = playerData.GetTotalPortfolioProfitLoss(currentPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PriceUSDT));

            Widgets.Label(new Rect(770f, yPos, 300f, 25f), $"Cash (USD): ${playerData.SilverDeposited:F2}");
            yPos += 25f;

            Widgets.Label(new Rect(770f, yPos, 300f, 25f), $"Total Portfolio Value: ${totalValue:F2}");
            yPos += 25f;

            GUI.color = totalPL >= 0 ? Color.green : Color.red;
            Widgets.Label(new Rect(770f, yPos, 300f, 25f), $"Total Profit/Loss: ${totalPL:F2}");
            GUI.color = Color.white;
            yPos += 40f;

            // Holdings table
            Widgets.Label(new Rect(0f, yPos, 200f, 25f), "Cryptocurrency Holdings:");
            yPos += 30f;

            // Table headers
            Widgets.DrawBoxSolid(new Rect(0f, yPos, contentRect.width, 25f), Color.grey);
            Widgets.Label(new Rect(10f, yPos, 100f, 25f), "Crypto");
            Widgets.Label(new Rect(120f, yPos, 120f, 25f), "Holdings");
            Widgets.Label(new Rect(250f, yPos, 100f, 25f), "Price");
            Widgets.Label(new Rect(360f, yPos, 120f, 25f), "Value");
            Widgets.Label(new Rect(490f, yPos, 120f, 25f), "Invested");
            Widgets.Label(new Rect(620f, yPos, 120f, 25f), "P&L");
            Widgets.Label(new Rect(750f, yPos, 80f, 25f), "P&L %");
            yPos += 30f;

            // Holdings rows
            foreach (var crypto in playerData.TrackedCryptos)
            {
                var holding = playerData.GetCryptoHolding(crypto);
                if (holding > 0 || crypto == "BTCUSDT") // Always show BTC
                {
                    var cryptoName = TradingService.GetCryptoDisplayName(crypto);
                    var invested = playerData.GetTotalInvested(crypto);

                    Widgets.Label(new Rect(10f, yPos, 100f, 25f), cryptoName);
                    Widgets.Label(new Rect(120f, yPos, 120f, 25f), $"{holding:F8}");

                    if (currentPrices.ContainsKey(crypto))
                    {
                        var price = currentPrices[crypto];
                        var value = holding * price.PriceUSDT;
                        var pl = value - invested;
                        var plPercent = invested > 0 ? (pl / invested) * 100 : 0;

                        Widgets.Label(new Rect(250f, yPos, 100f, 25f), price.FormattedPrice);
                        Widgets.Label(new Rect(360f, yPos, 120f, 25f), $"${value:F2}");
                        Widgets.Label(new Rect(490f, yPos, 120f, 25f), $"${invested:F2}");
                        
                        GUI.color = pl >= 0 ? Color.green : Color.red;
                        Widgets.Label(new Rect(620f, yPos, 120f, 25f), $"${pl:F2}");
                        Widgets.Label(new Rect(750f, yPos, 80f, 25f), $"{plPercent:F1}%");
                        GUI.color = Color.white;
                    }
                    else
                    {
                        Widgets.Label(new Rect(250f, yPos, 400f, 25f), "Loading...");
                    }

                    yPos += 25f;
                }
            }

            yPos += 20f;

            // Recent transactions
            Widgets.Label(new Rect(0f, yPos, 200f, 25f), "Recent Transactions:");
            yPos += 30f;

            var transactionRect = new Rect(0f, yPos, contentRect.width, contentRect.height - yPos);
            Widgets.DrawBoxSolid(transactionRect, Color.black);

            float transactionY = transactionRect.y + 5f;
            var recentTransactions = playerData.Transactions.Count > 15 ?
                playerData.Transactions.GetRange(playerData.Transactions.Count - 15, 15) :
                playerData.Transactions;

            foreach (var transaction in recentTransactions)
            {
                var color = transaction.Type == "BUY" ? Color.green : Color.red;
                var cryptoName = TradingService.GetCryptoDisplayName(transaction.Symbol ?? "BTCUSDT");
                GUI.color = color;
                Widgets.Label(new Rect(transactionRect.x + 5f, transactionY, transactionRect.width - 10f, 20f),
                    $"{transaction.Type} {transaction.Amount:F8} {cryptoName} @ ${transaction.Price:F2} - {transaction.Timestamp}");
                GUI.color = Color.white;
                transactionY += 22f;

                if (transactionY > transactionRect.y + transactionRect.height - 25f) break;
            }
        }

        private void DrawAddCryptoTab(Rect contentRect, PlayerCryptoData playerData)
        {
            float yPos = 10f;

            Widgets.Label(new Rect(750f, yPos, 300f, 25f), "Add New Cryptocurrency:");
            yPos += 40f;

            Widgets.Label(new Rect(770f, yPos, 200f, 25f), "Enter Symbol (e.g., ETHUSDT):");
            yPos += 30f;

            newCryptoSymbol = Widgets.TextField(new Rect(770f, yPos, 200f, 30f), newCryptoSymbol);
            
            if (Widgets.ButtonText(new Rect(1000f, yPos, 80f, 30f), "Add"))
            {
                AddNewCrypto(playerData, newCryptoSymbol);
            }

            yPos += 50f;

            // Popular cryptocurrencies quick add
            Widgets.Label(new Rect(0f, yPos, 300f, 25f), "Popular Cryptocurrencies:");
            yPos += 30f;

            var popularCryptos = new string[] { "ETHUSDT", "XLMUSDT", "LTCUSDT", "DOGEUSDT", "ADAUSDT", "DOTUSDT", "LINKUSDT", "BNBUSDT", "SOLUSDT" };
            var buttonWidth = 100f;
            var buttonsPerRow = 4;
            var currentRow = 0;
            var currentCol = 0;

            foreach (var crypto in popularCryptos)
            {
                if (!playerData.TrackedCryptos.Contains(crypto))
                {
                    var x = 20f + (currentCol * (buttonWidth + 10f));
                    var y = yPos + (currentRow * 35f);
                    
                    var cryptoName = TradingService.GetCryptoDisplayName(crypto);
                    if (Widgets.ButtonText(new Rect(x, y, buttonWidth, 30f), $"Add {cryptoName}"))
                    {
                        AddNewCrypto(playerData, crypto);
                    }

                    currentCol++;
                    if (currentCol >= buttonsPerRow)
                    {
                        currentCol = 0;
                        currentRow++;
                    }
                }
            }

            yPos += (currentRow + 1) * 35f + 20f;

            // Current tracked cryptocurrencies
            Widgets.Label(new Rect(0f, yPos, 300f, 25f), "Currently Tracked:");
            yPos += 30f;

            foreach (var crypto in playerData.TrackedCryptos.ToList())
            {
                var cryptoName = TradingService.GetCryptoDisplayName(crypto);
                Widgets.Label(new Rect(20f, yPos, 150f, 25f), cryptoName);
                
                if (crypto != "BTCUSDT") // Don't allow removing BTC
                {
                    if (Widgets.ButtonText(new Rect(180f, yPos, 80f, 25f), "Remove"))
                    {
                        playerData.RemoveTrackedCrypto(crypto);
                        SetFeedback($"Removed {cryptoName}");
                    }
                }
                
                yPos += 30f;
            }
        }

        private void AddNewCrypto(PlayerCryptoData playerData, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                SetFeedback("Please enter a symbol");
                return;
            }

            symbol = symbol.Trim().ToUpper();

            if (!TradingService.IsValidCryptoSymbol(symbol))
            {
                SetFeedback("Invalid symbol format");
                return;
            }

            if (playerData.TrackedCryptos.Contains(symbol))
            {
                SetFeedback("Already tracking this crypto");
                return;
            }

            // Try to fetch price to validate symbol
            BinanceApiService.GetCurrentPriceAsync(symbol, price =>
            {
                if (price != null)
                {
                    playerData.AddTrackedCrypto(symbol);
                    var cryptoName = TradingService.GetCryptoDisplayName(symbol);
                    SetFeedback($"Added {cryptoName} successfully!");
                    newCryptoSymbol = "";
                    FetchAllDataAsync(); // Refresh data
                }
                else
                {
                    SetFeedback("Symbol not found on exchange");
                }
            });
        }

        private void SetFeedback(string message)
        {
            feedbackMessage = message;
            feedbackMessageTime = Time.time;
        }

        private void DrawSimpleCandlestickChart(Rect chartRect, List<CandleData> data)
        {
            if (data.Count < 2) return;

            var minPrice = data.Min(c => c.Low);
            var maxPrice = data.Max(c => c.High);
            var priceRange = maxPrice - minPrice;
            if (priceRange == 0) return;

            var candleWidth = (chartRect.width - 20f) / data.Count;

            for (int i = 0; i < data.Count; i++)
            {
                var candle = data[i];
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
                if (bodyRect.height < 1f) bodyRect.height = 1f;
                Widgets.DrawBoxSolid(bodyRect, bodyColor);
            }
        }

        private void FetchAllDataAsync()
        {
            try
            {
                var playerData = TradingService.GetPlayerData();
                
                // Fetch prices for all tracked cryptocurrencies
                BinanceApiService.GetMultiplePricesAsync(playerData.TrackedCryptos, prices =>
                {
                    if (prices != null)
                    {
                        currentPrices = prices;
                    }
                });

                // Fetch candle data for selected crypto
                if (!string.IsNullOrEmpty(selectedCrypto))
                {
                    BinanceApiService.GetKlineDataAsync(selectedCrypto, candles =>
                    {
                        if (candles != null)
                        {
                            candleData[selectedCrypto] = candles;
                        }
                    }, "1m", 50);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CryptoTrader] Error fetching data: {ex.Message}");
            }
        }
    }
}
