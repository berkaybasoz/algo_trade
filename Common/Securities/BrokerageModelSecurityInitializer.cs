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

using QuantConnect.Brokerages;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="ISecurityInitializer"/> that initializes a security
    /// by settings the <see cref="Security.FillModel"/>, <see cref="Security.FeeModel"/>, 
    /// <see cref="Security.SlippageModel"/>, and the <see cref="Security.SettlementModel"/> properties
    /// </summary>
    public class BrokerageModelSecurityInitializer : ISecurityInitializer
    {
        private readonly IBrokerageModel _brokerageModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageModelSecurityInitializer"/> class
        /// for the specified algorithm
        /// </summary>
        /// <param name="brokerageModel">The brokerage model used to initialize the security models</param>
        public BrokerageModelSecurityInitializer(IBrokerageModel brokerageModel)
        {
            _brokerageModel = brokerageModel;
        }

        /// <summary>
        /// Initializes the specified security by setting up the models
        /// </summary>
        /// <param name="security">The security to be initialized</param>
        public virtual void Initialize(Security security)
        {
            // set leverage and models
            security.SetLeverage(_brokerageModel.GetLeverage(security));
            security.FillModel = _brokerageModel.GetFillModel(security);
            security.FeeModel = _brokerageModel.GetFeeModel(security);
            security.SlippageModel = _brokerageModel.GetSlippageModel(security);
            security.SettlementModel = _brokerageModel.GetSettlementModel(security, _brokerageModel.AccountType);
        }
    }
}
