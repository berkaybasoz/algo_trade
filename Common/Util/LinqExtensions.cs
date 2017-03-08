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
using System.Collections.ObjectModel;
using System.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides more extension methods for the enumerable types
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Creates a dictionary multimap from the lookup.
        /// </summary>
        /// <typeparam name="K">The key type</typeparam>
        /// <typeparam name="V">The value type</typeparam>
        /// <param name="lookup">The ILookup instance to convert to a dictionary</param>
        /// <returns>A dictionary holding the same data as 'lookup'</returns>
        public static Dictionary<K, List<V>> ToDictionary<K, V>(this ILookup<K, V> lookup)
        {
            return lookup.ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
        }

        /// <summary>
        /// Creates a dictionary enumerable of key value pairs
        /// </summary>
        /// <typeparam name="K">The key type</typeparam>
        /// <typeparam name="V">The value type</typeparam>
        /// <param name="enumerable">The IEnumerable of KeyValuePair instances to convert to a dictionary</param>
        /// <returns>A dictionary holding the same data as the enumerable</returns>
        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> enumerable)
        {
            return enumerable.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Creates a new read-only dictionary from the key value pairs
        /// </summary>
        /// <typeparam name="K">The key type</typeparam>
        /// <typeparam name="V">The value type</typeparam>
        /// <param name="enumerable">The IEnumerable of KeyValuePair instances to convert to a dictionary</param>
        /// <returns>A read-only dictionary holding the same data as the enumerable</returns>
        public static IReadOnlyDictionary<K, V> ToReadOnlyDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> enumerable)
        {
            return new ReadOnlyDictionary<K, V>(enumerable.ToDictionary());
        }

        /// <summary>
        /// Creates a new <see cref="HashSet{T}"/> from the elements in the specified enumerable
        /// </summary>
        /// <typeparam name="T">The item type in the hash set</typeparam>
        /// <param name="enumerable">The items to be placed into the enumerable</param>
        /// <returns>A new <see cref="HashSet{T}"/> containing the items in the enumerable</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }

        /// <summary>
        /// Creates a new <see cref="HashSet{T}"/> from the elements in the specified enumerable
        /// </summary>
        /// <typeparam name="T">The item type of the source enumerable</typeparam>
        /// <typeparam name="TResult">The type of the items in the output <see cref="HashSet{T}"/></typeparam>
        /// <param name="enumerable">The items to be placed into the enumerable</param>
        /// <param name="selector">Selects items from the enumerable to be placed into the <see cref="HashSet{T}"/></param>
        /// <returns>A new <see cref="HashSet{T}"/> containing the items in the enumerable</returns>
        public static HashSet<TResult> ToHashSet<T, TResult>(this IEnumerable<T> enumerable, Func<T, TResult> selector)
        {
            return new HashSet<TResult>(enumerable.Select(selector));
        }

        /// <summary>
        /// Returns true if the specified enumerable is null or has no elements
        /// </summary>
        /// <typeparam name="T">The enumerable's item type</typeparam>
        /// <param name="enumerable">The enumerable to check for a value</param>
        /// <returns>True if the enumerable has elements, false otherwise</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }

        /// <summary>
        /// Performs the specified selector before calling DefaultIfEmpty. This is just short hand for Select(selector).DefaultIfEmpty(defaultValue)
        /// </summary>
        public static IEnumerable<TResult> DefaultIfEmpty<T, TResult>(this IEnumerable<T> enumerable, Func<T, TResult> selector, TResult defaultValue = default(TResult))
        {
            return enumerable.Select(selector).DefaultIfEmpty(defaultValue);
        }

        /// <summary>
        /// Gets the median value in the collection
        /// </summary>
        /// <typeparam name="T">The item type in the collection</typeparam>
        /// <param name="enumerable">The enumerable of items to search</param>
        /// <returns>The median value, throws InvalidOperationException if no items are present</returns>
        public static T Median<T>(this IEnumerable<T> enumerable)
        {
            var collection = enumerable.ToList();
            return collection.OrderBy(x => x).Skip(collection.Count/2).First();
        }

        /// <summary>
        /// Gets the median value in the collection
        /// </summary>
        /// <typeparam name="T">The item type in the collection</typeparam>
        /// <typeparam name="TProperty">The type of the value selected</typeparam>
        /// <param name="collection">The collection of items to search</param>
        /// <param name="selector">Function used to select a value from collection items</param>
        /// <returns>The median value, throws InvalidOperationException if no items are present</returns>
        public static TProperty Median<T, TProperty>(this IEnumerable<T> collection, Func<T, TProperty> selector)
        {
            return collection.Select(selector).Median();
        }

        /// <summary>
        /// Performs a binary search on the specified collection.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <typeparam name="TSearch">The type of the searched item.</typeparam>
        /// <param name="list">The list to be searched.</param>
        /// <param name="value">The value to search for.</param>
        /// <param name="comparer">The comparer that is used to compare the value with the list items.</param>
        /// <returns>The index of the item if found, otherwise the bitwise complement where the value should be per MSDN specs</returns>
        public static int BinarySearch<TItem, TSearch>(this IList<TItem> list, TSearch value, Func<TSearch, TItem, int> comparer)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }
            if (comparer == null)
            {
                throw new ArgumentNullException("comparer");
            }

            var lower = 0;
            var upper = list.Count - 1;

            while (lower <= upper)
            {
                var middle = lower + (upper - lower) / 2;
                var comparisonResult = comparer(value, list[middle]);
                if (comparisonResult < 0)
                {
                    upper = middle - 1;
                }
                else if (comparisonResult > 0)
                {
                    lower = middle + 1;
                }
                else
                {
                    return middle;
                }
            }

            return ~lower;
        }

        /// <summary>
        /// Performs a binary search on the specified collection.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="list">The list to be searched.</param>
        /// <param name="value">The value to search for.</param>
        /// <returns>The index of the item if found, otherwise the bitwise complement where the value should be per MSDN specs</returns>
        public static int BinarySearch<TItem>(this IList<TItem> list, TItem value)
        {
            return BinarySearch(list, value, Comparer<TItem>.Default);
        }

        /// <summary>
        /// Performs a binary search on the specified collection.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="list">The list to be searched.</param>
        /// <param name="value">The value to search for.</param>
        /// <param name="comparer">The comparer that is used to compare the value with the list items.</param>
        /// <returns>The index of the item if found, otherwise the bitwise complement where the value should be per MSDN specs</returns>
        public static int BinarySearch<TItem>(this IList<TItem> list, TItem value, IComparer<TItem> comparer)
        {
            return list.BinarySearch(value, comparer.Compare);
        }

        /// <summary>
        /// Wraps the specified enumerable such that it will only be enumerated once
        /// </summary>
        /// <typeparam name="T">The enumerable's element type</typeparam>
        /// <param name="enumerable">The source enumerable to be wrapped</param>
        /// <returns>A new enumerable that can be enumerated multiple times without re-enumerating the source enumerable</returns>
        public static IEnumerable<T> Memoize<T>(this IEnumerable<T> enumerable)
        {
            return new MemoizingEnumerable<T>(enumerable);
        }

        /// <summary>
        /// Produces the an enumerable of the range of values between start and end using the specified
        /// incrementing function
        /// </summary>
        /// <typeparam name="T">The enumerable item type</typeparam>
        /// <param name="start">The start of the range</param>
        /// <param name="end">The end of the range, non-inclusive by default</param>
        /// <param name="incrementer">The incrementing function, with argument of the current item</param>
        /// <param name="includeEndPoint">True to emit the end point, false otherwise</param>
        /// <returns>An enumerable of the range of items between start and end</returns>
        public static IEnumerable<T> Range<T>(T start, T end, Func<T, T> incrementer, bool includeEndPoint = false)
            where T : IComparable
        {
            var current = start;
            if (includeEndPoint)
            {
                while (current.CompareTo(end) <= 0)
                {
                    yield return current;
                    current = incrementer(current);
                }
            }
            else
            {
                while (current.CompareTo(end) < 0)
                {
                    yield return current;
                    current = incrementer(current);
                }
            }
        }

        /// <summary>
        /// Creates a new enumerable that will be distinct by the specified property selector
        /// </summary>
        /// <typeparam name="T">The enumerable item type</typeparam>
        /// <typeparam name="TPropery">The selected property type</typeparam>
        /// <param name="enumerable">The source enumerable</param>
        /// <param name="selector">The property selector</param>
        /// <returns>A filtered enumerable distinct on the selected property</returns>
        public static IEnumerable<T> DistincyBy<T, TPropery>(this IEnumerable<T> enumerable, Func<T, TPropery> selector)
        {
            var hash = new HashSet<TPropery>();
            return enumerable.Where(x => hash.Add(selector(x)));
        }

        /// <summary>
        /// Groups adjacent elements of the enumerale using the specified grouper function
        /// </summary>
        /// <typeparam name="T">The enumerable item type</typeparam>
        /// <param name="enumerable">The source enumerable to be grouped</param>
        /// <param name="grouper">A function that accepts the previous value and the next value and returns
        /// true if the next value belongs in the same group as the previous value, otherwise returns false</param>
        /// <returns>A new enumerable of the groups defined by grouper. These groups don't have a key
        /// and are only grouped by being emitted separately from this enumerable</returns>
        public static IEnumerable<IEnumerable<T>> GroupAdjacentBy<T>(this IEnumerable<T> enumerable, Func<T, T, bool> grouper)
        {
            using (var e = enumerable.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    var list = new List<T> {e.Current};
                    var pred = e.Current;
                    while (e.MoveNext())
                    {
                        if (grouper(pred, e.Current))
                        {
                            list.Add(e.Current);
                        }
                        else
                        {
                            yield return list;
                            list = new List<T> {e.Current};
                        }
                        pred = e.Current;
                    }
                    yield return list;
                }
            }
        }
    }
}
