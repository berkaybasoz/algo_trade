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
 *
*/

using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// A data consolidator that can make bigger bars from smaller ones over a given
    /// time span or a count of pieces of data.
    /// 
    /// Use this consolidator to turn data of a lower resolution into data of a higher resolution,
    /// for example, if you subscribe to minute data but want to have a 15 minute bar.
    /// </summary>
    public class TradeBarConsolidator : TradeBarConsolidatorBase<TradeBar>
    {
        /// <summary>
        /// Create a new TradeBarConsolidator for the desired resolution
        /// </summary>
        /// <param name="resolution">The resoluton desired</param>
        /// <returns>A consolidator that produces data on the resolution interval</returns>
        public static TradeBarConsolidator FromResolution(Resolution resolution)
        {
            return new TradeBarConsolidator(resolution.ToTimeSpan());
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the period
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public TradeBarConsolidator(TimeSpan period)
            : base(period)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        public TradeBarConsolidator(int maxCount)
            : base(maxCount)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public TradeBarConsolidator(int maxCount, TimeSpan period)
            : base(maxCount, period)
        {
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new trade bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref TradeBar workingBar, TradeBar data)
        {
            if (workingBar == null)
            {
                workingBar = new TradeBar
                {
                    Time = GetRoundedBarTime(data.Time),
                    Symbol = data.Symbol,
                    Open = data.Open,
                    High = data.High,
                    Low = data.Low,
                    Close = data.Close,
                    Volume = data.Volume,
                    DataType = MarketDataType.TradeBar,
                    Period = data.Period
                };
            }
            else
            {
                //Aggregate the working bar
                workingBar.Close = data.Close;
                workingBar.Volume += data.Volume;
                workingBar.Period += data.Period;
                if (data.Low < workingBar.Low) workingBar.Low = data.Low;
                if (data.High > workingBar.High) workingBar.High = data.High;
            }
        }
    }
}