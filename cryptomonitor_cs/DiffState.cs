using System;
using System.IO;

namespace cryptomonitor_cs
{
    public class DiffState
    {
        private string symbol;
        private SymbolState objA;
        private SymbolState objB;
        private decimal minDiffValue;
        private decimal minStepValue;

        private decimal lastDiffAB;
        private bool wasProfitableAB;

        private decimal lastDiffBA;
        private bool wasProfitableBA;

        public DiffState(string sym, ref SymbolState a, ref SymbolState b, decimal minDiffValue_, decimal MinStepValue_)
        {
            this.symbol = sym;
            this.objA = a;
            this.objB = b;
            this.minDiffValue = minDiffValue_;
            this.minStepValue = MinStepValue_;
        }

        private void noLongerProfitableAB()
        {
            if (this.wasProfitableAB == true)
            {
                string message = $"{DateTime.Now} ";
                message += $"{symbol} diff {this.objA.Exchange} –> {this.objB.Exchange} no longer profitable";
                message += "\n-----------------------------\n";
                File.AppendAllText("Profit.log", message);                
            }
            this.wasProfitableAB = false;
            this.lastDiffAB = 0m;
        }

        private void noLongerProfitableBA()
        {
            if (this.wasProfitableBA == true)
            {
                string message = $"{DateTime.Now} ";
                message += $"{symbol} diff {this.objB.Exchange} –> {this.objA.Exchange} no longer profitable";
                message += "\n-----------------------------\n";
                File.AppendAllText("Profit.log", message);
            }
            this.wasProfitableBA = false;
            this.lastDiffBA = 0m;
        }

        public void WriteProfit()
        {
            if (this.objA.Ask != 0 && this.objA.Bid != 0 && this.objB.Ask != 0 && this.objB.Bid != 0)
            {
                string message = $"{DateTime.Now} ";

                if (this.objA.Ask < this.objB.Bid)
                {
                    decimal AB = ((this.objB.Bid - this.objA.Ask) / this.objA.Ask) * 100;
                    if (AB >= this.minDiffValue && (Math.Abs(AB - this.lastDiffAB) >= this.minStepValue))
                    {
                        message += $"{symbol} diff {this.objA.Exchange} –> {this.objB.Exchange} = {AB}";
                        message += "\n-----------------------------\n";
                        File.AppendAllText("Profit.log", message);
                        this.wasProfitableAB = true;
                        this.lastDiffAB = AB;
                    }
                    else
                    {
                        noLongerProfitableAB();
                    }
                }
                else if (this.objB.Ask < this.objA.Bid)
                {
                    decimal BA = ((this.objA.Bid - this.objB.Ask) / this.objB.Ask) * 100;
                    if (BA >= this.minDiffValue && (Math.Abs(BA - this.lastDiffBA) >= this.minStepValue))
                    {
                        message += $"{symbol} diff {this.objB.Exchange} –> {this.objA.Exchange} = {BA}";
                        message += "\n-----------------------------\n";
                        File.AppendAllText("Profit.log", message);
                        this.wasProfitableBA = true;
                        this.lastDiffBA = BA;
                        
                    }
                    else
                    {
                        noLongerProfitableBA();
                    }
                }
                else
                {
                    noLongerProfitableAB();
                    noLongerProfitableBA();
                }
            }
        }
    }
}
