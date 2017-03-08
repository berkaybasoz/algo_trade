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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using IB = Krs.Ats.IBNet;
using QuantConnect.Brokerages.TEB.FIX;
using Teb.FIX.Infra.Session;
using Teb.FIX.Infra.Context.Cash;
using Teb.DAL;
using Teb.Infra;
using Teb.Infra.Extension;
using Teb.FIX.Infra.App;
using Teb.FIX.Infra.Context;
using Teb.FIX.Model;
using System.Reflection;
using Teb.FIX.Collection;
using Teb.FIX.Contents;
using Teb.FIX.Infra.OrderState;
using QuantConnect.Debug;

namespace QuantConnect.Brokerages.TEB
{
    /// <summary>
    /// TEB brokerage
    /// </summary>
    public sealed partial class TEBBrokerage : Brokerage
    {
        #region Brokerage Implemantation

        /// <summary>
        /// Connects the client to the TEB gateway
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            // we're going to receive fresh values for both of these collections, so clear them
            _accountHoldings.Clear();
            _accountProperties.Clear();
            _fixContext.CashLoadOrders();
            _fixContext.CashConnect();

            //TODO : FIX contextin connecti asyncron oldugu icin emirlerin dolmasini bekliyoruz
            //Buradaki sleepi kaldir
            //Thread.Sleep(_fixConnectionTimeout);
            manualResetEvent.WaitOne(_fixConnectionTimeout);


        }
        /// <summary>
        /// Disconnects the client from the TEB gateway
        /// </summary>
        public override void Disconnect()
        {
            if (!IsConnected) return;

            _fixContext.CashDisconnect();
        }

        /// <summary>
        /// Connection status of the client to the TEB gateway
        /// </summary>
        public override bool IsConnected
        {
            get
            {
                if (_fixContext == null)
                    return false;
                return _fixContext.CashAppContext.IsLoggedOn;
            }
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(QuantConnect.Orders.Order order)
        {
            try
            {
                Log.Trace("TEBBrokerage.PlaceOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

                if (!IsConnected)
                {
                    Log.Error("TEBBrokerage.PlaceOrder(): Unable to place order while not connected.");
                    throw new InvalidOperationException("TEBBrokerage.PlaceOrder(): Unable to place order while not connected.");
                }


                //if (needsNewID)
                //{
                // the order ids are generated for us by the SecurityTransactionManaer
                int id = NextClientID();
                order.BrokerId.Add(id.ToString());
                int clOrdID = id;
                //}
                //else if (order.BrokerId.Any())
                //{
                //    // this is *not* perfect code
                //    clOrdID = int.Parse(order.BrokerId[0]);
                //}
                //else
                //{
                //    throw new ArgumentException("Expected order with populated BrokerId for updating orders.");
                //}

                var content = CreateContent(order, clOrdID);
                bool isSent = false;
                _fixContext.SendNewOrderToCash(content, out  isSent);
                return isSent;
            }
            catch (Exception err)
            {
                Log.Error("TEBBrokerage.PlaceOrder(): " + err);
                return false;
            }
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(QuantConnect.Orders.Order order)
        {
            try
            {
                Log.Trace("TEBBrokerage.UpdateOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity + " Status: " + order.Status);

                if (!IsConnected)
                {
                    Log.Error("TEBBrokerage.UpdateOrder(): Unable to place order while not connected.");
                    throw new InvalidOperationException("TEBBrokerage.UpdateOrder(): Unable to place order while not connected.");
                }


                int clOrdID = 0;
                //if (needsNewID)
                //{
                // the order ids are generated for us by the SecurityTransactionManaer
                int id = NextClientID();
                order.BrokerId.Add(id.ToString());
                clOrdID = id;
                //}
                //else if (order.BrokerId.Any())
                //{
                //    // this is *not* perfect code
                //    clOrdID = int.Parse(order.BrokerId[0]);
                //}
                //else
                //{
                //    throw new ArgumentException("Expected order with populated BrokerId for updating orders.");
                //}

                var content = CreateContent(order, clOrdID);
                bool isSent = false;
                _fixContext.SendNewOrderToCash(content, out  isSent);
                return isSent;
            }
            catch (Exception err)
            {
                Log.Error("TEBBrokerage.UpdateOrder(): " + err);
                return false;
            }
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(QuantConnect.Orders.Order order)
        {
            try
            {
                Log.Trace("TEBBrokerage.CancelOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

                if (!IsConnected)
                {
                    Log.Error("TEBBrokerage.CancelOrder(): Unable to place order while not connected.");
                    throw new InvalidOperationException("TEBBrokerage.CancelOrder(): Unable to place order while not connected.");
                }

                // this could be better
                foreach (var id in order.BrokerId)
                {
                    TEBCancelOrder(order, int.Parse(id));
                }

                // canceled order events fired upon confirmation, see HandleError
            }
            catch (Exception err)
            {
                Log.Error("TEBBrokerage.CancelOrder(): OrderID: " + order.Id + " - " + err);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets all open orders on the account
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<QuantConnect.Orders.Order> GetOpenOrders()
        {
            var orders = new List<QuantConnect.Orders.Order>();

            var tebOrders = _fixContext.CashAppContext.BISTOrdersView.Values.Select(convertHelper. ConvertToOrder).ToList();

            orders.AddRange(tebOrders);

            DebugHelper.Log(String.Format("----------- GetOpenOrders count {0} --", orders.Count));

            #region Backtesting brokerage
            //return Algorithm.Transactions.GetOpenOrders(); 
            #endregion

            //manualResetEvent.Set();
            //if (!tmpManualResetEvent.WaitOne(15000))
            //{
            //    throw new TimeoutException("InteractiveBrokersBrokerage.GetOpenOrders(): Operation took longer than 15 seconds.");
            //}

            return orders;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdings = GetPositions().Select(convertHelper.ConvertToHolding).Where(x => x.Quantity != 0).ToList();


            DebugHelper.Log(String.Format("----------- GetAccountHoldings count {0} --", holdings.Count));
            foreach (var h in holdings)
            {
                DebugHelper.Log(String.Format("----------- GetAccountHoldings {0}", h.ToString()));
            }
            //var quotes = GetQuotes(symbols).ToDictionary(x => x.Symbol);
            //foreach (var holding in holdings)
            //{
            //    TradierQuote quote;
            //    if (quotes.TryGetValue(holding.Symbol.Value, out quote))
            //    {
            //        holding.MarketPrice = quote.Last;
            //    }
            //}


            #region Backtesting brokerage
            //return (from security in Algorithm.Portfolio.Securities.Values.OrderBy(x => x.Symbol)
            //        where security.Holdings.AbsoluteQuantity > 0
            //        select new Holding(security)).ToList(); 
            #endregion

            #region IB brokerage
            //var holdings = _accountHoldings.Select(x => (Holding)ObjectActivator.Clone(x.Value)).Where(x => x.Quantity != 0).ToList();

            //// fire up tasks to resolve the conversion rates so we can do them in parallel
            //var tasks = holdings.Select(local =>
            //{
            //    // we need to resolve the conversion rate for non-USD currencies
            //    if (local.Type != SecurityType.Forex)
            //    {
            //        // this assumes all non-forex are us denominated, we should add the currency to 'holding'
            //        local.ConversionRate = 1m;
            //        return null;
            //    }
            //    //// if quote currency is in USD don't bother making the request
            //    //string currency = local.Symbol.Value.Substring(3);
            //    //if (currency == "USD")
            //    //{
            //    //    local.ConversionRate = 1m;
            //    //    return null;
            //    //}

            //    // this will allow us to do this in parallel
            //    return Task.Factory.StartNew(() => local.ConversionRate = 1);
            //}).Where(x => x != null).ToArray();

            //Task.WaitAll(tasks, 5000); 
            #endregion

            return holdings;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {


            return Algorithm.Portfolio.CashBook.Values.ToList();

            //TODO : _cashBalances ne zaman guncellenecek
            //List<Cash> balances = _cashBalances.Select(x => new Cash(x.Key, x.Value, 1)).ToList();

            //DebugHelper.Log(String.Format("-- GetCashBalance count {0} --", balances.Count));
            //foreach (var b in balances)
            //{
            //    DebugHelper.Log(String.Format("GetCashBalance {0}",b.ToString()));
            //}
             
            //return balances;
        }



        #endregion

    }



}
