using System;
using System.IO;

namespace cryptomonitor_cs
{
    public class MarketState
    {
        private string symbol;
        private SymbolState gateioObj;
        private SymbolState okexObj;
        private decimal lastGateioAsk;
        private decimal lastGateioBid;
        private decimal lastOkexAsk;
        private decimal lastOkexBid;

        private bool wasGateOkexProfitable;
        private decimal minDiffValue;
        private decimal minStepValue;

        private decimal lastGateOkex;
        private decimal lastOkexGate;


        public MarketState(string sym, ref SymbolState gateio, ref SymbolState okex, decimal minDiffValue_, decimal MinStepValue_)
        {
            this.symbol = sym;
            this.gateioObj = gateio;
            this.okexObj = okex;
            this.wasGateOkexProfitable = false;
            this.minDiffValue = minDiffValue_;
            this.minStepValue = MinStepValue_;
        }

        public bool IsGateOkex(SymbolState symA, SymbolState symB)
        {
            if (symA.Exchange == "gate.io" && symB.Exchange == "okex") { return true; }
            else { return false; }
        }

        public void WriteProfit(SymbolState symA, SymbolState symB)
        {
            if (symA.Ask != 0 && symA.Bid != 0 && symB.Ask != 0 && symB.Bid != 0)
            {
                string message = $"{DateTime.Now} ";

                if (symA.Ask < symB.Bid)
                {
                    decimal AB = ((symB.Bid - symA.Ask) / symA.Ask) * 100;
                    if (AB >= this.minDiffValue && (Math.Abs(AB-this.lastGateOkex) >= this.minStepValue))
                    {
                        message += $"{symbol} diff {symA.Exchange} –> {symB.Exchange} = {AB}";
                        message += "\n-----------------------------\n";
                        File.AppendAllText("Profit.log", message);
                        if (IsGateOkex(symA, symB))
                        {
                            wasGateOkexProfitable = true;
                            this.lastGateOkex = AB;
                        }
                    }
                }
                else if (symB.Ask < symA.Bid)
                {
                    decimal BA = ((symA.Bid - symB.Ask) / symB.Ask) * 100;
                    if (BA >= this.minDiffValue && (Math.Abs(BA - this.lastOkexGate) >= this.minStepValue))
                    {
                        message += $"{symbol} diff {symB.Exchange} –> {symA.Exchange} = {BA}";
                        message += "\n-----------------------------\n";
                        File.AppendAllText("Profit.log", message);
                        if (IsGateOkex(symA, symB))
                        {
                            wasGateOkexProfitable = true;
                            this.lastOkexGate = BA;
                        }
                    }

                }
                else
                {
                    if (wasGateOkexProfitable == true)
                    {
                        message += $"{symbol} diff {symA.Exchange} –> {symB.Exchange} no longer profitable";
                        message += "\n-----------------------------\n";
                        File.AppendAllText("Profit.log", message);
                    }                    
                    if (IsGateOkex(symA, symB)) { wasGateOkexProfitable = false; }
                }

            }
        }

        public void WriteMarket()
        {
            if (gateioObj.Ask != this.lastGateioAsk || okexObj.Ask != this.lastOkexAsk || gateioObj.Bid != this.lastGateioBid || okexObj.Bid != this.lastOkexBid)
            {
                string message = $"{DateTime.Now} {this.symbol}:\n{this.gateioObj.Exchange} – ask {gateioObj.Ask}; bid {gateioObj.Bid};\n{okexObj.Exchange} – ask {okexObj.Ask}; bid {okexObj.Bid};";
                File.AppendAllText("Market.log", message);
                File.AppendAllText("Market.log", "\n---------------------------\n");
                this.lastGateioAsk = gateioObj.Ask;
                this.lastOkexAsk = okexObj.Ask;
                this.lastGateioBid = gateioObj.Bid;
                this.lastOkexBid = okexObj.Bid;
                WriteProfit(this.gateioObj, this.okexObj);
            }
        }
    }
}
