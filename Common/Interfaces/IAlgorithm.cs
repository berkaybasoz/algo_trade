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
using NodaTime;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Statistics;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Interface for QuantConnect algorithm implementations. All algorithms must implement these
    /// basic members to allow interaction with the Lean Backtesting Engine.
    /// </summary>
    public interface IAlgorithm
    {
        /// <summary>
        /// Data subscription manager controls the information and subscriptions the algorithms recieves.
        /// Subscription configurations can be added through the Subscription Manager.
        /// </summary>
        SubscriptionManager SubscriptionManager
        {
            get;
        }

        /// <summary>
        /// Security object collection class stores an array of objects representing representing each security/asset
        /// we have a subscription for.
        /// </summary>
        /// <remarks>It is an IDictionary implementation and can be indexed by symbol</remarks>
        SecurityManager Securities
        {
            get;
        }

        /// <summary>
        /// Gets the collection of universes for the algorithm
        /// </summary>
        UniverseManager UniverseManager
        {
            get;
        }

        /// <summary>
        /// Security portfolio management class provides wrapper and helper methods for the Security.Holdings class such as
        /// IsLong, IsShort, TotalProfit
        /// </summary>
        /// <remarks>Portfolio is a wrapper and helper class encapsulating the Securities[].Holdings objects</remarks>
        SecurityPortfolioManager Portfolio
        {
            get;
        }

        /// <summary>
        /// Security transaction manager class controls the store and processing of orders.
        /// </summary>
        /// <remarks>The orders and their associated events are accessible here. When a new OrderEvent is recieved the algorithm portfolio is updated.</remarks>
        SecurityTransactionManager Transactions
        {
            get;
        }

        /// <summary>
        /// Gets the brokerage model used to emulate a real brokerage
        /// </summary>
        IBrokerageModel BrokerageModel
        {
            get;
        }

        /// <summary>
        /// Gets the brokerage message handler used to decide what to do
        /// with each message sent from the brokerage
        /// </summary>
        IBrokerageMessageHandler BrokerageMessageHandler
        {
            get;
            set;
        }

        /// <summary>
        /// Notification manager for storing and processing live event messages
        /// </summary>
        NotificationManager Notify
        {
            get;
        }

        /// <summary>
        /// Gets schedule manager for adding/removing scheduled events
        /// </summary>
        ScheduleManager Schedule
        {
            get;
        }

        /// <summary>
        /// Gets or sets the history provider for the algorithm
        /// </summary>
        IHistoryProvider HistoryProvider
        {
            get; 
            set;
        }

        /// <summary>
        /// Gets or sets the current status of the algorithm
        /// </summary>
        AlgorithmStatus Status
        {
            get; 
            set;
        }

        /// <summary>
        /// Gets whether or not this algorithm is still warming up
        /// </summary>
        bool IsWarmingUp
        {
            get;
        }

        /// <summary>
        /// Public name for the algorithm.
        /// </summary>
        /// <remarks>Not currently used but preserved for API integrity</remarks>
        string Name
        {
            get;
        }

        /// <summary>
        /// Current date/time in the algorithm's local time zone
        /// </summary>
        DateTime Time
        {
            get;
        }

        /// <summary>
        /// Gets the time zone of the algorithm
        /// </summary>
        DateTimeZone TimeZone
        {
            get;
        }

        /// <summary>
        /// Current date/time in UTC.
        /// </summary>
        DateTime UtcTime
        {
            get;
        }

        /// <summary>
        /// Algorithm start date for backtesting, set by the SetStartDate methods.
        /// </summary>
        DateTime StartDate
        {
            get;
        }

        /// <summary>
        /// Get Requested Backtest End Date
        /// </summary>
        DateTime EndDate
        {
            get;
        }

        /// <summary>
        /// AlgorithmId for the backtest
        /// </summary>
        string AlgorithmId
        {
            get;
        }

        /// <summary>
        /// Algorithm is running on a live server.
        /// </summary>
        bool LiveMode
        {
            get;
        }

        /// <summary>
        /// Gets the subscription settings to be used when adding securities via universe selection
        /// </summary>
        UniverseSettings UniverseSettings
        {
            get;
        }

        /// <summary>
        /// Debug messages from the strategy:
        /// </summary>
        List<string> DebugMessages
        {
            get;
        }

        /// <summary>
        /// Error messages from the strategy:
        /// </summary>
        List<string> ErrorMessages
        {
            get;
        }

        /// <summary>
        /// Log messages from the strategy:
        /// </summary>
        List<string> LogMessages
        {
            get;
        }

        /// <summary>
        /// Gets the run time error from the algorithm, or null if none was encountered.
        /// </summary>
        Exception RunTimeError
        {
            get;
            set;
        }

        /// <summary>
        /// Customizable dynamic statistics displayed during live trading:
        /// </summary>
        Dictionary<string, string> RuntimeStatistics
        {
            get;
        }

        /// <summary>
        /// Gets the function used to define the benchmark. This function will return
        /// the value of the benchmark at a requested date/time
        /// </summary>
        IBenchmark Benchmark
        { 
            get;
        }

        /// <summary>
        /// Gets an instance that is to be used to initialize newly created securities.
        /// </summary>
        ISecurityInitializer SecurityInitializer
        {
            get;
        }

        /// <summary>
        /// Gets the Trade Builder to generate trades from executions
        /// </summary>
        TradeBuilder TradeBuilder
        {
            get;
        }

        /// <summary>
        /// Initialise the Algorithm and Prepare Required Data:
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called by setup handlers after Initialize and allows the algorithm a chance to organize
        /// the data gather in the Initialize method
        /// </summary>
        void PostInitialize();

        /// <summary>
        /// Gets the parameter with the specified name. If a parameter
        /// with the specified name does not exist, null is returned
        /// </summary>
        /// <param name="name">The name of the parameter to get</param>
        /// <returns>The value of the specified parameter, or null if not found</returns>
        string GetParameter(string name);

        /// <summary>
        /// Sets the parameters from the dictionary
        /// </summary>
        /// <param name="parameters">Dictionary containing the parameter names to values</param>
        void SetParameters(Dictionary<string, string> parameters);

        /// <summary>
        /// Sets the brokerage model used to resolve transaction models, settlement models,
        /// and brokerage specified ordering behaviors.
        /// </summary>
        /// <param name="brokerageModel">The brokerage model used to emulate the real
        /// brokerage</param>
        void SetBrokerageModel(IBrokerageModel brokerageModel);

        // <summary>
        // v1.0 Handler for Tick Events [DEPRECATED June-2014]
        // </summary>
        // <param name="ticks">Tick Data Packet</param>
        //void OnTick(Dictionary<string, List<Tick>> ticks);

        // <summary>
        // v1.0 Handler for TradeBar Events [DEPRECATED June-2014]
        // </summary>
        // <param name="tradebars">TradeBar Data Packet</param>
        //void OnTradeBar(Dictionary<string, TradeBar> tradebars);

        // <summary>
        // v2.0 Handler for Generic Data Events
        // </summary>
        //void OnData(Ticks ticks);
        //void OnData(TradeBars tradebars);

        /// <summary>
        /// v3.0 Handler for all data types
        /// </summary>
        /// <param name="slice">The current slice of data</param>
        void OnData(Slice slice);

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes"></param>
        void OnSecuritiesChanged(SecurityChanges changes);

        /// <summary>
        /// Send debug message
        /// </summary>
        /// <param name="message"></param>
        void Debug(string message);

        /// <summary>
        /// Save entry to the Log
        /// </summary>
        /// <param name="message">String message</param>
        void Log(string message);

        /// <summary>
        /// Send an error message for the algorithm
        /// </summary>
        /// <param name="message">String message</param>
        void Error(string message);

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        void OnMarginCall(List<SubmitOrderRequest> requests);

        /// <summary>
        /// Margin call warning event handler. This method is called when Portoflio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
        /// </summary>
        void OnMarginCallWarning();

        /// <summary>
        /// Call this method at the end of each day of data.
        /// </summary>
        void OnEndOfDay();

        /// <summary>
        /// Call this method at the end of each day of data.
        /// </summary>
        void OnEndOfDay(Symbol symbol);

        /// <summary>
        /// Call this event at the end of the algorithm running.
        /// </summary>
        void OnEndOfAlgorithm();

        /// <summary>
        /// EXPERTS ONLY:: [-!-Async Code-!-]
        /// New order event handler: on order status changes (filled, partially filled, cancelled etc).
        /// </summary>
        /// <param name="newEvent">Event information</param>
        void OnOrderEvent(OrderEvent newEvent);

        /// <summary>
        /// Brokerage message event handler. This method is called for all types of brokerage messages.
        /// </summary>
        void OnBrokerageMessage(BrokerageMessageEvent messageEvent);

        /// <summary>
        /// Brokerage disconnected event handler. This method is called when the brokerage connection is lost.
        /// </summary>
        void OnBrokerageDisconnect();

        /// <summary>
        /// Brokerage reconnected event handler. This method is called when the brokerage connection is restored after a disconnection.
        /// </summary>
        void OnBrokerageReconnect();

        /// <summary>
        /// Set the DateTime Frontier: This is the master time and is
        /// </summary>
        /// <param name="time"></param>
        void SetDateTime(DateTime time);

        /// <summary>
        /// Set the algorithm Id for this backtest or live run. This can be used to identify the order and equity records.
        /// </summary>
        /// <param name="algorithmId">unique 32 character identifier for backtest or live server</param>
        void SetAlgorithmId(string algorithmId);

        /// <summary>
        /// Set the algorithm as initialized and locked. No more cash or security changes.
        /// </summary>
        void SetLocked();

        /// <summary>
        /// Gets whether or not this algorithm has been locked and fully initialized
        /// </summary>
        bool GetLocked();

        /// <summary>
        /// Get the chart updates since the last request:
        /// </summary>
        /// <param name="clearChartData"></param>
        /// <returns>List of Chart Updates</returns>
        List<Chart> GetChartUpdates(bool clearChartData = false);

        /// <summary>
        /// Set a required SecurityType-symbol and resolution for algorithm
        /// </summary>
        /// <param name="securityType">SecurityType Enum: Equity, Commodity, FOREX or Future</param>
        /// <param name="symbol">Symbol Representation of the MarketType, e.g. AAPL</param>
        /// <param name="resolution">Resolution of the MarketType required: MarketData, Second or Minute</param>
        /// <param name="market">The market the requested security belongs to, such as 'usa' or 'fxcm'</param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice.</param>
        /// <param name="leverage">leverage for this security</param>
        /// <param name="extendedMarketHours">ExtendedMarketHours send in data from 4am - 8pm, not used for FOREX</param>
        Security AddSecurity(SecurityType securityType, string symbol, Resolution resolution, string market, bool fillDataForward, decimal leverage, bool extendedMarketHours);

        /// <summary>
        /// Set the starting capital for the strategy
        /// </summary>
        /// <param name="startingCash">decimal starting capital, default $100,000</param>
        void SetCash(decimal startingCash);

        /// <summary>
        /// Set the cash for the specified symbol
        /// </summary>
        /// <param name="symbol">The cash symbol to set</param>
        /// <param name="startingCash">Decimal cash value of portfolio</param>
        /// <param name="conversionRate">The current conversion rate for the</param>
        void SetCash(string symbol, decimal startingCash, decimal conversionRate);

        /// <summary>
        /// Liquidate your portfolio holdings:
        /// </summary>
        /// <param name="symbolToLiquidate">Specific asset to liquidate, defaults to all.</param>
        /// <returns>list of order ids</returns>
        List<int> Liquidate(Symbol symbolToLiquidate = null);

        /// <summary>
        /// Set live mode state of the algorithm run: Public setter for the algorithm property LiveMode.
        /// </summary>
        /// <param name="live">Bool live mode flag</param>
        void SetLiveMode(bool live);

        /// <summary>
        /// Sets <see cref="IsWarmingUp"/> to false to indicate this algorithm has finished its warm up
        /// </summary>
        void SetFinishedWarmingUp();

        /// <summary>
        /// Gets the date/time warmup should begin
        /// </summary>
        /// <returns></returns>
        IEnumerable<HistoryRequest> GetWarmupHistoryRequests();

        /// <summary>
        /// Set the maximum number of orders the algortihm is allowed to process.
        /// </summary>
        /// <param name="max">Maximum order count int</param>
        void SetMaximumOrders(int max);
    }
}
