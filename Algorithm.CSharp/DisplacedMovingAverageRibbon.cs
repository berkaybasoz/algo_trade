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
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// Constructs a displaced moving average ribbon and buys when all are lined up, liquidates when they all line down
    /// Ribbons are great for visualizing trends
    ///   Signals are generated when they all line up in a paricular direction
    ///     A buy signal is when the values of the indicators are increasing (from slowest to fastest)
    ///     A sell signal is when the values of the indicators are decreasing (from slowest to fastest)
    /// </summary>
    public class DisplacedMovingAverageRibbon : QCAlgorithm
    {
        private const string Symbol = "SPY";
        private IndicatorBase<IndicatorDataPoint>[] _ribbon;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <seealso cref="QCAlgorithm.SetStartDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetEndDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public override void Initialize()
        {
            SetStartDate(2009, 01, 01);
            SetEndDate(2015, 01, 01);

            AddSecurity(SecurityType.Equity, Symbol, Resolution.Minute);

            int count = 6;
            int offset = 5;
            int period = 15;

            // define our sma as the base of the ribbon
            var sma = new SimpleMovingAverage(period);

            _ribbon = Enumerable.Range(0, count).Select(x =>
            {
                // define our offset to the zero sma, these various offsets will create our 'displaced' ribbon
                var delay = new Delay(offset*(x+1));

                // define an indicator that takes the output of the sma and pipes it into our delay indicator
                var delayedSma = delay.Of(sma);

                // register our new 'delayedSma' for automaic updates on a daily resolution
                RegisterIndicator(Symbol, delayedSma, Resolution.Daily, data => data.Value);

                return delayedSma;
            }).ToArray();
        }

        private DateTime _previous;

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            // wait for our entire ribbon to be ready
            if (!_ribbon.All(x => x.IsReady)) return;

            // only once per day
            if (_previous.Date == Time.Date) return;

            Plot(Symbol, "Price", data[Symbol].Price);
            Plot(Symbol, _ribbon);


            // check for a buy signal
            var values = _ribbon.Select(x => x.Current.Value).ToArray();

            var holding = Portfolio[Symbol];
            if (holding.Quantity <= 0 && IsAscending(values))
            {
                SetHoldings(Symbol, 1.0);
            }
            else if (holding.Quantity > 0 && IsDescending(values))
            {
                Liquidate(Symbol);
            }

            _previous = Time;
        }

        /// <summary>
        /// Returns true if the specified values are in ascending order
        /// </summary>
        private bool IsAscending(IEnumerable<decimal> values)
        {
            decimal? last = null;
            foreach (var val in values)
            {
                if (last == null)
                {
                    last = val;
                    continue;
                }

                if (last.Value < val)
                {
                    return false;
                }
                last = val;
            }
            return true;
        }

        /// <summary>
        /// Returns true if the specified values are in descending order
        /// </summary>
        private bool IsDescending(IEnumerable<decimal> values)
        {
            decimal? last = null;
            foreach (var val in values)
            {
                if (last == null)
                {
                    last = val;
                    continue;
                }

                if (last.Value > val)
                {
                    return false;
                }
                last = val;
            }
            return true;
        }
    }
}
