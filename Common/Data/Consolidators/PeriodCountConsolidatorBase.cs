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
    /// Provides a base class for consolidators that emit data based on the passing of a period of time
    /// or after seeing a max count of data points.
    /// </summary>
    /// <typeparam name="T">The input type of the consolidator</typeparam>
    /// <typeparam name="TConsolidated">The output type of the consolidator</typeparam>
    public abstract class PeriodCountConsolidatorBase<T, TConsolidated> : DataConsolidator<T>
        where T : class, IBaseData
        where TConsolidated : BaseData
    {
        //The minimum timespan between creating new bars.
        private readonly TimeSpan? _period;
        //The number of data updates between creating new bars.
        private readonly int? _maxCount;
        //The number of pieces of data we've accumulated since our last emit
        private int _currentCount;
        //The working bar used for aggregating the data
        private TConsolidated _workingBar;
        //The last time we emitted a consolidated bar
        private DateTime? _lastEmit;

        /// <summary>
        /// Creates a consolidator to produce a new <typeparamref name="TConsolidated"/> instance representing the period
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        protected PeriodCountConsolidatorBase(TimeSpan period)
        {
            _period = period;
        }

        /// <summary>
        /// Creates a consolidator to produce a new <typeparamref name="TConsolidated"/> instance representing the last count pieces of data
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        protected PeriodCountConsolidatorBase(int maxCount)
        {
            _maxCount = maxCount;
        }

        /// <summary>
        /// Creates a consolidator to produce a new <typeparamref name="TConsolidated"/> instance representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        protected PeriodCountConsolidatorBase(int maxCount, TimeSpan period)
        {
            _maxCount = maxCount;
            _period = period;
        }

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public override Type OutputType
        {
            get { return typeof(TConsolidated); }
        }

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override BaseData WorkingData
        {
            get { return _workingBar != null ? _workingBar.Clone() : null; }
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced. We define this as a 'new'
        /// event so we can expose it as a <typeparamref name="TConsolidated"/> instead of a <see cref="BaseData"/> instance
        /// </summary>
        public new event EventHandler<TConsolidated> DataConsolidated;

        /// <summary>
        /// Updates this consolidator with the specified data. This method is
        /// responsible for raising the DataConsolidated event
        /// In time span mode, the bar range is closed on the left and open on the right: [T, T+TimeSpan).
        /// For example, if time span is 1 minute, we have [10:00, 10:01): so data at 10:01 is not 
        /// included in the bar starting at 10:00.
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(T data)
        {
            if (!ShouldProcess(data))
            {
                // first allow the base class a chance to filter out data it doesn't want
                // before we start incrementing counts and what not
                return;
            }

            //Decide to fire the event
            var fireDataConsolidated = false;

            // decide to aggregate data before or after firing OnDataConsolidated event
            // always aggregate before firing in counting mode
            bool aggregateBeforeFire = _maxCount.HasValue;

            if (_maxCount.HasValue)
            {
                // we're in count mode
                _currentCount++;
                if (_currentCount >= _maxCount.Value)
                {
                    _currentCount = 0;
                    fireDataConsolidated = true;
                }
            }

            if (!_lastEmit.HasValue)
            {
                // initialize this value for period computations
                _lastEmit = data.Time;
            }

            if (_period.HasValue)
            {
                // we're in time span mode and initialized
                if (_workingBar != null && data.Time - _workingBar.Time >= _period.Value)
                {
                    fireDataConsolidated = true;
                }

                // special case: always aggregate before event trigger when TimeSpan is zero
                if (_period.Value == TimeSpan.Zero)
                {
                    fireDataConsolidated = true;
                    aggregateBeforeFire = true;
                }
            }

            if (aggregateBeforeFire)
            {
                AggregateBar(ref _workingBar, data);
            }

            //Fire the event
            if (fireDataConsolidated)
            {
                var workingTradeBar = _workingBar as TradeBar;
                if (workingTradeBar != null)
                {
                    // we kind of are cheating here...
                    if (_period.HasValue)
                    {
                        workingTradeBar.Period = _period.Value;
                    }
                    // since trade bar has period it aggregates this properly
                    else if (!(data is TradeBar))
                    {
                        workingTradeBar.Period = data.Time - _lastEmit.Value;
                    }
                }

                OnDataConsolidated(_workingBar);
                _lastEmit = data.Time;
                _workingBar = null;
            }

            if (!aggregateBeforeFire)
            {
                AggregateBar(ref _workingBar, data);
            }
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public override void Scan(DateTime currentLocalTime)
        {
            if (_period.HasValue)
            {
                if (_workingBar != null)
                {
                    var fireDataConsolidated = _period.Value == TimeSpan.Zero;
                    if (!fireDataConsolidated && currentLocalTime - _workingBar.Time >= _period.Value)
                    {
                        fireDataConsolidated = true;
                    }
                    if (fireDataConsolidated)
                    {
                        OnDataConsolidated(_workingBar);
                        _lastEmit = currentLocalTime;
                        _workingBar = null;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether or not the specified data should be processd
        /// </summary>
        /// <param name="data">The data to check</param>
        /// <returns>True if the consolidator should process this data, false otherwise</returns>
        protected virtual bool ShouldProcess(T data)
        {
            return true;
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new consolidated bar</param>
        /// <param name="data">The new data</param>
        protected abstract void AggregateBar(ref TConsolidated workingBar, T data);

        /// <summary>
        /// Gets a rounded-down bar time. Called by AggregateBar in derived classes.
        /// </summary>
        /// <param name="time">The bar time to be rounded down</param>
        /// <returns>The rounded bar time</returns>
        protected DateTime GetRoundedBarTime(DateTime time)
        {
            // rounding is performed only in time span mode
            return _period.HasValue && !_maxCount.HasValue ? time.RoundDown((TimeSpan)_period) : time;
        }

        /// <summary>
        /// Event invocator for the <see cref="DataConsolidated"/> event
        /// </summary>
        /// <param name="e">The consolidated data</param>
        protected virtual void OnDataConsolidated(TConsolidated e)
        {
            var handler = DataConsolidated;
            if (handler != null) handler(this, e);
            base.OnDataConsolidated(e);
        }
    }
}