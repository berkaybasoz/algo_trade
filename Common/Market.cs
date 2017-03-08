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

namespace QuantConnect
{
    /// <summary>
    /// Markets Collection: Soon to be expanded to a collection of items specifying the market hour, timezones and country codes.
    /// </summary>
    public static class Market
    {
        // the upper bound (non-inclusive) for market identifiers
        private const int MaxMarketIdentifier = 1000;

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, int> Markets = new Dictionary<string, int>();
        private static readonly Dictionary<int, string> ReverseMarkets = new Dictionary<int, string>();
        private static readonly IEnumerable<Tuple<string, int>> HardcodedMarkets = new List<Tuple<string, int>>
        {
            Tuple.Create("empty", 0),
            Tuple.Create(USA, 1),
            Tuple.Create(FXCM, 2),
            Tuple.Create(Oanda, 3),
            Tuple.Create(Dukascopy, 4)
        };

        static Market()
        {
            // initialize our maps
            foreach (var market in HardcodedMarkets)
            {
                Markets[market.Item1] = market.Item2;
                ReverseMarkets[market.Item2] = market.Item1;
            }
        }

        /// <summary>
        /// USA Market 
        /// </summary>
        public const string USA = "usa";

        /// <summary>
        /// Oanda Market
        /// </summary>
        public const string Oanda = "oanda";

        /// <summary>
        /// FXCM Market Hours
        /// </summary>
        public const string FXCM = "fxcm";

        /// <summary>
        /// Dukascopy Market
        /// </summary>
        public const string Dukascopy = "dukascopy";

        /// <summary>
        /// Adds the specified market to the map of available markets with the specified identifier.
        /// </summary>
        /// <param name="market">The market string to add</param>
        /// <param name="identifier">The identifier for the market, this value must be positive and less than 1000</param>
        public static void Add(string market, int identifier)
        {
            if (identifier >= MaxMarketIdentifier)
            {
                var message = string.Format("The market identifier is limited to positive values less than {0}.", MaxMarketIdentifier);
                throw new ArgumentOutOfRangeException("identifier", message);
            }

            market = market.ToLower();

            // we lock since we don't want multiple threads getting these two dictionaries out of sync
            lock (_lock)
            {
                int marketIdentifier;
                if (Markets.TryGetValue(market, out marketIdentifier) && identifier != marketIdentifier)
                {
                    throw new ArgumentException("Attempted to add an already added market with a different identifier. Market: " + market);
                }

                string existingMarket;
                if (ReverseMarkets.TryGetValue(identifier, out existingMarket))
                {
                    throw new ArgumentException("Attempted to add a market identifier that is already in use. New Market: " + market + " Existing Market: " + existingMarket);
                }

                // update our maps
                Markets[market] = identifier;
                ReverseMarkets[identifier] = market;
            }
        }

        /// <summary>
        /// Gets the market code for the specified market. Returns <c>null</c> if the market is not found
        /// </summary>
        /// <param name="market">The market to check for (case sensitive)</param>
        /// <returns>The internal code used for the market. Corresponds to the value used when calling <see cref="Add"/></returns>
        public static int? Encode(string market)
        {
            lock (_lock)
            {
                int code;
                return !Markets.TryGetValue(market, out code) ? (int?) null : code;
            }
        }

        /// <summary>
        /// Gets the market string for the specified market code.
        /// </summary>
        /// <param name="code">The market code to be decoded</param>
        /// <returns>The string representation of the market, or null if not found</returns>
        public static string Decode(int code)
        {
            lock (_lock)
            {
                string market;
                return !ReverseMarkets.TryGetValue(code, out market) ? null : market;
            }
        }
    }
}