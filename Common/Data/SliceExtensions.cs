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
using QuantConnect.Data.Market;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Provides extension methods to slice enumerables
    /// </summary>
    public static class SliceExtensions
    {
        /// <summary>
        /// Selects into the slice and returns the TradeBars that have data in order
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <returns>An enumerable of TradeBars</returns>
        public static IEnumerable<TradeBars> TradeBars(this IEnumerable<Slice> slices)
        {
            return slices.Where(x => x.Bars.Count > 0).Select(x => x.Bars);
        }

        /// <summary>
        /// Selects into the slice and returns the Ticks that have data in order
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <returns>An enumerable of Ticks</returns>
        public static IEnumerable<Ticks> Ticks(this IEnumerable<Slice> slices)
        {
            return slices.Where(x => x.Ticks.Count > 0).Select(x => x.Ticks);
        }

        /// <summary>
        /// Gets an enumerable of TradeBar for the given symbol. This method does not verify
        /// that the specified symbol points to a TradeBar
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <returns>An enumerable of TradeBar for the matching symbol, of no TradeBar found for symbol, empty enumerable is returned</returns>
        public static IEnumerable<TradeBar> Get(this IEnumerable<Slice> slices, Symbol symbol)
        {
            return slices.TradeBars().Where(x => x.ContainsKey(symbol)).Select(x => x[symbol]);
        }

        /// <summary>
        /// Gets an enumerable of T for the given symbol. This method does not vify
        /// that the specified symbol points to a T
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="dataDictionaries">The data dictionary enumerable to access</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <returns>An enumerable of T for the matching symbol, if no T is found for symbol, empty enumerable is returned</returns>
        public static IEnumerable<T> Get<T>(this IEnumerable<DataDictionary<T>> dataDictionaries, Symbol symbol)
            where T : BaseData
        {
            return dataDictionaries.Where(x => x.ContainsKey(symbol)).Select(x => x[symbol]);
        }

        /// <summary>
        /// Gets an enumerable of decimals by accessing the specified field on data for the symbol
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="dataDictionaries">An enumerable of data dictionaries</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <param name="field">The field to access</param>
        /// <returns>An enumerable of decimals</returns>
        public static IEnumerable<decimal> Get<T>(this IEnumerable<DataDictionary<T>> dataDictionaries, Symbol symbol, string field)
        {
            Func<T, decimal> selector;
            if (typeof (DynamicData).IsAssignableFrom(typeof (T)))
            {
                selector = data =>
                {
                    var dyn = (DynamicData) (object) data;
                    return (decimal) dyn.GetProperty(field);
                };
            }
            else if (typeof (T) == typeof (List<Tick>))
            {
                // perform the selection on the last tick
                // NOTE: This is a known bug, should be updated to perform the selection on each item in the list
                var dataSelector = (Func<Tick, decimal>) ExpressionBuilder.MakePropertyOrFieldSelector(typeof (Tick), field).Compile();
                selector = ticks => dataSelector(((List<Tick>) (object) ticks).Last());
            }
            else
            {
                selector = (Func<T, decimal>) ExpressionBuilder.MakePropertyOrFieldSelector(typeof (T), field).Compile();
            }

            foreach (var dataDictionary in dataDictionaries)
            {
                T item;
                if (dataDictionary.TryGetValue(symbol, out item))
                {
                    yield return selector(item);
                }
            }
        }

        /// <summary>
        /// Gets the data dictionaries of the requested type in each slice
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="slices">The enumerable of slice</param>
        /// <returns>An enumerable of data dictionary of the requested type</returns>
        public static IEnumerable<DataDictionary<T>> Get<T>(this IEnumerable<Slice> slices)
            where T : BaseData
        {
            return slices.Select(x => x.Get<T>()).Where(x => x.Count > 0);
        }

        /// <summary>
        /// Gets an enumerable of T by accessing the slices for the requested symbol
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="slices">The enumerable of slice</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <returns>An enumerable of T by accessing each slice for the requested symbol</returns>
        public static IEnumerable<T> Get<T>(this IEnumerable<Slice> slices, Symbol symbol)
            where T : BaseData
        {
            return slices.Select(x => x.Get<T>()).Where(x => x.ContainsKey(symbol)).Select(x => x[symbol]);
        }

        /// <summary>
        /// Gets an enumerable of decimal by accessing the slice for the symbol and then retrieving the specified
        /// field on each piece of data
        /// </summary>
        /// <param name="slices">The enumerable of slice</param>
        /// <param name="symbol">The symbol to retrieve</param>
        /// <param name="field">The field selector used to access the dats</param>
        /// <returns>An enumerable of decimal</returns>
        public static IEnumerable<decimal> Get(this IEnumerable<Slice> slices, Symbol symbol, Func<BaseData, decimal> field)
        {
            foreach (var slice in slices)
            {
                dynamic item;
                if (slice.TryGetValue(symbol, out item))
                {
                    if (item is List<Tick>) yield return field(item.Last());
                    else yield return field(item);
                }
            }
        }

        /// <summary>
        /// Converts the specified enumerable of decimals into a double array
        /// </summary>
        /// <param name="decimals">The enumerable of decimal</param>
        /// <returns>Double array representing the enumerable of decimal</returns>
        public static double[] ToDoubleArray(this IEnumerable<decimal> decimals)
        {
            return decimals.Select(x => (double) x).ToArray();
        }
    }
}