﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class LeanDataTests
    {
        [Test, TestCaseSource("GetLeanDataTestParameters")]
        public void GenerateZipFileName(LeanDataTestParameters parameters)
        {
            var zip = LeanData.GenerateZipFileName(parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedZipFileName, zip);
        }

        [Test, TestCaseSource("GetLeanDataTestParameters")]
        public void GenerateZipEntryName(LeanDataTestParameters parameters)
        {
            var entry = LeanData.GenerateZipEntryName(parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedZipEntryName, entry);
        }

        [Test, TestCaseSource("GetLeanDataTestParameters")]
        public void GenerateRelativeZipFilePath(LeanDataTestParameters parameters)
        {
            var relativePath = LeanData.GenerateRelativeZipFilePath(parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedRelativeZipFilePath, relativePath);
        }

        [Test, TestCaseSource("GetLeanDataTestParameters")]
        public void GenerateZipFilePath(LeanDataTestParameters parameters)
        {
            var path = LeanData.GenerateZipFilePath(Constants.DataFolder, parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedZipFilePath, path);
        }

        [Test, TestCaseSource("GetLeanDataLineTestParameters")]
        public void GenerateLine(LeanDataLineTestParameters parameters)
        {
            var line = LeanData.GenerateLine(parameters.Data, parameters.SecurityType, parameters.Resolution);
            Assert.AreEqual(parameters.ExpectedLine, line);
        }

        [Test, TestCaseSource("GetLeanDataLineTestParameters")]
        public void ParsesGeneratedLines(LeanDataLineTestParameters parameters)
        {
            // ignore time zone issues here, we'll just say everything is UTC, so no conversions are performed
            var factory = (BaseData) Activator.CreateInstance(parameters.Data.GetType());
            var parsed = factory.Reader(parameters.Config, parameters.ExpectedLine, parameters.Data.Time.Date, false);

            Assert.IsInstanceOf(parameters.Config.Type, parsed);
            Assert.AreEqual(parameters.Data.Time, parsed.Time);
            Assert.AreEqual(parameters.Data.EndTime, parsed.EndTime);
            Assert.AreEqual(parameters.Data.Symbol, parsed.Symbol);
            Assert.AreEqual(parameters.Data.Value, parsed.Value);
            if (parsed is Tick)
            {
                var expected = (Tick) parameters.Data;
                var actual = (Tick) parsed;
                Assert.AreEqual(expected.Quantity, actual.Quantity);
                Assert.AreEqual(expected.BidPrice, actual.BidPrice);
                Assert.AreEqual(expected.AskPrice, actual.AskPrice);
                Assert.AreEqual(expected.BidSize, actual.BidSize);
                Assert.AreEqual(expected.AskSize, actual.AskSize);
                Assert.AreEqual(expected.Exchange, actual.Exchange);
                Assert.AreEqual(expected.SaleCondition, actual.SaleCondition);
                Assert.AreEqual(expected.Suspicious, actual.Suspicious);
            }
            else if (parsed is TradeBar)
            {
                var expected = (TradeBar) parameters.Data;
                var actual = (TradeBar) parsed;
                AssertBarsAreEqual(expected, actual);
                Assert.AreEqual(expected.Volume, actual.Volume);
            }
            else if (parsed is QuoteBar)
            {
                var expected = (QuoteBar) parameters.Data;
                var actual = (QuoteBar) parsed;
                AssertBarsAreEqual(expected.Bid, actual.Bid);
                AssertBarsAreEqual(expected.Ask, actual.Ask);
                Assert.AreEqual(expected.LastBidSize, actual.LastBidSize);
                Assert.AreEqual(expected.LastAskSize, actual.LastAskSize);
            }
        }

        [Test, TestCaseSource("GetLeanDataLineTestParameters")]
        public void GetSourceMatchesGenerateZipFilePath(LeanDataLineTestParameters parameters)
        {
            var source = parameters.Data.GetSource(parameters.Config, parameters.Data.Time.Date, false);
            var normalizedSourcePath = new FileInfo(source.Source).FullName;
            var zipFilePath = LeanData.GenerateZipFilePath(Constants.DataFolder, parameters.Data.Symbol, parameters.Data.Time.Date, parameters.Resolution, parameters.TickType);
            var normalizeZipFilePath = new FileInfo(zipFilePath).FullName;
            Assert.AreEqual(normalizeZipFilePath, normalizedSourcePath);
        }

        private static void AssertBarsAreEqual(IBar expected, IBar actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }
            if (expected == null && actual != null)
            {
                Assert.Fail("Expected null bar");
            }
            Assert.AreEqual(expected.Open, actual.Open);
            Assert.AreEqual(expected.High, actual.High);
            Assert.AreEqual(expected.Low, actual.Low);
            Assert.AreEqual(expected.Close, actual.Close);
        }

        private static TestCaseData[] GetLeanDataTestParameters()
        {
            var date = new DateTime(2016, 02, 17);
            return new List<LeanDataTestParameters>
            {
                // equity
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Tick, TickType.Trade, "20160217_trade.zip", "20160217_spy_tick_trade.csv", "equity/usa/tick/spy"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Second, TickType.Trade, "20160217_trade.zip", "20160217_spy_second_trade.csv", "equity/usa/second/spy"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Minute, TickType.Trade, "20160217_trade.zip", "20160217_spy_minute_trade.csv", "equity/usa/minute/spy"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Hour, TickType.Trade, "spy.zip", "spy.csv", "equity/usa/hour"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Daily, TickType.Trade, "spy.zip", "spy.csv", "equity/usa/daily"),

                // equity option trades
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Tick, TickType.Trade, "20160217_trade_american.zip", "20160217_spy_tick_trade_american.csv", "option/usa/tick/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Tick, TickType.Quote, "20160217_quote_american.zip", "20160217_spy_tick_quote_american.csv", "option/usa/tick/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Second, TickType.Trade, "20160217_trade_american.zip", "20160217_spy_second_trade_american.csv", "option/usa/second/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Second, TickType.Quote, "20160217_quote_american.zip", "20160217_spy_second_quote_american.csv", "option/usa/second/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Minute, TickType.Trade, "20160217_trade_american.zip", "20160217_spy_minute_trade_american.csv", "option/usa/minute/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Minute, TickType.Quote, "20160217_quote_american.zip", "20160217_spy_minute_quote_american.csv", "option/usa/minute/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Hour, TickType.Trade, "spy_trade_american.zip", "spy_trade_american.csv", "option/usa/hour"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Hour, TickType.Quote, "spy_quote_american.zip", "spy_quote_american.csv", "option/usa/hour"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Daily, TickType.Trade, "spy_trade_american.zip", "spy_trade_american.csv", "option/usa/daily"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Daily, TickType.Quote, "spy_quote_american.zip", "spy_quote_american.csv", "option/usa/daily"),

                // forex
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Tick, TickType.Quote, "20160217_quote.zip", "20160217_eurusd_tick_quote.csv", "forex/fxcm/tick/eurusd"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Second, TickType.Quote, "20160217_quote.zip", "20160217_eurusd_second_quote.csv", "forex/fxcm/second/eurusd"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Minute, TickType.Quote, "20160217_quote.zip", "20160217_eurusd_minute_quote.csv", "forex/fxcm/minute/eurusd"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Hour, TickType.Quote, "eurusd.zip", "eurusd.csv", "forex/fxcm/hour"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Daily, TickType.Quote, "eurusd.zip", "eurusd.csv", "forex/fxcm/daily"),

                // cfd
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Tick, TickType.Quote, "20160217_quote.zip", "20160217_de10ybeur_tick_quote.csv", "cfd/fxcm/tick/de10ybeur"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Second, TickType.Quote, "20160217_quote.zip", "20160217_de10ybeur_second_quote.csv", "cfd/fxcm/second/de10ybeur"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Minute, TickType.Quote, "20160217_quote.zip", "20160217_de10ybeur_minute_quote.csv", "cfd/fxcm/minute/de10ybeur"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Hour, TickType.Quote, "de10ybeur.zip", "de10ybeur.csv", "cfd/fxcm/hour"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Daily, TickType.Quote, "de10ybeur.zip", "de10ybeur.csv", "cfd/fxcm/daily"),

            }.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
        }

        private static TestCaseData[] GetLeanDataLineTestParameters()
        {
            var time = new DateTime(2016, 02, 18, 9, 30, 0);
            return new List<LeanDataLineTestParameters>
            {
                //equity
                new LeanDataLineTestParameters(new Tick {Time = time, Symbol = Symbols.SPY, Value = 1, Quantity = 2, TickType = TickType.Trade, Exchange = "EX", SaleCondition = "SC", Suspicious = true}, SecurityType.Equity, Resolution.Tick,
                    "34200000,10000,2,EX,SC,1"),
                new LeanDataLineTestParameters(new TradeBar(time, Symbols.SPY, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(1)), SecurityType.Equity, Resolution.Minute,
                    "34200000,10000,20000,30000,40000,5"),
                new LeanDataLineTestParameters(new TradeBar(time.Date, Symbols.SPY, 1, 2, 3, 4, 5, TimeSpan.FromDays(1)), SecurityType.Equity, Resolution.Daily,
                    "20160218 00:00,10000,20000,30000,40000,5"),

                // options
                new LeanDataLineTestParameters(new QuoteBar(time, Symbols.SPY_P_192_Feb19_2016, null, 0, new Bar(6, 7, 8, 9), 10, TimeSpan.FromMinutes(1)) {Bid = null}, SecurityType.Option, Resolution.Minute,
                    "34200000,,,,,0,60000,70000,80000,90000,10"),
                new LeanDataLineTestParameters(new QuoteBar(time.Date, Symbols.SPY_P_192_Feb19_2016, new Bar(1, 2, 3, 4), 5, null, 0, TimeSpan.FromDays(1)) {Ask = null}, SecurityType.Option, Resolution.Daily,
                    "20160218 00:00,10000,20000,30000,40000,5,,,,,0"),
                new LeanDataLineTestParameters(new QuoteBar(time, Symbols.SPY_P_192_Feb19_2016, new Bar(1, 2, 3, 4), 5, new Bar(6, 7, 8, 9), 10, TimeSpan.FromMinutes(1)), SecurityType.Option, Resolution.Minute,
                    "34200000,10000,20000,30000,40000,5,60000,70000,80000,90000,10"),
                new LeanDataLineTestParameters(new QuoteBar(time.Date, Symbols.SPY_P_192_Feb19_2016, new Bar(1, 2, 3, 4), 5, new Bar(6, 7, 8, 9), 10, TimeSpan.FromDays(1)), SecurityType.Option, Resolution.Daily,
                    "20160218 00:00,10000,20000,30000,40000,5,60000,70000,80000,90000,10"),
                new LeanDataLineTestParameters(new Tick(time, Symbols.SPY_P_192_Feb19_2016, 0, 1, 3) {Value = 2m, TickType = TickType.Quote, BidSize = 2, AskSize = 4, Exchange = "EX", Suspicious = true}, SecurityType.Option, Resolution.Tick,
                    "34200000,10000,2,30000,4,EX,1"),
                new LeanDataLineTestParameters(new Tick {Time = time, Symbol = Symbols.SPY_P_192_Feb19_2016, Value = 1, Quantity = 2,TickType = TickType.Trade, Exchange = "EX", SaleCondition = "SC", Suspicious = true}, SecurityType.Option, Resolution.Tick,
                    "34200000,10000,2,EX,SC,1"),
                new LeanDataLineTestParameters(new TradeBar(time, Symbols.SPY_P_192_Feb19_2016, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(1)), SecurityType.Option, Resolution.Minute,
                    "34200000,10000,20000,30000,40000,5"),
                new LeanDataLineTestParameters(new TradeBar(time.Date, Symbols.SPY_P_192_Feb19_2016, 1, 2, 3, 4, 5, TimeSpan.FromDays(1)), SecurityType.Option, Resolution.Daily,
                    "20160218 00:00,10000,20000,30000,40000,5"),

                // forex
                new LeanDataLineTestParameters(new Tick {Time = time, Symbol = Symbols.EURUSD, BidPrice = 1, Value =1.5m, AskPrice = 2, TickType = TickType.Quote}, SecurityType.Forex, Resolution.Tick,
                    "34200000,1,2"),
                new LeanDataLineTestParameters(new TradeBar(time, Symbols.EURUSD, 1, 2, 3, 4, 0, TimeSpan.FromMinutes(1)), SecurityType.Forex, Resolution.Minute,
                    "34200000,1,2,3,4"),
                new LeanDataLineTestParameters(new TradeBar(time.Date, Symbols.EURUSD, 1, 2, 3, 4, 0, TimeSpan.FromDays(1)), SecurityType.Forex, Resolution.Daily,
                    "20160218 00:00,1,2,3,4"),

                // cfd
                new LeanDataLineTestParameters(new Tick {Time = time, Symbol = Symbols.DE10YBEUR, BidPrice = 1, Value = 1.5m, AskPrice = 2, TickType = TickType.Quote}, SecurityType.Cfd, Resolution.Tick,
                    "34200000,1,2"),
                new LeanDataLineTestParameters(new TradeBar(time, Symbols.DE10YBEUR, 1, 2, 3, 4, 0, TimeSpan.FromMinutes(1)), SecurityType.Cfd, Resolution.Minute,
                    "34200000,1,2,3,4"),
                new LeanDataLineTestParameters(new TradeBar(time.Date, Symbols.DE10YBEUR, 1, 2, 3, 4, 0, TimeSpan.FromDays(1)), SecurityType.Cfd, Resolution.Daily,
                    "20160218 00:00,1,2,3,4"),
            }.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
        }

        public class LeanDataTestParameters
        {
            public readonly string Name;
            public readonly Symbol Symbol;
            public readonly DateTime Date;
            public readonly Resolution Resolution;
            public readonly TickType TickType;
            public readonly string ExpectedZipFileName;
            public readonly string ExpectedZipEntryName;
            public readonly string ExpectedRelativeZipFilePath;
            public readonly string ExpectedZipFilePath;
            public SecurityType SecurityType { get { return Symbol.ID.SecurityType; } }

            public LeanDataTestParameters(Symbol symbol, DateTime date, Resolution resolution, TickType tickType, string expectedZipFileName, string expectedZipEntryName, string expectedRelativeZipFileDirectory = "")
            {
                Symbol = symbol;
                Date = date;
                Resolution = resolution;
                TickType = tickType;
                ExpectedZipFileName = expectedZipFileName;
                ExpectedZipEntryName = expectedZipEntryName;
                ExpectedRelativeZipFilePath = Path.Combine(expectedRelativeZipFileDirectory, expectedZipFileName).Replace("/", Path.DirectorySeparatorChar.ToString());
                ExpectedZipFilePath = Path.Combine(Constants.DataFolder, ExpectedRelativeZipFilePath);

                Name = SecurityType + "_" + resolution;
            }
        }

        public class LeanDataLineTestParameters
        {
            public readonly string Name;
            public readonly BaseData Data;
            public readonly SecurityType SecurityType;
            public readonly Resolution Resolution;
            public readonly string ExpectedLine;
            public readonly SubscriptionDataConfig Config;
            public readonly TickType TickType;

            public LeanDataLineTestParameters(BaseData data, SecurityType securityType, Resolution resolution, string expectedLine)
            {
                Data = data;
                SecurityType = securityType;
                Resolution = resolution;
                ExpectedLine = expectedLine;
                if (data is Tick)
                {
                    var tick = (Tick) data;
                    TickType = tick.TickType;
                }
                else if (data is TradeBar)
                {
                    TickType = TickType.Trade;
                }
                else if (data is QuoteBar)
                {
                    TickType = TickType.Quote;
                }
                else
                {
                    throw new NotImplementedException();
                }

                // override for forex/cfd
                if (data.Symbol.ID.SecurityType == SecurityType.Forex || data.Symbol.ID.SecurityType == SecurityType.Cfd)
                {
                    TickType = TickType.Quote;
                }

                Config = new SubscriptionDataConfig(Data.GetType(), Data.Symbol, Resolution, TimeZones.Utc, TimeZones.Utc, false, true, false, false, TickType);

                Name = SecurityType + "_" + data.GetType().Name;

                if (data.GetType() != typeof (Tick) || Resolution != Resolution.Tick)
                {
                    Name += "_" + Resolution;
                }

                if (data is Tick)
                {
                    Name += "_" + ((Tick) data).TickType;
                }
            }
        }
    }
}
