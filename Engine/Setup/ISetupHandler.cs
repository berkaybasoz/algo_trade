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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Interface to setup the algorithm. Pass in a raw algorithm, return one with portfolio, cash, etc already preset.
    /// </summary>
    [InheritedExport(typeof(ISetupHandler))]
    public interface ISetupHandler : IDisposable
    {
        /// <summary>
        /// Any errors from the initialization stored here:
        /// </summary>
        List<string> Errors 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Get the maximum runtime for this algorithm job.
        /// </summary>
        TimeSpan MaximumRuntime
        {
            get;
        }

        /// <summary>
        /// Algorithm starting capital for statistics calculations
        /// </summary>
        decimal StartingPortfolioValue
        {
            get;
        }

        /// <summary>
        /// Start date for analysis loops to search for data.
        /// </summary>
        DateTime StartingDate
        {
            get;
        }

        /// <summary>
        /// Maximum number of orders for the algorithm run -- applicable for backtests only.
        /// </summary>
        int MaxOrders
        {
            get;
        }

        /// <summary>
        /// Create a new instance of an algorithm from a physical dll path.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly's location</param>
        /// <param name="language">Language of the assembly.</param>
        /// <returns>A new instance of IAlgorithm, or throws an exception if there was an error</returns>
        IAlgorithm CreateAlgorithmInstance(string assemblyPath, Language language);

        /// <summary>
        /// Creates the brokerage as specified by the job packet
        /// </summary>
        /// <param name="algorithmNodePacket">Job packet</param>
        /// <param name="uninitializedAlgorithm">The algorithm instance before Initialize has been called</param>
        /// <returns>The brokerage instance, or throws if error creating instance</returns>
        IBrokerage CreateBrokerage(AlgorithmNodePacket algorithmNodePacket, IAlgorithm uninitializedAlgorithm);

        /// <summary>
        /// Primary entry point to setup a new algorithm
        /// </summary>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="brokerage">New brokerage output instance</param>
        /// <param name="job">Algorithm job task</param>
        /// <param name="resultHandler">The configured result handler</param>
        /// <param name="transactionHandler">The configurated transaction handler</param>
        /// <param name="realTimeHandler">The configured real time handler</param>
        /// <returns>True on successfully setting up the algorithm state, or false on error.</returns>
        bool Setup(IAlgorithm algorithm, IBrokerage brokerage, AlgorithmNodePacket job, IResultHandler resultHandler, ITransactionHandler transactionHandler, IRealTimeHandler realTimeHandler);
    }
}
