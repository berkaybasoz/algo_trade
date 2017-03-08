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
using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides the ability to synchronize subscriptions into time slices
    /// </summary>
    public class SubscriptionSynchronizer
    {
        private readonly UniverseSelection _universeSelection;

        /// <summary>
        /// Event fired when a subscription is finished
        /// </summary>
        public event EventHandler<Subscription> SubscriptionFinished;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionSynchronizer"/> class
        /// </summary>
        /// <param name="universeSelection">The universe selection instance used to handle universe
        /// selection subscription output</param>
        public SubscriptionSynchronizer(UniverseSelection universeSelection)
        {
            _universeSelection = universeSelection;
        }

        /// <summary>
        /// Syncs the specifies subscriptions at the frontier time
        /// </summary>
        /// <param name="frontier">The time used for syncing, data in the future won't be included in this time slice</param>
        /// <param name="subscriptions">The subscriptions to sync</param>
        /// <param name="sliceTimeZone">The time zone of the created slice object</param>
        /// <param name="cashBook">The cash book, used for creating the cash book updates</param>
        /// <param name="nextFrontier">The next frontier time as determined by the first piece of data in the future ahead of the frontier.
        /// This value will equal DateTime.MaxValue when the subscriptions are all finished</param>
        /// <returns>A time slice for the specified frontier time</returns>
        public TimeSlice Sync(DateTime frontier, IEnumerable<Subscription> subscriptions, DateTimeZone sliceTimeZone, CashBook cashBook, out DateTime nextFrontier)
        {
            var changes = SecurityChanges.None;
            nextFrontier = DateTime.MaxValue;
            var earlyBirdTicks = nextFrontier.Ticks;
            var data = new List<KeyValuePair<Security, List<BaseData>>>();

            SecurityChanges newChanges;
            do
            {
                newChanges = SecurityChanges.None;
                foreach (var subscription in subscriptions)
                {
                    if (subscription.EndOfStream)
                    {
                        OnSubscriptionFinished(subscription);
                        continue;
                    }

                    // prime if needed
                    if (subscription.Current == null)
                    {
                        if (!subscription.MoveNext())
                        {
                            OnSubscriptionFinished(subscription);
                            continue;
                        }
                    }

                    var cache = new KeyValuePair<Security, List<BaseData>>(subscription.Security, new List<BaseData>());
                    data.Add(cache);

                    var configuration = subscription.Configuration;
                    var offsetProvider = subscription.OffsetProvider;
                    var currentOffsetTicks = offsetProvider.GetOffsetTicks(frontier);
                    while (subscription.Current.EndTime.Ticks - currentOffsetTicks <= frontier.Ticks)
                    {
                        // we want bars rounded using their subscription times, we make a clone
                        // so we don't interfere with the enumerator's internal logic
                        var clone = subscription.Current.Clone(subscription.Current.IsFillForward);
                        clone.Time = clone.Time.ExchangeRoundDown(configuration.Increment, subscription.Security.Exchange.Hours, configuration.ExtendedMarketHours);
                        cache.Value.Add(clone);
                        if (!subscription.MoveNext())
                        {
                            OnSubscriptionFinished(subscription);
                            break;
                        }
                    }

                    // we have new universe data to select based on
                    if (subscription.IsUniverseSelectionSubscription && cache.Value.Count > 0)
                    {
                        // assume that if the first item is a base data collection then the enumerator handled the aggregation,
                        // otherwise, load all the the data into a new collection instance
                        var collection = cache.Value[0] as BaseDataCollection ?? new BaseDataCollection(frontier, subscription.Configuration.Symbol, cache.Value);
                        newChanges += _universeSelection.ApplyUniverseSelection(subscription.Universe, frontier, collection);
                    }

                    if (subscription.Current != null)
                    {
                        // take the earliest between the next piece of data or the next tz discontinuity
                        earlyBirdTicks = Math.Min(earlyBirdTicks, Math.Min(subscription.Current.EndTime.Ticks - currentOffsetTicks, offsetProvider.GetNextDiscontinuity()));
                    }
                }

                changes += newChanges;
            }
            while (newChanges != SecurityChanges.None);

            nextFrontier = new DateTime(Math.Max(earlyBirdTicks, frontier.Ticks), DateTimeKind.Utc);

            return TimeSlice.Create(frontier, sliceTimeZone, cashBook, data, changes);
        }

        /// <summary>
        /// Event invocator for the <see cref="SubscriptionFinished"/> event
        /// </summary>
        protected virtual void OnSubscriptionFinished(Subscription subscription)
        {
            var handler = SubscriptionFinished;
            if (handler != null) handler(this, subscription);
        }
    }
}