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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents common properties for a specific security, uniquely identified by market, symbol and security type
    /// </summary>
    public class SymbolProperties
    {
        /// <summary>
        /// The description of the security
        /// </summary>
        public string Description
        {
            get; 
            private set;
        }

        /// <summary>
        /// The quote currency of the security
        /// </summary>
        public string QuoteCurrency
        {
            get; 
            private set;
        }

        /// <summary>
        /// The contract multiplier for the security
        /// </summary>
        public decimal ContractMultiplier
        {
            get; 
            private set;
        }

        /// <summary>
        /// The pip size (tick size) for the security
        /// </summary>
        public decimal PipSize
        {
            get; 
            private set;
        }

        /// <summary>
        /// Creates an instance of the <see cref="SymbolProperties"/> class
        /// </summary>
        public SymbolProperties(string description, string quoteCurrency, decimal contractMultiplier, decimal pipSize)
        {
            Description = description;
            QuoteCurrency = quoteCurrency;
            ContractMultiplier = contractMultiplier;
            PipSize = pipSize;
        }

        /// <summary>
        /// Gets a default instance of the <see cref="SymbolProperties"/> class for the specified <paramref name="quoteCurrency"/>
        /// </summary>
        /// <param name="quoteCurrency">The quote currency of the symbol</param>
        /// <returns>A default instance of the<see cref="SymbolProperties"/> class</returns>
        public static SymbolProperties GetDefault(string quoteCurrency)
        {
            return new SymbolProperties("", quoteCurrency.ToUpper(), 1, 0.01m);
        }
    }
}
