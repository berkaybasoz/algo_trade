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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Represents an entire map file for a specified symbol
    /// </summary>
    public class MapFile : IEnumerable<MapFileRow>
    {
        private readonly SortedDictionary<DateTime, MapFileRow> _data;

        /// <summary>
        /// Gets the entity's unique symbol, i.e OIH.1
        /// </summary>
        public string Permtick { get; private set; }

        /// <summary>
        /// Gets the last date in the map file which is indicative of a delisting event
        /// </summary>
        public DateTime DelistingDate
        {
            get { return _data.Keys.Count == 0 ? Time.EndOfTime : _data.Keys.Last(); }
        }

        /// <summary>
        /// Gets the first date in this map file
        /// </summary>
        public DateTime FirstDate
        {
            get { return _data.Keys.Count == 0 ? Time.BeginningOfTime : _data.Keys.First(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFile"/> class.
        /// </summary>
        public MapFile(string permtick, IEnumerable<MapFileRow> data)
        {
            Permtick = permtick.ToUpper();
            _data = new SortedDictionary<DateTime, MapFileRow>(data.Distinct().ToDictionary(x => x.Date));
        }

        /// <summary>
        /// Memory overload search method for finding the mapped symbol for this date.
        /// </summary>
        /// <param name="searchDate">date for symbol we need to find.</param>
        /// <returns>Symbol on this date.</returns>
        public string GetMappedSymbol(DateTime searchDate)
        {
            var mappedSymbol = "";
            //Iterate backwards to find the most recent factor:
            foreach (var splitDate in _data.Keys)
            {
                if (splitDate < searchDate) continue;
                mappedSymbol = _data[splitDate].MappedSymbol;
                break;
            }
            return mappedSymbol;
        }

        /// <summary>
        /// Determines if there's data for the requested date
        /// </summary>
        public bool HasData(DateTime date)
        {
            // handle the case where we don't have any data
            if (_data.Count == 0)
            {
                return true;
            }

            if (date < _data.Keys.First() || date > _data.Keys.Last())
            {
                // don't even bother checking the disk if the map files state we don't have ze dataz
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads in an entire map file for the requested symbol from the DataFolder
        /// </summary>
        public static MapFile Read(string permtick, string market)
        {
            return new MapFile(permtick, MapFileRow.Read(permtick, market));
        }

        /// <summary>
        /// Constructs the map file path for the specified market and symbol
        /// </summary>
        /// <param name="permtick">The symbol as on disk, OIH or OIH.1</param>
        /// <param name="market">The market this symbol belongs to</param>
        /// <returns>The file path to the requested map file</returns>
        public static string GetMapFilePath(string permtick, string market)
        {
            return Path.Combine(Constants.DataFolder, "equity", market, "map_files", permtick.ToLower() + ".csv");
        }

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<MapFileRow> GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Reads all the map files in the specified directory
        /// </summary>
        /// <param name="mapFileDirectory">The map file directory path</param>
        /// <returns>An enumerable of all map files</returns>
        public static IEnumerable<MapFile> GetMapFiles(string mapFileDirectory)
        {
            return from file in Directory.EnumerateFiles(mapFileDirectory)
                   where file.EndsWith(".csv")
                   let permtick = Path.GetFileNameWithoutExtension(file)
                   let fileRead = SafeMapFileRowRead(file)
                   select new MapFile(permtick, fileRead);
        }

        /// <summary>
        /// Reads in the map file at the specified path, returning null if any exceptions are encountered
        /// </summary>
        private static List<MapFileRow> SafeMapFileRowRead(string file)
        {
            try
            {
                return MapFileRow.Read(file).ToList();
            }
            catch (Exception err)
            {
                Log.Error(err, "File: " + file);
                return new List<MapFileRow>();
            }
        }
    }
}