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
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages
{
    public class StopMarketOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _highLimit;
        private readonly decimal _lowLimit;

        public StopMarketOrderTestParameters(Symbol symbol, decimal highLimit, decimal lowLimit)
            : base(symbol)
        {
            _highLimit = highLimit;
            _lowLimit = lowLimit;
        }

        public override Order CreateShortOrder(int quantity)
        {
            return new StopMarketOrder(Symbol, -Math.Abs(quantity), _lowLimit, DateTime.Now);
        }

        public override Order CreateLongOrder(int quantity)
        {
            return new StopMarketOrder(Symbol, Math.Abs(quantity), _highLimit, DateTime.Now);
        }

        public override bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice)
        {
            var stop = (StopMarketOrder)order;
            var previousStop = stop.StopPrice;
            if (order.Quantity > 0)
            {
                // for stop buys we need to decrease the stop price
                stop.StopPrice = Math.Min(stop.StopPrice, Math.Max(stop.StopPrice / 2, lastMarketPrice));
            }
            else
            {
                // for stop sells we need to increase the stop price
                stop.StopPrice = Math.Max(stop.StopPrice, Math.Min(stop.StopPrice * 2, lastMarketPrice));
            }
            return stop.StopPrice != previousStop;
        }

        public override OrderStatus ExpectedStatus
        {
            // default limit orders will only be submitted, not filled
            get { return OrderStatus.Submitted; }
        }
    }
}