using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorldCryptoTrader.Models
{
    public class PlayerCryptoData : GameComponent
    {
        private decimal silverDeposited = 0m;
        private Dictionary<string, decimal> cryptoHoldings = new Dictionary<string, decimal>();
        private Dictionary<string, decimal> totalInvested = new Dictionary<string, decimal>();
        private List<TradeTransaction> transactions = new List<TradeTransaction>();
        private List<string> trackedCryptos = new List<string>();

        public decimal SilverDeposited { get => silverDeposited; set => silverDeposited = value; }
        public Dictionary<string, decimal> CryptoHoldings { get => cryptoHoldings; set => cryptoHoldings = value ?? new Dictionary<string, decimal>(); }
        public Dictionary<string, decimal> TotalInvested { get => totalInvested; set => totalInvested = value ?? new Dictionary<string, decimal>(); }
        public List<TradeTransaction> Transactions { get => transactions; set => transactions = value ?? new List<TradeTransaction>(); }
        public List<string> TrackedCryptos { get => trackedCryptos; set => trackedCryptos = value ?? new List<string>(); }

        // Legacy properties for backward compatibility
        public decimal GoldDeposited 
        { 
            get => silverDeposited; 
            set => silverDeposited = value; 
        }
        
        public decimal BTCHoldings 
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
            Scribe_Values.Look(ref silverDeposited, "silverDeposited", 0m);
            Scribe_Collections.Look(ref cryptoHoldings, "cryptoHoldings", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref totalInvested, "totalInvested", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref transactions, "transactions", LookMode.Deep);
            Scribe_Collections.Look(ref trackedCryptos, "trackedCryptos", LookMode.Value);

            // Legacy support - migrate old gold data to silver
            decimal legacyGold = 0m;
            decimal legacyBTC = 0m;
            decimal legacyTotalInvested = 0m;
            Scribe_Values.Look(ref legacyGold, "goldDeposited", 0m);
            Scribe_Values.Look(ref legacyBTC, "btcHoldings", 0m);
            Scribe_Values.Look(ref legacyTotalInvested, "totalInvested", 0m);

            if (Scribe.mode == LoadSaveMode.LoadingVars && legacyGold > 0)
            {
                silverDeposited = legacyGold;
                SetCryptoHolding("BTCUSDT", legacyBTC);
                SetTotalInvested("BTCUSDT", legacyTotalInvested);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                InitializeDefaults();
                if (cryptoHoldings == null) cryptoHoldings = new Dictionary<string, decimal>();
                if (totalInvested == null) totalInvested = new Dictionary<string, decimal>();
                if (transactions == null) transactions = new List<TradeTransaction>();
            }
        }

        public decimal GetCryptoHolding(string symbol)
        {
            return cryptoHoldings.TryGetValue(symbol, out decimal value) ? value : 0m;
        }

        public void SetCryptoHolding(string symbol, decimal amount)
        {
            cryptoHoldings[symbol] = amount;
        }

        public decimal GetTotalInvested(string symbol)
        {
            return totalInvested.TryGetValue(symbol, out decimal value) ? value : 0m;
        }

        public void SetTotalInvested(string symbol, decimal amount)
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

        public decimal CurrentValue(string symbol, decimal currentPrice)
        {
            return GetCryptoHolding(symbol) * currentPrice;
        }

        public decimal ProfitLoss(string symbol, decimal currentPrice)
        {
            return CurrentValue(symbol, currentPrice) - GetTotalInvested(symbol);
        }

        public decimal ProfitLossPercentage(string symbol, decimal currentPrice)
        {
            var invested = GetTotalInvested(symbol);
            if (invested == 0) return 0;
            return (ProfitLoss(symbol, currentPrice) / invested) * 100;
        }

        public decimal GetTotalPortfolioValue(Dictionary<string, decimal> currentPrices)
        {
            decimal total = 0m;
            foreach (var symbol in trackedCryptos)
            {
                if (currentPrices.TryGetValue(symbol, out decimal price))
                {
                    total += CurrentValue(symbol, price);
                }
            }
            return total + silverDeposited;
        }

        public decimal GetTotalPortfolioProfitLoss(Dictionary<string, decimal> currentPrices)
        {
            decimal totalPL = 0m;
            foreach (var symbol in trackedCryptos)
            {
                if (currentPrices.TryGetValue(symbol, out decimal price))
                {
                    totalPL += ProfitLoss(symbol, price);
                }
            }
            return totalPL;
        }

        // Legacy methods for backward compatibility
        public decimal CurrentValue(decimal currentPrice)
        {
            return CurrentValue("BTCUSDT", currentPrice);
        }

        public decimal ProfitLoss(decimal currentPrice)
        {
            return ProfitLoss("BTCUSDT", currentPrice);
        }

        public decimal ProfitLossPercentage(decimal currentPrice)
        {
            return ProfitLossPercentage("BTCUSDT", currentPrice);
        }
    }

    public class TradeTransaction : IExposable
    {
        private string type;
        private string symbol;
        private decimal amount;
        private decimal price;
        private decimal silverUsed;
        private string timestamp;

        public string Type { get => type; set => type = value; }
        public string Symbol { get => symbol; set => symbol = value; }
        public decimal Amount { get => amount; set => amount = value; }
        public decimal Price { get => price; set => price = value; }
        public decimal SilverUsed { get => silverUsed; set => silverUsed = value; }
        public string Timestamp { get => timestamp; set => timestamp = value; }

        // Legacy property for backward compatibility
        public decimal GoldUsed { get => silverUsed; set => silverUsed = value; }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref symbol, "symbol", "BTCUSDT");
            Scribe_Values.Look(ref amount, "amount");
            Scribe_Values.Look(ref price, "price");
            Scribe_Values.Look(ref silverUsed, "silverUsed");
            Scribe_Values.Look(ref timestamp, "timestamp");

            // Legacy support
            decimal legacyGoldUsed = 0m;
            Scribe_Values.Look(ref legacyGoldUsed, "goldUsed", 0m);
            if (Scribe.mode == LoadSaveMode.LoadingVars && legacyGoldUsed > 0 && silverUsed == 0)
            {
                silverUsed = legacyGoldUsed;
            }
        }
    }
}
