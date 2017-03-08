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

using System.Collections.Generic;
using QuantConnect.Algorithm;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    /// <summary>
    /// This type allows tests to easily create an algorith that is mostly initialized in one line
    /// </summary>
    public class AlgorithmStub : QCAlgorithm
    {
        public AlgorithmStub(Resolution resolution = Resolution.Second, List<string> equities = null, List<string> forex = null)
        {
            foreach (var ticker in equities ?? new List<string>())
            {
                AddSecurity(SecurityType.Equity, ticker, resolution);
                var symbol = SymbolCache.GetSymbol(ticker);
                Securities[symbol].Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            }
            foreach (var ticker in forex ?? new List<string>())
            {
                AddSecurity(SecurityType.Forex, ticker, resolution);
                var symbol = SymbolCache.GetSymbol(ticker);
                Securities[symbol].Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.EasternStandard));
            }
        }
    }
}