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
using System.Threading;
using QuantConnect.Securities;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides a single reference to an order for the algorithm to maintain. As the order gets
    /// updated this ticket will also get updated
    /// </summary>
    public sealed class OrderTicket
    {
        private readonly object _orderEventsLock = new object();
        private readonly object _updateRequestsLock = new object();
        private readonly object _setCancelRequestLock = new object();

        private Order _order;
        private OrderStatus? _orderStatusOverride;
        private CancelOrderRequest _cancelRequest;

        private int _quantityFilled;
        private decimal _averageFillPrice;

        private readonly int _orderId;
        private readonly List<OrderEvent> _orderEvents; 
        private readonly SubmitOrderRequest _submitRequest;
        private readonly ManualResetEvent _orderStatusClosedEvent;
        private readonly List<UpdateOrderRequest> _updateRequests;

        // we pull this in to provide some behavior/simplicity to the ticket API
        private readonly SecurityTransactionManager _transactionManager;

        /// <summary>
        /// Gets the order id of this ticket
        /// </summary>
        public int OrderId
        {
            get { return _orderId; }
        }

        /// <summary>
        /// Gets the current status of this order ticket
        /// </summary>
        public OrderStatus Status
        {
            get
            {
                if (_orderStatusOverride.HasValue) return _orderStatusOverride.Value;
                return _order == null ? OrderStatus.New : _order.Status;
            }
        }

        /// <summary>
        /// Gets the symbol being ordered
        /// </summary>
        public Symbol Symbol
        {
            get { return _submitRequest.Symbol; }
        }

        /// <summary>
        /// Gets the <see cref="Symbol"/>'s <see cref="SecurityType"/>
        /// </summary>
        public SecurityType SecurityType
        {
            get { return _submitRequest.SecurityType; }
        }

        /// <summary>
        /// Gets the number of units ordered
        /// </summary>
        public int Quantity
        {
            get { return _order == null ? _submitRequest.Quantity : _order.Quantity; }
        }

        /// <summary>
        /// Gets the average fill price for this ticket. If no fills have been processed
        /// then this will return a value of zero.
        /// </summary>
        public decimal AverageFillPrice
        {
            get { return _averageFillPrice; }
        }

        /// <summary>
        /// Gets the total qantity filled for this ticket. If no fills have been processed
        /// then this will return a value of zero.
        /// </summary>
        public decimal QuantityFilled
        {
            get { return _quantityFilled; }
        }

        /// <summary>
        /// Gets the time this order was last updated
        /// </summary>
        public DateTime Time
        {
            get { return GetMostRecentOrderRequest().Time; }
        }

        /// <summary>
        /// Gets the type of order
        /// </summary>
        public OrderType OrderType
        {
            get { return _submitRequest.OrderType; }
        }

        /// <summary>
        /// Gets the order's current tag
        /// </summary>
        public string Tag 
        {
            get { return _order == null ? _submitRequest.Tag : _order.Tag; }
        }

        /// <summary>
        /// Gets the <see cref="SubmitOrderRequest"/> that initiated this order
        /// </summary>
        public SubmitOrderRequest SubmitRequest
        {
            get { return _submitRequest; }
        }

        /// <summary>
        /// Gets a list of <see cref="UpdateOrderRequest"/> containing an item for each
        /// <see cref="UpdateOrderRequest"/> that was sent for this order id
        /// </summary>
        public IReadOnlyList<UpdateOrderRequest> UpdateRequests
        {
            get
            {
                lock (_updateRequestsLock)
                {
                    return _updateRequests.ToList();
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="CancelOrderRequest"/> if this order was canceled. If this order
        /// was not canceled, this will return null
        /// </summary>
        public CancelOrderRequest CancelRequest
        {
            get { return _cancelRequest; }
        }

        /// <summary>
        /// Gets a list of all order events for this ticket
        /// </summary>
        public IReadOnlyList<OrderEvent> OrderEvents
        {
            get
            {
                lock (_orderEventsLock)
                {
                    return _orderEvents.ToList();
                }
            }
        }

        /// <summary>
        /// Gets a wait handle that can be used to wait until this order has filled
        /// </summary>
        public WaitHandle OrderClosed
        {
            get { return _orderStatusClosedEvent; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderTicket"/> class
        /// </summary>
        /// <param name="transactionManager">The transaction manager used for submitting updates and cancels for this ticket</param>
        /// <param name="submitRequest">The order request that initiated this order ticket</param>
        public OrderTicket(SecurityTransactionManager transactionManager, SubmitOrderRequest submitRequest)
        {
            _submitRequest = submitRequest;
            _orderId = submitRequest.OrderId;
            _transactionManager = transactionManager;

            _orderEvents = new List<OrderEvent>();
            _updateRequests = new List<UpdateOrderRequest>();
            _orderStatusClosedEvent = new ManualResetEvent(false);
        }

        /// <summary>
        /// Gets the specified field from the ticket
        /// </summary>
        /// <param name="field">The order field to get</param>
        /// <returns>The value of the field</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public decimal Get(OrderField field)
        {
            switch (field)
            {
                case OrderField.LimitPrice:
                    if (_submitRequest.OrderType == OrderType.Limit)
                    {
                        return AccessOrder<LimitOrder>(this, field, o => o.LimitPrice, r => r.LimitPrice);
                    }
                    if (_submitRequest.OrderType == OrderType.StopLimit)
                    {
                        return AccessOrder<StopLimitOrder>(this, field, o => o.LimitPrice, r => r.LimitPrice);
                    }
                    break;

                case OrderField.StopPrice:
                    if (_submitRequest.OrderType == OrderType.StopLimit)
                    {
                        return AccessOrder<StopLimitOrder>(this, field, o => o.StopPrice, r => r.StopPrice);
                    }
                    if (_submitRequest.OrderType == OrderType.StopMarket)
                    {
                        return AccessOrder<StopMarketOrder>(this, field, o => o.StopPrice, r => r.StopPrice);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException("field", field, null);
            }
            throw new ArgumentException("Unable to get field " + field + " on order of type " + _submitRequest.OrderType);
        }

        /// <summary>
        /// Submits an <see cref="UpdateOrderRequest"/> with the <see cref="SecurityTransactionManager"/> to update
        /// the ticket with data specified in <paramref name="fields"/>
        /// </summary>
        /// <param name="fields">Defines what properties of the order should be updated</param>
        /// <returns>The <see cref="OrderResponse"/> from updating the order</returns>
        public OrderResponse Update(UpdateOrderFields fields)
        {
            _transactionManager.UpdateOrder(new UpdateOrderRequest(_transactionManager.UtcTime, SubmitRequest.OrderId, fields));
            return _updateRequests.Last().Response;
        }

        /// <summary>
        /// Submits a new request to cancel this order
        /// </summary>
        public OrderResponse Cancel(string tag = null)
        {
            var request = new CancelOrderRequest(_transactionManager.UtcTime, OrderId, tag);
            _transactionManager.ProcessRequest(request);
            return CancelRequest.Response;
        }

        /// <summary>
        /// Gets the most recent <see cref="OrderResponse"/> for this ticket
        /// </summary>
        /// <returns>The most recent <see cref="OrderResponse"/> for this ticket</returns>
        public OrderResponse GetMostRecentOrderResponse()
        {
            return GetMostRecentOrderRequest().Response;
        }

        /// <summary>
        /// Gets the most recent <see cref="OrderRequest"/> for this ticket
        /// </summary>
        /// <returns>The most recent <see cref="OrderRequest"/> for this ticket</returns>
        public OrderRequest GetMostRecentOrderRequest()
        {
            if (CancelRequest != null)
            {
                return CancelRequest;
            }
            var lastUpdate = _updateRequests.LastOrDefault();
            if (lastUpdate != null)
            {
                return lastUpdate;
            }
            return SubmitRequest;
        }

        /// <summary>
        /// Adds an order event to this ticket
        /// </summary>
        /// <param name="orderEvent">The order event to be added</param>
        internal void AddOrderEvent(OrderEvent orderEvent)
        {
            lock (_orderEventsLock)
            {
                _orderEvents.Add(orderEvent);
                if (orderEvent.FillQuantity != 0)
                {
                    // keep running totals of quantity filled and the average fill price so we
                    // don't need to compute these on demand
                    _quantityFilled += orderEvent.FillQuantity;
                    var quantityWeightedFillPrice = _orderEvents.Where(x => x.Status.IsFill()).Sum(x => x.AbsoluteFillQuantity*x.FillPrice);
                    _averageFillPrice = quantityWeightedFillPrice/Math.Abs(_quantityFilled);
                }
            }

            // fire the wait handle indicating this order is closed
            if (orderEvent.Status.IsClosed())
            {
                _orderStatusClosedEvent.Set();
            }
        }

        /// <summary>
        /// Updates the internal order object with the current state
        /// </summary>
        /// <param name="order">The order</param>
        internal void SetOrder(Order order)
        {
            if (_order != null && _order.Id != order.Id)
            {
                throw new ArgumentException("Order id mismatch");
            }

            _order = order;
        }

        /// <summary>
        /// Adds a new <see cref="UpdateOrderRequest"/> to this ticket.
        /// </summary>
        /// <param name="request">The recently processed <see cref="UpdateOrderRequest"/></param>
        internal void AddUpdateRequest(UpdateOrderRequest request)
        {
            if (request.OrderId != OrderId)
            {
                throw new ArgumentException("Received UpdateOrderRequest for incorrect order id.");
            }

            lock (_updateRequestsLock)
            {
                _updateRequests.Add(request);
            }
        }

        /// <summary>
        /// Sets the <see cref="CancelOrderRequest"/> for this ticket. This can only be performed once.
        /// </summary>
        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        /// <param name="request">The <see cref="CancelOrderRequest"/> that canceled this ticket.</param>
        /// <returns>False if the the CancelRequest has already been set, true if this call set it</returns>
        internal bool TrySetCancelRequest(CancelOrderRequest request)
        {
            if (request.OrderId != OrderId)
            {
                throw new ArgumentException("Received CancelOrderRequest for incorrect order id.");
            }
            lock (_setCancelRequestLock)
            {
                if (_cancelRequest != null)
                {
                    return false;
                }
                _cancelRequest = request;
            }
            return true;
        }

        /// <summary>
        /// Creates a new <see cref="OrderTicket"/> that represents trying to cancel an order for which no ticket exists
        /// </summary>
        public static OrderTicket InvalidCancelOrderId(SecurityTransactionManager transactionManager, CancelOrderRequest request)
        {
            var submit = new SubmitOrderRequest(OrderType.Market, SecurityType.Base, Symbol.Empty, 0, 0, 0, DateTime.MaxValue, string.Empty);
            submit.SetResponse(OrderResponse.UnableToFindOrder(request));
            var ticket = new OrderTicket(transactionManager, submit);
            request.SetResponse(OrderResponse.UnableToFindOrder(request));
            ticket.TrySetCancelRequest(request);
            ticket._orderStatusOverride = OrderStatus.Invalid;
            return ticket;
        }

        /// <summary>
        /// Creates a new <see cref="OrderTicket"/> tht represents trying to update an order for which no ticket exists
        /// </summary>
        public static OrderTicket InvalidUpdateOrderId(SecurityTransactionManager transactionManager, UpdateOrderRequest request)
        {
            var submit = new SubmitOrderRequest(OrderType.Market, SecurityType.Base, Symbol.Empty, 0, 0, 0, DateTime.MaxValue, string.Empty);
            submit.SetResponse(OrderResponse.UnableToFindOrder(request));
            var ticket = new OrderTicket(transactionManager, submit);
            request.SetResponse(OrderResponse.UnableToFindOrder(request));
            ticket.AddUpdateRequest(request);
            ticket._orderStatusOverride = OrderStatus.Invalid;
            return ticket;
        }

        /// <summary>
        /// Creates a new <see cref="OrderTicket"/> that represents trying to submit a new order that had errors embodied in the <paramref name="response"/>
        /// </summary>
        public static OrderTicket InvalidSubmitRequest(SecurityTransactionManager transactionManager, SubmitOrderRequest request, OrderResponse response)
        {
            request.SetResponse(response);
            return new OrderTicket(transactionManager, request) { _orderStatusOverride = OrderStatus.Invalid };
        }

        /// <summary>
        /// Creates a new <see cref="OrderTicket"/> that is invalidated because the algorithm was in the middle of warm up still
        /// </summary>
        public static OrderTicket InvalidWarmingUp(SecurityTransactionManager transactionManager, SubmitOrderRequest submit)
        {
            submit.SetResponse(OrderResponse.WarmingUp(submit));
            var ticket = new OrderTicket(transactionManager, submit);
            ticket._orderStatusOverride = OrderStatus.Invalid;
            return ticket;
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
            var counts = "Request Count: " + RequestCount() + " Response Count: " + ResponseCount();
            if (_order != null)
            {
                return OrderId + ": " + _order + " " + counts;
            }
            return OrderId + ": " + counts;
        }

        private int ResponseCount()
        {
            return (_submitRequest.Response == OrderResponse.Unprocessed ? 0 : 1) 
                 + (_cancelRequest == null || _cancelRequest.Response == OrderResponse.Unprocessed ? 0 : 1)
                 + _updateRequests.Count(x => x.Response != OrderResponse.Unprocessed);
        }

        private int RequestCount()
        {
            return 1 + _updateRequests.Count + (_cancelRequest == null ? 0 : 1);
        }

        /// <summary>
        /// This is provided for API backward compatibility and will resolve to the order ID, except during
        /// an error, where it will return the integer value of the <see cref="OrderResponseErrorCode"/> from
        /// the most recent response
        /// </summary>
        public static implicit operator int(OrderTicket ticket)
        {
            var response = ticket.GetMostRecentOrderResponse();
            if (response != null && response.IsError)
            {
                return (int) response.ErrorCode;
            }
            return ticket.OrderId;
        }


        private static decimal AccessOrder<T>(OrderTicket ticket, OrderField field, Func<T, decimal> orderSelector, Func<SubmitOrderRequest, decimal> requestSelector)
            where T : Order
        {
            var order = ticket._order;
            if (order == null)
            {
                return requestSelector(ticket._submitRequest);
            }
            var typedOrder = order as T;
            if (typedOrder != null)
            {
                return orderSelector(typedOrder);
            }
            throw new ArgumentException(string.Format("Unable to access property {0} on order of type {1}", field, order.Type));
        }
    }
}
