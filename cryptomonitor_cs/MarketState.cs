using System;
using System.IO;

namespace cryptomonitor_cs
{
    public class MarketState
    {
        private string symbol;
        private decimal minVolume;

        private SymbolState objA;
        private SymbolState objB;
        private SymbolState objC;

        private decimal lastA_Ask;
        private decimal lastA_Bid;
        private decimal lastB_Ask;
        private decimal lastB_Bid;
        private decimal lastC_Ask;
        private decimal lastC_Bid;

        DiffState AB;
        DiffState AC;
        DiffState BC;


        public MarketState(string sym, ref SymbolState a, ref SymbolState b, ref SymbolState c, decimal minDiffValue, decimal MinStepValue, decimal minVolume)
        {
            this.symbol = sym;
            this.minVolume = minVolume;
            this.objA = a;
            this.objB = b;
            this.objC = c;

            this.AB = new DiffState(sym, ref a, ref b, minDiffValue, MinStepValue);
            this.AC = new DiffState(sym, ref a, ref c, minDiffValue, MinStepValue);
            this.BC = new DiffState(sym, ref b, ref c, minDiffValue, MinStepValue);
        }
        

        public void WriteMarket()
        {
            if (this.objA.Ask != this.lastA_Ask || this.objB.Ask != this.lastB_Ask || this.objA.Bid != this.lastA_Bid || this.objB.Bid != this.lastB_Bid || this.objC.Ask != this.lastC_Ask || this.objC.Bid != this.lastC_Bid)
            {
                string message = $"{DateTime.Now} {this.symbol}:" +
                    $"\n{this.objA.Exchange} – ask {objA.Ask}; bid {objA.Bid};" +
                    $"\n{objB.Exchange} – ask {objB.Ask}; bid {objB.Bid};" +
                    $"\n{objC.Exchange} – ask {objC.Ask}; bid {objC.Bid};";
                message += "\n---------------------------\n";
                File.AppendAllText("Market.log", message);
                this.lastA_Ask = objA.Ask;
                this.lastB_Ask = objB.Ask;
                this.lastC_Ask = objC.Ask;
                this.lastA_Bid = objA.Bid;
                this.lastB_Bid = objB.Bid;
                this.lastC_Bid = objC.Bid;
                this.AB.WriteProfit();
                this.AC.WriteProfit();
                this.BC.WriteProfit();
            }
        }

        public decimal MinVolume
        {
            get
            {
                return this.minVolume;
            }
        }
    }
}
