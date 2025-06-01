using System.Collections.Generic;
using Verse;

namespace RimWorldCryptoTrader.Models
{
    public class PlayerCryptoData : GameComponent
    {
        private decimal goldDeposited = 0m;
        private decimal btcHoldings = 0m;
        private decimal totalInvested = 0m;
        private List<TradeTransaction> transactions = new List<TradeTransaction>();

        public decimal GoldDeposited { get => goldDeposited; set => goldDeposited = value; }
        public decimal BTCHoldings { get => btcHoldings; set => btcHoldings = value; }
        public decimal TotalInvested { get => totalInvested; set => totalInvested = value; }
        public List<TradeTransaction> Transactions { get => transactions; set => transactions = value ?? new List<TradeTransaction>(); }

        public PlayerCryptoData() { }
        public PlayerCryptoData(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref goldDeposited, "goldDeposited", 0m);
            Scribe_Values.Look(ref btcHoldings, "btcHoldings", 0m);
            Scribe_Values.Look(ref totalInvested, "totalInvested", 0m);
            Scribe_Collections.Look(ref transactions, "transactions", LookMode.Deep);
        }

        public decimal CurrentValue(decimal currentPrice)
        {
            return btcHoldings * currentPrice;
        }

        public decimal ProfitLoss(decimal currentPrice)
        {
            return CurrentValue(currentPrice) - totalInvested;
        }

        public decimal ProfitLossPercentage(decimal currentPrice)
        {
            if (totalInvested == 0) return 0;
            return (ProfitLoss(currentPrice) / totalInvested) * 100;
        }
    }

    public class TradeTransaction : IExposable
    {
        private string type;
        private decimal amount;
        private decimal price;
        private decimal goldUsed;
        private string timestamp;

        public string Type { get => type; set => type = value; }
        public decimal Amount { get => amount; set => amount = value; }
        public decimal Price { get => price; set => price = value; }
        public decimal GoldUsed { get => goldUsed; set => goldUsed = value; }
        public string Timestamp { get => timestamp; set => timestamp = value; }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref amount, "amount");
            Scribe_Values.Look(ref price, "price");
            Scribe_Values.Look(ref goldUsed, "goldUsed");
            Scribe_Values.Look(ref timestamp, "timestamp");
        }
    }
}