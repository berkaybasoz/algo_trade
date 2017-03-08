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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    [Ignore("These tests require the IBController and IB TraderWorkstation to be installed.")]
    public class InteractiveBrokersBrokerageTests
    {
        private readonly List<Order> _orders = new List<Order>(); 
        private InteractiveBrokersBrokerage _interactiveBrokersBrokerage;
        private const int buyQuantity = 100;
        private const SecurityType Type = SecurityType.Forex;

        [SetUp]
        public void InitializeBrokerage()
        {
            InteractiveBrokersGatewayRunner.Start(Config.Get("ib-controller-dir"), 
                Config.Get("ib-tws-dir"), 
                Config.Get("ib-user-name"), 
                Config.Get("ib-password"), 
                Config.GetBool("ib-use-tws")
                );

            // grabs account info from configuration
            _interactiveBrokersBrokerage = new InteractiveBrokersBrokerage(new OrderProvider(_orders), new SecurityProvider());
            _interactiveBrokersBrokerage.Connect();
        }

        [TearDown]
        public void Teardown()
        {
            try
            { // give the tear down a header so we can easily find it in the logs
                Log.Trace("-----");
                Log.Trace("InteractiveBrokersBrokerageTests.Teardown(): Starting teardown...");
                Log.Trace("-----");

                var canceledResetEvent = new ManualResetEvent(false);
                var filledResetEvent = new ManualResetEvent(false);
                _interactiveBrokersBrokerage.OrderStatusChanged += (sender, orderEvent) =>
                {
                    if (orderEvent.Status == OrderStatus.Filled)
                    {
                        filledResetEvent.Set();
                    }
                    if (orderEvent.Status == OrderStatus.Canceled)
                    {
                        canceledResetEvent.Set();
                    }
                };

                // cancel all open orders

                Log.Trace("InteractiveBrokersBrokerageTests.Teardown(): Canceling open orders...");

                var orders = _interactiveBrokersBrokerage.GetOpenOrders();
                foreach (var order in orders)
                {
                    _interactiveBrokersBrokerage.CancelOrder(order);
                    canceledResetEvent.WaitOne(3000);
                    canceledResetEvent.Reset();
                }

                Log.Trace("InteractiveBrokersBrokerageTests.Teardown(): Liquidating open positions...");

                // liquidate all positions
                var holdings = _interactiveBrokersBrokerage.GetAccountHoldings();
                foreach (var holding in holdings.Where(x => x.Quantity != 0))
                {
                    //var liquidate = new MarketOrder(holding.Symbol, (int) -holding.Quantity, DateTime.UtcNow, type: holding.Type);
                    //_interactiveBrokersBrokerage.PlaceOrder(liquidate);
                    //filledResetEvent.WaitOne(3000);
                    //filledResetEvent.Reset();
                }

                var openOrdersText = _interactiveBrokersBrokerage.GetOpenOrders().Select(x => x.Symbol.ToString() + " " + x.Quantity);
                Log.Trace("InteractiveBrokersBrokerageTests.Teardown(): Open orders: " + string.Join(", ", openOrdersText));
                //Assert.AreEqual(0, actualOpenOrderCount, "Failed to verify that there are zero open orders.");

                var holdingsText = _interactiveBrokersBrokerage.GetAccountHoldings().Where(x => x.Quantity != 0).Select(x => x.Symbol.ToString() + " " + x.Quantity);
                Log.Trace("InteractiveBrokersBrokerageTests.Teardown(): Account holdings: " + string.Join(", ", holdingsText));
                //Assert.AreEqual(0, holdingsCount, "Failed to verify that there are zero account holdings.");

                _interactiveBrokersBrokerage.Dispose();
                _interactiveBrokersBrokerage = null;
            }
            finally
            {
                InteractiveBrokersGatewayRunner.Stop();
            }
        }

        [Test]
        public void ClientConnects()
        {
            var ib = _interactiveBrokersBrokerage;
            Assert.IsTrue(ib.IsConnected);
        }

        [Test]
        public void PlacedOrderHasNewBrokerageOrderID()
        {
            var ib = _interactiveBrokersBrokerage;

            var order = new MarketOrder(Symbols.USDJPY, buyQuantity, DateTime.UtcNow);
            _orders.Add(order);
            ib.PlaceOrder(order);

            var brokerageID = order.BrokerId.Single();
            Assert.AreNotEqual(0, brokerageID);

            order = new MarketOrder(Symbols.USDJPY, buyQuantity, DateTime.UtcNow);
            _orders.Add(order);
            ib.PlaceOrder(order);

            Assert.AreNotEqual(brokerageID, order.BrokerId.Single());
        }

        [Test]
        public void ClientPlacesMarketOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = _interactiveBrokersBrokerage;

            ib.OrderStatusChanged += (sender, orderEvent) =>
            {
                if (orderEvent.Status == OrderStatus.Filled)
                {
                    orderFilled = true;
                    manualResetEvent.Set();
                }
            };

            var order = new MarketOrder(Symbols.USDJPY, buyQuantity, DateTime.UtcNow) {Id = 1};
            _orders.Add(order);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOne(2500);
            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.Market, orderFromIB.Type);
        }

        [Test]
        public void ClientSellsMarketOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);

            var ib = _interactiveBrokersBrokerage;

            ib.OrderStatusChanged += (sender, orderEvent) =>
            {
                if (orderEvent.Status == OrderStatus.Filled)
                {
                    orderFilled = true;
                    manualResetEvent.Set();
                }
            };

            // sell a single share
            var order = new MarketOrder(Symbols.USDJPY, -buyQuantity, DateTime.UtcNow);
            _orders.Add(order);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOne(2500);

            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.Market, orderFromIB.Type);
        }

        [Test]
        public void ClientPlacesLimitOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = _interactiveBrokersBrokerage;

            decimal price = 100m;
            decimal delta = 85.0m; // if we can't get a price then make the delta huge
            ib.OrderStatusChanged += (sender, orderEvent) =>
            {
                if (orderEvent.Status == OrderStatus.Filled)
                {
                    orderFilled = true;
                    manualResetEvent.Set();
                }
                price = orderEvent.FillPrice;
                delta = 0.02m;
            };

            // get the current market price, couldn't get RequestMarketData to fire tick events
            int id = 0;
            Order order = new MarketOrder(Symbols.USDJPY, buyQuantity, DateTime.UtcNow) { Id = ++id };
            _orders.Add(order);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOne(2000);
            manualResetEvent.Reset();

            // make a box around the current price +- a little

            order = new LimitOrder(Symbols.USDJPY, buyQuantity, price - delta, DateTime.UtcNow, null) { Id = ++id };
            _orders.Add(order);
            ib.PlaceOrder(order);

            order = new LimitOrder(Symbols.USDJPY, -buyQuantity, price + delta, DateTime.UtcNow, null) { Id = ++id };
            _orders.Add(order);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOne(1000);

            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.Limit, orderFromIB.Type);
        }

        [Test]
        public void ClientPlacesStopLimitOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = _interactiveBrokersBrokerage;

            decimal fillPrice = 100m;
            decimal delta = 85.0m; // if we can't get a price then make the delta huge
            ib.OrderStatusChanged += (sender, args) =>
            {
                orderFilled = true;
                fillPrice = args.FillPrice;
                delta = 0.02m;
                manualResetEvent.Set();
            };

            // get the current market price, couldn't get RequestMarketData to fire tick events
            int id = 0;
            Order order = new MarketOrder(Symbols.USDJPY, buyQuantity, DateTime.UtcNow) { Id = ++id };
            _orders.Add(order);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOne(2000);
            manualResetEvent.Reset();
            Assert.IsTrue(orderFilled);

            orderFilled = false;

            // make a box around the current price +- a little

            order = new StopMarketOrder(Symbols.USDJPY, buyQuantity, fillPrice - delta, DateTime.UtcNow) { Id = ++id };
            _orders.Add(order);
            ib.PlaceOrder(order);

            order = new StopMarketOrder(Symbols.USDJPY, -buyQuantity, fillPrice + delta, DateTime.UtcNow) { Id = ++id };
            _orders.Add(order);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOne(1000);

            var orderFromIB = AssertOrderOpened(orderFilled, ib, order);
            Assert.AreEqual(OrderType.StopMarket, orderFromIB.Type);
        }

        [Test]
        public void ClientUpdatesLimitOrder()
        {
            int id = 0;
            var ib = _interactiveBrokersBrokerage;

            bool filled = false;
            ib.OrderStatusChanged += (sender, args) =>
            {
                if (args.Status == OrderStatus.Filled)
                {
                    filled = true;
                }
            };

            const decimal limitPrice = 10000m;
            var order = new LimitOrder(Symbols.USDJPY, -buyQuantity, limitPrice, DateTime.UtcNow) {Id = ++id};
            _orders.Add(order);
            ib.PlaceOrder(order);

            var stopwatch = Stopwatch.StartNew();
            while (!filled && stopwatch.Elapsed.TotalSeconds < 10)
            {
                //Thread.MemoryBarrier();
                Thread.Sleep(1000);
                order.LimitPrice = order.LimitPrice/2;
                ib.UpdateOrder(order);
            }

            Assert.IsTrue(filled);
        }

        [Test]
        public void ClientCancelsLimitOrder()
        {
            var orderedResetEvent = new ManualResetEvent(false);
            var canceledResetEvent = new ManualResetEvent(false);

            var ib = _interactiveBrokersBrokerage;

            ib.OrderStatusChanged += (sender, orderEvent) =>
            {
                if (orderEvent.Status == OrderStatus.Submitted)
                {
                    orderedResetEvent.Set();
                }
                if (orderEvent.Status == OrderStatus.Canceled)
                {
                    canceledResetEvent.Set();
                }
            };

            // try to sell a single share at a ridiculous price, we'll cancel this later
            var order = new LimitOrder(Symbols.USDJPY, -buyQuantity, 100000, DateTime.UtcNow, null);
            _orders.Add(order);
            ib.PlaceOrder(order);
            orderedResetEvent.WaitOneAssertFail(2500, "Limit order failed to be submitted.");

            Thread.Sleep(500);

            ib.CancelOrder(order);

            canceledResetEvent.WaitOneAssertFail(2500, "Canceled event did not fire.");

            var openOrders = ib.GetOpenOrders();
            var cancelledOrder = openOrders.FirstOrDefault(x => x.BrokerId.Contains(order.BrokerId[0]));
            Assert.IsNull(cancelledOrder);
        }

        [Test]
        public void ClientFiresSingleOrderFilledEvent()
        {
            var ib = _interactiveBrokersBrokerage;

            var order = new MarketOrder(Symbols.USDJPY, buyQuantity, new DateTime()) {Id = 1};
            _orders.Add(order);

            int orderFilledEventCount = 0;
            var orderFilledResetEvent = new ManualResetEvent(false);
            ib.OrderStatusChanged += (sender, fill) =>
            {
                if (fill.Status == OrderStatus.Filled)
                {
                    orderFilledEventCount++;
                    orderFilledResetEvent.Set();
                }

                // mimic what the transaction handler would do
                order.Status = fill.Status;
            };

            ib.PlaceOrder(order);

            orderFilledResetEvent.WaitOneAssertFail(2500, "Didnt fire order filled event");

            // wait a little to see if we get multiple fill events
            Thread.Sleep(2000);

            Assert.AreEqual(1, orderFilledEventCount);
        }

        [Test]
        public void GetsAccountHoldings()
        {
            var ib = _interactiveBrokersBrokerage;

            Thread.Sleep(500);

            var previousHoldings = ib.GetAccountHoldings().ToDictionary(x => x.Symbol);

            foreach (var holding in previousHoldings)
            {
                Console.WriteLine(holding.Value);
            }

            Log.Trace("Quantity: " + previousHoldings[Symbols.USDJPY].Quantity);

            bool hasSymbol = previousHoldings.ContainsKey(Symbols.USDJPY);

            // wait for order to complete before request account holdings
            var orderResetEvent = new ManualResetEvent(false);
            ib.OrderStatusChanged += (sender, fill) =>
            {
                if (fill.Status == OrderStatus.Filled) orderResetEvent.Set();
            };

            // buy some currency
            const int quantity = -buyQuantity;
            var order = new MarketOrder(Symbols.USDJPY, quantity, DateTime.UtcNow);
            _orders.Add(order);
            ib.PlaceOrder(order);

            // wait for the order to go through
            orderResetEvent.WaitOneAssertFail(3000, "Didn't receive order event");

            // ib is slow to update tws
            Thread.Sleep(5000);

            var newHoldings = ib.GetAccountHoldings().ToDictionary(x => x.Symbol);
            Log.Trace("New Quantity: " + newHoldings[Symbols.USDJPY].Quantity);

            if (hasSymbol)
            {
                Assert.AreEqual(previousHoldings[Symbols.USDJPY].Quantity, newHoldings[Symbols.USDJPY].Quantity - quantity);
            }
            else
            {
                Assert.IsTrue(newHoldings.ContainsKey(Symbols.USDJPY));
                Assert.AreEqual(newHoldings[Symbols.USDJPY].Quantity, quantity);
            }
        }

        [Test]
        public void GetsCashBalanceAfterConnect()
        {
            var ib = _interactiveBrokersBrokerage;
            var cashBalance = ib.GetCashBalance();
            Assert.IsTrue(cashBalance.Any(x => x.Symbol == "USD"));
            foreach (var cash in cashBalance)
            {
                Console.WriteLine(cash);
                if (cash.Symbol == "USD")
                {
                    Assert.AreNotEqual(0m, cashBalance.Single(x => x.Symbol == "USD"));
                }
            }
        }

        [Test]
        public void FiresMultipleAccountBalanceEvents()
        {
            var ib = _interactiveBrokersBrokerage;

            var orderEventFired = new ManualResetEvent(false);
            ib.OrderStatusChanged += (sender, args) =>
            {
                if (args.Status == OrderStatus.Filled)
                {
                    orderEventFired.Set();
                }
            };

            var cashBalanceUpdates = new List<decimal>();
            var accountChangedFired = new ManualResetEvent(false);
            ib.AccountChanged += (sender, args) =>
            {
                cashBalanceUpdates.Add(args.CashBalance);
                accountChangedFired.Set();
            };

            const int orderCount = 3;
            for (int i = 0; i < orderCount; i++)
            {
                var order = new MarketOrder(Symbols.USDJPY, buyQuantity*(i + 1), new DateTime()) {Id = i + 1};
                _orders.Add(order);
                ib.PlaceOrder(order);

                orderEventFired.WaitOneAssertFail(1500, "Didnt receive order event #" + i);
                orderEventFired.Reset();

                accountChangedFired.WaitOneAssertFail(1500, "Didnt receive account event #" + i);
                accountChangedFired.Reset();
            }

            Assert.AreEqual(orderCount, cashBalanceUpdates.Count);
        }

        [Test]
        public void GetsCashBalanceAfterTrade()
        {
            var ib = _interactiveBrokersBrokerage;

            decimal balance = ib.GetCashBalance().Single(x => x.Symbol == "USD").Amount;

            // wait for our order to fill
            var manualResetEvent = new ManualResetEvent(false);
            ib.AccountChanged += (sender, orderEvent) => manualResetEvent.Set();

            var order = new MarketOrder(Symbols.USDJPY, buyQuantity, DateTime.UtcNow);
            _orders.Add(order);
            ib.PlaceOrder(order);

            manualResetEvent.WaitOneAssertFail(1500, "Didn't receive account changed event");

            decimal balanceAfterTrade = ib.GetCashBalance().Single(x => x.Symbol == "USD").Amount;

            Assert.AreNotEqual(balance, balanceAfterTrade);
        }

        [Test]
        public void GetExecutions()
        {
            var ib = _interactiveBrokersBrokerage;

            var orderEventFired = new ManualResetEvent(false);
            ib.OrderStatusChanged += (sender, args) =>
            {
                if (args.Status == OrderStatus.Filled)
                {
                    orderEventFired.Set();
                }
            };

            var order = new MarketOrder(Symbols.USDJPY, -buyQuantity, new DateTime());
            _orders.Add(order);
            ib.PlaceOrder(order);
            orderEventFired.WaitOne(1500);

            var executions = ib.GetExecutions(null, null, null, DateTime.UtcNow.AddDays(-1), null);

            Assert.IsTrue(executions.Any(x => order.BrokerId.Any(id => executions.Any(e => e.OrderId == int.Parse(id)))));
        }

        [Test]
        public void GetOpenOrders()
        {
            var ib = _interactiveBrokersBrokerage;

            var orderEventFired = new ManualResetEvent(false);
            ib.OrderStatusChanged += (sender, args) =>
            {
                if (args.Status == OrderStatus.Submitted)
                {
                    orderEventFired.Set();
                }
            };

            var order = new LimitOrder(Symbols.USDJPY, buyQuantity, 0.01m, DateTime.UtcNow);
            _orders.Add(order);
            ib.PlaceOrder(order);

            orderEventFired.WaitOne(1500);

            Thread.Sleep(250);

            var openOrders = ib.GetOpenOrders();

            Assert.AreNotEqual(0, openOrders.Count);
        }

        [Test, Ignore("This test requires disconnecting the internet to test for connection resiliency")]
        public void ClientReconnectsAfterInternetDisconnect()
        {
            var ib = _interactiveBrokersBrokerage;
            Assert.IsTrue(ib.IsConnected);

            var tenMinutes = TimeSpan.FromMinutes(10);
            
            Console.WriteLine("------");
            Console.WriteLine("Waiting for internet disconnection ");
            Console.WriteLine("------");

            // spin while we manually disconnect the internet
            while (ib.IsConnected)
            {
                Thread.Sleep(2500);
                Console.Write(".");
            }
            
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("------");
            Console.WriteLine("Trying to reconnect ");
            Console.WriteLine("------");

            // spin until we're reconnected
            while (!ib.IsConnected && stopwatch.Elapsed < tenMinutes)
            {
                Thread.Sleep(2500);
                Console.Write(".");
            }

            Assert.IsTrue(ib.IsConnected);
        }

        private static Order AssertOrderOpened(bool orderFilled, InteractiveBrokersBrokerage ib, Order order)
        {
            // if the order didn't fill check for it as an open order
            if (!orderFilled)
            {
                // find the right order and return it
                foreach (var openOrder in ib.GetOpenOrders())
                {
                    if (openOrder.BrokerId.Any(id => order.BrokerId.Any(x => x == id)))
                    {
                        return openOrder;
                    }
                }
                Assert.Fail("The order was not filled and was unable to be located via GetOpenOrders()");
            }

            Assert.Pass("The order was successfully filled!");
            return null;
        }

    }
}