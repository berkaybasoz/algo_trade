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
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.GoogleDownloader
{
    class Program
    {
        /// <summary>
        /// QuantConnect Google Downloader For LEAN Algorithmic Trading Engine.
        /// Original by @chrisdk2015, tidied by @jaredbroad
        /// </summary>
        public static void Main(string[] args)
        {
            //if (args.Length != 4)
            //{
            //    Console.WriteLine("Usage: GoogleDownloader SYMBOLS RESOLUTION FROMDATE TODATE");
            //    Console.WriteLine("SYMBOLS = eg SPY,AAPL");
            //    Console.WriteLine("RESOLUTION = Minute/Hour");
            //    Console.WriteLine("FROMDATE = yyyymmdd");
            //    Console.WriteLine("TODATE = yyyymmdd");
            //    Environment.Exit(1);
            //}

            try
            {
                // Load settings from command line
                var symbols = "GARAN,AKBNK".Split(',');
                var resolution = (Resolution)Enum.Parse(typeof(Resolution), "Minute");
                var startDate = DateTime.ParseExact("20150101", "yyyyMMdd", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact("20160101", "yyyyMMdd", CultureInfo.InvariantCulture);

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                // Create an instance of the downloader
                const string market = Market.USA;
                var downloader = new GoogleDataDownloader();

                foreach (var symbol in symbols)
                {
                    // Download the data
                    var symbolObject = Symbol.Create(symbol, SecurityType.Equity, market);
                    var data = downloader.Get(symbolObject, resolution, startDate, endDate);

                    // Save the data
                    var writer = new LeanDataWriter(resolution, symbolObject, dataDirectory);
                    writer.Write(data);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

    }
}
