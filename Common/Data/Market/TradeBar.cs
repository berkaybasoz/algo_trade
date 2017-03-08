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
using System.Globalization;
using System.IO;
using System.Threading;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// TradeBar class for second and minute resolution data: 
    /// An OHLC implementation of the QuantConnect BaseData class with parameters for candles.
    /// </summary>
    public class TradeBar : BaseData, IBar
    {
        // scale factor used in QC equity/forex data files
        private const decimal _scaleFactor = 1/10000m;

        private int _initialized;
        private decimal _open;
        private decimal _high;
        private decimal _low;

        /// <summary>
        /// Volume:
        /// </summary>
        public long Volume { get; set; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public decimal Open
        {
            get { return _open; }
            set
            {
                Initialize(value);
                _open = value;
            }
        }

        /// <summary>
        /// High price of the TradeBar during the time period.
        /// </summary>
        public decimal High
        {
            get { return _high; }
            set
            {
                Initialize(value);
                _high = value;
            }
        }

        /// <summary>
        /// Low price of the TradeBar during the time period.
        /// </summary>
        public decimal Low
        {
            get { return _low; }
            set
            {
                Initialize(value);
                _low = value;
            }
        }

        /// <summary>
        /// Closing price of the TradeBar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close
        {
            get { return Value; }
            set
            {
                Initialize(value);
                Value = value;
            }
        }

        /// <summary>
        /// The closing time of this bar, computed via the Time and Period
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time + Period; }
            set { Period = value - Time; } 
        }

        /// <summary>
        /// The period of this trade bar, (second, minute, daily, ect...)
        /// </summary>
        public TimeSpan Period { get; set; }

        //In Base Class: Alias of Closing:
        //public decimal Price;

        //Symbol of Asset.
        //In Base Class: public Symbol Symbol;

        //In Base Class: DateTime Of this TradeBar
        //public DateTime Time;

        /// <summary>
        /// Default initializer to setup an empty tradebar.
        /// </summary>
        public TradeBar()
        {
            Symbol = Symbol.Empty;
            DataType = MarketDataType.TradeBar;
            Period = TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Cloner constructor for implementing fill forward. 
        /// Return a new instance with the same values as this original.
        /// </summary>
        /// <param name="original">Original tradebar object we seek to clone</param>
        public TradeBar(TradeBar original)
        {
            DataType = MarketDataType.TradeBar;
            Time = new DateTime(original.Time.Ticks);
            Symbol = original.Symbol;
            Value = original.Close;
            Open = original.Open;
            High = original.High;
            Low = original.Low;
            Close = original.Close;
            Volume = original.Volume;
            Period = original.Period;
            _initialized = 1;
        }

        /// <summary>
        /// Initialize Trade Bar with OHLC Values:
        /// </summary>
        /// <param name="time">DateTime Timestamp of the bar</param>
        /// <param name="symbol">Market MarketType Symbol</param>
        /// <param name="open">Decimal Opening Price</param>
        /// <param name="high">Decimal High Price of this bar</param>
        /// <param name="low">Decimal Low Price of this bar</param>
        /// <param name="close">Decimal Close price of this bar</param>
        /// <param name="volume">Volume sum over day</param>
        /// <param name="period">The period of this bar, specify null for default of 1 minute</param>
        public TradeBar(DateTime time, Symbol symbol, decimal open, decimal high, decimal low, decimal close, long volume, TimeSpan? period = null)
        {
            Time = time;
            Symbol = symbol;
            Value = close;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            Period = period ?? TimeSpan.FromMinutes(1);
            DataType = MarketDataType.TradeBar;
            _initialized = 1;
        }

        /// <summary>
        /// TradeBar Reader: Fetch the data from the QC storage and feed it line by line into the engine.
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of this reader request</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Enumerable iterator for returning each line of the required data.</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode) 
        {
            //Handle end of file:
            if (line == null)
            {
                return null;
            }

            if (isLiveMode)
            {
                return new TradeBar();
            }

            try
            {
                switch (config.SecurityType)
                {
                    //Equity File Data Format:
                    case SecurityType.Equity:
                        return ParseEquity<TradeBar>(config, line, date);

                    //FOREX has a different data file format:
                    case SecurityType.Forex:
                        return ParseForex<TradeBar>(config, line, date);

                    case SecurityType.Cfd:
                        return ParseCfd<TradeBar>(config, line, date);

                    case SecurityType.Option:
                        return ParseOption<TradeBar>(config, line, date);
                }
            }
            catch (Exception err)
            {
                Log.Error(err, "SecurityType: " + config.SecurityType + " Line: " + line);
            }

            // if we couldn't parse it above return a default instance
            return new TradeBar{Symbol = config.Symbol, Period = config.Increment};
        }

        /// <summary>
        /// Parses the trade bar data line assuming QC data formats
        /// </summary>
        public static TradeBar Parse(SubscriptionDataConfig config, string line, DateTime baseDate)
        {
            switch (config.SecurityType)
            {
                case SecurityType.Equity:
                    return ParseEquity(config, line, baseDate);

                case SecurityType.Forex:
                    return ParseForex(config, line, baseDate);

                case SecurityType.Cfd:
                    return ParseCfd(config, line, baseDate);
            }

            return null;
        }

        /// <summary>
        /// Parses equity trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of this reader request</param>
        /// <returns></returns>
        public static T ParseEquity<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Symbol = config.Symbol,
                Period = config.Increment
            };

            var csv = line.ToCsv(6);
            if (config.Resolution == Resolution.Daily || config.Resolution == Resolution.Hour)
            {
                // hourly and daily have different time format, and can use slow, robust c# parser.
                tradeBar.Time = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }
            else
            {
                // Using custom "ToDecimal" conversion for speed on high resolution data.
                tradeBar.Time = date.Date.AddMilliseconds(csv[0].ToInt32()).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }

            tradeBar.Open = config.GetNormalizedPrice(csv[1].ToDecimal()*_scaleFactor);
            tradeBar.High = config.GetNormalizedPrice(csv[2].ToDecimal()*_scaleFactor);
            tradeBar.Low = config.GetNormalizedPrice(csv[3].ToDecimal()*_scaleFactor);
            tradeBar.Close = config.GetNormalizedPrice(csv[4].ToDecimal()*_scaleFactor);
            tradeBar.Volume = csv[5].ToInt64();

            return tradeBar;
        }

        /// <summary>
        /// Parses equity trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of this reader request</param>
        /// <returns></returns>
        public static TradeBar ParseEquity(SubscriptionDataConfig config, string line, DateTime date)
        {
            return ParseEquity<TradeBar>(config, line, date);
        }

        /// <summary>
        /// Parses forex trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseForex<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Symbol = config.Symbol,
                Period = config.Increment
            };

            var csv = line.ToCsv(5);
            if (config.Resolution == Resolution.Daily || config.Resolution == Resolution.Hour)
            {
                // hourly and daily have different time format, and can use slow, robust c# parser.
                tradeBar.Time = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }
            else
            {
                //Fast decimal conversion
                tradeBar.Time = date.Date.AddMilliseconds(csv[0].ToInt32()).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }

            tradeBar.Open = csv[1].ToDecimal();
            tradeBar.High = csv[2].ToDecimal();
            tradeBar.Low = csv[3].ToDecimal();
            tradeBar.Close = csv[4].ToDecimal();

            return tradeBar;
        }

        /// <summary>
        /// Parses forex trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseForex(SubscriptionDataConfig config, string line, DateTime date)
        {
            return ParseForex<TradeBar>(config, line, date);
        }

        /// <summary>
        /// Parses CFD trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseCfd<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            // CFD has the same data format as Forex
            return ParseForex<T>(config, line, date);
        }

        /// <summary>
        /// Parses CFD trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseCfd(SubscriptionDataConfig config, string line, DateTime date)
        {
            return ParseCfd<TradeBar>(config, line, date);
        }

        /// <summary>
        /// Parses CFD trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseOption<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };

            var csv = line.ToCsv(6);
            if (config.Resolution == Resolution.Daily || config.Resolution == Resolution.Hour)
            {
                // hourly and daily have different time format, and can use slow, robust c# parser.
                tradeBar.Time = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }
            else
            {
                // Using custom "ToDecimal" conversion for speed on high resolution data.
                tradeBar.Time = date.Date.AddMilliseconds(csv[0].ToInt32()).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }

            tradeBar.Open = config.GetNormalizedPrice(csv[1].ToDecimal() * _scaleFactor);
            tradeBar.High = config.GetNormalizedPrice(csv[2].ToDecimal() * _scaleFactor);
            tradeBar.Low = config.GetNormalizedPrice(csv[3].ToDecimal() * _scaleFactor);
            tradeBar.Close = config.GetNormalizedPrice(csv[4].ToDecimal() * _scaleFactor);
            tradeBar.Volume = csv[5].ToInt64();

            return tradeBar;
        }

        /// <summary>
        /// Parses CFD trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseOption(SubscriptionDataConfig config, string line, DateTime date)
        {
            return ParseOption<TradeBar>(config, line, date);
        }

        /// <summary>
        /// Update the tradebar - build the bar from this pricing information:
        /// </summary>
        /// <param name="lastTrade">This trade price</param>
        /// <param name="bidPrice">Current bid price (not used) </param>
        /// <param name="askPrice">Current asking price (not used) </param>
        /// <param name="volume">Volume of this trade</param>
        /// <param name="bidSize">The size of the current bid, if available</param>
        /// <param name="askSize">The size of the current ask, if available</param>
        public override void Update(decimal lastTrade, decimal bidPrice, decimal askPrice, decimal volume, decimal bidSize, decimal askSize)
        {
            Initialize(lastTrade);
            if (lastTrade > High) High = lastTrade;
            if (lastTrade < Low) Low = lastTrade;
            //Volume is the total summed volume of trades in this bar:
            Volume += Convert.ToInt32(volume);
            //Always set the closing price;
            Close = lastTrade;
        }

        /// <summary>
        /// Get Source for Custom Data File
        /// >> What source file location would you prefer for each type of usage:
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source request if source spread across multiple files</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String source location of the file</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.LocalFile);
            }

            var source = LeanData.GenerateZipFilePath(Constants.DataFolder, config.Symbol, date, config.Resolution, config.TickType);
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        public override BaseData Clone()
        {
            return (BaseData)MemberwiseClone();
        }

        /// <summary>
        /// Initializes this bar with a first data point
        /// </summary>
        /// <param name="value">The seed value for this bar</param>
        private void Initialize(decimal value)
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                _open = value;
                _low = value;
                _high = value;
            }
        }
    }
}
