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
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;

namespace QuantConnect.Securities.Equity 
{
    /// <summary>
    /// Equity Security Type : Extension of the underlying Security class for equity specific behaviours.
    /// </summary>
    /// <seealso cref="Security"/>
    public class Equity : Security
    {
        /// <summary>
        /// The default number of days required to settle an equity sale
        /// </summary>
        public const int DefaultSettlementDays = 3;

        /// <summary>
        /// The default time of day for settlement
        /// </summary>
        public static readonly TimeSpan DefaultSettlementTime = new TimeSpan(8, 0, 0);

        /// <summary>
        /// Construct the Equity Object
        /// </summary>
        public Equity(SecurityExchangeHours exchangeHours, SubscriptionDataConfig config, Cash quoteCurrency, SymbolProperties symbolProperties)
            :

            base(
                config,
                quoteCurrency,
                symbolProperties,
                new EquityExchange(exchangeHours),
                new EquityCache(),
                new SecurityPortfolioModel(),
                new TEBFillModel(), 
                new TEBFeeModel(0), 
                new TEBSlippageModel(0m), 
                new ImmediateSettlementModel(),
                new TEBSecurityMarginModel(1m), 
                new EquityDataFilter()
                )
            //berkay 2016-04-12

            //base(
            //    config,
            //    quoteCurrency,
            //    symbolProperties,
            //    new EquityExchange(exchangeHours),
            //    new EquityCache(),
            //    new SecurityPortfolioModel(),
            //    new ImmediateFillModel(),
            //    new InteractiveBrokersFeeModel(),
            //    new ConstantSlippageModel(0m),
            //    new ImmediateSettlementModel(),
            //    new SecurityMarginModel(2m),
            //    new EquityDataFilter()
            //    )
        {
            Holdings = new EquityHolding(this);
        }
    }
}
