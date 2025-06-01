using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorldCryptoTrader.Models
{
    public class PlayerCryptoData : GameComponent
    {
        private float silverDeposited = 0f;
        private Dictionary<string, float> cryptoHoldings = new Dictionary<string, float>();
        private Dictionary<string, float> totalInvested = new Dictionary<string, float>();
        private List<TradeTransaction> transactions = new List<TradeTransaction>();
        private List<string> trackedCryptos = new List<string>();

        public float SilverDeposited { get => silverDeposited; set => silverDeposited = value; }
        public Dictionary<string, float> CryptoHoldings { get => cryptoHoldings; set => cryptoHoldings = value ?? new Dictionary<string, float>(); }
        public Dictionary<string, float> TotalInvested { get => totalInvested; set => totalInvested = value ?? new Dictionary<string, float>(); }
        public List<TradeTransaction> Transactions { get => transactions; set => transactions = value ?? new List<TradeTransaction>(); }
        public List<string> TrackedCryptos { get => trackedCryptos; set => trackedCryptos = value ?? new List<string>(); }

        // Legacy properties for backward compatibility
        public float GoldDeposited 
        { 
            get => silverDeposited; 
            set => silverDeposited = value; 
        }
        
        public float BTCHoldings 
        { 
            get => GetCryptoHolding("BTCUSDT"); 
            set => SetCryptoHolding("BTCUSDT", value); 
        }

        public PlayerCryptoData() 
        { 
            InitializeDefaults();
        }
        
        public PlayerCryptoData(Game game) 
        { 
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            if (trackedCryptos == null || trackedCryptos.Count == 0)
            {
                trackedCryptos = new List<string> { "BTCUSDT" }; // Default to BTC
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref silverDeposited, "silverDeposited", 0f);
            Scribe_Collections.Look(ref cryptoHoldings, "cryptoHoldings", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref totalInvested, "totalInvested", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref transactions, "transactions", LookMode.Deep);
            Scribe_Collections.Look(ref trackedCryptos, "trackedCryptos", LookMode.Value);

            // Legacy support - migrate old gold data to silver
            float legacyGold = 0f;
            float legacyBTC = 0f;
            float legacyTotalInvested = 0f;
            Scribe_Values.Look(ref legacyGold, "goldDeposited", 0f);
            Scribe_Values.Look(ref legacyBTC, "btcHoldings", 0f);
            Scribe_Values.Look(ref legacyTotalInvested, "totalInvested", 0f);

            if (Scribe.mode == LoadSaveMode.LoadingVars && legacyGold > 0)
            {
                silverDeposited = legacyGold;
                SetCryptoHolding("BTCUSDT", legacyBTC);
                SetTotalInvested("BTCUSDT", legacyTotalInvested);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                InitializeDefaults();
                if (cryptoHoldings == null) cryptoHoldings = new Dictionary<string, float>();
                if (totalInvested == null) totalInvested = new Dictionary<string, float>();
                if (transactions == null) transactions = new List<TradeTransaction>();
            }
        }

        public float GetCryptoHolding(string symbol)
        {
            return cryptoHoldings.TryGetValue(symbol, out float value) ? value : 0f;
        }

        public void SetCryptoHolding(string symbol, float amount)
        {
            cryptoHoldings[symbol] = amount;
        }

        public float GetTotalInvested(string symbol)
        {
            return totalInvested.TryGetValue(symbol, out float value) ? value : 0f;
        }

        public void SetTotalInvested(string symbol, float amount)
        {
            totalInvested[symbol] = amount;
        }

        public void AddTrackedCrypto(string symbol)
        {
            if (!trackedCryptos.Contains(symbol))
            {
                trackedCryptos.Add(symbol);
            }
        }

        public void RemoveTrackedCrypto(string symbol)
        {
            if (symbol != "BTCUSDT") // Always keep BTC
            {
                trackedCryptos.Remove(symbol);
                cryptoHoldings.Remove(symbol);
                totalInvested.Remove(symbol);
            }
        }

        public float CurrentValue(string symbol, float currentPrice)
        {
            return GetCryptoHolding(symbol) * currentPrice;
        }

        public float ProfitLoss(string symbol, float currentPrice)
        {
            return CurrentValue(symbol, currentPrice) - GetTotalInvested(symbol);
        }

        public float ProfitLossPercentage(string symbol, float currentPrice)
        {
            var invested = GetTotalInvested(symbol);
            if (invested == 0) return 0;
            return (ProfitLoss(symbol, currentPrice) / invested) * 100;
        }

        public float GetTotalPortfolioValue(Dictionary<string, float> currentPrices)
        {
            float total = 0f;
            foreach (var symbol in trackedCryptos)
            {
                if (currentPrices.TryGetValue(symbol, out float price))
                {
                    total += CurrentValue(symbol, price);
                }
            }
            return total + silverDeposited;
        }

        public float GetTotalPortfolioProfitLoss(Dictionary<string, float> currentPrices)
        {
            float totalPL = 0f;
            foreach (var symbol in trackedCryptos)
            {
                if (currentPrices.TryGetValue(symbol, out float price))
                {
                    totalPL += ProfitLoss(symbol, price);
                }
            }
            return totalPL;
        }

        // Legacy methods for backward compatibility
        public float CurrentValue(float currentPrice)
        {
            return CurrentValue("BTCUSDT", currentPrice);
        }

        public float ProfitLoss(float currentPrice)
        {
            return ProfitLoss("BTCUSDT", currentPrice);
        }

        public float ProfitLossPercentage(float currentPrice)
        {
            return ProfitLossPercentage("BTCUSDT", currentPrice);
        }
    }

    public class TradeTransaction : IExposable
    {
        private string type;
        private string symbol;
        private float amount;
        private float price;
        private float silverUsed;
        private string timestamp;

        public string Type { get => type; set => type = value; }
        public string Symbol { get => symbol; set => symbol = value; }
        public float Amount { get => amount; set => amount = value; }
        public float Price { get => price; set => price = value; }
        public float SilverUsed { get => silverUsed; set => silverUsed = value; }
        public string Timestamp { get => timestamp; set => timestamp = value; }

        // Legacy property for backward compatibility
        public float GoldUsed { get => silverUsed; set => silverUsed = value; }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref symbol, "symbol", "BTCUSDT");
            Scribe_Values.Look(ref amount, "amount");
            Scribe_Values.Look(ref price, "price");
            Scribe_Values.Look(ref silverUsed, "silverUsed");
            Scribe_Values.Look(ref timestamp, "timestamp");

            // Legacy support
            float legacyGoldUsed = 0f;
            Scribe_Values.Look(ref legacyGoldUsed, "goldUsed", 0f);
            if (Scribe.mode == LoadSaveMode.LoadingVars && legacyGoldUsed > 0 && silverUsed == 0)
            {
                silverUsed = legacyGoldUsed;
            }
        }
    }
}
