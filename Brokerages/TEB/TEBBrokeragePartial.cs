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

namespace QuantConnect.Brokerages.TEB
{
    /// <summary>
    /// The Interactive Brokers brokerage
    /// </summary>
    public sealed partial class TEBBrokerage : Brokerage
    {

        private static int _nextClientID = 0;
        private string _account;
        private string _senderSubId = "algo";

        private readonly IOrderProvider _orderProvider;
        private ISecurityProvider _securityProvider;

        private readonly ConcurrentDictionary<string, decimal> _cashBalances = new ConcurrentDictionary<string, decimal>();
        private readonly ConcurrentDictionary<string, string> _accountProperties = new ConcurrentDictionary<string, string>();
        // number of shares per symbol
        private readonly ConcurrentDictionary<string, Holding> _accountHoldings = new ConcurrentDictionary<string, Holding>();


        private int _fixConnectionTimeout = 10000;

        private string _connectionString;
        private string _fixAppName;

        private FIXContext _fixContext;
        private SqlDbLogQueue _cashSqlDBQueue;
        private TradeDbHelper _tradeDbHelper;
        private ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        private readonly IAlgorithm Algorithm;
        private ConvertHelper convertHelper = new ConvertHelper();


        public TEBBrokerage(IAlgorithm algorithm, IOrderProvider orderProvider,
            ISecurityProvider securityProvider,
            string account,
            string connectionString,
            string applicationName,
            string senderSubId,
            int connectionTimeout)
            : base("TEB Brokerage")
        {
            Algorithm = algorithm;
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;
            _account = account;
            _senderSubId = senderSubId;
            _connectionString = connectionString;
            _fixAppName = applicationName;
            _fixConnectionTimeout = connectionTimeout;

            InitFix();

            InitDbHelper();

            //TODO burayı dinamik yap



            decimal cashBalance = 2500000;
            string currency = "USD";
            string key = TEBAccountValueKeys.CashBalance;

            _accountProperties[currency + ":" + key] = cashBalance.ToString();
            _cashBalances.AddOrUpdate(currency, cashBalance);
            OnAccountChanged(new AccountEvent(currency, cashBalance));
        }




        #region Helper Methods
        private Dictionary<string, string> GetAccountValues()
        {
            return new Dictionary<string, string>(_accountProperties);
        }



        private List<TEBPosition> GetPositions()
        {
            Dictionary<string, TEBPosition> tmpList = new Dictionary<string, TEBPosition>();

            IEnumerable<Teb.FIX.Model.Order> tmpOrders = _fixContext.CashAppContext.FilledOrdersView.Values;
            OrderViewType ViewType = OrderViewType.Filled;

            #region MyRegion
            if (tmpOrders != null)
            {
                TEBPosition to = null;
                //to.Seance = "Tüm Gün";
                int count = 0;
                foreach (var tmpOrder in tmpOrders)
                {
                    count++;
                    string symbol = tmpOrder.Symbol;

                    if (!tmpList.TryGetValue(symbol, out to))
                    {
                        to = new TEBPosition();
                        to.Symbol = symbol;
                        tmpList.Add(symbol, to);
                    }


                    bool isBuy = false;
                    decimal price = 0;
                    int qty = 0;

                    if (ViewType == OrderViewType.Filled)
                    {
                        if (tmpOrder.LastPx.HasValue)
                            price = tmpOrder.LastPx.Value;

                        if (tmpOrder.LastQty.HasValue)
                            qty = tmpOrder.LastQty.Value;
                    }
                    else
                    {
                        if (tmpOrder.Price.HasValue)
                            price = tmpOrder.Price.Value;

                        if (tmpOrder.LeavesQty.HasValue)
                            qty = tmpOrder.LeavesQty.Value;
                    }

                    //if (tmpOrder.OrderQty.HasValue)
                    //    qty = tmpOrder.OrderQty.Value;

                    decimal volume = price * qty;

                    if (tmpOrder.Core.Side.Equals(CashDefinition.SIDE_BUY))
                    {
                        isBuy = true;
                    }

                    if (isBuy)
                    {
                        to.BuyLotCount += qty;
                        to.BuyVolume += volume;
                    }
                    else
                    {
                        to.SellLotCount += qty;
                        to.SellVolume += volume;
                    }




                }

                if (count == 0)
                {
                    to = new TEBPosition();
                }
                else
                {
                    to.Count = count;

                    to.NetLotCount = to.BuyLotCount - to.SellLotCount;
                    to.NetVolume = to.SellVolume - to.BuyVolume;
                    to.TotalVolume = Math.Abs(to.BuyVolume) + Math.Abs(to.SellVolume);

                    if (to.NetLotCount != 0)
                        to.AvgNetPrice = Math.Abs(to.NetVolume / to.NetLotCount);
                    else
                        to.AvgNetPrice = 0;

                    if (to.BuyLotCount != 0)
                        to.AvgBuyPrice = Math.Abs(to.BuyVolume / to.BuyLotCount);
                    else
                        to.AvgBuyPrice = 0;

                    if (to.SellLotCount != 0)
                        to.AvgSellPrice = Math.Abs(to.SellVolume / to.SellLotCount);
                    else
                        to.AvgSellPrice = 0;
                }


                //tmpList.Add(to);
            }
            #endregion

            return tmpList.Values.ToList();

        }
        #endregion

        #region FIX Region

        private static int NextClientID()
        {
            return Interlocked.Increment(ref _nextClientID);
        }

        private void CashSqlDBQueue_OnDequeue(object sender, Teb.Infra.Collection.QueueEventArgs<Teb.FIX.Infra.Session.DbLog> arg)
        {
            try
            {
                DbManager dbManager = new DbManager(_connectionString);
                dbManager.Execute(arg.Entry.Query, arg.Entry.IsStoreProcedure, arg.Entry.Parameters);
            }
            catch (Exception ex)
            {
                string log = arg == null || arg.Entry == null ? "" : arg.Entry.ToString();
                TBYException tex = new TBYSqlException("5050", MethodBase.GetCurrentMethod().GetFullName(), String.Format("Hisse FIX Sql kuyruğunda hata oluştu.Sql({0})", log), ex);
                Error(tex);

            }
        }

        private void Error(Exception ex)
        {
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "FIX error", ex.ToString()));
            //Log.Error(tex.ToString());
        }

        private void CashSqlDBQueue_OnException(object sender, Teb.Infra.Collection.QueueEventArgs<Teb.FIX.Infra.Session.DbLog> arg)
        {
            string log = arg == null || arg.Entry == null ? "" : arg.Entry.ToString();

            TBYException tex = new TBYSqlException("5050", MethodBase.GetCurrentMethod().GetFullName(), String.Format("Hisse FIX Sql kuyruğunda hata oluştu.Sql({0})", log), arg.Exception);
            Error(tex);

        }

        private void CashAppContext_OnProcessDbMessage(DbMessage message)
        {
        }

        private void OrderHandler_OnLog(Logger.Log log)
        {
            if (log != null)
            {
                Log.Trace(log.ToString());
            }
        }

        private void CashOrderHandler_OnOrderUpdated(Teb.FIX.Model.Order order)
        {
            try
            {


                if (order.ServerStatus != ContentServerStatus.Waiting)
                {
                    const int orderFee = 0;
                    QuantConnect.Orders.Order quantOrder = convertHelper.ConvertToOrder(order);
                    OrderStatus status = convertHelper.ConvertToStatus(order);

                    var orderEvent = new OrderEvent(quantOrder, Algorithm.UtcTime, orderFee) { Status = status };
                    OnOrderEvent(orderEvent);
                }
                _tradeDbHelper.UpdateOrder(order);

            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), String.Format("DbID:{0} TEBID:{1} emir veritabanında güncellenirken hata oluştu", order == null ? "-1" : order.DbID.ToString(),
                   order == null ? "-1" : order.TEBID), ex);
                Error(tex);
            }
        }

        private void OrderHandler_OnMessageIncoming(Teb.FIX.Model.Order order, string fixMessage)
        {
            try
            {
                if (order.SecurityType == CashDefinition.SECURITY_CASH || order.SecurityType == FutureDefinition.SECURITY_FUTURE || order.SecurityType == FutureDefinition.SECURITY_OPTION)
                {
                    //if (Setting.DistributeFixMessages)
                    //{
                    //    FixMessage fm = new FixMessage()
                    //    {
                    //        Account = order.Account,
                    //        Message = fixMessage,
                    //        SecurityType = order.SecurityType,
                    //        DbID = order.DbID.ToString(),
                    //        TEBID = order.TEBID
                    //    };


                    //    TradeDistrubuter.FixMsgDistQueue.Enqueue(new FixMessageQueueItem() { Message = fm });
                    //}
                }
                else
                {
                    //Log(String.Format("{0} mesajının SecurityType = {1} değeri geçerli değildir", fixMessage, order.SecurityType));
                }

            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), String.Format("{0} mesajı alıcılara gönderilirken hata oluştu", fixMessage), ex);
                Error(tex);
            }
        }

        private void InitDbHelper()
        {
            _tradeDbHelper = new TradeDbHelper();

            _tradeDbHelper.ConnectionString = _connectionString;
        }

        private void TEBCancelOrder(QuantConnect.Orders.Order order, int origClOrdID)
        {
            Content co = ContentFactory.Create();
            co.ClientStatus = ContentClientStatus.PendingCancel;
            co.ClOrdID = NextClientID().ToString();
            co.OrigClOrdID = origClOrdID.ToString();
            //co.OrderID = Order.OrderID; 
            co.TransactTime = DateTime.Now;
            co.Symbol = order.Symbol;
            //co.SymbolSfx = Order.SymbolSfx;
            co.OrdType = convertHelper.ConvertToOrdType(order.Type);
            co.TimeInForce = convertHelper.ConvertToTimeInForce(order.Duration);
            co.Price = order.Price;
            co.OrderQty = order.Quantity;
            co.Side = convertHelper.ConvertToSide(order.Quantity);
            //co.ExpireDate = Order.ExpireDate;
            //co.Afe = Order.Afe;
            //co.Kafe = Order.Kafe;
            //co.IsSellShort = Order.IsSellShort;
            co.SecurityType = CashDefinition.SECURITY_CASH;
            //co.InstructionSource = Order.InstructionSource;
            //co.MarketSource = Order.MarketSource;
            //co.PartyID = Order.PartyID;
            co.SenderSubID = _senderSubId;
        }

        private Content CreateContent(QuantConnect.Orders.Order order, int clOrdId)
        {
            Content content = ContentFactory.Create();
            content.ServerStatus = ContentServerStatus.Waiting;
            content.ClientStatus = ContentClientStatus.PendingNew;
            content.TransactTime = DateTime.Now;
            content.ClOrdID = clOrdId.ToString();
            content.MarketID = clOrdId.ToString();
            content.Side = convertHelper.ConvertToSide(order.Quantity);
            //content.IsSellShort = builder.IsSellShort; 
            content.OrdType = convertHelper.ConvertToOrdType(order.Type);
            content.Account = _account;
            content.OrderCapacity = CashDefinition.ACCOUNT_TYPE_CUSTOMER;
            content.SenderSubID = _senderSubId;
            content.Symbol = order.Symbol;            //content.SymbolSfxMarket = builder.Market.Value.Trim();
            content.OrderQty = Math.Abs(order.Quantity);

            if (order is LimitOrder)
            {
                decimal limitPrice = (decimal)((LimitOrder)order).LimitPrice;
                content.Price = limitPrice;
            }
            else
            {
                content.Price = order.Price;
            }

            content.TransactTime = order.Time;
            content.OrderQty = Math.Abs(order.Quantity);
            content.TimeInForce = convertHelper.ConvertToTimeInForce(order.Type);
            content.SecurityType = CashDefinition.SECURITY_CASH;


            return content;
        }

        private void CashAppContext_OnLogonStatusChanged(BaseAppContext context, bool state)
        {
            if (state)
                manualResetEvent.Set();

            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "FIX connection", String.Format("{0} connection changed to {1}", context.AppName, state ? "logon" : "logout")));

        }

        private void InitFix()
        {
            _fixContext = new FIXContext();

            _fixContext.OnException += FIXContext_OnException;

            InitFixCash();
        }

        private void FIXContext_OnException(Exception ex)
        {
            Error(ex);
        }

        private void InitFixCash()
        {


            _cashSqlDBQueue = new SqlDbLogQueue();

            _cashSqlDBQueue.OnDequeue += CashSqlDBQueue_OnDequeue;
            _cashSqlDBQueue.OnException += CashSqlDBQueue_OnException;

            DBHelper dbHelperCash = new DBHelper();
            dbHelperCash.ConnectionString = _connectionString;

            _fixContext.CashAppContext = new CashAppContext();
            _fixContext.CashAppContext.OnLogonStatusChanged += CashAppContext_OnLogonStatusChanged;
            _fixContext.CashAppContext.ExchangeType = ExchangeType.Nasdaq;
            _fixContext.CashAppContext.GetMessages = dbHelperCash.GetCashMessageDataSet;
            _fixContext.CashAppContext.OnProcessDbMessage += CashAppContext_OnProcessDbMessage;
            _fixContext.CashAppContext.OrderHandler.OnLog += OrderHandler_OnLog;
            _fixContext.CashAppContext.NotifyDbMessages = true;
            _fixContext.CashAppContext.OnOrderUpdated += CashOrderHandler_OnOrderUpdated;
            _fixContext.CashAppContext.OrderHandler.OnMessageIncoming += OrderHandler_OnMessageIncoming;



            DbManager cashDbManager = new DbManager(_connectionString);

            SessionSetting settingCash = new SessionDBSetting(StoreType.Sql, cashDbManager, null,
                _cashSqlDBQueue, _fixAppName, "", "");

            string nextClOrdId = settingCash.GetSessionSettings().Get().GetString("NextClOrdId");
            int tmpClOrdId;
            if (int.TryParse(nextClOrdId, out tmpClOrdId))
            {
                _nextClientID = tmpClOrdId;
            }
            _fixContext.CashAppContext.SetApp(settingCash);
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "FIX Client ID", String.Format("{0} connection last client ID {1}", _fixContext.CashAppContext.AppName, _nextClientID)));
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Disposes the TEBBrokerage instance
        /// </summary>
        public void Dispose()
        {
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, "FIX Client Disposing", "TEBBrokerage is disposing"));
            Disconnect();
            if (_fixContext != null)
            {
                _fixContext.Dispose();
            }
        }
        #endregion
    }


    static class TEBAccountValueKeys
    {
        public const string CashBalance = "CashBalance";
        public const string AccruedCash = "AccruedCash";
        public const string NetLiquidationByCurrency = "NetLiquidationByCurrency";
    }

    /// <summary>
    /// TEBPosition
    /// </summary>
    public class TEBPosition
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; }
        /// <summary>
        /// AvgBuyPrice
        /// </summary>
        public decimal AvgBuyPrice { get; set; }
        /// <summary>
        /// AvgNetPrice
        /// </summary>
        public decimal AvgNetPrice { get; set; }
        /// <summary>
        /// AvgSellPrice
        /// </summary>
        public decimal AvgSellPrice { get; set; }
        /// <summary>
        /// BuyLotCount
        /// </summary>
        public int BuyLotCount { get; set; }
        /// <summary>
        /// BuyVolume
        /// </summary>
        public decimal BuyVolume { get; set; }
        /// <summary>
        /// Count
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// NetLotCount
        /// </summary>
        public int NetLotCount { get; set; }
        /// <summary>
        /// NetVolume
        /// </summary>
        public decimal NetVolume { get; set; }
        /// <summary>
        /// Seance
        /// </summary>
        public string Seance { get; set; }
        /// <summary>
        /// SellLotCount
        /// </summary>
        public int SellLotCount { get; set; }
        /// <summary>
        /// SellVolume
        /// </summary>
        public decimal SellVolume { get; set; }
        /// <summary>
        /// TotalVolume
        /// </summary>
        public decimal TotalVolume { get; set; }
    }
}
