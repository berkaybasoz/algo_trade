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
 *
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Tradier Class: 
    ///  - Handle authentication.
    ///  - Data requests.
    ///  - Rate limiting.
    ///  - Placing orders.
    ///  - Getting user data.
    /// </summary>
    public class TradierBrokerage : Brokerage
    {
        private readonly long _accountID;

        // we're reusing the equity exchange here to grab typical exchange hours
        private static readonly EquityExchange Exchange =
            new EquityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity, TimeZones.NewYork));
        
        //Access and Refresh Tokens:
        private string _previousResponseRaw = "";
        private DateTime _issuedAt;
        private TimeSpan _lifeSpan = TimeSpan.FromSeconds(86399); // 1 second less than a day
        private readonly object _lockAccessCredentials = new object();

        // polling timers for refreshing access tokens and checking for fill events
        private Timer _refreshTimer;
        private Timer _orderFillTimer;

        //Tradier Spec:
        private readonly Dictionary<TradierApiRequestType, TimeSpan> _rateLimitPeriod;
        private readonly Dictionary<TradierApiRequestType, DateTime> _rateLimitNextRequest;

        //Endpoints:
        private const string RequestEndpoint = @"https://api.tradier.com/v1/";
        private readonly IOrderProvider _orderProvider;
        private readonly ISecurityProvider _securityProvider;

        private readonly object _fillLock = new object();
        private readonly DateTime _initializationDateTime = DateTime.Now;
        private readonly ConcurrentDictionary<long, TradierCachedOpenOrder> _cachedOpenOrdersByTradierOrderID;
        // this is used to block reentrance when doing look ups for orders with IDs we don't have cached
        private readonly HashSet<long> _reentranceGuardByTradierOrderID = new HashSet<long>();
        private readonly FixedSizeHashQueue<long> _filledTradierOrderIDs = new FixedSizeHashQueue<long>(10000); 
        // this is used to handle the zero crossing case, when the first order is filled we'll submit the next order
        private readonly ConcurrentDictionary<long, ContingentOrderQueue> _contingentOrdersByQCOrderID = new ConcurrentDictionary<long, ContingentOrderQueue>();
        // this is used to block reentrance when handling contingent orders
        private readonly HashSet<long> _contingentReentranceGuardByQCOrderID = new HashSet<long>();
        private readonly HashSet<long> _unknownTradierOrderIDs = new HashSet<long>(); 
        private readonly FixedSizeHashQueue<long> _verifiedUnknownTradierOrderIDs = new FixedSizeHashQueue<long>(1000);
        private readonly FixedSizeHashQueue<int> _cancelledQcOrderIDs = new FixedSizeHashQueue<int>(10000);  

        /// <summary>
        /// Event fired when our session has been refreshed/tokens updated
        /// </summary>
        public event EventHandler<TokenResponse> SessionRefreshed;

        /// <summary>
        /// When we expect this access token to expire, leaves an hour of padding
        /// </summary>
        private DateTime ExpectedExpiry
        {
            get { return _issuedAt + _lifeSpan - TimeSpan.FromMinutes(60); }
        }

        /// <summary>
        /// Access Token Access:
        /// </summary>
        public string AccessToken { get; private set; }

        /// <summary>
        /// Refresh Token Access:
        /// </summary>
        public string RefreshToken { get; private set; }

        /// <summary>
        /// The QC User id, used for refreshing the session
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Get the last string returned
        /// </summary>
        public string LastResponse
        {
            get { return _previousResponseRaw; }
        }

        /// <summary>
        /// Create a new Tradier Object:
        /// </summary>
        public TradierBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, long accountID)
            : base("Tradier Brokerage")
        {
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;
            _accountID = accountID;

            _cachedOpenOrdersByTradierOrderID = new ConcurrentDictionary<long, TradierCachedOpenOrder>();

            //Tradier Specific Initialization:
            _rateLimitPeriod = new Dictionary<TradierApiRequestType, TimeSpan>();
            _rateLimitNextRequest = new Dictionary<TradierApiRequestType, DateTime>();

            //Go through each API request type and initialize:
            foreach (TradierApiRequestType requestType in Enum.GetValues(typeof(TradierApiRequestType)))
            {
                //Sandbox and most live are 1sec
                _rateLimitPeriod.Add(requestType, TimeSpan.FromSeconds(1));
                _rateLimitNextRequest.Add(requestType, new DateTime());
            }

            //Swap into sandbox end points / modes.
            _rateLimitPeriod[TradierApiRequestType.Standard] = TimeSpan.FromMilliseconds(500);
            _rateLimitPeriod[TradierApiRequestType.Data] = TimeSpan.FromMilliseconds(500);
        }

        #region Tradier client implementation

        /// <summary>
        /// Set the access token and login information for the tradier brokerage 
        /// </summary>
        /// <param name="userId">Userid for this brokerage</param>
        /// <param name="accessToken">Viable access token</param>
        /// <param name="refreshToken">Our refresh token</param>
        /// <param name="issuedAt">When the token was issued</param>
        /// <param name="lifeSpan">Life span for our token.</param>
        public void SetTokens(int userId, string accessToken, string refreshToken, DateTime issuedAt, TimeSpan lifeSpan)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            _issuedAt = issuedAt;
            _lifeSpan = lifeSpan;
            UserId = userId;

            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
            }
            if (_orderFillTimer != null)
            {
                _orderFillTimer.Dispose();
            }
            
            var dueTime = ExpectedExpiry - DateTime.UtcNow;
            if (dueTime < TimeSpan.Zero) dueTime = TimeSpan.Zero;
            var period = TimeSpan.FromDays(1).Subtract(TimeSpan.FromMinutes(-1));
            _refreshTimer = new Timer(state => RefreshSession(), null, dueTime, period);

            // we can poll orders once a second in sandbox and twice a second in production
            double orderPollingIntervalInSeconds = Config.GetDouble("tradier-order-poll-interval", 1.0);
            var interval = (int)(1000*orderPollingIntervalInSeconds);
            _orderFillTimer = new Timer(state => CheckForFills(), null, interval, interval);
        }

        /// <summary>
        /// Execute a authenticated call:
        /// </summary>
        public T Execute<T>(RestRequest request, TradierApiRequestType type, string rootName = "", int attempts = 0, int max = 10) where T : new()
        {
            var response = default(T);

            var method = "TradierBrokerage.Execute." + request.Resource;
            var parameters = request.Parameters.Select(x => x.Name + ": " + x.Value);

            if (attempts != 0)
            {
                Log.Trace(method + "(): Begin attempt " + attempts);
            }

            lock (_lockAccessCredentials)
            {
                var client = new RestClient(RequestEndpoint);
                client.AddDefaultHeader("Accept", "application/json");
                client.AddDefaultHeader("Authorization", "Bearer " + AccessToken);
                //client.AddDefaultHeader("Content-Type", "application/x-www-form-urlencoded");

                //Wait for the API rate limiting
                while (DateTime.Now < _rateLimitNextRequest[type]) Thread.Sleep(10);
                _rateLimitNextRequest[type] = DateTime.Now + _rateLimitPeriod[type];

                //Send the request:
                var raw = client.Execute(request);
                _previousResponseRaw = raw.Content;
                //Log.Trace("TradierBrokerage.Execute: " + raw.Content);

                try
                {
                    if (rootName != "")
                    {
                        response = DeserializeRemoveRoot<T>(raw.Content, rootName);
                    }
                    else
                    {
                        response = JsonConvert.DeserializeObject<T>(raw.Content);
                    }
                }
                catch(Exception err)
                {
                    // tradier sometimes sends back poorly formed messages, response will be null
                    // and we'll extract from it below
                    Log.Error(err, "Poorly formed message. Content: " + raw.Content);
                }

                if (response == null)
                {
                    TradierFaultContainer fault = null;
                    try
                    {
                        fault = JsonConvert.DeserializeObject<TradierFaultContainer>(raw.Content);
                    }
                    catch
                    {
                        // tradier sometimes sends back poorly formed messages, response will be null
                        // and we'll extract from it below
                    }
                    if (fault != null && fault.Fault != null)
                    {
                        // JSON Errors:
                        Log.Trace(method + "(1): Parameters: " + string.Join(",", parameters));
                        Log.Error(method + "(1): " + fault.Fault.Description);
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "JsonError", fault.Fault.Description));
                    }
                    else
                    {
                        // this happens when we try to cancel a filled order
                        if (raw.Content.Contains("order already in finalized state: filled"))
                        {
                            if (request.Method == Method.DELETE)
                            {
                                string orderId = "[unknown]";
                                var parameter = request.Parameters.FirstOrDefault(x => x.Name == "orderId");
                                if (parameter != null) orderId = parameter.Value.ToString();
                                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "OrderAlreadyFilled",
                                    "Unable to cancel the order because it has already been filled. TradierOrderId: " + orderId
                                    ));

                                
                            }
                            return new T();
                        }
                        // Text Errors:
                        Log.Trace(method + "(2): Parameters: " + string.Join(",", parameters));
                        Log.Error(method + "(2): Response: " + raw.Content);
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "Unknown", raw.Content));
                    }
                }

                if (raw.ErrorException != null)
                {
                    if (attempts++ < max)
                    {
                        Log.Trace(method + "(3): Attempting again...");
                        // this will retry on time outs and other transport exception
                        Thread.Sleep(3000);
                        return Execute<T>(request, type, rootName, attempts, max);
                    }

                    Log.Trace(method + "(3): Parameters: " + string.Join(",", parameters));
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, raw.ErrorException.GetType().Name, raw.ErrorException.ToString()));

                    const string message = "Error retrieving response.  Check inner details for more info.";
                    throw new ApplicationException(message, raw.ErrorException);
                }
            }

            if (response == null)
            {
                if (attempts++ < max)
                {
                    Log.Trace(method + "(4): Attempting again...");
                    // this will retry on time outs and other transport exception
                    Thread.Sleep(3000);
                    return Execute<T>(request, type, rootName, attempts, max);
                }

                Log.Trace(method + "(4): Parameters: " + string.Join(",", parameters));
                Log.Error(method + "(4): NULL Response: Raw Response: " + _previousResponseRaw);
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "NullResponse", _previousResponseRaw));
            }

            return response;
        }

        /// <summary>
        /// Verify we have a user session; or refresh the access token.
        /// </summary>
        public bool RefreshSession()
        {
            //Send: 
            //Get: {"sAccessToken":"123123","iExpiresIn":86399,"dtIssuedAt":"2014-10-15T16:59:52-04:00","sRefreshToken":"123123","sScope":"read write market trade stream","sStatus":"approved","success":true}
            // Or: {"success":false}
            var raw = "";
            bool success;
            lock (_lockAccessCredentials)
            {
                try
                {
                    //Create the client for connection:
                    var client = new RestClient("https://www.quantconnect.com/terminal/");

                    //Create the GET call:
                    var request = new RestRequest("processTradier", Method.GET);
                    request.AddParameter("uid", UserId.ToString(), ParameterType.GetOrPost);
                    request.AddParameter("accessToken", AccessToken, ParameterType.GetOrPost);
                    request.AddParameter("refreshToken", RefreshToken, ParameterType.GetOrPost);

                    //Submit the call:
                    var result = client.Execute(request);
                    raw = result.Content;

                    //Decode to token response: update internal access parameters:
                    var newTokens = JsonConvert.DeserializeObject<TokenResponse>(result.Content);
                    if (newTokens != null && newTokens.Success)
                    {
                        AccessToken = newTokens.AccessToken;
                        RefreshToken = newTokens.RefreshToken;
                        _issuedAt = newTokens.IssuedAt;
                        _lifeSpan = TimeSpan.FromSeconds(newTokens.ExpiresIn);
                        Log.Trace("SESSION REFRESHED: Access: " + AccessToken + " Refresh: " + RefreshToken + " Issued At: " + _lifeSpan + " JSON>>"
                            + result.Content);
                        OnSessionRefreshed(newTokens);
                        success = true;
                    }
                    else
                    {
                        Log.Error("Tradier.RefreshSession(): Error Refreshing Session: URL: " + client.BuildUri(request) + " Response: " + result.Content);
                        success = false;
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err, "Raw: " + raw);
                    success = false;
                }
            }

            if (!success)
            {
                // if we can't refresh our tokens then we must stop the algorithm
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "RefreshSession", "Failed to refresh access token: " + raw));
            }

            return success;
        }

        /// <summary>
        /// Using this auth token get the tradier user:
        /// </summary>
        /// <remarks>
        /// Returns null if the request was unsucessful
        /// </remarks>
        /// <returns>Tradier user model:</returns>
        public TradierUser GetUserProfile()
        {
            var request = new RestRequest("user/profile", Method.GET);
            var userContainer = Execute<TradierUserContainer>(request, TradierApiRequestType.Standard);
            return userContainer.Profile;
        }

        /// <summary>
        /// Get all the users balance information:
        /// </summary>
        /// <remarks>
        /// Returns null if the request was unsucessful
        /// </remarks>
        /// <returns>Balance</returns>
        public TradierBalanceDetails GetBalanceDetails(long accountId)
        {
            var request = new RestRequest("accounts/{accountId}/balances", Method.GET);
            request.AddParameter("accountId", accountId, ParameterType.UrlSegment);
            var balContainer = Execute<TradierBalance>(request, TradierApiRequestType.Standard);
            //Log.Trace("TradierBrokerage.GetBalanceDetails(): Bal Container: " + JsonConvert.SerializeObject(balContainer));
            return balContainer.Balances;
        }

        /// <summary>
        /// Get a list of the tradier positions for this account:
        /// </summary>
        /// <remarks>
        /// Returns null if the request was unsucessful
        /// </remarks>
        /// <returns>Array of the symbols we hold.</returns>
        public List<TradierPosition> GetPositions()
        {
            var request = new RestRequest("accounts/{accountId}/positions", Method.GET);
            request.AddParameter("accountId", _accountID, ParameterType.UrlSegment);
            var positionContainer = Execute<TradierPositionsContainer>(request, TradierApiRequestType.Standard);

            if (positionContainer.TradierPositions == null || positionContainer.TradierPositions.Positions == null)
            {
                // we had a successful call but there weren't any positions
                Log.Trace("Tradier.Positions(): No positions found");
                return new List<TradierPosition>();
            }

            return positionContainer.TradierPositions.Positions;
        }

        /// <summary>
        /// Get a list of historical events for this account:
        /// </summary>
        /// <remarks>
        /// Returns null if the request was unsucessful
        /// </remarks>
        public List<TradierEvent> GetAccountEvents(long accountId)
        {
            var request = new RestRequest("accounts/{accountId}/history", Method.GET);
            request.AddUrlSegment("accountId", accountId.ToString());

            var eventContainer = Execute<TradierEventContainer>(request, TradierApiRequestType.Standard);

            if (eventContainer.TradierEvents == null || eventContainer.TradierEvents.Events == null)
            {
                // we had a successful call but there weren't any events
                Log.Trace("Tradier.GetAccountEvents(): No events found");
                return new List<TradierEvent>();
            }

            return eventContainer.TradierEvents.Events;
        }

        /// <summary>
        /// GainLoss of recent trades for this account:
        /// </summary>
        public List<TradierGainLoss> GetGainLoss(long accountId)
        {
            var request = new RestRequest("accounts/{accountId}/gainloss");
            request.AddUrlSegment("accountId", accountId.ToString());

            var gainLossContainer = Execute<TradierGainLossContainer>(request, TradierApiRequestType.Standard);

            if (gainLossContainer.GainLossClosed == null || gainLossContainer.GainLossClosed.ClosedPositions == null)
            {
                // we had a successful call but there weren't any records returned
                Log.Trace("Tradier.GetGainLoss(): No gain loss found");
                return new List<TradierGainLoss>();
            }

            return gainLossContainer.GainLossClosed.ClosedPositions;
        }

        /// <summary>
        /// Get Intraday and pending orders for users account: accounts/{account_id}/orders
        /// </summary>
        public List<TradierOrder> GetIntradayAndPendingOrders()
        {
            var request = new RestRequest("accounts/{accountId}/orders");
            request.AddUrlSegment("accountId", _accountID.ToString());
            var ordersContainer = Execute<TradierOrdersContainer>(request, TradierApiRequestType.Standard);

            if (ordersContainer.Orders == null)
            {
                // we had a successful call but there weren't any orders returned
                Log.Trace("Tradier.FetchOrders(): No orders found");
                return new List<TradierOrder>();
            }

            return ordersContainer.Orders.Orders;
        }

        /// <summary>
        /// Get information about a specific order: accounts/{account_id}/orders/{id}
        /// </summary>
        public TradierOrderDetailed GetOrder(long orderId)
        {
            var request = new RestRequest("accounts/{accountId}/orders/" + orderId);
            request.AddUrlSegment("accountId", _accountID.ToString());
            var detailsParent = Execute<TradierOrderDetailedContainer>(request, TradierApiRequestType.Standard);
            if (detailsParent == null || detailsParent.DetailedOrder == null)
            {
                Log.Error("Tradier.GetOrder(): Null response.");
                return new TradierOrderDetailed();
            }
            return detailsParent.DetailedOrder;
        }

        /// <summary>
        /// Place Order through API.
        /// accounts/{account-id}/orders
        /// </summary>
        public TradierOrderResponse PlaceOrder(long accountId,
            TradierOrderClass classification,
            TradierOrderDirection direction,
            string symbol,
            decimal quantity,
            decimal price = 0,
            decimal stop = 0,
            string optionSymbol = "",
            TradierOrderType type = TradierOrderType.Market,
            TradierOrderDuration duration = TradierOrderDuration.GTC)
        {
            //Compose the request:
            var request = new RestRequest("accounts/{accountId}/orders");
            request.AddUrlSegment("accountId", accountId.ToString());

            //Add data:
            request.AddParameter("class", GetEnumDescription(classification));
            request.AddParameter("symbol", symbol);
            request.AddParameter("duration", GetEnumDescription(duration));
            request.AddParameter("type", GetEnumDescription(type));
            request.AddParameter("quantity", quantity);
            request.AddParameter("side", GetEnumDescription(direction));

            //Add optionals:
            if (price > 0) request.AddParameter("price", Math.Round(price, 2));
            if (stop > 0) request.AddParameter("stop", Math.Round(stop, 2));
            if (optionSymbol != "") request.AddParameter("option_symbol", optionSymbol);

            //Set Method:
            request.Method = Method.POST;

            return Execute<TradierOrderResponse>(request, TradierApiRequestType.Orders);
        }

        /// <summary>
        /// Update an exiting Tradier Order:
        /// </summary>
        public TradierOrderResponse ChangeOrder(long accountId,
            long orderId,
            TradierOrderType type = TradierOrderType.Market,
            TradierOrderDuration duration = TradierOrderDuration.GTC,
            decimal price = 0,
            decimal stop = 0)
        {
            //Create Request:
            var request = new RestRequest("accounts/{accountId}/orders/{orderId}");
            request.AddUrlSegment("accountId", accountId.ToString());
            request.AddUrlSegment("orderId", orderId.ToString());
            request.Method = Method.PUT;

            //Add Data:
            request.AddParameter("type", GetEnumDescription(type));
            request.AddParameter("duration", GetEnumDescription(duration));
            if (price != 0) request.AddParameter("price", Math.Round(price, 2).ToString(CultureInfo.InvariantCulture));
            if (stop != 0) request.AddParameter("stop", Math.Round(stop, 2).ToString(CultureInfo.InvariantCulture));

            //Send:
            return Execute<TradierOrderResponse>(request, TradierApiRequestType.Orders);
        }

        /// <summary>
        /// Cancel the order with this account and id number
        /// </summary>
        public TradierOrderResponse CancelOrder(long accountId, long orderId)
        {
            //Compose Request:
            var request = new RestRequest("accounts/{accountId}/orders/{orderId}");
            request.AddUrlSegment("accountId", accountId.ToString());
            request.AddUrlSegment("orderId", orderId.ToString());
            request.Method = Method.DELETE;

            //Transmit Request:
            return Execute<TradierOrderResponse>(request, TradierApiRequestType.Orders);
        }

        /// <summary>
        /// List of quotes for symbols 
        /// </summary>
        public List<TradierQuote> GetQuotes(List<string> symbols)
        {
            if (symbols.Count == 0)
            {
                return new List<TradierQuote>();
            }

            //Send Request:
            var request = new RestRequest("markets/quotes", Method.GET);
            var csvSymbols = String.Join(",", symbols);
            request.AddParameter("symbols", csvSymbols, ParameterType.QueryString);

            var dataContainer = Execute<TradierQuoteContainer>(request, TradierApiRequestType.Data, "quotes");
            return dataContainer.Quotes;
        }

        /// <summary>
        /// Get the historical bars for this period
        /// </summary>
        public List<TradierTimeSeries> GetTimeSeries(string symbol, DateTime start, DateTime end, TradierTimeSeriesIntervals interval)
        {
            //Send Request:
            var request = new RestRequest("markets/timesales", Method.GET);
            request.AddParameter("symbol", symbol, ParameterType.QueryString);
            request.AddParameter("interval", GetEnumDescription(interval), ParameterType.QueryString);
            request.AddParameter("start", start.ToString("yyyy-MM-dd HH:mm"), ParameterType.QueryString);
            request.AddParameter("end", end.ToString("yyyy-MM-dd HH:mm"), ParameterType.QueryString);
            var dataContainer = Execute<TradierTimeSeriesContainer>(request, TradierApiRequestType.Data, "series");
            return dataContainer.TimeSeries;
        }

        /// <summary>
        /// Get full daily, weekly or monthly bars of historical periods:
        /// </summary>
        public List<TradierHistoryBar> GetHistoricalData(string symbol,
            DateTime start,
            DateTime end,
            TradierHistoricalDataIntervals interval = TradierHistoricalDataIntervals.Daily)
        {
            var request = new RestRequest("markets/history", Method.GET);
            request.AddParameter("symbol", symbol, ParameterType.QueryString);
            request.AddParameter("start", start.ToString("yyyy-MM-dd"), ParameterType.QueryString);
            request.AddParameter("end", end.ToString("yyyy-MM-dd"), ParameterType.QueryString);
            request.AddParameter("interval", GetEnumDescription(interval));
            var dataContainer = Execute<TradierHistoryDataContainer>(request, TradierApiRequestType.Data, "history");
            return dataContainer.Data;
        }

        /// <summary>
        /// Get the current market status
        /// </summary>
        public TradierMarketStatus GetMarketStatus()
        {
            var request = new RestRequest("markets/clock", Method.GET);
            return Execute<TradierMarketStatus>(request, TradierApiRequestType.Data, "clock");
        }

        /// <summary>
        /// Get the list of days status for this calendar month, year:
        /// </summary>
        public List<TradierCalendarDay> GetMarketCalendar(int month, int year)
        {
            var request = new RestRequest("markets/calendar", Method.GET);
            request.AddParameter("month", month.ToString());
            request.AddParameter("year", year.ToString());
            var calendarContainer = Execute<TradierCalendarStatus>(request, TradierApiRequestType.Data, "calendar");
            return calendarContainer.Days.Days;
        }

        /// <summary>
        /// Get the list of days status for this calendar month, year:
        /// </summary>
        public List<TradierSearchResult> Search(string query, bool includeIndexes = true)
        {
            var request = new RestRequest("markets/search", Method.GET);
            request.AddParameter("q", query);
            request.AddParameter("indexes", includeIndexes.ToString());
            var searchContainer = Execute<TradierSearchContainer>(request, TradierApiRequestType.Data, "securities");
            return searchContainer.Results;
        }

        /// <summary>
        /// Get the list of days status for this calendar month, year:
        /// </summary>
        public List<TradierSearchResult> LookUpSymbol(string query, bool includeIndexes = true)
        {
            var request = new RestRequest("markets/lookup", Method.GET);
            request.AddParameter("q", query);
            request.AddParameter("indexes", includeIndexes.ToString());
            var searchContainer = Execute<TradierSearchContainer>(request, TradierApiRequestType.Data, "securities");
            return searchContainer.Results;
        }

        /// <summary>
        /// Get the current market status
        /// </summary>
        public TradierStreamSession CreateStreamSession()
        {
            var request = new RestRequest("markets/events/session", Method.POST);
            return Execute<TradierStreamSession>(request, TradierApiRequestType.Data, "stream");
        }

        /// <summary>
        /// Connect to tradier API strea:
        /// </summary>
        /// <param name="symbols">symbol list</param>
        /// <returns></returns>
        public IEnumerable<TradierStreamData> Stream(List<string> symbols)
        {
            bool success;
            var symbolJoined = String.Join(",", symbols);
            var session = CreateStreamSession();

            if (session == null || session.SessionId == null || session.Url == null)
            {
                Log.Error("Tradier.Stream(): Failed to Created Stream Session", true);
                yield break;
            }
            Log.Trace("Tradier.Stream(): Created Stream Session Id: " + session.SessionId + " Url:" + session.Url, true);


            HttpWebRequest request;
            do
            {
                //Connect to URL:
                success = true;
                request = (HttpWebRequest) WebRequest.Create(session.Url);

                //Authenticate a request:
                request.Accept = "application/json";
                request.Headers.Add("Authorization", "Bearer " + AccessToken);

                //Add the desired data:
                var postData = "symbols=" + symbolJoined + "&sessionid=" + session.SessionId;
                
                var encodedData = Encoding.ASCII.GetBytes(postData);

                //Set post:
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = encodedData.Length;

                //Send request:
                try
                {
                    using (var postStream = request.GetRequestStream())
                    {
                        postStream.Write(encodedData, 0, encodedData.Length);
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err, "Failed to write session parameters to URL", true);
                    success = false;
                }
            }
            while (!success);

            //Get response as a stream:
            Log.Trace("Tradier.Stream(): Session Created, Reading Stream...", true);
            var response = (HttpWebResponse) request.GetResponse();
            var tradierStream = response.GetResponseStream();
            if (tradierStream == null)
            {
                yield break;
            }

            using (var sr = new StreamReader(tradierStream))
            using (var jsonReader = new JsonTextReader(sr))
            {
                var serializer = new JsonSerializer();
                jsonReader.SupportMultipleContent = true;

                // keep going until we fail to read more from the stream
                while (true)
                {
                    bool successfulRead;
                    try
                    {
                        //Read the jsonSocket in a safe manner: might close and so need handlers, but can't put handlers around a yield.
                        successfulRead = jsonReader.Read();
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        break;
                    }

                    if (!successfulRead)
                    {
                        // if we couldn't get a successful read just end the enumerable
                        yield break;
                    }

                    //Have a Tradier JSON Object:
                    TradierStreamData tsd = null;
                    try
                    {
                        tsd = serializer.Deserialize<TradierStreamData>(jsonReader);
                    }
                    catch (Exception err)
                    {
                        // Do nothing for now. Can come back later to fix. Errors are from Tradier not properly json encoding values E.g. "NaN" string.
                        Log.Error(err);
                    }

                    // don't yield garbage, just wait for the next one
                    if (tsd != null)
                    {
                        yield return tsd;
                    }
                }
            }
        }

        /// <summary>
        /// Convert the C# Enums back to the Tradier API Equivalent:
        /// </summary>
        private string GetEnumDescription(Enum value)
        {
            // Get the Description attribute value for the enum value
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (EnumMemberAttribute[]) fi.GetCustomAttributes(typeof (EnumMemberAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].Value;
            }
            else
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Get the rype inside the nested root:
        /// </summary>
        private T DeserializeRemoveRoot<T>(string json, string rootName)
        {
            var obj = default(T);

            try
            {
                //Dynamic deserialization:
                dynamic dynDeserialized = JsonConvert.DeserializeObject(json);
                obj = JsonConvert.DeserializeObject<T>(dynDeserialized[rootName].ToString());
            }
            catch (Exception err)
            {
                Log.Error(err, "RootName: " + rootName);
            }

            return obj;
        }

        /// <summary>
        /// Event invocator for the SessionRefreshed event
        /// </summary>
        protected virtual void OnSessionRefreshed(TokenResponse e)
        {
            var handler = SessionRefreshed;
            if (handler != null) handler(this, e);
        }

        #endregion

        #region IBrokerage implementation

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get { return _issuedAt + _lifeSpan > DateTime.Now; }
        }

        /// <summary>
        /// Gets all open orders on the account. 
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<Order> GetOpenOrders()
        {
            var orders = new List<Order>();
            var openOrders = GetIntradayAndPendingOrders().Where(OrderIsOpen);
            
            foreach (var openOrder in openOrders)
            {
                // make sure our internal collection is up to date as well
                UpdateCachedOpenOrder(openOrder.Id, openOrder);
                orders.Add(ConvertOrder(openOrder));
            }

            return orders;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdings = GetPositions().Select(ConvertHolding).Where(x => x.Quantity != 0).ToList();
            var symbols = holdings.Select(x => x.Symbol.Value).ToList();
            var quotes = GetQuotes(symbols).ToDictionary(x => x.Symbol);
            foreach (var holding in holdings)
            {
                TradierQuote quote;
                if (quotes.TryGetValue(holding.Symbol.Value, out quote))
                {
                    holding.MarketPrice = quote.Last;
                }
            }
            return holdings;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            return new List<Cash>
            {
                new Cash("USD", GetBalanceDetails(_accountID).TotalCash, 1.0m)
            };
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            Log.Trace("TradierBrokerage.PlaceOrder(): " + order);

            if (_cancelledQcOrderIDs.Contains(order.Id))
            {
                Log.Trace("TradierBrokerage.PlaceOrder(): Cancelled Order: " + order.Id + " - " + order);
                return false;
            }

            // before doing anything, verify only one outstanding order per symbol
            var cachedOpenOrder = _cachedOpenOrdersByTradierOrderID.FirstOrDefault(x => x.Value.Order.Symbol == order.Symbol.Value).Value;
            if (cachedOpenOrder != null)
            {
                var qcOrder = _orderProvider.GetOrderByBrokerageId(cachedOpenOrder.Order.Id);
                if (qcOrder == null)
                {
                    // clean up our mess, this should never be encountered.
                    TradierCachedOpenOrder tradierOrder;
                    Log.Error("TradierBrokerage.PlaceOrder(): Unable to locate existing QC Order when verifying single outstanding order per symbol.");
                    _cachedOpenOrdersByTradierOrderID.TryRemove(cachedOpenOrder.Order.Id, out tradierOrder);
                }
                // if the qc order is still listed as open, then we have an issue, attempt to cancel it before placing this new order
                else if (qcOrder.Status.IsOpen())
                {
                    // let the world know what we're doing
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "OneOrderPerSymbol",
                        "Tradier Brokerage currently only supports one outstanding order per symbol. Canceled old order: " + qcOrder.Id)
                        );

                    // cancel the open order and clear out any contingents
                    ContingentOrderQueue contingent;
                    _contingentOrdersByQCOrderID.TryRemove(qcOrder.Id, out contingent);
                    // don't worry about the response here, if it couldn't be canceled it was
                    // more than likely already filled, either way we'll trust we're clean to proceed
                    // with this new order
                    CancelOrder(qcOrder);
                }
            }


            // tradier supports market on open by allowing placement of market orders after hours
            // however, tradier does not support market on close orders, so we need to simulate it
            // we'll place the market order at 3:59:45 PM, this allows 15 seconds for process and fill
            if (order.Type == OrderType.MarketOnClose && DateTime.Now < DateTime.Today.Add(new TimeSpan(12+3, 59, 40))) // stop this behavior at 3:59:40
            {
                // just recall this PlaceOrder function so it can go through the normal path
                Timer t = null;
                t = new Timer(state =>
                {
                    PlaceOrder(order);
                    // be sure to dispose of this
                    t.Dispose();
                });

                // Figure how much time until 3:59:45
                var now = DateTime.Now;
                var placeOrderTime = now.Date.Add(new TimeSpan(12+3, 59, 45));

                // set timer for delta between now and when we want it to execute
                int milliseconds = (int)((placeOrderTime - now).TotalMilliseconds);
                t.Change(milliseconds, Timeout.Infinite);
                // even though 't' goes out of scope here, the internal scheduler (TimerQueue) maintains a reference
            }

            var holdingQuantity = _securityProvider.GetHoldingsQuantity(order.Symbol);

            var orderRequest = new TradierPlaceOrderRequest(order, TradierOrderClass.Equity,  holdingQuantity);

            // do we need to split the order into two pieces?
            bool crossesZero = OrderCrossesZero(order);
            if (crossesZero)
            {
                // first we need an order to close out the current position
                var firstOrderQuantity = -holdingQuantity;
                var secondOrderQuantity = order.Quantity - firstOrderQuantity;

                orderRequest.Quantity = Math.Abs(firstOrderQuantity);

                // we actually can't place this order until the closingOrder is filled
                // create another order for the rest, but we'll convert the order type to not be a stop
                // but a market or a limit order
                var restOfOrder = new TradierPlaceOrderRequest(order, TradierOrderClass.Equity, 0) {Quantity = Math.Abs(secondOrderQuantity)};
                restOfOrder.ConvertStopOrderTypes();

                _contingentOrdersByQCOrderID.AddOrUpdate(order.Id, new ContingentOrderQueue(order, restOfOrder));

                // issue the first order to close the position
                var response = TradierPlaceOrder(orderRequest);
                bool success = response.Errors.Errors.IsNullOrEmpty();
                if (!success)
                {
                    // remove the contingent order if we weren't succesful in placing the first
                    ContingentOrderQueue contingent;
                    _contingentOrdersByQCOrderID.TryRemove(order.Id, out contingent);
                    return false;
                }

                var closingOrderID = response.Order.Id;
                order.BrokerId.Add(closingOrderID.ToString());
                return true;
            }
            else
            {
                var response = TradierPlaceOrder(orderRequest);
                if (!response.Errors.Errors.IsNullOrEmpty())
                {
                    return false;
                }
                order.BrokerId.Add(response.Order.Id.ToString());
                return true;
            }
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            Log.Trace("TradierBrokerage.UpdateOrder(): " + order);

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform an update
                Log.Trace("TradierBrokerage.UpdateOrder(): Unable to update order without BrokerId.");
                return false;
            }

            // there's only one active tradier order per qc order, find it
            var activeOrder = (
                from brokerId in order.BrokerId
                let id = long.Parse(brokerId)
                where _cachedOpenOrdersByTradierOrderID.ContainsKey(id)
                select _cachedOpenOrdersByTradierOrderID[id]
                ).SingleOrDefault();
            
            if (activeOrder == null)
            {
                Log.Trace("Unable to locate active Tradier order for QC order id: " + order.Id + " with Tradier ids: " + string.Join(", ", order.BrokerId));
                return false;
            }

            decimal quantity = activeOrder.Order.Quantity;

            // also sum up the contingent orders
            ContingentOrderQueue contingent;
            if (_contingentOrdersByQCOrderID.TryGetValue(order.Id, out contingent))
            {
                quantity = contingent.QCOrder.AbsoluteQuantity;
            }

            if (quantity != order.AbsoluteQuantity)
            {
                Log.Trace("TradierBrokerage.UpdateOrder(): Unable to update order quantity.");
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UpdateRejected", "Unable to modify Tradier order quantities."));
                return false;
            }

            // we only want to update the active order, and if successful, we'll update contingents as well in memory

            var orderType = ConvertOrderType(order.Type);
            var orderDuration = GetOrderDuration(order.Duration);
            var limitPrice = GetLimitPrice(order);
            var stopPrice = GetStopPrice(order);
            var response = ChangeOrder(_accountID, activeOrder.Order.Id,
                orderType,
                orderDuration,
                limitPrice,
                stopPrice
                );

            if (!response.Errors.Errors.IsNullOrEmpty())
            {
                string errors = string.Join(", ", response.Errors.Errors);
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UpdateFailed", "Failed to update Tradier order id: " + activeOrder.Order.Id + ". " + errors));
                return false;
            }
            
            // success
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0) {Status = OrderStatus.Submitted});

            // if we have contingents, update them as well
            if (contingent != null)
            {
                foreach (var orderRequest in contingent.Contingents)
                {
                    orderRequest.Type = orderType;
                    orderRequest.Duration = orderDuration;
                    orderRequest.Price = limitPrice;
                    orderRequest.Stop = stopPrice;
                    orderRequest.ConvertStopOrderTypes();
                }
            }

            return true;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace("TradierBrokerage.CancelOrder(): " + order);

            if (!order.BrokerId.Any())
            {
                Log.Trace("TradierBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            // remove any contingent orders
            ContingentOrderQueue contingent;
            _contingentOrdersByQCOrderID.TryRemove(order.Id, out contingent);

            // add this id to the cancelled list, this is to prevent resubmits of certain simulated order
            // types, such as market on close
            _cancelledQcOrderIDs.Add(order.Id);

            foreach (var orderID in order.BrokerId)
            {
                var id = long.Parse(orderID);
                var response = CancelOrder(_accountID, id);
                if (response == null)
                {
                    // this can happen if the order has already been filled
                    return false;
                }
                if (response.Errors.Errors.IsNullOrEmpty() && response.Order.Status == "ok")
                {
                    TradierCachedOpenOrder tradierOrder;
                    _cachedOpenOrdersByTradierOrderID.TryRemove(id, out tradierOrder);
                    const int orderFee = 0;
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Tradier Fill Event") { Status = OrderStatus.Canceled });
                }
            }

            return true;
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;
            RefreshSession();
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            // NOP - token will eventually expire
        }

        /// <summary>
        /// Event invocator for the Message event
        /// </summary>
        /// <param name="e">The error</param>
        protected override void OnMessage(BrokerageMessageEvent e)
        {
            var message = e;
            if (Exchange.DateTimeIsOpen(DateTime.Now) && ErrorsDuringMarketHours.Contains(e.Code))
            {
                // elevate this to an error
                message = new BrokerageMessageEvent(BrokerageMessageType.Error, e.Code, e.Message);
            }
            base.OnMessage(message);
        }

        private TradierOrderResponse TradierPlaceOrder(TradierPlaceOrderRequest order)
        {
            const TradierOrderClass classification = TradierOrderClass.Equity;

            string stopLimit = string.Empty;
            if (order.Price != 0 || order.Stop != 0)
            {
                stopLimit = string.Format(" at{0}{1}", 
                    order.Stop == 0 ? "" : " stop " + order.Stop, 
                    order.Price == 0 ? "" : " limit " + order.Price
                    );
            }

            Log.Trace(string.Format("TradierBrokerage.TradierPlaceOrder(): {0} to {1} {2} units of {3}{4}", 
                order.Type, order.Direction, order.Quantity, order.Symbol, stopLimit)
                );
 
            var response = PlaceOrder(_accountID, 
                order.Classification,
                order.Direction,
                order.Symbol,
                order.Quantity,
                order.Price,
                order.Stop,
                order.OptionSymbol,
                order.Type,
                order.Duration
                );

            // if no errors, add to our open orders collection
            if (response != null && response.Errors.Errors.IsNullOrEmpty())
            {
                // send the submitted event
                const int orderFee = 0;
                OnOrderEvent(new OrderEvent(order.QCOrder, DateTime.UtcNow, orderFee) { Status = OrderStatus.Submitted });

                // mark this in our open orders before we submit so it's gauranteed to be there when we poll for updates
                UpdateCachedOpenOrder(response.Order.Id, new TradierOrderDetailed
                {
                    Id = response.Order.Id,
                    Quantity = order.Quantity,
                    Status = TradierOrderStatus.Submitted,
                    Symbol = order.Symbol,
                    Type = order.Type,
                    TransactionDate = DateTime.Now,
                    AverageFillPrice = 0m,
                    Class = classification,
                    CreatedDate = DateTime.Now,
                    Direction = order.Direction,
                    Duration = order.Duration,
                    LastFillPrice = 0m,
                    LastFillQuantity = 0m,
                    Legs = new List<TradierOrderLeg>(),
                    NumberOfLegs = 0,
                    Price = order.Price,
                    QuantityExecuted = 0m,
                    RemainingQuantity = order.Quantity,
                    StopPrice = order.Stop
                });
            }
            else
            {
                // invalidate the order, bad request
                const int orderFee = 0;
                OnOrderEvent(new OrderEvent(order.QCOrder, DateTime.UtcNow, orderFee) { Status = OrderStatus.Invalid });

                string message = _previousResponseRaw;
                if (response != null && response.Errors != null && !response.Errors.Errors.IsNullOrEmpty())
                {
                    message = "Order " + order.QCOrder.Id + ": " + string.Join(Environment.NewLine, response.Errors.Errors);
                    if (string.IsNullOrEmpty(order.QCOrder.Tag))
                    {
                        order.QCOrder.Tag = message;
                    }
                }

                // send this error through to the console
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "OrderError", message));

                // if we weren't given a broker ID, make an async request to fetch it and set the broker ID property on the qc order
                if (response == null || response.Order == null || response.Order.Id == 0)
                {
                    Task.Run(() =>
                    {
                        var orders = GetIntradayAndPendingOrders()
                            .Where(x => x.Status == TradierOrderStatus.Rejected)
                            .Where(x => DateTime.UtcNow - x.TransactionDate < TimeSpan.FromSeconds(2));

                        var recentOrder = orders.OrderByDescending(x => x.TransactionDate).FirstOrDefault(x => x.Symbol == order.Symbol && x.Quantity == order.Quantity && x.Direction == order.Direction && x.Type == order.Type);
                        if (recentOrder == null)
                        {
                            // without this we're going to corrupt the algorithm state
                            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "OrderError", "Unable to resolve rejected Tradier order id for QC order: " + order.QCOrder.Id));
                            return;
                        }

                        order.QCOrder.BrokerId.Add(recentOrder.Id.ToString());
                        Log.Trace("TradierBrokerage.TradierPlaceOrder(): Successfully resolved missing order ID: " + recentOrder.Id);
                    });
                }
            }

            return response;
        }

        /// <summary>
        /// Checks for fill events by polling FetchOrders for pending orders and diffing against the last orders seen
        /// </summary>
        private void CheckForFills()
        {
            // reentrance guard
            if (!Monitor.TryEnter(_fillLock))
            {
                return;
            }

            try
            {
                var intradayAndPendingOrders = GetIntradayAndPendingOrders();
                if (intradayAndPendingOrders == null)
                {
                    Log.Error("TradierBrokerage.CheckForFills(): Returned null response!");
                    return;
                }

                var updatedOrders = intradayAndPendingOrders.ToDictionary(x => x.Id);

                // loop over our cache of orders looking for changes in status for fill quantities
                foreach (var cachedOrder in _cachedOpenOrdersByTradierOrderID)
                {
                    TradierOrder updatedOrder;
                    var hasUpdatedOrder = updatedOrders.TryGetValue(cachedOrder.Key, out updatedOrder);
                    if (hasUpdatedOrder)
                    {
                        // determine if the order has been updated and produce fills accordingly
                        ProcessPotentiallyUpdatedOrder(cachedOrder.Value, updatedOrder);

                        // if the order is still open, update the cached value
                        if (!OrderIsClosed(updatedOrder)) UpdateCachedOpenOrder(cachedOrder.Key, updatedOrder);
                        continue;
                    }

                    // if we made it here this may be a canceled order via another portal, so we need to figure this one 
                    // out with its own rest call, do this async to not block this thread
                    if (!_reentranceGuardByTradierOrderID.Add(cachedOrder.Key))
                    {
                        // we don't want to reenter this task, so we'll use a hashset to keep track of what orders are currently in there
                        continue;
                    }

                    var cachedOrderLocal = cachedOrder;
                    Task.Run(() =>
                    {
                        try
                        {
                            var updatedOrderLocal = GetOrder(cachedOrderLocal.Key);
                            if (updatedOrderLocal == null)
                            {
                                Log.Error(string.Format("TradierBrokerage.CheckForFills(): Unable to locate order {0} in cached open orders.", cachedOrderLocal.Key));
                                throw new Exception("TradierBrokerage.CheckForFills(): GetOrder() return null response");
                            }

                            UpdateCachedOpenOrder(cachedOrderLocal.Key, updatedOrderLocal);
                            ProcessPotentiallyUpdatedOrder(cachedOrderLocal.Value, updatedOrderLocal);
                        }
                        catch (Exception err)
                        {
                            Log.Error(err);
                            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "PendingOrderNotReturned",
                                "An error ocurred while trying to resolve fill events from Tradier orders: " + err));
                        }
                        finally
                        {
                            // signal that we've left the task
                            _reentranceGuardByTradierOrderID.Remove(cachedOrderLocal.Key);
                        }
                    });
                }

                // if we get order updates for orders we're unaware of we need to bail, this can corrupt the algorithm state
                var unknownOrderIDs = updatedOrders.Where(IsUnknownOrderID).ToHashSet(x => x.Key);
                unknownOrderIDs.ExceptWith(_verifiedUnknownTradierOrderIDs);
                var fireTask = unknownOrderIDs.Count != 0 && _unknownTradierOrderIDs.Count == 0;
                foreach (var unknownOrderID in unknownOrderIDs)
                {
                    _unknownTradierOrderIDs.Add(unknownOrderID);
                }

                if (fireTask)
                {
                    // wait a second and then check the order provider to see if we have these broker IDs, maybe they came in later (ex, symbol denied for short trading)
                    Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t =>
                    {
                        var localUnknownTradierOrderIDs = _unknownTradierOrderIDs.ToHashSet();
                        _unknownTradierOrderIDs.Clear();
                        try
                        {
                            // verify we don't have them in the order provider
                            Log.Trace("TradierBrokerage.CheckForFills(): Verifying missing brokerage IDs: " + string.Join(",", localUnknownTradierOrderIDs));
                            var orders = localUnknownTradierOrderIDs.Select(x => _orderProvider.GetOrderByBrokerageId(x)).Where(x => x != null);
                            var stillUnknownOrderIDs = localUnknownTradierOrderIDs.Where(x => !orders.Any(y => y.BrokerId.Contains(x.ToString()))).ToList();
                            if (stillUnknownOrderIDs.Count > 0)
                            {
                                // fetch all rejected intraday orders within the last minute, we're going to exclude rejected orders from the error condition
                                var recentOrders = GetIntradayAndPendingOrders().Where(x => x.Status == TradierOrderStatus.Rejected)
                                    .Where(x => DateTime.UtcNow - x.TransactionDate < TimeSpan.FromMinutes(1)).ToHashSet(x => x.Id);

                                // remove recently rejected orders, sometimes we'll get updates for these but we've already marked them as rejected
                                stillUnknownOrderIDs.RemoveAll(x => recentOrders.Contains(x));

                                if (stillUnknownOrderIDs.Count > 0)
                                {
                                    // if we still have unknown IDs then we've gotta bail on the algorithm
                                    var ids = string.Join(", ", stillUnknownOrderIDs);
                                    Log.Error("TradierBrokerage.CheckForFills(): Unable to verify all missing brokerage IDs: " + ids);
                                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "UnknownOrderId", "Received unknown Tradier order id(s): " + ids));
                                    return;
                                }
                            }
                            foreach (var unknownTradierOrderID in localUnknownTradierOrderIDs)
                            {
                                // add these to the verified list so we don't check them again
                                _verifiedUnknownTradierOrderIDs.Add(unknownTradierOrderID);
                            }
                            Log.Trace("TradierBrokerage.CheckForFills(): Verified all missing brokerage IDs.");
                        }
                        catch (Exception err)
                        {
                            // we need to recheck these order ids since we failed, so add them back to the set
                            foreach (var id in localUnknownTradierOrderIDs) _unknownTradierOrderIDs.Add(id);

                            Log.Error(err);
                            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "UnknownIdResolution", "An error ocurred while trying to resolve unknown Tradier order IDs: " + err));
                        }
                    });
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "CheckForFillsError", "An error ocurred while checking for fills: " + err));
            }
            finally
            {
                Monitor.Exit(_fillLock);
            }
        }

        private bool IsUnknownOrderID(KeyValuePair<long, TradierOrder> x)
        {
                // we don't have it in our local cache
            return !_cachedOpenOrdersByTradierOrderID.ContainsKey(x.Key)
                // the transaction happened after we initialized, make sure they're in the same time zone
                && x.Value.TransactionDate.ToUniversalTime() > _initializationDateTime.ToUniversalTime()
                // we don't have a record of it in our last 10k filled orders
                && !_filledTradierOrderIDs.Contains(x.Key);
        }

        private void ProcessPotentiallyUpdatedOrder(TradierCachedOpenOrder cachedOrder, TradierOrder updatedOrder)
        {
            // check for fills or status changes, for either fire a fill event
            if (updatedOrder.RemainingQuantity != cachedOrder.Order.RemainingQuantity
             || ConvertStatus(updatedOrder.Status) != ConvertStatus(cachedOrder.Order.Status))
            {
                var qcOrder = _orderProvider.GetOrderByBrokerageId(updatedOrder.Id);
                const int orderFee = 0;
                var fill = new OrderEvent(qcOrder, DateTime.UtcNow, orderFee, "Tradier Fill Event")
                {
                    Status = ConvertStatus(updatedOrder.Status),
                    // this is guaranteed to be wrong in the event we have multiple fills within our polling interval,
                    // we're able to partially cope with the fill quantity by diffing the previous info vs current info
                    // but the fill price will always be the most recent fill, so if we have two fills with 1/10 of a second
                    // we'll get the latter fill price, so for large orders this can lead to inconsistent state
                    FillPrice = updatedOrder.LastFillPrice,
                    FillQuantity = (int)(updatedOrder.QuantityExecuted - cachedOrder.Order.QuantityExecuted)
                };

                // flip the quantity on sell actions
                if (IsShort(updatedOrder.Direction))
                {
                    fill.FillQuantity *= -1;
                }

                if (!cachedOrder.EmittedOrderFee)
                {
                    cachedOrder.EmittedOrderFee = true;
                    var security = _securityProvider.GetSecurity(qcOrder.Symbol);
                    fill.OrderFee = security.FeeModel.GetOrderFee(security, qcOrder);
                }

                // if we filled the order and have another contingent order waiting, submit it
                ContingentOrderQueue contingent;
                if (fill.Status == OrderStatus.Filled && _contingentOrdersByQCOrderID.TryGetValue(qcOrder.Id, out contingent))
                {
                    // prevent submitting the contingent order multiple times
                    if (_contingentReentranceGuardByQCOrderID.Add(qcOrder.Id))
                    {
                        var order = contingent.Next();
                        if (order == null || contingent.Contingents.Count == 0)
                        {
                            // we've finished with this contingent order
                            _contingentOrdersByQCOrderID.TryRemove(qcOrder.Id, out contingent);
                        }
                        // fire this off in a task so we don't block this thread
                        if (order != null)
                        {
                            // if we have a contingent that needs to be submitted then we can't respect the 'Filled' state from the order
                            // because the QC order hasn't been technically filled yet, so mark it as 'PartiallyFilled'
                            fill.Status = OrderStatus.PartiallyFilled;

                            Task.Run(() =>
                            {
                                try
                                {
                                    Log.Trace("TradierBrokerage.SubmitContingentOrder(): Submitting contingent order for QC id: " + qcOrder.Id);

                                    var response = TradierPlaceOrder(order);
                                    if (response.Errors.Errors.IsNullOrEmpty())
                                    {
                                        // add the new brokerage id for retrieval later
                                        qcOrder.BrokerId.Add(response.Order.Id.ToString());
                                    }
                                    else
                                    {
                                        // if we failed to place this order I don't know what to do, we've filled the first part
                                        // and failed to place the second... strange. Should we invalidate the rest of the order??
                                        Log.Error("TradierBrokerage.SubmitContingentOrder(): Failed to submit contingent order.");
                                        var message = string.Format("{0} Failed submitting contingent order for QC id: {1} Filled Tradier Order id: {2}", qcOrder.Symbol, qcOrder.Id, updatedOrder.Id);
                                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ContingentOrderFailed", message));
                                        OnOrderEvent(new OrderEvent(qcOrder, DateTime.UtcNow, orderFee) { Status = OrderStatus.Canceled });
                                    }
                                }
                                catch (Exception err)
                                {
                                    Log.Error(err);
                                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "ContingentOrderError", "An error ocurred while trying to submit an Tradier contingent order: " + err));
                                    OnOrderEvent(new OrderEvent(qcOrder, DateTime.UtcNow, orderFee) { Status = OrderStatus.Canceled });
                                }
                                finally
                                {
                                    _contingentReentranceGuardByQCOrderID.Remove(qcOrder.Id);
                                }
                            });
                        }
                    }
                }

                OnOrderEvent(fill);
            }

            // remove from open orders since it's now closed
            if (OrderIsClosed(updatedOrder))
            {
                _filledTradierOrderIDs.Add(updatedOrder.Id);
                _cachedOpenOrdersByTradierOrderID.TryRemove(updatedOrder.Id, out cachedOrder);
            }
        }

        private void UpdateCachedOpenOrder(long key, TradierOrder updatedOrder)
        {
            TradierCachedOpenOrder cachedOpenOrder;
            if (_cachedOpenOrdersByTradierOrderID.TryGetValue(key, out cachedOpenOrder))
            {
                cachedOpenOrder.Order = updatedOrder;
            }
            else
            {
                _cachedOpenOrdersByTradierOrderID[key] = new TradierCachedOpenOrder(updatedOrder);
            }
        }

        #endregion

        #region Conversion routines

        /// <summary>
        /// Returns true if the specified order is considered open, otherwise false
        /// </summary>
        protected static bool OrderIsOpen(TradierOrder order)
        {
            return order.Status != TradierOrderStatus.Filled
                && order.Status != TradierOrderStatus.Canceled
                && order.Status != TradierOrderStatus.Expired
                && order.Status != TradierOrderStatus.Rejected;
        }

        /// <summary>
        /// Returns true if the specified order is considered close, otherwise false
        /// </summary>
        protected static bool OrderIsClosed(TradierOrder order)
        {
            return !OrderIsOpen(order);
        }

        /// <summary>
        /// Returns true if the specified tradier order direction represents a short position
        /// </summary>
        protected static bool IsShort(TradierOrderDirection direction)
        {
            switch (direction)
            {
                case TradierOrderDirection.Sell:
                case TradierOrderDirection.SellShort:
                case TradierOrderDirection.SellToOpen:
                case TradierOrderDirection.SellToClose:
                    return true;
                case TradierOrderDirection.Buy:
                case TradierOrderDirection.BuyToCover:
                case TradierOrderDirection.BuyToClose:
                case TradierOrderDirection.BuyToOpen:
                case TradierOrderDirection.None:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException("direction", direction, null);
            }
        }

        /// <summary>
        /// Converts the specified tradier order into a qc order.
        /// The 'task' will have a value if we needed to issue a rest call for the stop price, otherwise it will be null
        /// </summary>
        protected Order ConvertOrder(TradierOrder order)
        {
            Order qcOrder;
            switch (order.Type)
            {
                case TradierOrderType.Limit:
                    qcOrder = new LimitOrder {LimitPrice = order.Price};
                    break;
                case TradierOrderType.Market:
                    qcOrder = new MarketOrder();
                    break;
                case TradierOrderType.StopMarket:
                    qcOrder = new StopMarketOrder {StopPrice = GetOrder(order.Id).StopPrice};
                    break;
                case TradierOrderType.StopLimit:
                    qcOrder = new StopLimitOrder {LimitPrice = order.Price, StopPrice = GetOrder(order.Id).StopPrice};
                    break;
                
                //case TradierOrderType.Credit:
                //case TradierOrderType.Debit:
                //case TradierOrderType.Even:
                default:
                    throw new NotImplementedException("The Tradier order type " + order.Type + " is not implemented.");
            }
            qcOrder.Symbol = Symbol.Create(order.Symbol, SecurityType.Equity, Market.USA);
            qcOrder.Quantity = ConvertQuantity(order);
            qcOrder.Status = ConvertStatus(order.Status);
            qcOrder.BrokerId.Add(order.Id.ToString());
            //qcOrder.ContingentId =
            qcOrder.Duration = ConvertDuration(order.Duration);
            var orderByBrokerageId = _orderProvider.GetOrderByBrokerageId(order.Id);
            if (orderByBrokerageId != null)
            {
                qcOrder.Id = orderByBrokerageId.Id;
            }
            qcOrder.Time = order.TransactionDate;
            return qcOrder;
        }

        /// <summary>
        /// Converts the qc order type into a tradier order type
        /// </summary>
        protected TradierOrderType ConvertOrderType(OrderType type)
        {
            switch (type)
            {
                case OrderType.Market:
                case OrderType.MarketOnOpen:
                case OrderType.MarketOnClose:
                    return TradierOrderType.Market;

                case OrderType.Limit:
                    return TradierOrderType.Limit;

                case OrderType.StopMarket:
                    return TradierOrderType.StopMarket;

                case OrderType.StopLimit:
                    return TradierOrderType.StopLimit;

                default:
                    throw new ArgumentOutOfRangeException("type", type, null);
            }
        }

        /// <summary>
        /// Converts the tradier order duration into a qc order duration
        /// </summary>
        protected OrderDuration ConvertDuration(TradierOrderDuration duration)
        {
            switch (duration)
            {
                case TradierOrderDuration.GTC:
                    return OrderDuration.GTC;
                case TradierOrderDuration.Day:
                    return (OrderDuration) 1; //.Day;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Converts the tradier order status into a qc order status
        /// </summary>
        protected OrderStatus ConvertStatus(TradierOrderStatus status)
        {
            switch (status)
            {
                case TradierOrderStatus.Filled:
                    return OrderStatus.Filled;

                case TradierOrderStatus.Canceled:
                    return OrderStatus.Canceled;

                case TradierOrderStatus.Open:
                case TradierOrderStatus.Submitted:
                    return OrderStatus.Submitted;

                case TradierOrderStatus.Expired:
                case TradierOrderStatus.Rejected:
                    return OrderStatus.Invalid;

                case TradierOrderStatus.Pending:
                    return OrderStatus.New;

                case TradierOrderStatus.PartiallyFilled:
                    return OrderStatus.PartiallyFilled;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Converts the qc order status into a tradier order status
        /// </summary>
        protected TradierOrderStatus ConvertStatus(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.New:
                    return TradierOrderStatus.Pending;

                case OrderStatus.Submitted:
                    return TradierOrderStatus.Submitted;
                    
                case OrderStatus.PartiallyFilled:
                    return TradierOrderStatus.PartiallyFilled;
                
                case OrderStatus.Filled:
                    return TradierOrderStatus.Filled;
                
                case OrderStatus.Canceled:
                    return TradierOrderStatus.Canceled;
                
                case OrderStatus.None:
                    return TradierOrderStatus.Pending;
                
                case OrderStatus.Invalid:
                    return TradierOrderStatus.Rejected;
                
                default:
                    throw new ArgumentOutOfRangeException("status", status, null);
            }
        }

        /// <summary>
        /// Converts the tradier order quantity into a qc quantity
        /// </summary>
        /// <remarks>
        /// Tradier quantities are always positive and use the direction to denote +/-, where as qc
        /// order quantities determine the direction
        /// </remarks>
        protected int ConvertQuantity(TradierOrder order)
        {
            switch (order.Direction)
            {
                case TradierOrderDirection.Buy:
                case TradierOrderDirection.BuyToCover:
                case TradierOrderDirection.BuyToClose:
                case TradierOrderDirection.BuyToOpen:
                    return (int) order.Quantity;

                case TradierOrderDirection.SellShort:
                case TradierOrderDirection.Sell:
                case TradierOrderDirection.SellToOpen:
                case TradierOrderDirection.SellToClose:
                    return -(int) order.Quantity;

                case TradierOrderDirection.None:
                    return 0;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Converts the tradier position into a qc holding
        /// </summary>
        protected Holding ConvertHolding(TradierPosition position)
        {
            return new Holding
            {
                Symbol = Symbol.Create(position.Symbol, SecurityType.Equity, Market.USA),
                Type = SecurityType.Equity,
                AveragePrice = position.CostBasis/position.Quantity,
                ConversionRate = 1.0m,
                CurrencySymbol = "$",
                MarketPrice = 0m, //--> GetAccountHoldings does a call to GetQuotes to fill this data in
                Quantity = position.Quantity
            };
        }

        /// <summary>
        /// Converts the QC order direction to a tradier order direction
        /// </summary>
        protected static TradierOrderDirection ConvertDirection(OrderDirection direction, decimal holdingQuantity)
        {
            // this mapping assumes we're dealing with equity types, options have different codes, buy_to_open and sell_to_close
            // Tradier has 4 types of orders for this: buy/sell/buy to cover and sell short.
            // 2 of the types are specifically for opening, lets handle those first:
            if (holdingQuantity == 0)
            {
                //Open a position: Both open long and open short:
                switch (direction)
                {
                    case OrderDirection.Buy:
                        return TradierOrderDirection.Buy;
                    case OrderDirection.Sell:
                        return TradierOrderDirection.SellShort;
                }
            }
            else if (holdingQuantity > 0)
            {
                switch (direction)
                {
                    case OrderDirection.Buy:
                        //Increasing existing position:
                        return TradierOrderDirection.Buy;
                    case OrderDirection.Sell:
                        //Reducing existing position:
                        return TradierOrderDirection.Sell;
                }
            }
            else if (holdingQuantity < 0)
            {
                switch (direction)
                {
                    case OrderDirection.Buy:
                        //Reducing existing short position:
                        return TradierOrderDirection.BuyToCover;
                    case OrderDirection.Sell:
                        //Increasing existing short position:
                        return TradierOrderDirection.SellShort;
                }
            }
            return TradierOrderDirection.None;
        }

        /// <summary>
        /// Determines whether or not the specified order will bring us across the zero line for holdings
        /// </summary>
        protected bool OrderCrossesZero(Order order)
        {
            var holdingQuantity = _securityProvider.GetHoldingsQuantity(order.Symbol);

            //We're reducing position or flipping:
            if (holdingQuantity > 0 && order.Quantity < 0)
            {
                if ((holdingQuantity + order.Quantity) < 0)
                {
                    //We dont have enough holdings so will cross through zero:
                    return true;
                }
            }
            else if (holdingQuantity < 0 && order.Quantity > 0)
            {
                if ((holdingQuantity + order.Quantity) > 0)
                {
                    //Crossed zero: need to split into 2 orders:
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Converts the qc order duration into a tradier order duration
        /// </summary>
        protected static TradierOrderDuration GetOrderDuration(OrderDuration duration)
        {
            switch (duration)
            {
                case OrderDuration.GTC:
                    return TradierOrderDuration.GTC;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Converts the qc order type into a tradier order type
        /// </summary>
        protected static TradierOrderType ConvertOrderType(Order order)
        {
            switch (order.Type)
            {
                case OrderType.Market:
                    return TradierOrderType.Market;
                case OrderType.Limit:
                    return TradierOrderType.Limit;
                case OrderType.StopMarket:
                    return TradierOrderType.StopMarket;
                case OrderType.StopLimit:
                    return TradierOrderType.StopLimit;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Gets the stop price used in API calls with tradier from the specified qc order instance
        /// </summary>
        protected static decimal GetStopPrice(Order order)
        {
            var stopm = order as StopMarketOrder;
            if (stopm != null)
            {
                return stopm.StopPrice;
            }
            var stopl = order as StopLimitOrder;
            if (stopl != null)
            {
                return stopl.StopPrice;
            }
            return 0;
        }

        /// <summary>
        /// Gets the limit price used in API calls with tradier from the specified qc order instance
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        protected static decimal GetLimitPrice(Order order)
        {
            var limit = order as LimitOrder;
            if (limit != null)
            {
                return limit.LimitPrice;
            }
            var stopl = order as StopLimitOrder;
            if (stopl != null)
            {
                return stopl.LimitPrice;
            }
            return 0;
        }

        #endregion

        private readonly HashSet<string> ErrorsDuringMarketHours = new HashSet<string>
        {
            "CheckForFillsError", "UnknownIdResolution", "ContingentOrderError", "NullResponse", "PendingOrderNotReturned"
        };

        class ContingentOrderQueue
        {
            /// <summary>
            /// The original order produced by the algorithm
            /// </summary>
            public readonly Order QCOrder;
            /// <summary>
            /// A queue of contingent orders to be placed after fills
            /// </summary>
            public readonly Queue<TradierPlaceOrderRequest> Contingents;

            public ContingentOrderQueue(Order qcOrder, params TradierPlaceOrderRequest[] contingents)
            {
                QCOrder = qcOrder;
                Contingents = new Queue<TradierPlaceOrderRequest>(contingents);
            }

            /// <summary>
            /// Dequeues the next contingent order, or null if there are none left
            /// </summary>
            public TradierPlaceOrderRequest Next()
            {
                if (Contingents.Count == 0)
                {
                    return null;
                }
                return Contingents.Dequeue();
            }
        }

        class TradierCachedOpenOrder
        {
            public bool EmittedOrderFee;
            public TradierOrder Order;

            public TradierCachedOpenOrder(TradierOrder order)
            {
                Order = order;
            }
        }

        class TradierPlaceOrderRequest
        {
            public Order QCOrder;
            public TradierOrderClass Classification;
            public TradierOrderDirection Direction;
            public string Symbol;
            public decimal Quantity;
            public decimal Price;
            public decimal Stop;
            public string OptionSymbol;
            public TradierOrderType Type;
            public TradierOrderDuration Duration;

            public TradierPlaceOrderRequest(Order order, TradierOrderClass classification, decimal holdingQuantity)
            {
                QCOrder = order;
                Classification = classification;
                Symbol = order.Symbol.Value;
                Direction = ConvertDirection(order.Direction, holdingQuantity);
                Quantity = Math.Abs(order.Quantity);
                Price = GetLimitPrice(order);
                Stop = GetStopPrice(order);
                Type = ConvertOrderType(order);
                Duration = GetOrderDuration(order.Duration);
            }

            public void ConvertStopOrderTypes()
            {
                // when this is a contingent order we'll want to convert stop types into their base order type
                if (Type == TradierOrderType.StopMarket)
                {
                    Type = TradierOrderType.Market;
                    Stop = 0m;
                }
                else if (Type == TradierOrderType.StopLimit)
                {
                    Type = TradierOrderType.Limit;
                    Stop = 0m;
                }
            }
        }
    }
}
