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
    public class StopLimitOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _highLimit;
        private readonly decimal _lowLimit;

        public StopLimitOrderTestParameters(Symbol symbol, decimal highLimit, decimal lowLimit)
            : base(symbol)
        {
            _highLimit = highLimit;
            _lowLimit = lowLimit;
        }

        public override Order CreateShortOrder(int quantity)
        {
            return new StopLimitOrder(Symbol, -Math.Abs(quantity), _lowLimit, _highLimit, DateTime.Now);
        }

        public override Order CreateLongOrder(int quantity)
        {
            return new StopLimitOrder(Symbol, Math.Abs(quantity), _highLimit, _lowLimit, DateTime.Now);
        }

        public override bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice)
        {
            var stop = (StopLimitOrder) order;
            var previousStop = stop.StopPrice;
            if (order.Quantity > 0)
            {
                // for stop buys we need to decrease the stop price
                stop.StopPrice = Math.Min(stop.StopPrice, Math.Max(stop.StopPrice/2, Math.Round(lastMarketPrice, 2, MidpointRounding.AwayFromZero)));
                
                //change behaviour for forex type unit tests
                if(order.SecurityType == SecurityType.Forex)
                {
                    stop.StopPrice = Math.Min(stop.StopPrice, Math.Max(stop.StopPrice / 2, Math.Round(lastMarketPrice, 4, MidpointRounding.AwayFromZero)));
                }
            }
            else
            {
                // for stop sells we need to increase the stop price
                stop.StopPrice = Math.Max(stop.StopPrice, Math.Min(stop.StopPrice * 2, Math.Round(lastMarketPrice, 2, MidpointRounding.AwayFromZero)));


                //change behaviour for forex type unit tests
                if (order.SecurityType == SecurityType.Forex)
                {
                    stop.StopPrice = Math.Max(stop.StopPrice, Math.Min(stop.StopPrice * 2, Math.Round(lastMarketPrice, 4, MidpointRounding.AwayFromZero)));
                }
            }
            stop.LimitPrice = stop.StopPrice;
            return stop.StopPrice != previousStop;
        }

        public override OrderStatus ExpectedStatus
        {
            // default limit orders will only be submitted, not filled
            get { return OrderStatus.Submitted; }
        }
    }
}