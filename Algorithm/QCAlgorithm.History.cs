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
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Gets or sets the history provider for the algorithm
        /// </summary>
        public IHistoryProvider HistoryProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets whether or not this algorithm is still warming up
        /// </summary>
        public bool IsWarmingUp
        {
            get;
            private set;
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        public void SetWarmup(TimeSpan timeSpan)
        {
            _warmupBarCount = null;
            _warmupTimeSpan = timeSpan;
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        public void SetWarmUp(TimeSpan timeSpan)
        {
            SetWarmup(timeSpan);
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm. The highest (smallest) resolution in the securities collection will be used.
        /// For example, if an algorithm has minute and daily data and 200 bars are requested, that would
        /// use 200 minute bars.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        public void SetWarmup(int barCount)
        {
            _warmupTimeSpan = null;
            _warmupBarCount = barCount;
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm. The highest (smallest) resolution in the securities collection will be used.
        /// For example, if an algorithm has minute and daily data and 200 bars are requested, that would
        /// use 200 minute bars.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        public void SetWarmUp(int barCount)
        {
            SetWarmup(barCount);
        }

        /// <summary>
        /// Sets <see cref="IAlgorithm.IsWarmingUp"/> to false to indicate this algorithm has finished its warm up
        /// </summary>
        public void SetFinishedWarmingUp()
        {
            IsWarmingUp = false;
        }

        /// <summary>
        /// Gets the history requests required for provide warm up data for the algorithm
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HistoryRequest> GetWarmupHistoryRequests()
        {
            if (_warmupBarCount.HasValue)
            {
                return CreateBarCountHistoryRequests(Securities.Keys, _warmupBarCount.Value);
            }
            if (_warmupTimeSpan.HasValue)
            {
                var end = UtcTime.ConvertFromUtc(TimeZone);
                return CreateDateRangeHistoryRequests(Securities.Keys, end - _warmupTimeSpan.Value, end);
            }

            // if not warmup requested return nothing
            return Enumerable.Empty<HistoryRequest>();
        }

        /// <summary>
        /// Get the history for all configured securities over the requested span.
        /// This will use the resolution and other subscription settings for each security.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="span">The span over which to request data. This is a calendar span, so take into consideration weekends and such</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing data over the most recent span for all configured securities</returns>
        public IEnumerable<Slice> History(TimeSpan span, Resolution? resolution = null)
        {
            return History(Securities.Keys, Time - span, Time, resolution);
        }

        /// <summary>
        /// Get the history for all configured securities over the requested span.
        /// This will use the resolution and other subscription settings for each security.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing data over the most recent span for all configured securities</returns>
        public IEnumerable<Slice> History(int periods, Resolution? resolution = null)
        {
            return History(Securities.Keys, periods, resolution);
        }

        /// <summary>
        /// Gets the historical data for all symbols of the requested type over the requested span.
        /// The symbol's configured values for resolution and fill forward behavior will be used
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<DataDictionary<T>> History<T>(TimeSpan span, Resolution? resolution = null)
            where T : BaseData
        {
            return History<T>(Securities.Keys, span, resolution);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbols</typeparam>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<DataDictionary<T>> History<T>(IEnumerable<Symbol> symbols, TimeSpan span, Resolution? resolution = null)
            where T : BaseData
        {
            return History<T>(symbols, Time - span, Time, resolution);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols. The exact number of bars will be returned for
        /// each symbol. This may result in some data start earlier/later than others due to when various
        /// exchanges are open. The symbols must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbols</typeparam>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<DataDictionary<T>> History<T>(IEnumerable<Symbol> symbols, int periods, Resolution? resolution = null) 
            where T : BaseData
        {
            var requests = symbols.Select(x =>
            {
                var security = Securities[x];
                // don't make requests for symbols of the wrong type
                if (!typeof(T).IsAssignableFrom(security.SubscriptionDataConfig.Type)) return null;

                Resolution? res = resolution ?? security.Resolution;
                var start = GetStartTimeAlgoTz(x, periods, resolution).ConvertToUtc(TimeZone);
                return CreateHistoryRequest(security, start, UtcTime.RoundDown(res.Value.ToTimeSpan()), resolution);
            });

            return History(requests.Where(x => x != null)).Get<T>();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbols</typeparam>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<DataDictionary<T>> History<T>(IEnumerable<Symbol> symbols, DateTime start, DateTime end, Resolution? resolution = null) 
            where T : BaseData
        {
            var requests = symbols.Select(x =>
            {
                var security = Securities[x];
                // don't make requests for symbols of the wrong type
                if (!typeof (T).IsAssignableFrom(security.SubscriptionDataConfig.Type)) return null;
                return CreateHistoryRequest(security, start, end, resolution);
            });

            return History(requests.Where(x => x != null)).Get<T>();
        }

        /// <summary>
        /// Gets the historical data for the specified symbol over the request span. The symbol must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbol</typeparam>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<T> History<T>(Symbol symbol, TimeSpan span, Resolution? resolution = null)
            where T : BaseData
        {
            return History<T>(symbol, Time - span, Time, resolution);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol. The exact number of bars will be returned. 
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<TradeBar> History(Symbol symbol, int periods, Resolution? resolution = null)
        {
            var security = Securities[symbol];
            var start = GetStartTimeAlgoTz(symbol, periods, resolution);
            return History(new[] {symbol}, start, Time.RoundDown((resolution ?? security.Resolution).ToTimeSpan()), resolution).Get(symbol);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol. The exact number of bars will be returned. 
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbol</typeparam>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<T> History<T>(Symbol symbol, int periods, Resolution? resolution = null)
            where T : BaseData
        {
            if (resolution == Resolution.Tick) throw new ArgumentException("History functions that accept a 'periods' parameter can not be used with Resolution.Tick");
            var security = Securities[symbol];
            // verify the types match
            var actualType = security.SubscriptionDataConfig.Type;
            var requestedType = typeof(T);
            if (!requestedType.IsAssignableFrom(actualType))
            {
                throw new ArgumentException("The specified security is not of the requested type. Symbol: " + symbol.ToString() + " Requested Type: " + requestedType.Name + " Actual Type: " + actualType);
            }

            var start = GetStartTimeAlgoTz(symbol, periods, resolution);
            return History<T>(symbol, start, Time.RoundDown((resolution ?? security.Resolution).ToTimeSpan()), resolution);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol between the specified dates. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<T> History<T>(Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null)
            where T : BaseData
        {
            var security = Securities[symbol];

            // verify the types match
            var actualType = security.SubscriptionDataConfig.Type;
            var requestedType = typeof(T);
            if (!requestedType.IsAssignableFrom(actualType))
            {
                throw new ArgumentException("The specified security is not of the requested type. Symbol: " + symbol.ToString() + " Requested Type: " + requestedType.Name + " Actual Type: " + actualType);
            }

            var request = CreateHistoryRequest(security, start, end, resolution);
            return History(request).Get<T>(symbol);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol over the request span. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<TradeBar> History(Symbol symbol, TimeSpan span, Resolution? resolution = null)
        {
            return History(new[] {symbol}, span, resolution).Get(symbol);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol over the request span. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<TradeBar> History(Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null)
        {
            return History(new[] {symbol}, start, end, resolution).Get(symbol);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbol's configured values for resolution and fill forward behavior will be used
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<Slice> History(IEnumerable<Symbol> symbols, TimeSpan span, Resolution? resolution = null)
        {
            return History(symbols, Time - span, Time, resolution);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols. The exact number of bars will be returned for
        /// each symbol. This may result in some data start earlier/later than others due to when various
        /// exchanges are open. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<Slice> History(IEnumerable<Symbol> symbols, int periods, Resolution? resolution = null)
        {
            if (resolution == Resolution.Tick) throw new ArgumentException("History functions that accept a 'periods' parameter can not be used with Resolution.Tick");
            return History(CreateBarCountHistoryRequests(symbols, periods, resolution));
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarket">True to include extended market hours data, false otherwise</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<Slice> History(IEnumerable<Symbol> symbols, DateTime start, DateTime end, Resolution? resolution = null, bool? fillForward = null, bool? extendedMarket = null)
        {
            return History(CreateDateRangeHistoryRequests(symbols, start, end, resolution, fillForward, extendedMarket));
        }

        /// <summary>
        /// Gets the start time required for the specified bar count in terms of the algorithm's time zone
        /// </summary>
        private DateTime GetStartTimeAlgoTz(Symbol symbol, int periods, Resolution? resolution = null)
        {
            var security = Securities[symbol];
            var timeSpan = (resolution ?? security.Resolution).ToTimeSpan();
            // make this a minimum of one second
            timeSpan = timeSpan < QuantConnect.Time.OneSecond ? QuantConnect.Time.OneSecond : timeSpan;
            var localStartTime = QuantConnect.Time.GetStartTimeForTradeBars(security.Exchange.Hours, UtcTime.ConvertFromUtc(security.Exchange.TimeZone), timeSpan, periods, security.IsExtendedMarketHours);
            return localStartTime.ConvertTo(security.Exchange.TimeZone, TimeZone);
        }

        /// <summary>
        /// Executes the specified history request
        /// </summary>
        /// <param name="request">the history request to execute</param>
        /// <returns>An enumerable of slice satisfying the specified history request</returns>
        public IEnumerable<Slice> History(HistoryRequest request)
        {
            return History(new[] {request});
        }

        /// <summary>
        /// Executes the specified history requests
        /// </summary>
        /// <param name="requests">the history requests to execute</param>
        /// <returns>An enumerable of slice satisfying the specified history request</returns>
        public IEnumerable<Slice> History(IEnumerable<HistoryRequest> requests)
        {
            return History(requests, TimeZone);
        }

        private IEnumerable<Slice> History(IEnumerable<HistoryRequest> requests, DateTimeZone timeZone)
        {
            var sentMessage = false;
            var reqs = requests.ToList();
            foreach (var request in reqs)
            {
                // prevent future requests
                if (request.EndTimeUtc > UtcTime)
                {
                    request.EndTimeUtc = UtcTime;
                    if (request.StartTimeUtc > request.EndTimeUtc)
                    {
                        request.StartTimeUtc = request.EndTimeUtc;
                    }
                    if (!sentMessage)
                    {
                        sentMessage = true;
                        Debug("Request for future history modified to end now.");
                    }
                }
            }

            // filter out future data to prevent look ahead bias
            return ((IAlgorithm) this).HistoryProvider.GetHistory(reqs, timeZone);
        }

        /// <summary>
        /// Helper method to create history requests from a date range
        /// </summary>
        private IEnumerable<HistoryRequest> CreateDateRangeHistoryRequests(IEnumerable<Symbol> symbols, DateTime startAlgoTz, DateTime endAlgoTz, Resolution? resolution = null, bool? fillForward = null, bool? extendedMarket = null)
        {
            return symbols.Select(x =>
            {
                var security = Securities[x];
                var request = CreateHistoryRequest(security, startAlgoTz, endAlgoTz, resolution);

                // apply overrides
                Resolution? res = resolution ?? security.Resolution;
                if (fillForward.HasValue) request.FillForwardResolution = fillForward.Value ? res : null;
                if (extendedMarket.HasValue) request.IncludeExtendedMarketHours = extendedMarket.Value;
                return request;
            });
        }

        /// <summary>
        /// Helper methods to create a history request for the specified symbols and bar count
        /// </summary>
        private IEnumerable<HistoryRequest> CreateBarCountHistoryRequests(IEnumerable<Symbol> symbols, int periods, Resolution? resolution = null)
        {
            return symbols.Select(x =>
            {
                var security = Securities[x];
                Resolution? res = resolution ?? security.Resolution;
                var start = GetStartTimeAlgoTz(x, periods, res);
                return CreateHistoryRequest(security, start, Time.RoundDown(res.Value.ToTimeSpan()), resolution);
            });
        }

        private HistoryRequest CreateHistoryRequest(Security security, DateTime startAlgoTz, DateTime endAlgoTz, Resolution? resolution)
        {
            resolution = resolution ?? security.Resolution;
            var request = new HistoryRequest(security, startAlgoTz.ConvertToUtc(TimeZone), endAlgoTz.ConvertToUtc(TimeZone))
            {
                DataType = security.SubscriptionDataConfig.IsCustomData ? security.SubscriptionDataConfig.Type : resolution == Resolution.Tick ? typeof(Tick) : typeof(TradeBar),
                Resolution = resolution.Value,
                FillForwardResolution = security.IsFillDataForward ? resolution : null
            };
            return request;
        }
    }
}