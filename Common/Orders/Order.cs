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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Order struct for placing new trade
    /// </summary>
    public abstract class Order 
    {
        /// <summary>
        /// Order ID.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        public int ContingentId { get; internal set; }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        public List<string> BrokerId { get; internal set; }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        public Symbol Symbol { get; internal set; }
        
        /// <summary>
        /// Price of the Order.
        /// </summary>
        public decimal Price { get; internal set; }

        /// <summary>
        /// Time the order was created.
        /// </summary>
        public DateTime Time { get; internal set; }

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public int Quantity { get; internal set; }

        /// <summary>
        /// Order Type
        /// </summary>
        public abstract OrderType Type { get; }

        /// <summary>
        /// Status of the Order
        /// </summary>
        public OrderStatus Status { get; internal set; }

        /// <summary>
        /// Order duration - GTC or Day. Day not supported in backtests.
        /// </summary>
        public OrderDuration Duration { get; internal set; }

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        public string Tag { get; internal set; }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        public SecurityType SecurityType { get { return Symbol.ID.SecurityType; } }

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        public OrderDirection Direction 
        {
            get 
            {
                if (Quantity > 0) 
                {
                    return OrderDirection.Buy;
                } 
                if (Quantity < 0) 
                {
                    return OrderDirection.Sell;
                }
                return OrderDirection.Hold;
            }
        }

        /// <summary>
        /// Get the absolute quantity for this order
        /// </summary>
        public decimal AbsoluteQuantity
        {
            get { return Math.Abs(Quantity); }
        }

        /// <summary>
        /// Gets the executed value of this order. If the order has not yet filled,
        /// then this will return zero.
        /// </summary>
        public decimal Value
        {
            get { return Quantity*Price; }
        }

        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        protected Order()
        {
            Time = new DateTime();
            Price = 0;
            Quantity = 0;
            Symbol = Symbol.Empty;
            Status = OrderStatus.None;
            Tag = "";
            Duration = OrderDuration.GTC;
            BrokerId = new List<string>();
            ContingentId = 0;
            DurationValue = DateTime.MaxValue;
        }

        /// <summary>
        /// New order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="tag">User defined data tag for this order</param>
        protected Order(Symbol symbol, int quantity, DateTime time, string tag = "")
        {
            Time = time;
            Price = 0;
            Quantity = quantity;
            Symbol = symbol;
            Status = OrderStatus.None;
            Tag = tag;
            Duration = OrderDuration.GTC;
            BrokerId = new List<string>();
            ContingentId = 0;
            DurationValue = DateTime.MaxValue;
        }

        /// <summary>
        /// Gets the value of this order at the given market price in units of the account currency
        /// NOTE: Some order types derive value from other parameters, such as limit prices
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        /// <returns>The value of this order given the current market price</returns>
        public decimal GetValue(Security security)
        {
            var value = GetValueImpl(security);
            return value*security.QuoteCurrency.ConversionRate*security.SymbolProperties.ContractMultiplier;
        }

        /// <summary>
        /// Gets the order value in units of the security's quote currency for a single unit.
        /// A single unit here is a single share of stock, or a single barrel of oil, or the
        /// cost of a single share in an option contract.
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        protected abstract decimal GetValueImpl(Security security);

        /// <summary>
        /// Modifies the state of this order to match the update request
        /// </summary>
        /// <param name="request">The request to update this order object</param>
        public virtual void ApplyUpdateOrderRequest(UpdateOrderRequest request)
        {
            if (request.OrderId != Id)
            {
                throw new ArgumentException("Attempted to apply updates to the incorrect order!");
            }
            if (request.Quantity.HasValue)
            {
                Quantity = request.Quantity.Value;
            }
            if (request.Tag != null)
            {
                Tag = request.Tag;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("OrderId: {0} {1} {2} order for {3} unit{4} of {5}", Id, Status, Type, Quantity, Quantity == 1 ? "" : "s", Symbol);
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public abstract Order Clone();

        /// <summary>
        /// Copies base Order properties to the specified order
        /// </summary>
        /// <param name="order">The target of the copy</param>
        protected void CopyTo(Order order)
        {
            order.Id = Id;
            order.Time = Time;
            order.BrokerId = BrokerId.ToList();
            order.ContingentId = ContingentId;
            order.Duration = Duration;
            order.Price = Price;
            order.Quantity = Quantity;
            order.Status = Status;
            order.Symbol = Symbol;
            order.Tag = Tag;
        }

        /// <summary>
        /// Creates an <see cref="Order"/> to match the specified <paramref name="request"/>
        /// </summary>
        /// <param name="request">The <see cref="SubmitOrderRequest"/> to create an order for</param>
        /// <returns>The <see cref="Order"/> that matches the request</returns>
        public static Order CreateOrder(SubmitOrderRequest request)
        {
            Order order;
            switch (request.OrderType)
            {
                case OrderType.Market:
                    order = new MarketOrder(request.Symbol, request.Quantity, request.Time, request.Tag);
                    break;
                case OrderType.Limit:
                    order = new LimitOrder(request.Symbol, request.Quantity, request.LimitPrice, request.Time, request.Tag);
                    break;
                case OrderType.StopMarket:
                    order = new StopMarketOrder(request.Symbol, request.Quantity, request.StopPrice, request.Time, request.Tag);
                    break;
                case OrderType.StopLimit:
                    order = new StopLimitOrder(request.Symbol, request.Quantity, request.StopPrice, request.LimitPrice, request.Time, request.Tag);
                    break;
                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder(request.Symbol, request.Quantity, request.Time, request.Tag);
                    break;
                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder(request.Symbol, request.Quantity, request.Time, request.Tag);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            order.Status = OrderStatus.New;
            order.Id = request.OrderId;
            if (request.Tag != null)
            {
                order.Tag = request.Tag;
            }
            return order;
        }

        /// <summary>
        /// Order Expiry on a specific UTC time.
        /// </summary>
        public DateTime DurationValue;
    }
}
