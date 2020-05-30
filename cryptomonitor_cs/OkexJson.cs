using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Collections;
using System.Text.Json;

namespace cryptomonitor_cs
{
    public class OkexJson
    {
        public class Request
        {
            [JsonPropertyName("op")]
            public string Op { get; set; }
            [JsonPropertyName("args")]
            public ArrayList Args { get; set; }
        }

        public class OkexInit
        {
            [JsonPropertyName("event")]
            public string Event { get; set; }
            [JsonPropertyName("channel")]
            public string Channel { get; set; }
        }

        public class Data
        {
            [JsonPropertyName("asks")]
            public ArrayList Asks { get; set; }
            [JsonPropertyName("bids")]
            public ArrayList Bids { get; set; }
            [JsonPropertyName("instrument_id")]
            public string InstrumentID { get; set; }
            [JsonPropertyName("timestamp")]
            public string Timestamp { get; set; }
        }

        public class OkexCoin
        {
            [JsonPropertyName("table")]
            public string Table { get; set; }
            [JsonPropertyName("action")]
            public string Action { get; set; }
            [JsonPropertyName("data")]
            public ArrayList Data { get; set; }
        }
    }
}
