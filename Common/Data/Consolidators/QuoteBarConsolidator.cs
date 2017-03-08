/*
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
 *
*/
using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Consolidates quotebars into larger quotebars
    /// </summary>
    public class QuoteBarConsolidator : PeriodCountConsolidatorBase<QuoteBar, QuoteBar>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TickQuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public QuoteBarConsolidator(TimeSpan period)
            : base(period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickQuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        public QuoteBarConsolidator(int maxCount)
            : base(maxCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickQuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public QuoteBarConsolidator(int maxCount, TimeSpan period)
            : base(maxCount, period)
        {
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new consolidated bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref QuoteBar workingBar, QuoteBar data)
        {
            var bid = data.Bid;
            var ask = data.Ask;

            if (workingBar == null)
            {
                workingBar = new QuoteBar
                {
                    Symbol = data.Symbol,
                    Time = GetRoundedBarTime(data.Time),
                    Bid = bid == null ? null : bid.Clone(),
                    Ask = ask == null ? null : ask.Clone()
                };
            }

            // update the bid and ask
            if (bid != null)
            {
                workingBar.LastBidSize = data.LastBidSize;
                if (workingBar.Bid == null)
                {
                    workingBar.Bid = new Bar(bid.Open, bid.High, bid.Low, bid.Close);
                }
                else
                {
                    workingBar.Bid.Close = bid.Close;
                    if (workingBar.Bid.High < bid.High) workingBar.Bid.High = bid.High;
                    if (workingBar.Bid.Low > bid.Low) workingBar.Bid.Low = bid.Low;
                }
            }
            if (ask != null)
            {
                workingBar.LastAskSize = data.LastAskSize;
                if (workingBar.Ask == null)
                {
                    workingBar.Ask = new Bar(ask.Open, ask.High, ask.Low, ask.Close);
                }
                else
                {
                    workingBar.Ask.Close = ask.Close;
                    if (workingBar.Ask.High < ask.High) workingBar.Ask.High = ask.High;
                    if (workingBar.Ask.Low > ask.Low) workingBar.Ask.Low = ask.Low;
                }
            }
        }
    }
}