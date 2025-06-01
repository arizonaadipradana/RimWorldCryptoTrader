using RimWorldCryptoTrader.Models;
using RimWorldCryptoTrader.Services;
using RimWorldCryptoTrader.Core;
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
        private Dictionary<string, ChartIndicators> indicatorCache = new Dictionary<string, ChartIndicators>();
        private Vector2 scrollPosition = Vector2.zero; // Add scroll position
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 1f; // Realtime update (1 second)
        private string lastUpdateStatus = "Initializing...";
        private bool isUpdating = false;
        private float nextUpdateCountdown = 0f;

        // Chart settings
        private bool showMA = true;
        private bool showEMA = true;
        private bool showBollinger = true;
        private bool showRSI = true;
        private bool showMACD = true;
        private bool showVolume = true;
        private int maPeriod = 20;
        private int emaPeriod = 12;
        private int rsiPeriod = 14;

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

        public override Vector2 InitialSize => new Vector2(1400f, 900f);

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
                // Calculate countdown for next update
                nextUpdateCountdown = Math.Max(0f, UPDATE_INTERVAL - (Time.time - lastUpdateTime));
                
                // Update data periodically (only if not already updating)
                if (!isUpdating && nextUpdateCountdown <= 0f)
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
            // Implement scrolling for the trading tab
            var scrollViewRect = new Rect(0f, 0f, contentRect.width, contentRect.height);
            var scrollContentHeight = 1200f; // Total height needed for all content
            var scrollContentRect = new Rect(0f, 0f, contentRect.width - 20f, scrollContentHeight);
            
            Widgets.BeginScrollView(scrollViewRect, ref scrollPosition, scrollContentRect);
            
            float yPos = 10f;

            // Account summary
            DrawAccountSummary(scrollContentRect, ref yPos, playerData);

            // Crypto selector
            DrawCryptoSelector(scrollContentRect, ref yPos, playerData);

            // Current price section
            DrawCurrentPriceSection(scrollContentRect, ref yPos);

            // Chart section (enhanced)
            DrawAdvancedChartSection(scrollContentRect, ref yPos);

            // Trading controls
            DrawTradingControls(scrollContentRect, ref yPos, playerData);
            
            Widgets.EndScrollView();
        }

        private void DrawAccountSummary(Rect contentRect, ref float yPos, PlayerCryptoData playerData)
        {
            Widgets.Label(new Rect(950f, yPos, 200f, 25f), "Account Summary:");
            yPos += 30f;
            
            // Show conversion rate prominently
            GUI.color = Color.yellow;
            Widgets.Label(new Rect(970f, yPos, 300f, 25f), $"Rate: 1 silver = ${CryptoTraderConfig.SilverToUsdRate} USD");
            GUI.color = Color.white;
            yPos += 25f;

            // Colony silver count
            var colonySilver = TradingService.GetColonySilverCount();
            var potentialUSD = colonySilver * CryptoTraderConfig.SilverToUsdRate;
            Widgets.Label(new Rect(970f, yPos, 300f, 25f), $"Colony Silver: {colonySilver:N0} (≈${potentialUSD:N0} USD)");
            yPos += 25f;

            Widgets.Label(new Rect(970f, yPos, 300f, 25f), $"Deposited USD: ${playerData.SilverDeposited:F2}");
            yPos += 25f;

            var totalPortfolioValue = playerData.GetTotalPortfolioValue(currentPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PriceUSDT));
            Widgets.Label(new Rect(970f, yPos, 300f, 25f), $"Total Portfolio: ${totalPortfolioValue:F2}");
            yPos += 25f;

            var totalPL = playerData.GetTotalPortfolioProfitLoss(currentPrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PriceUSDT));
            GUI.color = totalPL >= 0 ? Color.green : Color.red;
            Widgets.Label(new Rect(970f, yPos, 300f, 25f), $"Total P&L: ${totalPL:F2}");
            GUI.color = Color.white;

            yPos += 40f;
        }

        private void DrawCryptoSelector(Rect contentRect, ref float yPos, PlayerCryptoData playerData)
        {
            yPos -= 50;
            Widgets.Label(new Rect(0f, yPos, 200f, 25f), "Select Cryptocurrency:");
            yPos += 30f;

            var dropdownRect = new Rect(20f, yPos, 200f, 30f);
            if (Widgets.ButtonText(dropdownRect, TradingService.GetCryptoDisplayName(selectedCrypto)))
            {
                var options = new List<FloatMenuOption>();
                foreach (var crypto in playerData.TrackedCryptos)
                {
                    var displayName = TradingService.GetCryptoDisplayName(crypto);
                    options.Add(new FloatMenuOption(displayName, () => {
                        selectedCrypto = crypto;
                        // Clear indicator cache when switching cryptos for fresh calculations
                        indicatorCache.Remove(crypto);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            yPos += 40f;
        }

        private void DrawCurrentPriceSection(Rect contentRect, ref float yPos)
        {
            var priceRect = new Rect(0f, yPos, contentRect.width, 80f);
            // Simple background using grey color like the original
            Widgets.DrawBoxSolid(priceRect, Color.grey);

            if (currentPrices.ContainsKey(selectedCrypto))
            {
                var price = currentPrices[selectedCrypto];
                Text.Font = GameFont.Small;
                var cryptoName = TradingService.GetCryptoDisplayName(selectedCrypto);
                var priceText = $"{cryptoName}/USDT: {price.FormattedPrice}";
                var changeColor = price.ChangePercent24h >= 0 ? Color.green : Color.red;
                var changeText = $"24h: {price.ChangePercent24h:F2}%";
                var lastUpdate = $"Last update: {price.Timestamp:HH:mm:ss}";

                GUI.color = Color.white;
                Widgets.Label(new Rect(10f, yPos + 5f, 400f, 30f), priceText);
                GUI.color = changeColor;
                Widgets.Label(new Rect(10f, yPos + 30f, 400f, 25f), changeText);
                GUI.color = Color.white;
                Widgets.Label(new Rect(10f, yPos + 50f, 300f, 20f), lastUpdate);
                GUI.color = Color.white;
                
                Text.Font = GameFont.Small;
            }
            else
            {
                GUI.color = Color.white;
                Widgets.Label(new Rect(10f, yPos + 20f, 200f, 25f), "Loading price data...");
                GUI.color = Color.white;
            }

            yPos += 90f;
        }

        private void DrawAdvancedChartSection(Rect contentRect, ref float yPos)
        {
            var totalChartHeight = 500f; // Increased height for better indicators
            var chartRect = new Rect(0f, yPos, contentRect.width - 200f, totalChartHeight);
            
            // Simple background using grey color like the original
            Widgets.DrawBoxSolid(chartRect, Color.grey);

            // Chart controls panel
            DrawChartControls(new Rect(chartRect.xMax + 10f, yPos, 180f, totalChartHeight));

            if (candleData.ContainsKey(selectedCrypto) && candleData[selectedCrypto].Count > 0)
            {
                var data = candleData[selectedCrypto];
                
                // Calculate indicators if not cached
                if (!indicatorCache.ContainsKey(selectedCrypto))
                {
                    indicatorCache[selectedCrypto] = CalculateIndicators(data);
                }
                
                var indicators = indicatorCache[selectedCrypto];
                
                // Split chart into sections for better indicator display
                var priceChartHeight = totalChartHeight * 0.5f; // 50% for price
                var rsiChartHeight = totalChartHeight * 0.15f;   // 15% for RSI
                var macdChartHeight = totalChartHeight * 0.15f;  // 15% for MACD
                var volumeChartHeight = totalChartHeight * 0.2f; // 20% for volume
                
                var priceChartRect = new Rect(chartRect.x, chartRect.y, chartRect.width, priceChartHeight);
                var rsiChartRect = new Rect(chartRect.x, priceChartRect.yMax, chartRect.width, rsiChartHeight);
                var macdChartRect = new Rect(chartRect.x, rsiChartRect.yMax, chartRect.width, macdChartHeight);
                var volumeChartRect = new Rect(chartRect.x, macdChartRect.yMax, chartRect.width, volumeChartHeight);

                // Draw price chart with indicators
                DrawPriceChart(priceChartRect, data, indicators);
                
                // Draw individual indicator charts
                if (showRSI)
                    DrawRSIChart(rsiChartRect, indicators.RSI);
                
                if (showMACD)
                    DrawMACDChart(macdChartRect, indicators.MACD, indicators.MACDSignal, indicators.MACDHistogram);
                
                if (showVolume)
                    DrawVolumeChart(volumeChartRect, data);
            }
            else
            {
                GUI.color = Color.white;
                Widgets.Label(new Rect(chartRect.x + 10f, chartRect.y + chartRect.height / 2f, 200f, 25f), "Loading chart data...");
                GUI.color = Color.white;
            }

            yPos += totalChartHeight + 20f;
        }

        private void DrawChartControls(Rect controlRect)
        {
            float yPos = controlRect.y + 10f;
            
            // Simple background using grey color like the original
            Widgets.DrawBoxSolid(controlRect, Color.grey);
            
            GUI.color = Color.white;
            Widgets.Label(new Rect(controlRect.x + 5f, yPos, controlRect.width, 25f), "Chart Indicators:");
            yPos += 30f;
            
            // Moving Average controls
            var maRect = new Rect(controlRect.x + 5f, yPos, 20f, 20f);
            Widgets.Checkbox(maRect.x, maRect.y, ref showMA);
            Widgets.Label(new Rect(maRect.xMax + 5f, yPos, 100f, 20f), $"MA ({maPeriod})");
            yPos += 25f;
            
            // EMA controls
            var emaRect = new Rect(controlRect.x + 5f, yPos, 20f, 20f);
            Widgets.Checkbox(emaRect.x, emaRect.y, ref showEMA);
            Widgets.Label(new Rect(emaRect.xMax + 5f, yPos, 100f, 20f), $"EMA ({emaPeriod})");
            yPos += 25f;
            
            // Bollinger Bands
            var bollingerRect = new Rect(controlRect.x + 5f, yPos, 20f, 20f);
            Widgets.Checkbox(bollingerRect.x, bollingerRect.y, ref showBollinger);
            Widgets.Label(new Rect(bollingerRect.xMax + 5f, yPos, 100f, 20f), "Bollinger");
            yPos += 25f;
            
            // RSI
            var rsiRect = new Rect(controlRect.x + 5f, yPos, 20f, 20f);
            Widgets.Checkbox(rsiRect.x, rsiRect.y, ref showRSI);
            Widgets.Label(new Rect(rsiRect.xMax + 5f, yPos, 100f, 20f), $"RSI ({rsiPeriod})");
            yPos += 25f;
            
            // MACD
            var macdRect = new Rect(controlRect.x + 5f, yPos, 20f, 20f);
            Widgets.Checkbox(macdRect.x, macdRect.y, ref showMACD);
            Widgets.Label(new Rect(macdRect.xMax + 5f, yPos, 100f, 20f), "MACD");
            yPos += 25f;
            
            // Volume
            var volumeRect = new Rect(controlRect.x + 5f, yPos, 20f, 20f);
            Widgets.Checkbox(volumeRect.x, volumeRect.y, ref showVolume);
            Widgets.Label(new Rect(volumeRect.xMax + 5f, yPos, 100f, 20f), "Volume");
            yPos += 35f;
            
            // Period settings
            Widgets.Label(new Rect(controlRect.x + 5f, yPos, controlRect.width, 20f), "MA Period:");
            yPos += 20f;
            var maPeriodStr = maPeriod.ToString();
            maPeriodStr = Widgets.TextField(new Rect(controlRect.x + 5f, yPos, 60f, 25f), maPeriodStr);
            if (int.TryParse(maPeriodStr, out int newMAPeriod) && newMAPeriod > 0 && newMAPeriod <= 100)
            {
                if (newMAPeriod != maPeriod)
                {
                    maPeriod = newMAPeriod;
                    indicatorCache.Remove(selectedCrypto); // Recalculate
                }
            }
            yPos += 35f;
            
            Widgets.Label(new Rect(controlRect.x + 5f, yPos, controlRect.width, 20f), "RSI Period:");
            yPos += 20f;
            var rsiPeriodStr = rsiPeriod.ToString();
            rsiPeriodStr = Widgets.TextField(new Rect(controlRect.x + 5f, yPos, 60f, 25f), rsiPeriodStr);
            if (int.TryParse(rsiPeriodStr, out int newRSIPeriod) && newRSIPeriod > 0 && newRSIPeriod <= 100)
            {
                if (newRSIPeriod != rsiPeriod)
                {
                    rsiPeriod = newRSIPeriod;
                    indicatorCache.Remove(selectedCrypto); // Recalculate
                }
            }
            
            GUI.color = Color.white;
        }

        private void DrawPriceChart(Rect chartRect, List<CandleData> data, ChartIndicators indicators)
        {
            if (data.Count < 2) return;

            var padding = 10f;
            var drawRect = new Rect(chartRect.x + padding, chartRect.y + padding, 
                                   chartRect.width - padding * 2, chartRect.height - padding * 2);

            // Calculate price range including Bollinger bands
            var minPrice = data.Min(c => c.Low);
            var maxPrice = data.Max(c => c.High);
            
            if (showBollinger && indicators.BollingerLower.Count > 0 && indicators.BollingerUpper.Count > 0)
            {
                minPrice = Math.Min(minPrice, indicators.BollingerLower.Min());
                maxPrice = Math.Max(maxPrice, indicators.BollingerUpper.Max());
            }
            
            var priceRange = maxPrice - minPrice;
            if (priceRange == 0) return;

            // Draw grid lines
            DrawSimpleGrid(drawRect);

            var candleWidth = drawRect.width / data.Count;

            // Draw Bollinger Bands first (as background)
            if (showBollinger && indicators.BollingerUpper.Count == data.Count)
            {
                DrawBollingerBands(drawRect, indicators.BollingerUpper, indicators.BollingerLower, minPrice, priceRange, candleWidth);
            }

            // Draw candlesticks using the draw rect (same price range as other indicators)
            DrawCandlesticksInRect(drawRect, data, minPrice, priceRange);

            // Draw moving averages as simple lines
            if (showMA && indicators.SMA.Count == data.Count)
            {
                DrawSimpleLine(drawRect, indicators.SMA, minPrice, priceRange, candleWidth, Color.blue);
            }

            if (showEMA && indicators.EMA.Count == data.Count)
            {
                DrawSimpleLine(drawRect, indicators.EMA, minPrice, priceRange, candleWidth, Color.magenta);
            }

            // Draw price labels
            DrawSimplePriceLabels(chartRect, minPrice, maxPrice);
            
            // Add chart title
            GUI.color = Color.white;
            Widgets.Label(new Rect(chartRect.x + 10f, chartRect.y + 10f, 200f, 20f), "Price Chart");
            GUI.color = Color.white;
        }

        private void DrawSimpleGrid(Rect rect)
        {
            // Simple grid using basic lines
            var gridColor = new Color(0.9f, 0.9f, 0.9f);
            
            // Horizontal lines
            for (int i = 0; i <= 5; i++)
            {
                var y = rect.y + (rect.height / 5f) * i;
                Widgets.DrawLine(new Vector2(rect.x, y), new Vector2(rect.xMax, y), gridColor, 1f);
            }

            // Vertical lines
            for (int i = 0; i <= 8; i++)
            {
                var x = rect.x + (rect.width / 8f) * i;
                Widgets.DrawLine(new Vector2(x, rect.y), new Vector2(x, rect.yMax), gridColor, 1f);
            }
        }

        private void DrawSimpleCandlestickChart(Rect drawRect, List<CandleData> data)
        {
            if (data.Count < 2) return;

            // Use the same price calculation as the parent method
            var minPrice = data.Min(c => c.Low);
            var maxPrice = data.Max(c => c.High);
            var priceRange = maxPrice - minPrice;
            if (priceRange == 0) return;

            var candleWidth = drawRect.width / data.Count;

            for (int i = 0; i < data.Count; i++)
            {
                var candle = data[i];
                var x = drawRect.x + (i * candleWidth);

                var highY = drawRect.y + (float)((maxPrice - candle.High) / priceRange) * drawRect.height;
                var lowY = drawRect.y + (float)((maxPrice - candle.Low) / priceRange) * drawRect.height;
                var openY = drawRect.y + (float)((maxPrice - candle.Open) / priceRange) * drawRect.height;
                var closeY = drawRect.y + (float)((maxPrice - candle.Close) / priceRange) * drawRect.height;

                // Draw wick
                Widgets.DrawLine(new Vector2(x + candleWidth / 2, highY), new Vector2(x + candleWidth / 2, lowY), Color.white, 1f);

                // Draw body
                var bodyColor = candle.Close >= candle.Open ? Color.green : Color.red;
                var bodyRect = new Rect(x + 1f, Math.Min(openY, closeY), candleWidth - 2f, Math.Abs(closeY - openY));
                if (bodyRect.height < 1f) bodyRect.height = 1f;
                Widgets.DrawBoxSolid(bodyRect, bodyColor);
            }
        }

        private void DrawSimpleLine(Rect drawRect, List<float> values, float minPrice, float priceRange, 
                                  float candleWidth, Color color)
        {
            if (values.Count < 2) return;

            var maxPrice = minPrice + priceRange;

            for (int i = 1; i < values.Count; i++)
            {
                var x1 = drawRect.x + ((i - 1) * candleWidth) + candleWidth / 2;
                var x2 = drawRect.x + (i * candleWidth) + candleWidth / 2;
                
                var y1 = drawRect.y + (float)((maxPrice - values[i - 1]) / priceRange) * drawRect.height;
                var y2 = drawRect.y + (float)((maxPrice - values[i]) / priceRange) * drawRect.height;

                Widgets.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), color, 2f);
            }
        }

        private void DrawCandlesticksInRect(Rect drawRect, List<CandleData> data, float minPrice, float priceRange)
        {
            if (data.Count < 2 || priceRange == 0) return;

            var maxPrice = minPrice + priceRange;
            var candleWidth = drawRect.width / data.Count;

            for (int i = 0; i < data.Count; i++)
            {
                var candle = data[i];
                var x = drawRect.x + (i * candleWidth);

                var highY = drawRect.y + (float)((maxPrice - candle.High) / priceRange) * drawRect.height;
                var lowY = drawRect.y + (float)((maxPrice - candle.Low) / priceRange) * drawRect.height;
                var openY = drawRect.y + (float)((maxPrice - candle.Open) / priceRange) * drawRect.height;
                var closeY = drawRect.y + (float)((maxPrice - candle.Close) / priceRange) * drawRect.height;

                // Draw wick
                Widgets.DrawLine(new Vector2(x + candleWidth / 2, highY), new Vector2(x + candleWidth / 2, lowY), Color.white, 1f);

                // Draw body
                var bodyColor = candle.Close >= candle.Open ? Color.green : Color.red;
                var bodyRect = new Rect(x + 1f, Math.Min(openY, closeY), candleWidth - 2f, Math.Abs(closeY - openY));
                if (bodyRect.height < 1f) bodyRect.height = 1f;
                Widgets.DrawBoxSolid(bodyRect, bodyColor);
            }
        }

        private void DrawBollingerBands(Rect drawRect, List<float> upper, List<float> lower, float minPrice, float priceRange, float candleWidth)
        {
            if (upper.Count < 2 || lower.Count < 2) return;

            var maxPrice = minPrice + priceRange;

            // Draw upper band
            for (int i = 1; i < upper.Count; i++)
            {
                var x1 = drawRect.x + ((i - 1) * candleWidth) + candleWidth / 2;
                var x2 = drawRect.x + (i * candleWidth) + candleWidth / 2;
                
                var y1 = drawRect.y + (float)((maxPrice - upper[i - 1]) / priceRange) * drawRect.height;
                var y2 = drawRect.y + (float)((maxPrice - upper[i]) / priceRange) * drawRect.height;

                Widgets.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), Color.cyan, 1f);
            }

            // Draw lower band
            for (int i = 1; i < lower.Count; i++)
            {
                var x1 = drawRect.x + ((i - 1) * candleWidth) + candleWidth / 2;
                var x2 = drawRect.x + (i * candleWidth) + candleWidth / 2;
                
                var y1 = drawRect.y + (float)((maxPrice - lower[i - 1]) / priceRange) * drawRect.height;
                var y2 = drawRect.y + (float)((maxPrice - lower[i]) / priceRange) * drawRect.height;

                Widgets.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), Color.cyan, 1f);
            }
        }

        private void DrawRSIChart(Rect chartRect, List<float> rsiValues)
        {
            if (rsiValues.Count < 2) return;

            var padding = 5f;
            var drawRect = new Rect(chartRect.x + padding, chartRect.y + padding, 
                                   chartRect.width - padding * 2, chartRect.height - padding * 2);

            // Draw background
            Widgets.DrawBoxSolid(chartRect, Color.black);

            // Draw reference lines (30, 50, 70)
            var oversoldY = drawRect.y + (30f / 100f) * drawRect.height;
            var midlineY = drawRect.y + (50f / 100f) * drawRect.height;
            var overboughtY = drawRect.y + (70f / 100f) * drawRect.height;

            Widgets.DrawLine(new Vector2(drawRect.x, oversoldY), new Vector2(drawRect.xMax, oversoldY), Color.red, 1f);
            Widgets.DrawLine(new Vector2(drawRect.x, midlineY), new Vector2(drawRect.xMax, midlineY), Color.white, 1f);
            Widgets.DrawLine(new Vector2(drawRect.x, overboughtY), new Vector2(drawRect.xMax, overboughtY), Color.red, 1f);

            // Draw RSI line
            var candleWidth = drawRect.width / rsiValues.Count;
            for (int i = 1; i < rsiValues.Count; i++)
            {
                var x1 = drawRect.x + ((i - 1) * candleWidth) + candleWidth / 2;
                var x2 = drawRect.x + (i * candleWidth) + candleWidth / 2;
                
                var y1 = drawRect.y + ((100f - rsiValues[i - 1]) / 100f) * drawRect.height;
                var y2 = drawRect.y + ((100f - rsiValues[i]) / 100f) * drawRect.height;

                Widgets.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), Color.yellow, 2f);
            }

            // Label
            GUI.color = Color.white;
            Widgets.Label(new Rect(chartRect.x + 5f, chartRect.y + 2f, 100f, 20f), $"RSI: {rsiValues.LastOrDefault():F1}");
            GUI.color = Color.white;
        }

        private void DrawMACDChart(Rect chartRect, List<float> macd, List<float> signal, List<float> histogram)
        {
            if (macd.Count < 2) return;

            var padding = 5f;
            var drawRect = new Rect(chartRect.x + padding, chartRect.y + padding, 
                                   chartRect.width - padding * 2, chartRect.height - padding * 2);

            // Draw background
            Widgets.DrawBoxSolid(chartRect, Color.black);

            // Calculate range
            var allValues = macd.Concat(signal).Concat(histogram);
            var minValue = allValues.Min();
            var maxValue = allValues.Max();
            var range = maxValue - minValue;
            if (range == 0) return;

            // Draw zero line
            var zeroY = drawRect.y + (float)((maxValue - 0) / range) * drawRect.height;
            Widgets.DrawLine(new Vector2(drawRect.x, zeroY), new Vector2(drawRect.xMax, zeroY), Color.white, 1f);

            var candleWidth = drawRect.width / macd.Count;

            // Draw histogram bars
            for (int i = 0; i < histogram.Count; i++)
            {
                var x = drawRect.x + (i * candleWidth);
                var histY = drawRect.y + (float)((maxValue - histogram[i]) / range) * drawRect.height;
                
                var barHeight = Math.Abs(histY - zeroY);
                var barRect = new Rect(x + 1f, Math.Min(histY, zeroY), candleWidth - 2f, barHeight);
                
                var barColor = histogram[i] >= 0 ? Color.green : Color.red;
                Widgets.DrawBoxSolid(barRect, barColor);
            }

            // Draw MACD line
            for (int i = 1; i < macd.Count; i++)
            {
                var x1 = drawRect.x + ((i - 1) * candleWidth) + candleWidth / 2;
                var x2 = drawRect.x + (i * candleWidth) + candleWidth / 2;
                
                var y1 = drawRect.y + (float)((maxValue - macd[i - 1]) / range) * drawRect.height;
                var y2 = drawRect.y + (float)((maxValue - macd[i]) / range) * drawRect.height;

                Widgets.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), Color.blue, 2f);
            }

            // Draw Signal line
            for (int i = 1; i < signal.Count; i++)
            {
                var x1 = drawRect.x + ((i - 1) * candleWidth) + candleWidth / 2;
                var x2 = drawRect.x + (i * candleWidth) + candleWidth / 2;
                
                var y1 = drawRect.y + (float)((maxValue - signal[i - 1]) / range) * drawRect.height;
                var y2 = drawRect.y + (float)((maxValue - signal[i]) / range) * drawRect.height;

                Widgets.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), Color.magenta, 2f);
            }

            // Label
            GUI.color = Color.white;
            Widgets.Label(new Rect(chartRect.x + 5f, chartRect.y + 2f, 150f, 20f), $"MACD: {macd.LastOrDefault():F4}");
            GUI.color = Color.white;
        }

        private void DrawVolumeChart(Rect chartRect, List<CandleData> data)
        {
            if (data.Count < 2) return;

            var padding = 5f;
            var drawRect = new Rect(chartRect.x + padding, chartRect.y + padding, 
                                   chartRect.width - padding * 2, chartRect.height - padding * 2);

            // Draw background
            Widgets.DrawBoxSolid(chartRect, Color.black);

            var maxVolume = data.Max(c => c.Volume);
            if (maxVolume == 0) return;

            var candleWidth = drawRect.width / data.Count;

            // Draw volume bars
            for (int i = 0; i < data.Count; i++)
            {
                var candle = data[i];
                var x = drawRect.x + (i * candleWidth);
                var volumeHeight = (candle.Volume / maxVolume) * drawRect.height;
                var barRect = new Rect(x + 1f, drawRect.yMax - volumeHeight, candleWidth - 2f, volumeHeight);
                
                var barColor = candle.Close >= candle.Open ? 
                              new Color(0.0f, 0.8f, 0.0f, 0.7f) : new Color(0.8f, 0.0f, 0.0f, 0.7f);
                Widgets.DrawBoxSolid(barRect, barColor);
            }

            // Label
            GUI.color = Color.white;
            Widgets.Label(new Rect(chartRect.x + 5f, chartRect.y + 2f, 150f, 20f), $"Volume: {data.LastOrDefault().Volume:N0}");
            GUI.color = Color.white;
        }

        private void DrawSimplePriceLabels(Rect chartRect, float minPrice, float maxPrice)
        {
            var labelWidth = 80f;
            
            // Draw 5 price levels
            for (int i = 0; i <= 4; i++)
            {
                var price = maxPrice - (maxPrice - minPrice) * (i / 4f);
                var y = chartRect.y + (chartRect.height / 4f) * i;
                
                GUI.color = Color.white;
                Widgets.Label(new Rect(chartRect.xMax - labelWidth, y - 10f, labelWidth, 20f), $"${price:F2}");
            }
            GUI.color = Color.white;
        }

        private ChartIndicators CalculateIndicators(List<CandleData> data)
        {
            var indicators = new ChartIndicators();
            
            if (data.Count < Math.Max(maPeriod, rsiPeriod))
            {
                Log.Message($"[CryptoTrader] Not enough data for indicators. Data count: {data.Count}, Required: {Math.Max(maPeriod, rsiPeriod)}");
                return indicators;
            }

            var closes = data.Select(c => c.Close).ToList();
            Log.Message($"[CryptoTrader] Calculating indicators for {data.Count} candles. Price range: ${closes.Min():F2} - ${closes.Max():F2}");
            
            // Simple Moving Average
            indicators.SMA = CalculateSMA(closes, maPeriod);
            Log.Message($"[CryptoTrader] SMA calculated: {indicators.SMA.Count} values, last: {indicators.SMA.LastOrDefault():F2}");
            
            // Exponential Moving Average
            indicators.EMA = CalculateEMA(closes, emaPeriod);
            Log.Message($"[CryptoTrader] EMA calculated: {indicators.EMA.Count} values, last: {indicators.EMA.LastOrDefault():F2}");
            
            // Bollinger Bands
            var bollinger = CalculateBollingerBands(closes, maPeriod, 2.0f);
            indicators.BollingerUpper = bollinger.Item1;
            indicators.BollingerLower = bollinger.Item2;
            Log.Message($"[CryptoTrader] Bollinger Bands calculated: Upper: {indicators.BollingerUpper.LastOrDefault():F2}, Lower: {indicators.BollingerLower.LastOrDefault():F2}");
            
            // RSI
            indicators.RSI = CalculateRSI(closes, rsiPeriod);
            Log.Message($"[CryptoTrader] RSI calculated: {indicators.RSI.Count} values, last: {indicators.RSI.LastOrDefault():F1}");
            
            // MACD
            var macd = CalculateMACD(closes, 12, 26, 9);
            indicators.MACD = macd.Item1;
            indicators.MACDSignal = macd.Item2;
            indicators.MACDHistogram = macd.Item3;
            Log.Message($"[CryptoTrader] MACD calculated: {indicators.MACD.Count} values, last MACD: {indicators.MACD.LastOrDefault():F4}, Signal: {indicators.MACDSignal.LastOrDefault():F4}");
            
            return indicators;
        }

        private List<float> CalculateSMA(List<float> prices, int period)
        {
            var sma = new List<float>();
            
            for (int i = 0; i < prices.Count; i++)
            {
                if (i < period - 1)
                {
                    sma.Add(prices[i]); // Not enough data yet, use current price
                }
                else
                {
                    var sum = 0f;
                    for (int j = i - period + 1; j <= i; j++)
                        sum += prices[j];
                    sma.Add(sum / period);
                }
            }
            
            return sma;
        }

        private List<float> CalculateEMA(List<float> prices, int period)
        {
            var ema = new List<float>();
            var multiplier = 2f / (period + 1f);
            
            for (int i = 0; i < prices.Count; i++)
            {
                if (i == 0)
                {
                    ema.Add(prices[i]);
                }
                else
                {
                    var value = (prices[i] * multiplier) + (ema[i - 1] * (1 - multiplier));
                    ema.Add(value);
                }
            }
            
            return ema;
        }

        private (List<float>, List<float>) CalculateBollingerBands(List<float> prices, int period, float stdDev)
        {
            var upperBand = new List<float>();
            var lowerBand = new List<float>();
            var sma = CalculateSMA(prices, period);
            
            for (int i = 0; i < prices.Count; i++)
            {
                if (i < period - 1)
                {
                    upperBand.Add(prices[i]);
                    lowerBand.Add(prices[i]);
                }
                else
                {
                    // Calculate standard deviation
                    var sum = 0f;
                    for (int j = i - period + 1; j <= i; j++)
                    {
                        var diff = prices[j] - sma[i];
                        sum += diff * diff;
                    }
                    var variance = sum / period;
                    var std = Mathf.Sqrt(variance);
                    
                    upperBand.Add(sma[i] + (std * stdDev));
                    lowerBand.Add(sma[i] - (std * stdDev));
                }
            }
            
            return (upperBand, lowerBand);
        }

        private List<float> CalculateRSI(List<float> prices, int period)
        {
            var rsi = new List<float>();
            var gains = new List<float>();
            var losses = new List<float>();
            
            for (int i = 0; i < prices.Count; i++)
            {
                if (i == 0)
                {
                    gains.Add(0);
                    losses.Add(0);
                    rsi.Add(50); // Neutral RSI
                }
                else
                {
                    var change = prices[i] - prices[i - 1];
                    gains.Add(change > 0 ? change : 0);
                    losses.Add(change < 0 ? -change : 0);
                    
                    if (i < period)
                    {
                        rsi.Add(50); // Not enough data
                    }
                    else
                    {
                        var avgGain = gains.Skip(i - period + 1).Take(period).Average();
                        var avgLoss = losses.Skip(i - period + 1).Take(period).Average();
                        
                        if (avgLoss == 0)
                        {
                            rsi.Add(100);
                        }
                        else
                        {
                            var rs = avgGain / avgLoss;
                            var rsiValue = 100 - (100 / (1 + rs));
                            rsi.Add(rsiValue);
                        }
                    }
                }
            }
            
            return rsi;
        }

        private (List<float>, List<float>, List<float>) CalculateMACD(List<float> prices, int fastPeriod, int slowPeriod, int signalPeriod)
        {
            var fastEMA = CalculateEMA(prices, fastPeriod);
            var slowEMA = CalculateEMA(prices, slowPeriod);
            
            var macdLine = new List<float>();
            for (int i = 0; i < prices.Count; i++)
            {
                macdLine.Add(fastEMA[i] - slowEMA[i]);
            }
            
            var signalLine = CalculateEMA(macdLine, signalPeriod);
            
            var histogram = new List<float>();
            for (int i = 0; i < macdLine.Count; i++)
            {
                histogram.Add(macdLine[i] - signalLine[i]);
            }
            
            return (macdLine, signalLine, histogram);
        }

        private void DrawTradingControls(Rect contentRect, ref float yPos, PlayerCryptoData playerData)
        {
            yPos += 20;
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

        private void FetchAllDataAsync()
        {
            if (isUpdating) return;
            
            try
            {
                isUpdating = true;
                lastUpdateStatus = "Updating prices...";
                
                var playerData = TradingService.GetPlayerData();
                
                // Fetch prices for all tracked cryptocurrencies
                BinanceApiService.GetMultiplePricesAsync(playerData.TrackedCryptos, prices =>
                {
                    if (prices != null && prices.Count > 0)
                    {
                        currentPrices = prices;
                        lastUpdateStatus = $"Updated {prices.Count} prices at {DateTime.Now:HH:mm:ss}";
                        
                        // Log specific price for debugging
                        if (prices.ContainsKey(selectedCrypto))
                        {
                            var price = prices[selectedCrypto];
                            Log.Message($"[CryptoTrader UI] {selectedCrypto} price updated: ${price.PriceUSDT:F2} at {price.Timestamp:HH:mm:ss}");
                        }
                    }
                    else
                    {
                        lastUpdateStatus = "Failed to fetch prices";
                    }
                    
                    isUpdating = false;
                });

                // Fetch candle data for selected crypto with more data points for better indicators
                if (!string.IsNullOrEmpty(selectedCrypto))
                {
                    BinanceApiService.GetKlineDataAsync(selectedCrypto, candles =>
                    {
                        if (candles != null)
                        {
                            candleData[selectedCrypto] = candles;
                            // Clear indicator cache to force recalculation with new data
                            indicatorCache.Remove(selectedCrypto);
                        }
                    }, "5m", 100); // 5-minute intervals with 100 candles for better analysis
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CryptoTrader] Error fetching data: {ex.Message}");
                lastUpdateStatus = "Error fetching data";
                isUpdating = false;
            }
        }
    }

    // Helper class to store calculated indicators
    public class ChartIndicators
    {
        public List<float> SMA { get; set; } = new List<float>();
        public List<float> EMA { get; set; } = new List<float>();
        public List<float> BollingerUpper { get; set; } = new List<float>();
        public List<float> BollingerLower { get; set; } = new List<float>();
        public List<float> RSI { get; set; } = new List<float>();
        public List<float> MACD { get; set; } = new List<float>();
        public List<float> MACDSignal { get; set; } = new List<float>();
        public List<float> MACDHistogram { get; set; } = new List<float>();
    }
}