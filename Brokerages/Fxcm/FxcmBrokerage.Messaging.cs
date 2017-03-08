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
using com.fxcm.external.api.transport;
using com.fxcm.fix;
using com.fxcm.fix.other;
using com.fxcm.fix.posttrade;
using com.fxcm.fix.pretrade;
using com.fxcm.fix.trade;
using com.fxcm.messaging;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Fxcm
{
    /// <summary>
    /// FXCM brokerage - Java API related functions and interface implementations
    /// </summary>
    public partial class FxcmBrokerage
    {
        private IGateway _gateway;

        private readonly object _locker = new object();
        private string _currentRequest;
        private const int ResponseTimeout = 2500;
        private bool _isOrderUpdateOrCancelRejected;
        private bool _isOrderSubmitRejected;

        private readonly Dictionary<string, TradingSecurity> _fxcmInstruments = new Dictionary<string, TradingSecurity>();
        private readonly Dictionary<string, CollateralReport> _accounts = new Dictionary<string, CollateralReport>();
        private readonly Dictionary<string, MarketDataSnapshot> _rates = new Dictionary<string, MarketDataSnapshot>();

        private readonly Dictionary<string, ExecutionReport> _openOrders = new Dictionary<string, ExecutionReport>();
        private readonly Dictionary<string, PositionReport> _openPositions = new Dictionary<string, PositionReport>();

        private readonly Dictionary<string, Order> _mapRequestsToOrders = new Dictionary<string, Order>();
        private readonly Dictionary<string, Order> _mapFxcmOrderIdsToOrders = new Dictionary<string, Order>();
        private readonly Dictionary<string, AutoResetEvent> _mapRequestsToAutoResetEvents = new Dictionary<string, AutoResetEvent>();

        private string _fxcmAccountCurrency = "USD";

        private void LoadInstruments()
        {
            // Note: requestTradingSessionStatus() MUST be called just after login

            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.requestTradingSessionStatus();
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException(string.Format("FxcmBrokerage.LoadInstruments(): Operation took longer than {0} seconds.", (decimal)ResponseTimeout / 1000));
        }

        private void LoadAccounts()
        {
            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.requestAccounts();
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException(string.Format("FxcmBrokerage.LoadAccounts(): Operation took longer than {0} seconds.", (decimal)ResponseTimeout / 1000));

            if (!_accounts.ContainsKey(_accountId))
                throw new ArgumentException("FxcmBrokerage.LoadAccounts(): The account id is invalid: " + _accountId);

            // Hedging MUST be disabled on the account
            if (_accounts[_accountId].getParties().getFXCMPositionMaintenance() == "Y")
            {
                throw new NotSupportedException("FxcmBrokerage.LoadAccounts(): The Lean engine does not support accounts with Hedging enabled. Please contact FXCM support to disable Hedging.");
            }
        }

        private void LoadOpenOrders()
        {
            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.requestOpenOrders(Convert.ToInt64(_accountId));
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException(string.Format("FxcmBrokerage.LoadOpenOrders(): Operation took longer than {0} seconds.", (decimal)ResponseTimeout / 1000));
        }

        private void LoadOpenPositions()
        {
            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.requestOpenPositions(Convert.ToInt64(_accountId));
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException(string.Format("FxcmBrokerage.LoadOpenPositions(): Operation took longer than {0} seconds.", (decimal)ResponseTimeout / 1000));
        }

        /// <summary>
        /// Provides as public access to this data without requiring consumers to reference
        /// IKVM libraries
        /// </summary>
        public List<Tick> GetBidAndAsk(List<string> fxcmSymbols)
        {
            return GetQuotes(fxcmSymbols).Select(x => new Tick
            {
                Symbol = _symbolMapper.GetLeanSymbol(
                    x.getInstrument().getSymbol(),
                    _symbolMapper.GetBrokerageSecurityType(x.getInstrument().getSymbol()), 
                    Market.FXCM),
                BidPrice = (decimal) x.getBidClose(),
                AskPrice = (decimal) x.getAskClose()
            }).ToList();
        }

        /// <summary>
        /// Gets the quotes for the symbol
        /// </summary>
        private List<MarketDataSnapshot> GetQuotes(List<string> fxcmSymbols)
        {
            // get current quotes for the instrument
            var request = new MarketDataRequest();
            request.setMDEntryTypeSet(MarketDataRequest.MDENTRYTYPESET_ALL);
            request.setSubscriptionRequestType(SubscriptionRequestTypeFactory.SNAPSHOT);
            foreach (var fxcmSymbol in fxcmSymbols)
            {
                request.addRelatedSymbol(_fxcmInstruments[fxcmSymbol]);
            }

            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.sendMessage(request);
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException(string.Format("FxcmBrokerage.GetQuotes(): Operation took longer than {0} seconds.", (decimal)ResponseTimeout / 1000));

            return _rates.Where(x => fxcmSymbols.Contains(x.Key)).Select(x => x.Value).ToList();
        }

        /// <summary>
        /// Gets the current conversion rate into USD
        /// </summary>
        /// <remarks>Synchronous, blocking</remarks>
        private decimal GetUsdConversion(string currency)
        {
            if (currency == "USD")
                return 1m;

            // determine the correct symbol to choose
            var normalSymbol = currency + "/USD";
            var invertedSymbol = "USD/" + currency;
            var isInverted = _fxcmInstruments.ContainsKey(invertedSymbol);
            var fxcmSymbol = isInverted ? invertedSymbol : normalSymbol;

            // get current quotes for the instrument
            var quotes = GetQuotes(new List<string> { fxcmSymbol });

            var rate = (decimal)(quotes[0].getBidClose() + quotes[0].getAskClose()) / 2;

            return isInverted ? 1 / rate : rate;
        }

        #region IGenericMessageListener implementation

        /// <summary>
        /// Receives generic messages from the FXCM API
        /// </summary>
        /// <param name="message">Generic message received</param>
        public void messageArrived(ITransportable message)
        {
            // Dispatch message to specific handler

            lock (_locker)
            {
                if (message is TradingSessionStatus)
                    OnTradingSessionStatus((TradingSessionStatus)message);

                else if (message is CollateralReport)
                    OnCollateralReport((CollateralReport)message);

                else if (message is MarketDataSnapshot)
                    OnMarketDataSnapshot((MarketDataSnapshot)message);

                else if (message is ExecutionReport)
                    OnExecutionReport((ExecutionReport)message);

                else if (message is RequestForPositionsAck)
                    OnRequestForPositionsAck((RequestForPositionsAck)message);

                else if (message is PositionReport)
                    OnPositionReport((PositionReport)message);

                else if (message is OrderCancelReject)
                    OnOrderCancelReject((OrderCancelReject)message);

                else if (message is UserResponse || message is CollateralInquiryAck ||
                    message is MarketDataRequestReject || message is BusinessMessageReject || message is SecurityStatus)
                {
                    // Unused messages, no handler needed
                }

                else
                {
                    // Should never get here, if it does log and ignore message
                    // New messages added in future api updates should be added to the unused list above
                    Log.Trace("FxcmBrokerage.messageArrived(): Unknown message: {0}", message);
                }
            }
        }

        /// <summary>
        /// TradingSessionStatus message handler
        /// </summary>
        private void OnTradingSessionStatus(TradingSessionStatus message)
        {
            if (message.getRequestID() == _currentRequest)
            {
                // load instrument list into a dictionary
                var securities = message.getSecurities();
                while (securities.hasMoreElements())
                {
                    var security = (TradingSecurity)securities.nextElement();
                    _fxcmInstruments[security.getSymbol()] = security;
                }

                // get account base currency
                _fxcmAccountCurrency = message.getParameter("BASE_CRNCY").getValue();

                _mapRequestsToAutoResetEvents[_currentRequest].Set();
                _mapRequestsToAutoResetEvents.Remove(_currentRequest);
            }
        }

        /// <summary>
        /// CollateralReport message handler
        /// </summary>
        private void OnCollateralReport(CollateralReport message)
        {
            // add the trading account to the account list
            _accounts[message.getAccount()] = message;

            if (message.getRequestID() == _currentRequest)
            {
                // set the state of the request to be completed only if this is the last collateral report requested
                if (message.isLastRptRequested())
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// MarketDataSnapshot message handler
        /// </summary>
        private void OnMarketDataSnapshot(MarketDataSnapshot message)
        {
            // update the current prices for the instrument
            var instrument = message.getInstrument();
            _rates[instrument.getSymbol()] = message;

            // if instrument is subscribed, add ticks to list
            var securityType = _symbolMapper.GetBrokerageSecurityType(instrument.getSymbol());
            var symbol = _symbolMapper.GetLeanSymbol(instrument.getSymbol(), securityType, Market.FXCM);

            if (_subscribedSymbols.Contains(symbol))
            {
                var time = FromJavaDate(message.getDate().toDate());
                var bidPrice = Convert.ToDecimal(message.getBidClose());
                var askPrice = Convert.ToDecimal(message.getAskClose());
                var tick = new Tick(time, symbol, bidPrice, askPrice);

                lock (_ticks)
                {
                    _ticks.Add(tick);
                }
            }

            if (message.getRequestID() == _currentRequest)
            {
                if (message.getFXCMContinuousFlag() == IFixValueDefs.__Fields.FXCMCONTINUOUS_END)
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// ExecutionReport message handler
        /// </summary>
        private void OnExecutionReport(ExecutionReport message)
        {
            var orderId = message.getOrderID();
            var orderStatus = message.getFXCMOrdStatus();

            if (orderId != "NONE" && message.getAccount() == _accountId)
            {
                if (_openOrders.ContainsKey(orderId) && OrderIsClosed(orderStatus.getCode()))
                {
                    _openOrders.Remove(orderId);
                }
                else
                {
                    _openOrders[orderId] = message;
                }

                Order order;
                if (_mapFxcmOrderIdsToOrders.TryGetValue(orderId, out order))
                {
                    // existing order
                    if (!OrderIsBeingProcessed(orderStatus.getCode()))
                    {
                        var orderEvent = new OrderEvent(order, DateTime.UtcNow, 0)
                        {
                            Status = ConvertOrderStatus(orderStatus),
                            FillPrice = Convert.ToDecimal(message.getPrice()),
                            FillQuantity = Convert.ToInt32(message.getSide() == SideFactory.BUY ? message.getLastQty() : -message.getLastQty())
                        };

                        // we're catching the first fill so we apply the fees only once
                        if ((int)message.getCumQty() == (int)message.getLastQty() && message.getLastQty() > 0)
                        {
                            var security = _securityProvider.GetSecurity(order.Symbol);
                            orderEvent.OrderFee = security.FeeModel.GetOrderFee(security, order);
                        }

                        _orderEventQueue.Enqueue(orderEvent);
                    }
                }
                else if (_mapRequestsToOrders.TryGetValue(message.getRequestID(), out order))
                {
                    _mapFxcmOrderIdsToOrders[orderId] = order;
                    order.BrokerId.Add(orderId);

                    // new order
                    var orderEvent = new OrderEvent(order, DateTime.UtcNow, 0)
                    {
                        Status = ConvertOrderStatus(orderStatus)
                    };

                    _orderEventQueue.Enqueue(orderEvent);
                }
            }

            if (message.getRequestID() == _currentRequest)
            {
                if (message.isLastRptRequested())
                {
                    if (orderId == "NONE" && orderStatus.getCode() == IFixValueDefs.__Fields.FXCMORDSTATUS_REJECTED)
                    {
                        if (message.getSide() != SideFactory.UNDISCLOSED)
                        {
                            var messageText = message.getFXCMErrorDetails().Replace("\n", "");
                            Log.Trace("FxcmBrokerage.OnExecutionReport(): " + messageText);
                            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "OrderSubmitReject", messageText));
                        }

                        _isOrderSubmitRejected = true;
                    }

                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// RequestForPositionsAck message handler
        /// </summary>
        private void OnRequestForPositionsAck(RequestForPositionsAck message)
        {
            if (message.getRequestID() == _currentRequest)
            {
                if (message.getTotalNumPosReports() == 0)
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// PositionReport message handler
        /// </summary>
        private void OnPositionReport(PositionReport message)
        {
            if (message.getAccount() == _accountId)
            {
                if (_openPositions.ContainsKey(message.getCurrency()) && message is ClosedPositionReport)
                {
                    _openPositions.Remove(message.getCurrency());
                }
                else
                {
                    _openPositions[message.getCurrency()] = message;
                }
            }

            if (message.getRequestID() == _currentRequest)
            {
                if (message.isLastRptRequested())
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// OrderCancelReject message handler
        /// </summary>
        private void OnOrderCancelReject(OrderCancelReject message)
        {
            if (message.getRequestID() == _currentRequest)
            {
                var messageText = message.getFXCMErrorDetails().Replace("\n", "");
                Log.Trace("FxcmBrokerage.OnOrderCancelReject(): " + messageText);
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "OrderUpdateOrCancelReject", messageText));

                _isOrderUpdateOrCancelRejected = true;

                _mapRequestsToAutoResetEvents[_currentRequest].Set();
                _mapRequestsToAutoResetEvents.Remove(_currentRequest);
            }
        }

        #endregion

        #region IStatusMessageListener implementation

        /// <summary>
        /// Receives status messages from the FXCM API
        /// </summary>
        /// <param name="message">Status message received</param>
        public void messageArrived(ISessionStatus message)
        {
            switch (message.getStatusCode())
            {
                case ISessionStatus.__Fields.STATUSCODE_READY:
                    lock (_lockerConnectionMonitor)
                    {
                        _lastReadyMessageTime = DateTime.UtcNow;
                    }
                    _connectionError = false;
                    break;

                case ISessionStatus.__Fields.STATUSCODE_ERROR:
                    _connectionError = true;
                    break;
            }
        }

        #endregion

    }
}
