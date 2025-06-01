using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RimWorldCryptoTrader.Models
{
    // Simple JSON parser to avoid System.Numerics dependencies
    public static class SimpleJsonParser
    {
        public static BinanceTickerResponse24h ParseTickerResponse(string json)
        {
            try
            {
                var response = new BinanceTickerResponse24h();
                
                // Extract values using regex - simple but effective
                var symbolMatch = Regex.Match(json, @"""symbol""\s*:\s*""([^""]+)""");
                var lastPriceMatch = Regex.Match(json, @"""lastPrice""\s*:\s*""([^""]+)""");
                var priceChangeMatch = Regex.Match(json, @"""priceChange""\s*:\s*""([^""]+)""");
                var priceChangePercentMatch = Regex.Match(json, @"""priceChangePercent""\s*:\s*""([^""]+)""");
                
                if (symbolMatch.Success)
                    response.Symbol = symbolMatch.Groups[1].Value;
                    
                if (lastPriceMatch.Success && decimal.TryParse(lastPriceMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal lastPrice))
                    response.LastPrice = lastPrice;
                    
                if (priceChangeMatch.Success && decimal.TryParse(priceChangeMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal priceChange))
                    response.PriceChange = priceChange;
                    
                if (priceChangePercentMatch.Success && decimal.TryParse(priceChangePercentMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal priceChangePercent))
                    response.PriceChangePercent = priceChangePercent;
                
                return response;
            }
            catch
            {
                return null;
            }
        }
        
        public static List<CandleData> ParseKlineResponse(string json)
        {
            try
            {
                var result = new List<CandleData>();
                
                // Find all array elements [timestamp, open, high, low, close, volume, ...]
                var arrayPattern = @"\[([^\]]+)\]";
                var matches = Regex.Matches(json, arrayPattern);
                
                foreach (Match match in matches)
                {
                    var values = match.Groups[1].Value.Split(',');
                    if (values.Length >= 6)
                    {
                        try
                        {
                            var candle = new CandleData();
                            
                            // Parse timestamp (first value)
                            if (long.TryParse(values[0].Trim('"', ' '), out long timestamp))
                                candle.Time = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                            
                            // Parse OHLCV values (remove quotes and parse)
                            if (decimal.TryParse(values[1].Trim('"', ' '), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal open))
                                candle.Open = open;
                                
                            if (decimal.TryParse(values[2].Trim('"', ' '), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal high))
                                candle.High = high;
                                
                            if (decimal.TryParse(values[3].Trim('"', ' '), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal low))
                                candle.Low = low;
                                
                            if (decimal.TryParse(values[4].Trim('"', ' '), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal close))
                                candle.Close = close;
                                
                            if (decimal.TryParse(values[5].Trim('"', ' '), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal volume))
                                candle.Volume = volume;
                            
                            result.Add(candle);
                        }
                        catch
                        {
                            // Skip invalid entries
                            continue;
                        }
                    }
                }
                
                return result;
            }
            catch
            {
                return new List<CandleData>();
            }
        }
    }
    
    public class BinanceTickerResponse
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public long Time { get; set; }
    }

    public class BinanceTickerResponse24h
    {
        public string Symbol { get; set; }
        public decimal LastPrice { get; set; }
        public decimal PriceChange { get; set; }
        public decimal PriceChangePercent { get; set; }
    }

    public class BinanceKlineResponse
    {
        public long OpenTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public long CloseTime { get; set; }
    }

    public class CryptoPrice
    {
        public string Symbol { get; set; }
        public decimal PriceUSDT { get; set; }
        public DateTime Timestamp { get; set; }
        public string FormattedPrice => $"${PriceUSDT:N2}";
        public decimal Change24h { get; set; }
        public decimal ChangePercent24h { get; set; }
    }

    public class CandleData
    {
        public DateTime Time { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}