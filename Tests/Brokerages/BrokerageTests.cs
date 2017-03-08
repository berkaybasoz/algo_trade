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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    public abstract class BrokerageTests
    {
        // ideally this class would be abstract, but I wanted to keep the order test cases here which use the
        // various parameters required from derived types

        private IBrokerage _brokerage;
        private OrderProvider _orderProvider;
        private SecurityProvider _securityProvider;

        /// <summary>
        /// Provides the data required to test each order type in various cases
        /// </summary>
        public virtual TestCaseData[] OrderParameters
        {
            get
            {
                return new []
                {
                    new TestCaseData(new MarketOrderTestParameters(Symbol)).SetName("MarketOrder"),
                    new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("LimitOrder"),
                    new TestCaseData(new StopMarketOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("StopMarketOrder"),
                    new TestCaseData(new StopLimitOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("StopLimitOrder")
                };
            }
        }

        #region Test initialization and cleanup

        [SetUp]
        public void Setup()
        {
            Log.Trace("");
            Log.Trace("");
            Log.Trace("--- SETUP ---");
            Log.Trace("");
            Log.Trace("");
            // we want to regenerate these for each test
            _brokerage = null;
            _orderProvider = null;
            _securityProvider = null;
            Thread.Sleep(1000);
            CancelOpenOrders();
            LiquidateHoldings();
            Thread.Sleep(1000);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                Log.Trace("");
                Log.Trace("");
                Log.Trace("--- TEARDOWN ---");
                Log.Trace("");
                Log.Trace("");
                Thread.Sleep(1000);
                CancelOpenOrders();
                LiquidateHoldings();
                Thread.Sleep(1000);
            }
            finally
            {
                if (_brokerage != null)
                {
                    DisposeBrokerage(_brokerage);
                }
            }
        }

        public IBrokerage Brokerage
        {
            get
            {
                if (_brokerage == null)
                {
                    _brokerage = InitializeBrokerage();
                }
                return _brokerage;
            }
        }

        private IBrokerage InitializeBrokerage()
        {
            Log.Trace("");
            Log.Trace("- INITIALIZING BROKERAGE -");
            Log.Trace("");

            var brokerage = CreateBrokerage(OrderProvider, SecurityProvider);
            brokerage.Connect();

            if (!brokerage.IsConnected)
            {
                Assert.Fail("Failed to connect to brokerage");
            }

            Log.Trace("");
            Log.Trace("GET OPEN ORDERS");
            Log.Trace("");
            foreach (var openOrder in brokerage.GetOpenOrders())
            {
                OrderProvider.Add(openOrder);
            }

            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS");
            Log.Trace("");
            foreach (var accountHolding in brokerage.GetAccountHoldings())
            {
                // these securities don't need to be real, just used for the ISecurityProvider impl, required
                // by brokerages to track holdings
                SecurityProvider[accountHolding.Symbol] = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new SubscriptionDataConfig(typeof (TradeBar), accountHolding.Symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, false),
                    new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            }
            brokerage.OrderStatusChanged += (sender, args) =>
            {
                Log.Trace("");
                Log.Trace("ORDER STATUS CHANGED: " + args);
                Log.Trace("");

                // we need to keep this maintained properly
                if (args.Status == OrderStatus.Filled || args.Status == OrderStatus.PartiallyFilled)
                {
                    Log.Trace("FILL EVENT: " + args.FillQuantity + " units of " + args.Symbol.ToString());

                    Security security;
                    if (_securityProvider.TryGetValue(args.Symbol, out security))
                    {
                        var holding = _securityProvider[args.Symbol].Holdings;
                        holding.SetHoldings(args.FillPrice, holding.Quantity + args.FillQuantity);
                    }
                    else
                    {
                        var accountHoldings = brokerage.GetAccountHoldings().ToDictionary(x => x.Symbol);
                        if (accountHoldings.ContainsKey(args.Symbol))
                        {
                            _securityProvider[args.Symbol].Holdings.SetHoldings(args.FillPrice, args.FillQuantity);
                        }
                        else
                        {
                            _securityProvider[args.Symbol] = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                                new SubscriptionDataConfig(typeof (TradeBar), args.Symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, false),
                                new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
                            _securityProvider[args.Symbol].Holdings.SetHoldings(args.FillPrice, args.FillQuantity);
                        }
                    }

                    Log.Trace("--HOLDINGS: " + _securityProvider[args.Symbol]);

                    // update order mapping
                    var order = _orderProvider.GetOrderById(args.OrderId);
                    order.Status = args.Status;
                }
            };
            return brokerage;
        }

        public OrderProvider OrderProvider 
        {
            get { return _orderProvider ?? (_orderProvider = new OrderProvider()); }
        }

        public SecurityProvider SecurityProvider
        {
            get { return _securityProvider ?? (_securityProvider = new SecurityProvider()); }
        }

        /// <summary>
        /// Creates the brokerage under test and connects it
        /// </summary>
        /// <returns>A connected brokerage instance</returns>
        protected abstract IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider);

        /// <summary>
        /// Disposes of the brokerage and any external resources started in order to create it
        /// </summary>
        /// <param name="brokerage">The brokerage instance to be disposed of</param>
        protected virtual void DisposeBrokerage(IBrokerage brokerage)
        {
        }

        /// <summary>
        /// This is used to ensure each test starts with a clean, known state.
        /// </summary>
        protected void LiquidateHoldings()
        {
            Log.Trace("");
            Log.Trace("LIQUIDATE HOLDINGS");
            Log.Trace("");

            var holdings = Brokerage.GetAccountHoldings();

            foreach (var holding in holdings)
            {
                if (holding.Quantity == 0) continue;
                Log.Trace("Liquidating: " + holding);
                var order = new MarketOrder(holding.Symbol, (int)-holding.Quantity, DateTime.Now);
                _orderProvider.Add(order);
                PlaceOrderWaitForStatus(order, OrderStatus.Filled);
            }
        }

        protected void CancelOpenOrders()
        {
            Log.Trace("");
            Log.Trace("CANCEL OPEN ORDERS");
            Log.Trace("");
            var openOrders = Brokerage.GetOpenOrders();
            foreach (var openOrder in openOrders)
            {
                Log.Trace("Canceling: " + openOrder);
                Brokerage.CancelOrder(openOrder);
            }
        }

        #endregion

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected abstract Symbol Symbol { get; }

        /// <summary>
        /// Gets the security type associated with the <see cref="Symbol"/>
        /// </summary>
        protected abstract SecurityType SecurityType { get; }

        /// <summary>
        /// Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected abstract decimal HighPrice { get; }

        /// <summary>
        /// Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected abstract decimal LowPrice { get; }

        /// <summary>
        /// Gets the current market price of the specified security
        /// </summary>
        protected abstract decimal GetAskPrice(Symbol symbol);

        /// <summary>
        /// Gets the default order quantity
        /// </summary>
        protected virtual int GetDefaultQuantity()
        {
            return 1; 
        }

        [Test]
        public void IsConnected()
        {
            Assert.IsTrue(Brokerage.IsConnected);
        }

        [Test, TestCaseSource("OrderParameters")]
        public void LongFromZero(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("LONG FROM ZERO");
            Log.Trace("");
            PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        [Test, TestCaseSource("OrderParameters")]
        public void CloseFromLong(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("CLOSE FROM LONG");
            Log.Trace("");
            // first go long
            PlaceOrderWaitForStatus(parameters.CreateLongMarketOrder(GetDefaultQuantity()), OrderStatus.Filled);

            // now close it
            PlaceOrderWaitForStatus(parameters.CreateShortOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        [Test, TestCaseSource("OrderParameters")]
        public void ShortFromZero(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("SHORT FROM ZERO");
            Log.Trace("");
            PlaceOrderWaitForStatus(parameters.CreateShortOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        [Test, TestCaseSource("OrderParameters")]
        public void CloseFromShort(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("CLOSE FROM SHORT");
            Log.Trace("");
            // first go short
            PlaceOrderWaitForStatus(parameters.CreateShortMarketOrder(GetDefaultQuantity()), OrderStatus.Filled);

            // now close it
            PlaceOrderWaitForStatus(parameters.CreateLongOrder(GetDefaultQuantity()), parameters.ExpectedStatus);
        }

        [Test, TestCaseSource("OrderParameters")]
        public void ShortFromLong(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("SHORT FROM LONG");
            Log.Trace("");
            // first go long
            PlaceOrderWaitForStatus(parameters.CreateLongMarketOrder(GetDefaultQuantity()));

            // now go net short
            var order = PlaceOrderWaitForStatus(parameters.CreateShortOrder(2 * GetDefaultQuantity()), parameters.ExpectedStatus);

            if (parameters.ModifyUntilFilled)
            {
                ModifyOrderUntilFilled(order, parameters);
            }
        }

        [Test, TestCaseSource("OrderParameters")]
        public void LongFromShort(OrderTestParameters parameters)
        {
            Log.Trace("");
            Log.Trace("LONG FROM SHORT");
            Log.Trace("");
            // first fo short
            PlaceOrderWaitForStatus(parameters.CreateShortMarketOrder(-GetDefaultQuantity()), OrderStatus.Filled);

            // now go long
            var order = PlaceOrderWaitForStatus(parameters.CreateLongOrder(2 * GetDefaultQuantity()), parameters.ExpectedStatus);

            if (parameters.ModifyUntilFilled)
            {
                ModifyOrderUntilFilled(order, parameters);
            }
        }

        [Test]
        public void GetCashBalanceContainsUSD()
        {
            Log.Trace("");
            Log.Trace("GET CASH BALANCE");
            Log.Trace("");
            var balance = Brokerage.GetCashBalance();
            Assert.AreEqual(1, balance.Count(x => x.Symbol == "USD"));
        }

        [Test]
        public void GetAccountHoldings()
        {
            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS");
            Log.Trace("");
            var before = Brokerage.GetAccountHoldings();

            PlaceOrderWaitForStatus(new MarketOrder(Symbol, GetDefaultQuantity(), DateTime.Now));

            var after = Brokerage.GetAccountHoldings();

            var beforeHoldings = before.FirstOrDefault(x => x.Symbol == Symbol);
            var afterHoldings = after.FirstOrDefault(x => x.Symbol == Symbol);

            var beforeQuantity = beforeHoldings == null ? 0 : beforeHoldings.Quantity;
            var afterQuantity = afterHoldings == null ? 0 : afterHoldings.Quantity;

            Assert.AreEqual(GetDefaultQuantity(), afterQuantity - beforeQuantity);
        }

        [Test, Ignore("This test requires reading the output and selection of a low volume security for the Brokerageage")]
        public void PartialFills()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);

            var qty = 1000000;
            var remaining = qty;
            var sync = new object();
            Brokerage.OrderStatusChanged += (sender, orderEvent) =>
            {
                lock (sync)
                {
                    remaining -= orderEvent.FillQuantity;
                    Console.WriteLine("Remaining: " + remaining + " FillQuantity: " + orderEvent.FillQuantity);
                    if (orderEvent.Status == OrderStatus.Filled)
                    {
                        orderFilled = true;
                        manualResetEvent.Set();
                    }
                }
            };

            // pick a security with low, but some, volume
            var symbol = Symbols.EURUSD;
            var order = new MarketOrder(symbol, qty, DateTime.UtcNow) { Id = 1 };
            Brokerage.PlaceOrder(order);

            // pause for a while to wait for fills to come in
            manualResetEvent.WaitOne(2500);
            manualResetEvent.WaitOne(2500);
            manualResetEvent.WaitOne(2500);

            Console.WriteLine("Remaining: " + remaining);
            Assert.AreEqual(0, remaining);
        }

        /// <summary>
        /// Updates the specified order in the brokerage until it fills or reaches a timeout
        /// </summary>
        /// <param name="order">The order to be modified</param>
        /// <param name="parameters">The order test parameters that define how to modify the order</param>
        /// <param name="secondsTimeout">Maximum amount of time to wait until the order fills</param>
        protected void ModifyOrderUntilFilled(Order order, OrderTestParameters parameters, double secondsTimeout = 90)
        {
            if (order.Status == OrderStatus.Filled)
            {
                return;
            }

            var filledResetEvent = new ManualResetEvent(false);
            EventHandler<OrderEvent> brokerageOnOrderStatusChanged = (sender, args) =>
            {
                if (args.Status == OrderStatus.Filled)
                {
                    filledResetEvent.Set();
                }
                if (args.Status == OrderStatus.Canceled || args.Status == OrderStatus.Invalid)
                {
                    Log.Trace("ModifyOrderUntilFilled(): " + order);
                    Assert.Fail("Unexpected order status: " + args.Status);
                }
            };

            Brokerage.OrderStatusChanged += brokerageOnOrderStatusChanged;

            Log.Trace("");
            Log.Trace("MODIFY UNTIL FILLED: " + order);
            Log.Trace("");
            var stopwatch = Stopwatch.StartNew();
            while (!filledResetEvent.WaitOne(3000) && stopwatch.Elapsed.TotalSeconds < secondsTimeout)
            {
                filledResetEvent.Reset();
                if (order.Status == OrderStatus.PartiallyFilled) continue;

                var marketPrice = GetAskPrice(order.Symbol);
                Log.Trace("BrokerageTests.ModifyOrderUntilFilled(): Ask: " + marketPrice);

                var updateOrder = parameters.ModifyOrderToFill(Brokerage, order, marketPrice);
                if (updateOrder)
                {
                    if (order.Status == OrderStatus.Filled) break;

                    Log.Trace("BrokerageTests.ModifyOrderUntilFilled(): " + order);
                    if (!Brokerage.UpdateOrder(order))
                    {
                        Assert.Fail("Brokerage failed to update the order");
                    }
                }
            }

            Brokerage.OrderStatusChanged -= brokerageOnOrderStatusChanged;
        }

        /// <summary>
        /// Places the specified order with the brokerage and wait until we get the <paramref name="expectedStatus"/> back via an OrderStatusChanged event.
        /// This function handles adding the order to the <see cref="IOrderProvider"/> instance as well as incrementing the order ID.
        /// </summary>
        /// <param name="order">The order to be submitted</param>
        /// <param name="expectedStatus">The status to wait for</param>
        /// <param name="secondsTimeout">Maximum amount of time to wait for <paramref name="expectedStatus"/></param>
        /// <returns>The same order that was submitted.</returns>
        protected Order PlaceOrderWaitForStatus(Order order, OrderStatus expectedStatus = OrderStatus.Filled, double secondsTimeout = 10.0, bool allowFailedSubmission = false)
        {
            var requiredStatusEvent = new ManualResetEvent(false);
            var desiredStatusEvent = new ManualResetEvent(false);
            EventHandler<OrderEvent> brokerageOnOrderStatusChanged = (sender, args) =>
            {
                // no matter what, every order should fire at least one of these
                if (args.Status == OrderStatus.Submitted || args.Status == OrderStatus.Invalid)
                {
                    Log.Trace("");
                    Log.Trace("SUBMITTED: " + args);
                    Log.Trace("");
                    requiredStatusEvent.Set();
                }
                // make sure we fire the status we're expecting
                if (args.Status == expectedStatus)
                {
                    Log.Trace("");
                    Log.Trace("EXPECTED: " + args);
                    Log.Trace("");
                    desiredStatusEvent.Set();
                }
            };

            Brokerage.OrderStatusChanged += brokerageOnOrderStatusChanged;
            
            OrderProvider.Add(order);
            if (!Brokerage.PlaceOrder(order) && !allowFailedSubmission)
            {
                Assert.Fail("Brokerage failed to place the order: " + order);
            }
            requiredStatusEvent.WaitOneAssertFail((int) (1000*secondsTimeout), "Expected every order to fire a submitted or invalid status event");
            desiredStatusEvent.WaitOneAssertFail((int) (1000*secondsTimeout), "OrderStatus " + expectedStatus + " was not encountered within the timeout.");

            Brokerage.OrderStatusChanged -= brokerageOnOrderStatusChanged;

            return order;
        }
    }
}