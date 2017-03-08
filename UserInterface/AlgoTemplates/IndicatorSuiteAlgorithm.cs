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
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect
{
    /// <summary>
    /// QuantConnect University: Indicator Suite Example.
    /// </summary>
    public class IndicatorSuiteAlgorithm : QCAlgorithm
    {
        string _symbol = "SPY";
        string _customSymbol = "BTC";


        Indicators _indicators;
        Indicators _selectorIndicators;
        IndicatorBase<IndicatorDataPoint> _ratio;

            //RSI Custom Data:
        RelativeStrengthIndex _rsiCustom;
        Minimum _minCustom;
        Maximum _maxCustom;

        decimal _price;

        /// <summary>
        /// Initialize the data and resolution you require for your strategy
        /// </summary>
        public override void Initialize()
        {
            //Initialize
            SetStartDate(2013, 1, 1);
            SetEndDate(2014, 12, 31);
            SetCash(25000);

            //Add as many securities as you like. All the data will be passed into the event handler:
            AddSecurity(SecurityType.Equity, _symbol, Resolution.Minute);

            //Add the Custom Data:
            AddData<Bitcoin>("BTC");

            //Set up default Indicators, these indicators are defined on the Value property of incoming data (except ATR and AROON which use the full TradeBar object)
            _indicators = new Indicators
            {
                BB = BB(_symbol, 20, 1, MovingAverageType.Simple, Resolution.Daily),
                RSI = RSI(_symbol, 14, MovingAverageType.Simple, Resolution.Daily),
                ATR = ATR(_symbol, 14, MovingAverageType.Simple, Resolution.Daily),
                EMA = EMA(_symbol, 14, Resolution.Daily),
                SMA = SMA(_symbol, 14, Resolution.Daily),
                MACD = MACD(_symbol, 12, 26, 9, MovingAverageType.Simple, Resolution.Daily),
                AROON = AROON(_symbol, 20, Resolution.Daily),
                MOM = MOM(_symbol, 20, Resolution.Daily),
                MOMP = MOMP(_symbol, 20, Resolution.Daily),
                STD = STD(_symbol, 20, Resolution.Daily),
                MIN = MIN(_symbol, 14, Resolution.Daily), // by default if the symbol is a tradebar type then it will be the min of the low property
                MAX = MAX(_symbol, 14, Resolution.Daily)  // by default if the symbol is a tradebar type then it will be the max of the high property
            };

            // Here we're going to define indicators using 'selector' functions. These 'selector' functions will define what data gets sent into the indicator
            //  These functions have a signature like the following: decimal Selector(BaseData baseData), and can be defined like: baseData => baseData.Value
            //  We'll define these 'selector' functions to select the Low value
            //
            //  For more information on 'anonymous functions' see: http://en.wikipedia.org/wiki/Anonymous_function
            //                                                     https://msdn.microsoft.com/en-us/library/bb397687.aspx
            //
            _selectorIndicators = new Indicators
            {
                BB = BB(_symbol, 20, 1, MovingAverageType.Simple, Resolution.Daily, Field.Low),
                RSI = RSI(_symbol, 14, MovingAverageType.Simple, Resolution.Daily, Field.Low),
                EMA = EMA(_symbol, 14, Resolution.Daily, Field.Low),
                SMA = SMA(_symbol, 14, Resolution.Daily, Field.Low),
                MACD = MACD(_symbol, 12, 26, 9, MovingAverageType.Simple, Resolution.Daily, Field.Low),
                MOM = MOM(_symbol, 20, Resolution.Daily, Field.Low),
                MOMP = MOMP(_symbol, 20, Resolution.Daily, Field.Low),
                STD = STD(_symbol, 20, Resolution.Daily, Field.Low),
                MIN = MIN(_symbol, 14, Resolution.Daily, Field.High), // this will find the 14 day min of the high property
                MAX = MAX(_symbol, 14, Resolution.Daily, Field.Low),  // this will find the 14 day max of the low property

                // ATR and AROON are special in that they accept a TradeBar instance instead of a decimal, we could easily project and/or transform the input TradeBar
                // before it gets sent to the ATR/AROON indicator, here we use a function that will multiply the input trade bar by a factor of two
                ATR = ATR(_symbol, 14, MovingAverageType.Simple, Resolution.Daily, SelectorDoubleTradeBar),
                AROON = AROON(_symbol, 20, Resolution.Daily, SelectorDoubleTradeBar)
            };

            //Custom Data Indicator:
            _rsiCustom = RSI(_customSymbol, 14, MovingAverageType.Simple, Resolution.Daily);
            _minCustom = MIN(_customSymbol, 14, Resolution.Daily);
            _maxCustom = MAX(_customSymbol, 14, Resolution.Daily);

            // in addition to defining indicators on a single security, you can all define 'composite' indicators.
            // these are indicators that require multiple inputs. the most common of which is a ratio.
            // suppose we seek the ratio of BTC to SPY, we could write the following:
            var spyClose = Identity(_symbol);
            var btcClose = Identity(_customSymbol);
            // this will create a new indicator whose value is BTC/SPY
            _ratio = btcClose.Over(spyClose);
            // we can also easily plot our indicators each time they update using th PlotIndicator function
            PlotIndicator("Ratio", _ratio);
        }

        /// <summary>
        /// Custom data event handler:
        /// </summary>
        /// <param name="data">Bitcoin - dictionary of TradeBarlike Bars of Bitcoin Data</param>
        public void OnData(Bitcoin data)
        {
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (!_indicators.BB.IsReady || !_indicators.RSI.IsReady) return;

            _price = data["SPY"].Close;

            if (!Portfolio.HoldStock)
            {
                int quantity = (int)Math.Floor(Portfolio.Cash / data[_symbol].Close);

                //Order function places trades: enter the string symbol and the quantity you want:
                Order(_symbol, quantity);

                //Debug sends messages to the user console: "Time" is the algorithm time keeper object 
                Debug("Purchased SPY on " + Time.ToShortDateString());
            }
        }

        /// <summary>
        /// Fire plotting events once per day.
        /// </summary>
        public override void OnEndOfDay()
        {
            if (!_indicators.BB.IsReady) return;

            Plot("BB", "Price", _price);
            Plot("BB", _indicators.BB.UpperBand, _indicators.BB.MiddleBand, _indicators.BB.LowerBand);

            Plot("RSI", _indicators.RSI);

            //Custom data indicator
            Plot("RSI-BTC", _rsiCustom);

            Plot("ATR", _indicators.ATR);

            Plot("STD", _indicators.STD);

            Plot("AROON", _indicators.AROON.AroonUp, _indicators.AROON.AroonDown);

            Plot("MOM", _indicators.MOM);
            Plot("MOMP", _indicators.MOMP);

            Plot("MACD", "Price", _price);
            Plot("MACD", _indicators.MACD.Fast, _indicators.MACD.Slow, _indicators.MACD.Signal);

            Plot("Averages", _indicators.EMA, _indicators.SMA);
        }

        /// <summary>
        /// Class to hold a bunch of different indicators for this example
        /// </summary>
        class Indicators
        {
            public BollingerBands BB;
            public SimpleMovingAverage SMA;
            public ExponentialMovingAverage EMA;
            public RelativeStrengthIndex RSI;
            public AverageTrueRange ATR;
            public StandardDeviation STD;
            public AroonOscillator AROON;
            public Momentum MOM;
            public MomentumPercent MOMP;
            public MovingAverageConvergenceDivergence MACD;
            public Minimum MIN;
            public Maximum MAX;
        }

        /// <summary>
        /// Function used to select a trade bar that has double the values of the input trade bar
        /// </summary>
        private static TradeBar SelectorDoubleTradeBar(BaseData baseData)
        {
            var bar = (TradeBar)baseData;
            return new TradeBar
            {
                Close = 2 * bar.Close,
                DataType = bar.DataType,
                High = 2 * bar.High,
                Low = 2 * bar.Low,
                Open = 2 * bar.Open,
                Symbol = bar.Symbol,
                Time = bar.Time,
                Value = 2 * bar.Value,
                Volume = 2 * bar.Volume,
                Period = bar.Period
            };
        }
    }
}