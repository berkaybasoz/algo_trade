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
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class DividendAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(1998, 01, 01);  //Set Start Date
            SetEndDate(2006, 01, 01);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "MSFT", Resolution.Daily);
            Securities["MSFT"].SetDataNormalizationMode(DataNormalizationMode.Raw);

            // this will use the Tradier Brokerage open order split behavior
            //     forward split will modify open order to maintain order value
            //     reverse split open orders will be cancelled
            SetBrokerageModel(BrokerageName.TradierBrokerage);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (Transactions.OrdersCount == 0)
            {
                SetHoldings("MSFT", .5);
                // place some orders that won't fill, when the split comes in they'll get modified to reflect the split
                Debug("Purchased Stock: " + Securities["MSFT"].Price);
                StopMarketOrder("MSFT", -CalculateOrderQuantity("MSFT", .25), data["MSFT"].Low/2);
                LimitOrder("MSFT", -CalculateOrderQuantity("MSFT", .25), data["MSFT"].High*2);
            }
        }

        /// <summary>
        /// Raises the data event.
        /// </summary>
        /// <param name="data">Data.</param>
        public void OnData(Dividends data) // update this to Dividends dictionary
        {
            var dividend = data["MSFT"];
            Console.WriteLine("{0} >> DIVIDEND >> {1} - {2} - {3} - {4}", dividend.Time.ToString("o"), dividend.Symbol, dividend.Distribution.ToString("C"), Portfolio.Cash, Portfolio["MSFT"].Price.ToString("C"));
        }

        /// <summary>
        /// Raises the data event.
        /// </summary>
        /// <param name="data">Data.</param>
        public void OnData(Splits data)
        {
            Debug("MSFT: " + Securities["MSFT"].Price);
            var split = data["MSFT"];
            Console.WriteLine("{0} >> SPLIT >> {1} - {2} - {3} - {4}", split.Time.ToString("o"), split.Symbol, split.SplitFactor, Portfolio.Cash, Portfolio["MSFT"].Quantity);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // orders get adjusted based on split events to maintain order value
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            Console.WriteLine("{0} >> ORDER >> " + order, Time);
        }
    }
}