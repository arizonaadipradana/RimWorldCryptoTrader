using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using RimWorldCryptoTrader.Models;
using Verse;

namespace RimWorldCryptoTrader.Services
{
    public class BinanceApiService
    {
        private const string BINANCE_API_BASE = "https://api.binance.com/api/v3";
        private const float TIMEOUT_SECONDS = 10f;
        private static Dictionary<string, CryptoPrice> cachedPrices = new Dictionary<string, CryptoPrice>();
        private static float lastCacheUpdate = 0f;
        private const float CACHE_DURATION = 30f; // Cache for 30 seconds

        public static void GetCurrentPriceAsync(string symbol, System.Action<CryptoPrice> callback)
        {
            // Check cache first
            if (Time.time - lastCacheUpdate < CACHE_DURATION && cachedPrices.ContainsKey(symbol))
            {
                callback?.Invoke(cachedPrices[symbol]);
                return;
            }

            var gameObject = new GameObject("CryptoApiRequest");
            var component = gameObject.AddComponent<ApiRequestComponent>();
            component.StartCoroutine(GetCurrentPriceCoroutine(symbol, callback, gameObject));
        }

        public static void GetMultiplePricesAsync(List<string> symbols, System.Action<Dictionary<string, CryptoPrice>> callback)
        {
            var gameObject = new GameObject("CryptoMultiApiRequest");
            var component = gameObject.AddComponent<ApiRequestComponent>();
            component.StartCoroutine(GetMultiplePricesCoroutine(symbols, callback, gameObject));
        }

        public static void GetKlineDataAsync(string symbol, System.Action<List<CandleData>> callback, string interval = "1m", int limit = 100)
        {
            var gameObject = new GameObject("CryptoKlineRequest");
            var component = gameObject.AddComponent<ApiRequestComponent>();
            component.StartCoroutine(GetKlineDataCoroutine(symbol, callback, gameObject, interval, limit));
        }

        // Legacy method for backward compatibility
        public static void GetCurrentPriceAsync(System.Action<CryptoPrice> callback)
        {
            GetCurrentPriceAsync("BTCUSDT", callback);
        }

        // Legacy method for backward compatibility
        public static void GetKlineDataAsync(System.Action<List<CandleData>> callback, string interval = "1m", int limit = 100)
        {
            GetKlineDataAsync("BTCUSDT", callback, interval, limit);
        }

        private static IEnumerator GetCurrentPriceCoroutine(string symbol, System.Action<CryptoPrice> callback, GameObject cleanup)
        {
            var url = $"{BINANCE_API_BASE}/ticker/24hr?symbol={symbol}";
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)TIMEOUT_SECONDS;
                request.SetRequestHeader("User-Agent", "RimWorld-CryptoTrader/1.0");
                
                yield return request.SendWebRequest();
                
                try
                {
                    if (!request.isNetworkError && !request.isHttpError)
                    {
                        var response = request.downloadHandler.text;
                        var data = SimpleJsonParser.ParseTickerResponse(response);
                        
                        if (data != null)
                        {
                            var price = new CryptoPrice
                            {
                                Symbol = symbol,
                                PriceUSDT = data.LastPrice,
                                Change24h = data.PriceChange,
                                ChangePercent24h = data.PriceChangePercent,
                                Timestamp = DateTime.Now
                            };
                            
                            // Update cache
                            cachedPrices[symbol] = price;
                            lastCacheUpdate = Time.time;
                            
                            callback?.Invoke(price);
                        }
                        else
                        {
                            Log.Error($"[CryptoTrader] Failed to parse price response for {symbol}");
                            callback?.Invoke(null);
                        }
                    }
                    else
                    {
                        Log.Error($"[CryptoTrader] Failed to fetch price for {symbol}: {request.error}");
                        callback?.Invoke(null);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[CryptoTrader] Error parsing price response for {symbol}: {ex.Message}");
                    callback?.Invoke(null);
                }
            }
            
            // Cleanup
            if (cleanup != null)
            {
                UnityEngine.Object.Destroy(cleanup);
            }
        }

        private static IEnumerator GetMultiplePricesCoroutine(List<string> symbols, System.Action<Dictionary<string, CryptoPrice>> callback, GameObject cleanup)
        {
            var results = new Dictionary<string, CryptoPrice>();
            var remainingRequests = symbols.Count;

            foreach (var symbol in symbols)
            {
                // Check cache first
                if (Time.time - lastCacheUpdate < CACHE_DURATION && cachedPrices.ContainsKey(symbol))
                {
                    results[symbol] = cachedPrices[symbol];
                    remainingRequests--;
                    continue;
                }

                var url = $"{BINANCE_API_BASE}/ticker/24hr?symbol={symbol}";
                
                using (var request = UnityWebRequest.Get(url))
                {
                    request.timeout = (int)TIMEOUT_SECONDS;
                    request.SetRequestHeader("User-Agent", "RimWorld-CryptoTrader/1.0");
                    
                    yield return request.SendWebRequest();
                    
                    try
                    {
                        if (!request.isNetworkError && !request.isHttpError)
                        {
                            var response = request.downloadHandler.text;
                            var data = SimpleJsonParser.ParseTickerResponse(response);
                            
                            if (data != null)
                            {
                                var price = new CryptoPrice
                                {
                                    Symbol = symbol,
                                    PriceUSDT = data.LastPrice,
                                    Change24h = data.PriceChange,
                                    ChangePercent24h = data.PriceChangePercent,
                                    Timestamp = DateTime.Now
                                };
                                
                                results[symbol] = price;
                                cachedPrices[symbol] = price;
                            }
                        }
                        else
                        {
                            Log.Error($"[CryptoTrader] Failed to fetch price for {symbol}: {request.error}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[CryptoTrader] Error parsing price response for {symbol}: {ex.Message}");
                    }
                    
                    remainingRequests--;
                }
                
                // Small delay between requests to avoid rate limiting
                yield return new WaitForSeconds(0.1f);
            }
            
            lastCacheUpdate = Time.time;
            callback?.Invoke(results);
            
            // Cleanup
            if (cleanup != null)
            {
                UnityEngine.Object.Destroy(cleanup);
            }
        }

        private static IEnumerator GetKlineDataCoroutine(string symbol, System.Action<List<CandleData>> callback, GameObject cleanup, string interval, int limit)
        {
            var url = $"{BINANCE_API_BASE}/klines?symbol={symbol}&interval={interval}&limit={limit}";
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)TIMEOUT_SECONDS;
                request.SetRequestHeader("User-Agent", "RimWorld-CryptoTrader/1.0");
                
                yield return request.SendWebRequest();
                
                try
                {
                    if (!request.isNetworkError && !request.isHttpError)
                    {
                        var response = request.downloadHandler.text;
                        var candleData = SimpleJsonParser.ParseKlineResponse(response);
                        
                        callback?.Invoke(candleData);
                    }
                    else
                    {
                        Log.Error($"[CryptoTrader] Failed to fetch kline data for {symbol}: {request.error}");
                        callback?.Invoke(new List<CandleData>());
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[CryptoTrader] Error parsing kline response for {symbol}: {ex.Message}");
                    callback?.Invoke(new List<CandleData>());
                }
            }
            
            // Cleanup
            if (cleanup != null)
            {
                UnityEngine.Object.Destroy(cleanup);
            }
        }

        // Helper component for running coroutines
        private class ApiRequestComponent : MonoBehaviour { }
    }
}
