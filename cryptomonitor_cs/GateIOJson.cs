using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Collections;
using System.Text.Json;

namespace cryptomonitor_cs
{
    public class GateIOJson
    {
        public class Request
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }
            [JsonPropertyName("method")]
            public string Method { get; set; }
            [JsonPropertyName("params")]
            public ArrayList Params { get; set; }
        }

        public class Result
        {
            [JsonPropertyName("status")]
            public string Status { get; set; }
        }

        public class GateIOInit
        {
            [JsonPropertyName("error")]
            public string Error { get; set; }
            [JsonPropertyName("result")]
            public Result Result { get; set; }
            [JsonPropertyName("id")]
            public int Id { get; set; }
        }

        public class GateIOCoin
        {
            [JsonPropertyName("method")]
            public string Method { get; set; }
            [JsonPropertyName("params")]
            public ArrayList Params { get; set; }
        }

        public class ParamsAskBid
        {
            [JsonPropertyName("asks")]
            public ArrayList Asks { get; set; }
            [JsonPropertyName("bids")]
            public ArrayList Bids { get; set; }
        }

        public class ParamsAsk
        {
            [JsonPropertyName("asks")]
            public ArrayList Asks { get; set; }
        }

        public class ParamsBid
        {
            [JsonPropertyName("bids")]
            public ArrayList Bids { get; set; }
        }
    }
}
