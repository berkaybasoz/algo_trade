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

using System.Collections.Generic;
using QuantConnect.Securities;

namespace QuantConnect
{
    /// <summary>
    /// Provides commonly used currency pairs and symbols
    /// </summary>
    public static class Currencies
    {
        /// <summary>
        /// Gets the listing of currently supported currency pairs.
        /// </summary>
        /// <remarks>
        /// This listing should be in sync with the data available at: https://www.quantconnect.com/data/FOREX#2.1.1
        /// It must include all currency pairs needed to resolve quote currencies in <see cref="Cash.EnsureCurrencyDataFeed"/>
        /// </remarks>
        public static readonly IReadOnlyList<string> CurrencyPairs = new List<string>
        {
            // these are listed at the top to ensure they get selected first when resolving
            // currency data feeds. the case that showcases the issue is we would select jpyusd
            // instead of usdjpy, even though jpyusd is much less common
            "AUDJPY",
            "AUDUSD",
            "EURCHF",
            "EURGBP",
            "EURJPY",
            "EURUSD",
            "GBPAUD",
            "GBPJPY",
            "GBPUSD",
            "NZDUSD",
            "USDCAD",
            "USDCHF",
            "USDJPY",
            "USDHKD",
            "USDSGD",
            "XAGUSD",


            "AUDCAD",
            "AUDCHF",
            "AUDCNY",
            "AUDCZK",
            "AUDDKK",
            "AUDHKD",
            "AUDHUF",
            "AUDINR",
            "AUDMXN",
            "AUDNOK",
            "AUDNZD",
            "AUDPLN",
            "AUDSAR",
            "AUDSEK",
            "AUDSGD",
            "AUDTHB",
            "AUDTRY",
            "AUDTWD",
            "AUDZAR",
            "CADAUD",
            "CADCHF",
            "CADCNY",
            "CADCZK",
            "CADDKK",
            "CADHKD",
            "CADHUF",
            "CADINR",
            "CADJPY",
            "CADMXN",
            "CADNOK",
            "CADNZD",
            "CADPLN",
            "CADSAR",
            "CADSEK",
            "CADSGD",
            "CADTHB",
            "CADTRY",
            "CADTWD",
            "CADZAR",
            "CHFAUD",
            "CHFCAD",
            "CHFCNY",
            "CHFCZK",
            "CHFDKK",
            "CHFHKD",
            "CHFHUF",
            "CHFINR",
            "CHFJPY",
            "CHFMXN",
            "CHFNOK",
            "CHFNZD",
            "CHFPLN",
            "CHFSAR",
            "CHFSEK",
            "CHFSGD",
            "CHFTHB",
            "CHFTRY",
            "CHFTWD",
            "CHFUSD",
            "CHFZAR",
            "CNYJPY",
            "CZKJPY",
            "DKKJPY",
            "EURAUD",
            "EURCAD",
            "EURCNY",
            "EURCZK",
            "EURDKK",
            "EURHKD",
            "EURHUF",
            "EURINR",
            "EURMXN",
            "EURNOK",
            "EURNZD",
            "EURPLN",
            "EURSAR",
            "EURSEK",
            "EURSGD",
            "EURTHB",
            "EURTRY",
            "EURTWD",
            "EURZAR",
            "GBPCAD",
            "GBPCHF",
            "GBPCNY",
            "GBPCZK",
            "GBPDKK",
            "GBPHKD",
            "GBPHUF",
            "GBPINR",
            "GBPMXN",
            "GBPNOK",
            "GBPNZD",
            "GBPPLN",
            "GBPSAR",
            "GBPSEK",
            "GBPSGD",
            "GBPTHB",
            "GBPTRY",
            "GBPTWD",
            "GBPZAR",
            "HKDCNY",
            "HKDCZK",
            "HKDDKK",
            "HKDHUF",
            "HKDINR",
            "HKDJPY",
            "HKDMXN",
            "HKDNOK",
            "HKDPLN",
            "HKDSAR",
            "HKDSEK",
            "HKDSGD",
            "HKDTHB",
            "HKDTRY",
            "HKDTWD",
            "HKDZAR",
            "INRJPY",
            "JPYHUF",
            "JPYUSD",
            "MXNJPY",
            "NOKJPY",
            "NZDCAD",
            "NZDCHF",
            "NZDHKD",
            "NZDJPY",
            "NZDSGD",
            "PLNJPY",
            "SARJPY",
            "SEKJPY",
            "SGDCHF",
            "SGDCNY",
            "SGDCZK",
            "SGDDKK",
            "SGDHKD",
            "SGDHUF",
            "SGDINR",
            "SGDJPY",
            "SGDMXN",
            "SGDNOK",
            "SGDPLN",
            "SGDSAR",
            "SGDSEK",
            "SGDTHB",
            "SGDTRY",
            "SGDTWD",
            "SGDZAR",
            "THBJPY",
            "TRYJPY",
            "TWDJPY",
            "USDAUD",
            "USDCNY",
            "USDCZK",
            "USDDKK",
            "USDEUR",
            "USDGBP",
            "USDHUF",
            "USDINR",
            "USDMXN",
            "USDNOK",
            "USDPLN",
            "USDSAR",
            "USDSEK",
            "USDTHB",
            "USDTRY",
            "USDTWD",
            "USDZAR",
            "ZARJPY"
        };

        /// <summary>
        /// A mapping of currency codes to their display symbols
        /// </summary>
        /// <remarks>
        /// Now used by Forex and CFD, should probably be moved out into its own class
        /// </remarks>
        public static readonly IReadOnlyDictionary<string, string> CurrencySymbols = new Dictionary<string, string>
        {
            {"USD", "$"},
            {"GBP", "₤"},
            {"JPY", "¥"},
            {"EUR", "€"},
            {"NZD", "$"},
            {"AUD", "$"},
            {"CAD", "$"},
            {"CHF", "Fr"},
            {"HKD", "$"},
            {"SGD", "$"},
            {"XAG", "Ag"},
            {"CNY", "¥"},
            {"CZK", "Kč"},
            {"DKK", "kr"},
            {"HUF", "Ft"},
            {"INR", "₹"},
            {"MXN", "$"},
            {"NOK", "kr"},
            {"PLN", "zł"},
            {"SAR", "﷼"},
            {"SEK", "kr"},
            {"THB", "฿"},
            {"TRY", "₺"},
            {"TWD", "NT$"},
            {"ZAR", "R"}
        };
    }
}
