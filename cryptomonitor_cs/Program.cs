using System;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.IO;
using System.IO.Compression;
using System.Configuration;
using System.Linq;
using System.Text;

namespace cryptomonitor_cs
{
    public class ValueVolume
    {
        public decimal Value { get; set; }
        public decimal Volume { get; set; }
    }

    class Program
    {
        // в Responce.log записываются все запросы
        // в Market.log цена аска/бида для каждой монеты на каждой бирже
        // в Profit.log запись о профите в процентах с шагом и минимальным значением
        // также создаются логи, содержащие "стакан" асков и бидов по 10 на каждую монету и биржу
        // все логи в /cryptomonitor_cs/bin/Debug/netcoreapp3.0/
        // запуск приложения /cryptomonitor_cs/bin/Debug/netcoreapp3.0/dotnet cryptomonitor_cs.dll (по крайней мере на Unix)
        // при запуске в консоли должно отобразиться ОК для каждой монеты-биржи
        private static readonly object GATE1 = new object();

        public static string DecompressOkex(byte[] baseBytes)
        {
            using (var decompressedStream = new MemoryStream())
            using (var compressedStream = new MemoryStream(baseBytes))
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(decompressedStream);
                decompressedStream.Position = 0;
                using (var streamReader = new StreamReader(decompressedStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        public static byte[] DecompressHuobi(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        public static void WriteResponce(string exchName, string Responce)
        {
            string message = $"{DateTime.Now} {exchName}: {Responce} \n";

            File.AppendAllText("Responce.log", message);
        }

        public static void WriteGlass(string exchName, string sym, List<ValueVolume> listAsk, List<ValueVolume> listBid)
        {
            string message = $"\n{DateTime.Now} {exchName} {sym}:\n";
            message += "----------------------------ASK\n";
            int i = 0;
            foreach (ValueVolume vv in listAsk)
            {
                if (i >= 10) { break;}
                message += $"{vv.Value} {vv.Volume}\n";
                i++;
            }
            message += "----------------------------BID\n";
            i = 0;
            foreach (ValueVolume vv in listBid)
            {
                if (i >=10) { break; }
                message += $"{vv.Value} {vv.Volume}\n";
                i++;
            }
            message += "-------------------------------";
            File.AppendAllText($"Glass{exchName}{sym}.log", message);
        }

        public static WebsocketClient StartGateIO(Uri gateio_url, SymbolState symObj, MarketState marketObj)
        {
            List<ValueVolume> asks_vols = new List<ValueVolume>();
            List<ValueVolume> bids_vols = new List<ValueVolume>();

            var gateioClient = new WebsocketClient(gateio_url);
            gateioClient.ReconnectTimeout = TimeSpan.FromSeconds(30);
            gateioClient.ReconnectionHappened.Subscribe(info =>
                Console.WriteLine($"Reconnection happened, type: {info.Type}"));
            gateioClient
                .MessageReceived
                .ObserveOn(TaskPoolScheduler.Default)
                .Synchronize(GATE1)
                .Subscribe(msg =>
            {             
            string gateioJson = msg.ToString();
            WriteResponce("gate.io", gateioJson);

            if (gateioJson.Contains("result"))
            {
                GateIOJson.GateIOInit gateio = JsonSerializer.Deserialize<GateIOJson.GateIOInit>(gateioJson);
                if (gateio.Result.Status == "success")
                {
                    Console.WriteLine($"gateio: {symObj.Symbol}-USDT ok!");
                }
            }
            else if (gateioJson.Contains("method"))
            {
                GateIOJson.GateIOCoin gateio = JsonSerializer.Deserialize<GateIOJson.GateIOCoin>(gateioJson);
                string symbol = gateio.Params[2].ToString();
                bool isClean = Convert.ToBoolean(gateio.Params[0].ToString());

                if (symbol == $"{symObj.Symbol}_USDT")
                {
                    if (isClean)
                    {
                        asks_vols.Clear();
                        bids_vols.Clear();
                        GateIOJson.ParamsAskBid gateioAskBid = JsonSerializer.Deserialize<GateIOJson.ParamsAskBid>(gateio.Params[1].ToString());
                        foreach (object array in gateioAskBid.Asks)
                        {
                            List<string> ask_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                            decimal value = Convert.ToDecimal(ask_vol[0], CultureInfo.InvariantCulture);
                            decimal volume = Convert.ToDecimal(ask_vol[1], CultureInfo.InvariantCulture);
                            asks_vols.Add(new ValueVolume() { Value = value, Volume = volume });
                        }

                        foreach (object array in gateioAskBid.Bids)
                        {
                            List<string> bid_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                            decimal value = Convert.ToDecimal(bid_vol[0], CultureInfo.InvariantCulture);
                            decimal volume = Convert.ToDecimal(bid_vol[1], CultureInfo.InvariantCulture);
                            bids_vols.Add(new ValueVolume() { Value = value, Volume = volume });
                        }

                    }
                    else if (!isClean)
                    {
                        if (gateio.Params[1].ToString().Contains("asks") && !(gateio.Params[1].ToString().Contains("bids")))
                        {
                            GateIOJson.ParamsAsk gateioAsk = JsonSerializer.Deserialize<GateIOJson.ParamsAsk>(gateio.Params[1].ToString());
                            foreach (object array in gateioAsk.Asks)
                            {
                                List<string> ask_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                                decimal value = Convert.ToDecimal(ask_vol[0], CultureInfo.InvariantCulture);
                                decimal volume = Convert.ToDecimal(ask_vol[1], CultureInfo.InvariantCulture);
                                bool isFound = false;
                                foreach (ValueVolume vv in asks_vols)
                                {
                                    if (value == vv.Value)
                                    {
                                        vv.Volume = volume;
                                        isFound = true;
                                    }
                                }
                                if (!isFound) { asks_vols.Add(new ValueVolume() { Value = value, Volume = volume }); }
                            }
                        }
                        else if (gateio.Params[1].ToString().Contains("bids") && !(gateio.Params[1].ToString().Contains("asks")))
                        {
                            GateIOJson.ParamsBid gateioBid = JsonSerializer.Deserialize<GateIOJson.ParamsBid>(gateio.Params[1].ToString());
                            foreach (object array in gateioBid.Bids)
                            {
                                List<string> bid_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                                decimal value = Convert.ToDecimal(bid_vol[0], CultureInfo.InvariantCulture);
                                decimal volume = Convert.ToDecimal(bid_vol[1], CultureInfo.InvariantCulture);
                                bool isFound = false;
                                foreach (ValueVolume vv in bids_vols)
                                {
                                    if (value == vv.Value)
                                    {
                                        vv.Volume = volume;
                                        isFound = true;
                                    }
                                }
                                if (!isFound)
                                {
                                    bids_vols.Add(new ValueVolume() { Value = value, Volume = volume });
                                }
                            }
                        }
                        else if (gateio.Params[1].ToString().Contains("bids") && gateio.Params[1].ToString().Contains("asks"))
                        {
                            GateIOJson.ParamsAskBid gateioAskBid = JsonSerializer.Deserialize<GateIOJson.ParamsAskBid>(gateio.Params[1].ToString());
                            foreach (object array in gateioAskBid.Asks)
                            {
                                List<string> ask_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                                decimal value = Convert.ToDecimal(ask_vol[0], CultureInfo.InvariantCulture);
                                decimal volume = Convert.ToDecimal(ask_vol[1], CultureInfo.InvariantCulture);
                                bool isFound = false;
                                foreach (ValueVolume vv in asks_vols)
                                {
                                    if (value == vv.Value)
                                    {
                                        vv.Volume = volume;
                                        isFound = true;
                                    }
                                }
                                if (!isFound) { asks_vols.Add(new ValueVolume() { Value = value, Volume = volume }); }
                            }
                            foreach (object array in gateioAskBid.Bids)
                            {
                                List<string> bid_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                                decimal value = Convert.ToDecimal(bid_vol[0], CultureInfo.InvariantCulture);
                                decimal volume = Convert.ToDecimal(bid_vol[1], CultureInfo.InvariantCulture);
                                bool isFound = false;
                                foreach (ValueVolume vv in bids_vols)
                                {
                                    if (value == vv.Value)
                                    {
                                        vv.Volume = volume;
                                        isFound = true;
                                    }
                                }
                                if (!isFound)
                                {
                                    bids_vols.Add(new ValueVolume() { Value = value, Volume = volume });
                                }
                            }
                        }
                    }

                    asks_vols.RemoveAll(x => x.Volume <= marketObj.MinVolume);
                    bids_vols.RemoveAll(x => x.Volume <= marketObj.MinVolume);

                    asks_vols.Sort((a, b) => a.Value.CompareTo(b.Value));
                    bids_vols.Sort((a, b) => b.Value.CompareTo(a.Value));

                    WriteGlass("gate.io", symObj.Symbol, asks_vols, bids_vols);

                    foreach (ValueVolume vv in asks_vols)
                    {
                        if (vv.Volume >= marketObj.MinVolume)
                        {
                            symObj.Ask = vv.Value;
                            marketObj.WriteMarket();
                            break;
                        }
                    }
                    foreach (ValueVolume vv in bids_vols)
                    {
                        if (vv.Volume >= marketObj.MinVolume)
                        {
                            symObj.Bid = vv.Value;
                            marketObj.WriteMarket();
                            break;
                        }
                    }
                }
            }
        });
            return gateioClient;
        }

        public static WebsocketClient StartOkex(Uri okex_url, SymbolState symObj, MarketState marketObj)
        {
            List<ValueVolume> asks_vols = new List<ValueVolume>();
            List<ValueVolume> bids_vols = new List<ValueVolume>();
            var okexClient = new WebsocketClient(okex_url);
            okexClient.ReconnectTimeout = TimeSpan.FromSeconds(30);
            okexClient.ReconnectionHappened.Subscribe(info =>
                Console.WriteLine($"Reconnection happened, type: {info.Type}"));
            okexClient
            .MessageReceived
            .ObserveOn(TaskPoolScheduler.Default)
            .Synchronize(GATE1)
            .Subscribe(msg =>
            {
                byte[] bytes = msg.Binary;
                string okexJson = DecompressOkex(bytes);
                WriteResponce("okex", okexJson);

                if (okexJson.Contains("event"))
                {
                    OkexJson.OkexInit okex = JsonSerializer.Deserialize<OkexJson.OkexInit>(okexJson);
                    if (okex.Channel == $"spot/depth:{symObj.Symbol}-USDT" && okex.Event == "subscribe")
                    {
                        Console.WriteLine($"okex: {symObj.Symbol}-USDT ok!");
                    }
                }
                else if (okexJson.Contains("table"))
                {
                    OkexJson.OkexCoin okex = JsonSerializer.Deserialize<OkexJson.OkexCoin>(okexJson);

                    if (okex.Table == "spot/depth")
                    {
                        if (okex.Action == "partial")
                        {
                            asks_vols.Clear();
                            bids_vols.Clear();
                            OkexJson.Data okexData = JsonSerializer.Deserialize<OkexJson.Data>(okex.Data[0].ToString());
                            if (okexData.InstrumentID == $"{symObj.Symbol}-USDT")
                            {                               
                                foreach (object array in okexData.Asks)
                                {
                                    List<string> ask_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                                    decimal value = Convert.ToDecimal(ask_vol[0], CultureInfo.InvariantCulture);
                                    decimal volume = Convert.ToDecimal(ask_vol[1], CultureInfo.InvariantCulture);
                                    asks_vols.Add(new ValueVolume() { Value = value, Volume = volume });                                    
                                }
                                foreach (object array in okexData.Bids)
                                {
                                    List<string> bid_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                                    decimal value = Convert.ToDecimal(bid_vol[0], CultureInfo.InvariantCulture);
                                    decimal volume = Convert.ToDecimal(bid_vol[1], CultureInfo.InvariantCulture);
                                    bids_vols.Add(new ValueVolume() { Value = value, Volume = volume });
                                }
                            }
                        }
                        else if (okex.Action == "update")
                        {
                            OkexJson.Data okexData = JsonSerializer.Deserialize<OkexJson.Data>(okex.Data[0].ToString());
                            if (okexData.InstrumentID == $"{symObj.Symbol}-USDT")
                            {
                                foreach (object array in okexData.Asks)
                                {
                                    List<string> ask_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                                    decimal value = Convert.ToDecimal(ask_vol[0], CultureInfo.InvariantCulture);
                                    decimal volume = Convert.ToDecimal(ask_vol[1], CultureInfo.InvariantCulture);
                                    bool isFound = false;
                                    foreach (ValueVolume vv in asks_vols)
                                    {
                                        if (value == vv.Value)
                                        {
                                            vv.Volume = volume;
                                            isFound = true;
                                        }
                                    }
                                    if (!isFound) { asks_vols.Add(new ValueVolume() { Value = value, Volume = volume }); }
                                }
                                foreach (object array in okexData.Bids)
                                {
                                    List<string> bid_vol = JsonSerializer.Deserialize<List<string>>(array.ToString());
                                    decimal value = Convert.ToDecimal(bid_vol[0], CultureInfo.InvariantCulture);
                                    decimal volume = Convert.ToDecimal(bid_vol[1], CultureInfo.InvariantCulture);
                                    bool isFound = false;
                                    foreach (ValueVolume vv in bids_vols)
                                    {
                                        if (value == vv.Value)
                                        {
                                            vv.Volume = volume;
                                            isFound = true;
                                        }
                                    }
                                    if (!isFound) { bids_vols.Add(new ValueVolume() { Value = value, Volume = volume }); }
                                }
                            }
                        }

                        asks_vols.RemoveAll(x => x.Volume <= marketObj.MinVolume);
                        bids_vols.RemoveAll(x => x.Volume <= marketObj.MinVolume);

                        asks_vols.Sort((a, b) => a.Value.CompareTo(b.Value));
                        bids_vols.Sort((a, b) => b.Value.CompareTo(a.Value));

                        WriteGlass("okex", symObj.Symbol, asks_vols, bids_vols);

                        foreach (ValueVolume vv in asks_vols)
                        {
                            if (vv.Volume >= marketObj.MinVolume)
                            {
                                symObj.Ask = vv.Value;
                                marketObj.WriteMarket();
                                break;
                            }
                        }
                        foreach (ValueVolume vv in bids_vols)
                        {
                            if (vv.Volume >= marketObj.MinVolume)
                            {
                                symObj.Bid = vv.Value;
                                marketObj.WriteMarket();
                                break;
                            }
                        }
                    }
                }
            });
            return okexClient;
        }



        public static WebsocketClient StartHuobi(Uri huobi_url, SymbolState symObj, MarketState marketObj)
        {
            var huobiClient = new WebsocketClient(huobi_url);
            huobiClient.ReconnectTimeout = TimeSpan.FromSeconds(30);
            huobiClient.ReconnectionHappened.Subscribe(info =>
                Console.WriteLine($"Reconnection happened, type: {info.Type}"));
            huobiClient
            .MessageReceived
            .ObserveOn(TaskPoolScheduler.Default)
            .Synchronize(GATE1)
            .Subscribe(msg =>
            {
                byte[] bytes = msg.Binary;
                string huobiJson = Encoding.UTF8.GetString(DecompressHuobi(bytes));
                //Console.WriteLine(huobiJson);
                WriteResponce("Huobi Global", huobiJson);
                if (huobiJson.Contains("status"))
                {
                    HuobiJson.Init huobiData = JsonSerializer.Deserialize<HuobiJson.Init>(huobiJson);
                    if (huobiData.Status == "ok" && huobiData.Subbed == $"market.{symObj.Symbol.ToLower()}usdt.depth.step0")
                    {
                        Console.WriteLine($"huobi: {symObj.Symbol}-USDT ok!");
                    }
                }
                else if (huobiJson.Contains("tick"))
                {
                    HuobiJson.Data huobiData = JsonSerializer.Deserialize<HuobiJson.Data>(huobiJson);
                    foreach (object Ask in huobiData.Tick.Asks)
                    {
                        List<decimal> ask_vol = JsonSerializer.Deserialize<List<decimal>>(Ask.ToString());
                        if (ask_vol[1] >= marketObj.MinVolume)
                        {
                            decimal ask = ask_vol[0];
                            symObj.Ask = ask;
                            break;
                        }
                    }
                    foreach (object Bid in huobiData.Tick.Bids)
                    {
                        List<decimal> bid_vol = JsonSerializer.Deserialize<List<decimal>>(Bid.ToString());
                        if (bid_vol[1] >= marketObj.MinVolume)
                        {
                            decimal bid = bid_vol[0];
                            symObj.Bid = bid;
                            break;
                        }
                    }
                    marketObj.WriteMarket();
                }                
                else if (huobiJson.Contains("ping"))
                {
                    HuobiJson.Ping huobiData = JsonSerializer.Deserialize<HuobiJson.Ping>(huobiJson);
                    long pingId = huobiData.PingId;
                    HuobiJson.Pong pongReq = new HuobiJson.Pong() { PongId = pingId };
                    string pongJson = JsonSerializer.Serialize<HuobiJson.Pong>(pongReq);
                    Task.Run(() => huobiClient.Send($"{pongJson}"));
                    //Console.WriteLine($"Huobi {symObj.Symbol} Pong");
                }
            });
            return huobiClient;
        }

        public static string GateioRequestGenerator(string sym)
        {
            ArrayList GateioParamsList = new ArrayList();
            GateioParamsList.Add($"{sym}_USDT");
            GateioParamsList.Add(30);
            GateioParamsList.Add("0.00000001");
            GateIOJson.Request gateioRequest = new GateIOJson.Request() { Id = 1, Method = "depth.subscribe", Params = GateioParamsList };
            string gateioRequestJson = JsonSerializer.Serialize<GateIOJson.Request>(gateioRequest);
            Console.WriteLine(gateioRequestJson);
            return gateioRequestJson;
        }

        public static string OkexRequestGenerator(string sym)
        {
            ArrayList OkexArgsList = new ArrayList();
            OkexArgsList.Add($"spot/depth:{sym}-USDT");
            OkexJson.Request okexRequest = new OkexJson.Request() { Op = "subscribe", Args = OkexArgsList };
            string okexRequestJson = JsonSerializer.Serialize<OkexJson.Request>(okexRequest);
            Console.WriteLine(okexRequestJson);
            return okexRequestJson;
        }

        public static string HuobiRequestGenerator(string sym)
        {
            string sub = $"market.{sym.ToLower()}usdt.depth.step0";
            string id = "1";
            HuobiJson.Request huobiRequest = new HuobiJson.Request() { Sub = sub, Id = id };
            string huobiRequestJson = JsonSerializer.Serialize<HuobiJson.Request>(huobiRequest);
            Console.WriteLine(huobiRequestJson);
            return huobiRequestJson;
        }

        static void Main(string[] args)
        {
            var exitEvent = new ManualResetEvent(false);
            var gateio_url = new Uri("wss://ws.gate.io/v3/");
            var okex_url = new Uri("wss://real.OKEx.com:8443/ws/v3");
            var huobi_url = new Uri("wss://api.huobi.pro/ws");

            // BTC
            // параметры: название биржи, название символа
            SymbolState gateioBTC = new SymbolState("gate.io", "BTC");
            SymbolState okexBTC = new SymbolState("okex", "BTC");
            SymbolState huobiBTC = new SymbolState("huobi", "BTC");
            // параметры: название символа, объекты SymbilState, минимальное значение разницы, минимальный шаг и минимальный объем аска/бида
            MarketState BTC = new MarketState("BTC", ref gateioBTC, ref okexBTC, ref huobiBTC, 1m, 0.1m, 0.01m);

            // запуск WebSocket клиента и отправка запроса для каждой пары Монета-Биржа
            WebsocketClient gateioClientBTC = StartGateIO(gateio_url, gateioBTC, BTC);
            Task.Run(() => gateioClientBTC.Start());
            string gateioRequestJsonBTC = GateioRequestGenerator(gateioBTC.Symbol);
            Task.Run(() => gateioClientBTC.Send($"{gateioRequestJsonBTC}"));

            WebsocketClient okexClientBTC = StartOkex(okex_url, okexBTC, BTC);
            Task.Run(() => okexClientBTC.Start());
            string okexRequestJsonBTC = OkexRequestGenerator(okexBTC.Symbol);
            Task.Run(() => okexClientBTC.Send($"{okexRequestJsonBTC}"));

            WebsocketClient huobiClientBTC = StartHuobi(huobi_url, huobiBTC, BTC);
            Task.Run(() => huobiClientBTC.Start());
            string huobiRequestJsonBTC = HuobiRequestGenerator(huobiBTC.Symbol);
            Task.Run(() => huobiClientBTC.Send($"{huobiRequestJsonBTC}"));


            // LTC
            SymbolState gateioLTC = new SymbolState("gate.io", "LTC");
            SymbolState okexLTC = new SymbolState("okex", "LTC");
            SymbolState huobiLTC = new SymbolState("huobi", "LTC");
            MarketState LTC = new MarketState("LTC", ref gateioLTC, ref okexLTC, ref huobiLTC, 1m, 0.1m, 1m);

            WebsocketClient gateioClientLTC = StartGateIO(gateio_url, gateioLTC, LTC);
            Task.Run(() => gateioClientLTC.Start());
            string gateioRequestJsonLTC = GateioRequestGenerator(gateioLTC.Symbol);
            Task.Run(() => gateioClientLTC.Send($"{gateioRequestJsonLTC}"));

            WebsocketClient okexClientLTC = StartOkex(okex_url, okexLTC, LTC);
            Task.Run(() => okexClientLTC.Start());
            string okexRequestJsonLTC = OkexRequestGenerator(okexLTC.Symbol);
            Task.Run(() => okexClientLTC.Send($"{okexRequestJsonLTC}"));

            WebsocketClient huobiClientLTC = StartHuobi(huobi_url, huobiLTC, LTC);
            Task.Run(() => huobiClientLTC.Start());
            string huobiRequestJsonLTC = HuobiRequestGenerator(huobiLTC.Symbol);
            Task.Run(() => huobiClientLTC.Send($"{huobiRequestJsonLTC}"));


            // ONT
            SymbolState gateioONT = new SymbolState("gate.io", "ONT");
            SymbolState okexONT = new SymbolState("okex", "ONT");
            SymbolState huobiONT = new SymbolState("huobi", "ONT");
            MarketState ONT = new MarketState("ONT", ref gateioONT, ref okexONT, ref huobiONT, 1m, 0.1m, 10m);

            WebsocketClient gateioClientONT = StartGateIO(gateio_url, gateioONT, ONT);
            Task.Run(() => gateioClientONT.Start());
            string gateioRequestJsonONT = GateioRequestGenerator(gateioONT.Symbol);
            Task.Run(() => gateioClientONT.Send($"{gateioRequestJsonONT}"));

            WebsocketClient okexClientONT = StartOkex(okex_url, okexONT, ONT);
            Task.Run(() => okexClientONT.Start());
            string okexRequestJsonONT = OkexRequestGenerator(okexONT.Symbol);
            Task.Run(() => okexClientONT.Send($"{okexRequestJsonONT}"));

            WebsocketClient huobiClientONT = StartHuobi(huobi_url, huobiONT, ONT);
            Task.Run(() => huobiClientONT.Start());
            string huobiRequestJsonONT = HuobiRequestGenerator(huobiONT.Symbol);
            Task.Run(() => huobiClientONT.Send($"{huobiRequestJsonONT}"));


            // EOS
            SymbolState gateioEOS = new SymbolState("gate.io", "EOS");
            SymbolState okexEOS = new SymbolState("okex", "EOS");
            SymbolState huobiEOS = new SymbolState("huobi", "EOS");
            MarketState EOS = new MarketState("EOS", ref gateioEOS, ref okexEOS, ref huobiEOS, 1m, 0.1m, 10m);

            WebsocketClient gateioClientEOS = StartGateIO(gateio_url, gateioEOS, EOS);
            Task.Run(() => gateioClientEOS.Start());
            string gateioRequestJsonEOS = GateioRequestGenerator(gateioEOS.Symbol);
            Task.Run(() => gateioClientEOS.Send($"{gateioRequestJsonEOS}"));

            WebsocketClient okexClientEOS = StartOkex(okex_url, okexEOS, EOS);
            Task.Run(() => okexClientEOS.Start());
            string okexRequestJsonEOS = OkexRequestGenerator(okexEOS.Symbol);
            Task.Run(() => okexClientEOS.Send($"{okexRequestJsonEOS}"));

            WebsocketClient huobiClientEOS = StartHuobi(huobi_url, huobiEOS, EOS);
            Task.Run(() => huobiClientEOS.Start());
            string huobiRequestJsonEOS = HuobiRequestGenerator(huobiEOS.Symbol);
            Task.Run(() => huobiClientEOS.Send($"{huobiRequestJsonEOS}"));

            exitEvent.WaitOne();
        }
    }
}


            
            






            
      

