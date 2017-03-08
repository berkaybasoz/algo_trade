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

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Represents different environments available for the REST API.
    /// </summary>
    public enum Environment
    {
        /// <summary>
        /// An environment purely for testing; it is not as fast, stable and reliable as the other environments (i.e. it can go down once in a while). 
        /// Market data returned from this environment is simulated (not real market data).
        /// </summary>
        Sandbox,

        /// <summary>
        /// A stable environment; recommended for testing with your fxTrade Practice account and your personal access token.
        /// </summary>
        Practice,

        /// <summary>
        /// A stable environment; recommended for production-ready code to execute with your fxTrade account and your personal access token.
        /// </summary>
        Trade
    }
}
