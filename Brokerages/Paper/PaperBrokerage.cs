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
using System.Linq;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.Paper
{
    /// <summary>
    /// Paper Trading Brokerage
    /// </summary>
    public class PaperBrokerage : BacktestingBrokerage
    {
        private readonly LiveNodePacket _job;

        /// <summary>
        /// Creates a new PaperBrokerage
        /// </summary>
        /// <param name="algorithm">The algorithm under analysis</param>
        /// <param name="job">The job packet</param>
        public PaperBrokerage(IAlgorithm algorithm, LiveNodePacket job) 
            : base(algorithm, "Paper Brokerage")
        {
            _job = job;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            string value;
            if (_job.BrokerageData.TryGetValue("project-paper-equity", out value))
            {
                // remove the key, we really only want to return the cached value on the first request
                _job.BrokerageData.Remove("project-paper-equity");
                return new List<Cash>{new Cash("USD", decimal.Parse(value), 1)};
            }

            // if we've already begun running, just return the current state
            return Algorithm.Portfolio.CashBook.Values.ToList();
        }
    }
}
