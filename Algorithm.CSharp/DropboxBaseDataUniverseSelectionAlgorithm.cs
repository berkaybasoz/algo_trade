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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// In this algortihm we show how you can easily use the universe selection feature to fetch symbols
    /// to be traded using the BaseData custom data system in combination with the AddUniverse{T} method.
    /// AddUniverse{T} requires a function that will return the symbols to be traded.
    /// </summary>
    public class DropboxBaseDataUniverseSelectionAlgorithm : QCAlgorithm
    {
        // the changes from the previous universe selection
        private SecurityChanges _changes = SecurityChanges.None;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <seealso cref="QCAlgorithm.SetStartDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetEndDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2013, 01, 01);
            SetEndDate(2013, 12, 31);

            AddUniverse<StockDataSource>("my-stock-data-source", stockDataSource =>
            {
                return stockDataSource.SelectMany(x => x.Symbols);
            });
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <code>
        /// TradeBars bars = slice.Bars;
        /// Ticks ticks = slice.Ticks;
        /// TradeBar spy = slice["SPY"];
        /// List{Tick} aaplTicks = slice["AAPL"]
        /// Quandl oil = slice["OIL"]
        /// dynamic anySymbol = slice[symbol];
        /// DataDictionary{Quandl} allQuandlData = slice.Get{Quand}
        /// Quandl oil = slice.Get{Quandl}("OIL")
        /// </code>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (slice.Bars.Count == 0) return;
            if (_changes == SecurityChanges.None) return;

            // start fresh
            
            Liquidate();

            var percentage = 1m / slice.Bars.Count;
            foreach (var tradeBar in slice.Bars.Values)
            {
                SetHoldings(tradeBar.Symbol, percentage);
            }

            // reset changes
            _changes = SecurityChanges.None;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes"></param>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            // each time our securities change we'll be notified here
            _changes = changes;
        }

        /// <summary>
        /// Our custom data type that defines where to get and how to read our backtest and live data.
        /// </summary>
        class StockDataSource : BaseData
        {
            private const string LiveUrl = @"https://www.dropbox.com/s/2az14r5xbx4w5j6/daily-stock-picker-live.csv?dl=1";
            private const string BacktestUrl = @"https://www.dropbox.com/s/rmiiktz0ntpff3a/daily-stock-picker-backtest.csv?dl=1";

            /// <summary>
            /// The symbols to be selected
            /// </summary>
            public List<string> Symbols { get; set; }

            /// <summary>
            /// Required default constructor
            /// </summary>
            public StockDataSource()
            {
                // initialize our list to empty
                Symbols = new List<string>();
            }

            /// <summary>
            /// Return the URL string source of the file. This will be converted to a stream 
            /// </summary>
            /// <param name="config">Configuration object</param>
            /// <param name="date">Date of this source file</param>
            /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
            /// <returns>String URL of source file.</returns>
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                var url = isLiveMode ? LiveUrl : BacktestUrl;
                return new SubscriptionDataSource(url, SubscriptionTransportMedium.RemoteFile);
            }

            /// <summary>
            /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object 
            /// each time it is called. The returned object is assumed to be time stamped in the config.ExchangeTimeZone.
            /// </summary>
            /// <param name="config">Subscription data config setup object</param>
            /// <param name="line">Line of the source document</param>
            /// <param name="date">Date of the requested data</param>
            /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
            /// <returns>Instance of the T:BaseData object generated by this line of the CSV</returns>
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                try
                {
                    // create a new StockDataSource and set the symbol using config.Symbol
                    var stocks = new StockDataSource {Symbol = config.Symbol};
                    // break our line into csv pieces
                    var csv = line.ToCsv();
                    if (isLiveMode)
                    {
                        // our live mode format does not have a date in the first column, so use date parameter
                        stocks.Time = date;
                        stocks.Symbols.AddRange(csv);
                    }
                    else
                    {
                        // our backtest mode format has the first column as date, parse it
                        stocks.Time = DateTime.ParseExact(csv[0], "yyyyMMdd", null);
                        // any following comma separated values are symbols, save them off
                        stocks.Symbols.AddRange(csv.Skip(1));
                    }
                    return stocks;
                }
                // return null if we encounter any errors
                catch { return null; }
            }
        }
    }
}