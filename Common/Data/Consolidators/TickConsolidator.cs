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
*/

using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{    
    /// <summary>
    /// A data consolidator that can make bigger bars from ticks over a given
    /// time span or a count of pieces of data.
    /// </summary>
    public class TickConsolidator : TradeBarConsolidatorBase<Tick>
    {
        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the period
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public TickConsolidator(TimeSpan period)
            : base(period)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        public TickConsolidator(int maxCount)
            : base(maxCount)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public TickConsolidator(int maxCount, TimeSpan period)
            : base(maxCount, period)
        {
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref TradeBar workingBar, Tick data)
        {
            if (workingBar == null)
            {
                workingBar = new TradeBar
                {
                    Symbol = data.Symbol,
                    Time = GetRoundedBarTime(data.Time),
                    Close = data.Value,
                    High = data.Value,
                    Low = data.Value,
                    Open = data.Value,
                    DataType = data.DataType,
                    Value = data.Value,
                    Volume = data.Quantity
                };
            }
            else
            {
                //Aggregate the working bar
                workingBar.Close = data.Value;
                workingBar.Volume += data.Quantity;
                if (data.Value < workingBar.Low) workingBar.Low = data.Value;
                if (data.Value > workingBar.High) workingBar.High = data.Value;
            }
        }
    }
}