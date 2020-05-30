using System;
using System.Collections.Generic;

namespace cryptomonitor_cs
{
    public class SymbolState
    {
        private string exchangeName;
        private string symbol;
        private decimal ask;
        private decimal bid;
        Dictionary<decimal, decimal> asks_vols { get; set; }
        Dictionary<decimal, decimal> bids_vols { get; set; }

        public SymbolState(string exchName, string sym)
        {
            this.exchangeName = exchName;
            this.symbol = sym;
        }
        public string Exchange
        {
            get
            {
                return this.exchangeName;
            }
        }
        public string Symbol
        {
            get
            {
                return this.symbol;
            }
        }
        public decimal Ask
        {
            get
            {
                return this.ask;
            }
            set
            {
                if (value != this.ask)
                {
                    this.ask = value;
                }
            }
        }
        public decimal Bid
        {
            get
            {
                return this.bid;
            }
            set
            {
                if (value != this.bid)
                {
                    this.bid = value;
                }
            }
        }
    }
}
