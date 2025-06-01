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
        private const string SYMBOL = "BTCUSDT";
        private const float TIMEOUT_SECONDS = 10f;

        public static void GetCurrentPriceAsync(System.Action<CryptoPrice> callback)
        {
            var gameObject = new GameObject("CryptoApiRequest");
            var component = gameObject.AddComponent<ApiRequestComponent>();
            component.StartCoroutine(GetCurrentPriceCoroutine(callback, gameObject));
        }

        public static void GetKlineDataAsync(System.Action<List<CandleData>> callback, string interval = "1m", int limit = 100)
        {
            var gameObject = new GameObject("CryptoKlineRequest");
            var component = gameObject.AddComponent<ApiRequestComponent>();
            component.StartCoroutine(GetKlineDataCoroutine(callback, gameObject, interval, limit));
        }

        private static IEnumerator GetCurrentPriceCoroutine(System.Action<CryptoPrice> callback, GameObject cleanup)
        {
            var url = $"{BINANCE_API_BASE}/ticker/24hr?symbol={SYMBOL}";
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)TIMEOUT_SECONDS;
                request.SetRequestHeader("User-Agent", "RimWorld-CryptoTrader/1.0");
                
                yield return request.SendWebRequest();
                
                try
                {
                    // Check for errors using older Unity API
                    if (!request.isNetworkError && !request.isHttpError)
                    {
                        var response = request.downloadHandler.text;
                        var data = SimpleJsonParser.ParseTickerResponse(response);
                        
                        if (data != null)
                        {
                            var price = new CryptoPrice
                            {
                                Symbol = SYMBOL,
                                PriceUSDT = data.LastPrice,
                                Change24h = data.PriceChange,
                                ChangePercent24h = data.PriceChangePercent,
                                Timestamp = DateTime.Now
                            };
                            
                            callback?.Invoke(price);
                        }
                        else
                        {
                            Log.Error("[CryptoTrader] Failed to parse price response");
                            callback?.Invoke(null);
                        }
                    }
                    else
                    {
                        Log.Error($"[CryptoTrader] Failed to fetch price: {request.error}");
                        callback?.Invoke(null);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[CryptoTrader] Error parsing price response: {ex.Message}");
                    callback?.Invoke(null);
                }
            }
            
            // Cleanup
            if (cleanup != null)
            {
                UnityEngine.Object.Destroy(cleanup);
            }
        }

        private static IEnumerator GetKlineDataCoroutine(System.Action<List<CandleData>> callback, GameObject cleanup, string interval, int limit)
        {
            var url = $"{BINANCE_API_BASE}/klines?symbol={SYMBOL}&interval={interval}&limit={limit}";
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)TIMEOUT_SECONDS;
                request.SetRequestHeader("User-Agent", "RimWorld-CryptoTrader/1.0");
                
                yield return request.SendWebRequest();
                
                try
                {
                    // Check for errors using older Unity API
                    if (!request.isNetworkError && !request.isHttpError)
                    {
                        var response = request.downloadHandler.text;
                        var candleData = SimpleJsonParser.ParseKlineResponse(response);
                        
                        callback?.Invoke(candleData);
                    }
                    else
                    {
                        Log.Error($"[CryptoTrader] Failed to fetch kline data: {request.error}");
                        callback?.Invoke(new List<CandleData>());
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[CryptoTrader] Error parsing kline response: {ex.Message}");
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