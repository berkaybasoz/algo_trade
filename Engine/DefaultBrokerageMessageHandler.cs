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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Provides a default implementation o <see cref="IBrokerageMessageHandler"/> that will forward
    /// messages as follows:
    /// Information -> IResultHandler.Debug
    /// Warning     -> IResultHandler.Error &amp;&amp; IApi.SendUserEmail
    /// Error       -> IResultHandler.Error &amp;&amp; IAlgorithm.RunTimeError
    /// </summary>
    public class DefaultBrokerageMessageHandler : IBrokerageMessageHandler
    {
        private static readonly TimeSpan DefaultOpenThreshold = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DefaultInitialDelay = TimeSpan.FromMinutes(15);

        private volatile bool _connected;

        private readonly IApi _api;
        private readonly IAlgorithm _algorithm;
        private readonly IResultHandler _results;
        private readonly TimeSpan _openThreshold;
        private readonly AlgorithmNodePacket _job;
        private readonly TimeSpan _initialDelay;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageMessageHandler"/> class
        /// </summary>
        /// <param name="algorithm">The running algorithm</param>
        /// <param name="job">The job that produced the algorithm</param>
        /// <param name="results">The result handler for the algorithm</param>
        /// <param name="api">The api for the algorithm</param>
        /// <param name="initialDelay"></param>
        /// <param name="openThreshold">Defines how long before market open to re-check for brokerage reconnect message</param>
        public DefaultBrokerageMessageHandler(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler results, IApi api, TimeSpan? initialDelay = null, TimeSpan? openThreshold = null)
        {
            _api = api;
            _job = job;
            _results = results;
            _algorithm = algorithm;
            _connected = true;
            _openThreshold = openThreshold ?? DefaultOpenThreshold;
            _initialDelay = initialDelay ?? DefaultInitialDelay;
        }

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="message">The message to be handled</param>
        public void Handle(BrokerageMessageEvent message)
        {
            // based on message type dispatch to result handler
            switch (message.Type)
            {
                case BrokerageMessageType.Information:
                    _results.DebugMessage("Brokerage Info: " + message.Message);
                    break;
                
                case BrokerageMessageType.Warning:
                    _results.ErrorMessage("Brokerage Warning: " + message.Message);
                    _api.SendUserEmail(_job.AlgorithmId, "Brokerage Warning", message.Message);
                    break;

                case BrokerageMessageType.Error:
                    _results.ErrorMessage("Brokerage Error: " + message.Message);
                    _algorithm.RunTimeError = new Exception(message.Message);
                    break;

                case BrokerageMessageType.Disconnect:
                    _connected = false;
                    Log.Trace("DefaultBrokerageMessageHandler.Handle(): Disconnected.");

                    // check to see if any non-custom security exchanges are open within the next x minutes
                    var open = (from kvp in _algorithm.Securities
                                let security = kvp.Value
                                where security.Type != SecurityType.Base
                                let exchange = security.Exchange
                                let localTime = _algorithm.UtcTime.ConvertFromUtc(exchange.TimeZone)
                                where exchange.IsOpenDuringBar(localTime, localTime + _openThreshold, security.IsExtendedMarketHours)
                                select security).Any();

                    // if any are open then we need to kill the algorithm
                    if (open)
                    {
                        // wait 15 minutes before killing algorithm
                        StartCheckReconnected(_initialDelay, message);
                    }
                    else
                    {
                        Log.Trace("DefaultBrokerageMessageHandler.Handle(): Disconnect when exchanges are closed, checking back before exchange open.");

                        // if they aren't open, we'll need to check again a little bit before markets open
                        DateTime nextMarketOpenUtc;
                        if (_algorithm.Securities.Count != 0)
                        {
                            nextMarketOpenUtc = (from kvp in _algorithm.Securities
                                                 let security = kvp.Value
                                                 where security.Type != SecurityType.Base
                                                 let exchange = security.Exchange
                                                 let localTime = _algorithm.UtcTime.ConvertFromUtc(exchange.TimeZone)
                                                 let marketOpen = exchange.Hours.GetNextMarketOpen(localTime, security.IsExtendedMarketHours)
                                                 let marketOpenUtc = marketOpen.ConvertToUtc(exchange.TimeZone)
                                                 select marketOpenUtc).Min();
                        }
                        else
                        {
                            // if we have no securities just make next market open an hour from now
                            nextMarketOpenUtc = DateTime.UtcNow.AddHours(1);
                        }

                        var timeUntilNextMarketOpen = nextMarketOpenUtc - DateTime.UtcNow - _openThreshold;

                        // wake up 5 minutes before market open and check if we've reconnected
                        StartCheckReconnected(timeUntilNextMarketOpen, message);
                    }
                    break;

                case BrokerageMessageType.Reconnect:
                    _connected = true;
                    Log.Trace("DefaultBrokerageMessageHandler.Handle(): Reconnected.");

                    if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Cancel();
                    }
                    break;
            }
        }

        private void StartCheckReconnected(TimeSpan delay, BrokerageMessageEvent message)
        {
            _cancellationTokenSource = new CancellationTokenSource(delay);

            Task.Run(() =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }

                CheckReconnected(message);

            }, _cancellationTokenSource.Token);
        }

        private void CheckReconnected(BrokerageMessageEvent message)
        {
            if (!_connected)
            {
                Log.Error("DefaultBrokerageMessageHandler.Handle(): Still disconnected, goodbye.");
                _results.ErrorMessage("Brokerage Disconnect: " + message.Message);
                _algorithm.RunTimeError = new Exception(message.Message);
            }
        }
    }
}