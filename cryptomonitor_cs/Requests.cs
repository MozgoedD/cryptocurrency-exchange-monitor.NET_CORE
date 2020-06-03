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

    public class HuobiJson
    {
        public class Request
        {
            [JsonPropertyName("sub")]
            public string Sub { get; set; }
            [JsonPropertyName("id")]
            public string Id { get; set; }
        }
        public class Init
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
            [JsonPropertyName("status")]
            public string Status { get; set; }
            [JsonPropertyName("subbed")]
            public string Subbed { get; set; }
        }
        public class Tick
        {
            [JsonPropertyName("bids")]
            public ArrayList Bids { get; set; }
            [JsonPropertyName("asks")]
            public ArrayList Asks { get; set; }
        }
        public class Data
        {
            [JsonPropertyName("ch")]
            public string Ch { get; set; }
            [JsonPropertyName("tick")]
            public Tick Tick { get; set; }
        }
        public class Ping
        {
            [JsonPropertyName("ping")]
            public long PingId { get; set; }
        }
        public class Pong
        {
            [JsonPropertyName("pong")]
            public long PongId { get; set; }
        }
    }
}
