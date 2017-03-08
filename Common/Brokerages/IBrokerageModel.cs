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
 *
*/

using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Models brokerage transactions, fees, and order
    /// </summary>
    public interface IBrokerageModel
    {
        /// <summary>
        /// Gets or sets the account type used by this model
        /// </summary>
        AccountType AccountType
        {
            get;
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message);

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested updated to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message);

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the 
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        bool CanExecuteOrder(Security security, Order order);

        /// <summary>
        /// Applies the split to the specified order ticket
        /// </summary>
        /// <param name="tickets">The open tickets matching the split event</param>
        /// <param name="split">The split event data</param>
        void ApplySplit(List<OrderTicket> tickets, Split split);

        /// <summary>
        /// Gets the brokerage's leverage for the specified security
        /// </summary>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        decimal GetLeverage(Security security);

        /// <summary>
        /// Gets a new fill model that represents this brokerage's fill behavior
        /// </summary>
        /// <param name="security">The security to get fill model for</param>
        /// <returns>The new fill model for this brokerage</returns>
        IFillModel GetFillModel(Security security);

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        IFeeModel GetFeeModel(Security security);

        /// <summary>
        /// Gets a new slippage model that represents this brokerage's fill slippage behavior
        /// </summary>
        /// <param name="security">The security to get a slippage model for</param>
        /// <returns>The new slippage model for this brokerage</returns>
        ISlippageModel GetSlippageModel(Security security);

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The settlement model for this brokerage</returns>
        ISettlementModel GetSettlementModel(Security security, AccountType accountType);
    }

    /// <summary>
    /// Provides factory method for creating an <see cref="IBrokerageModel"/> from the <see cref="BrokerageName"/> enum
    /// </summary>
    public static class BrokerageModel
    {
        /// <summary>
        /// Creates a new <see cref="IBrokerageModel"/> for the specified <see cref="BrokerageName"/>
        /// </summary>
        /// <param name="brokerage">The name of the brokerage</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The model for the specified brokerage</returns>
        public static IBrokerageModel Create(BrokerageName brokerage, AccountType accountType)
        {
            switch (brokerage)
            {
                case BrokerageName.Default:
                    return new DefaultBrokerageModel(accountType);

                case BrokerageName.InteractiveBrokersBrokerage:
                    return new InteractiveBrokersBrokerageModel(accountType);

                case BrokerageName.TradierBrokerage:
                    return new TradierBrokerageModel(accountType);
                    
                case BrokerageName.OandaBrokerage:
                    return new OandaBrokerageModel(accountType);
                    
                case BrokerageName.FxcmBrokerage:
                    return new FxcmBrokerageModel(accountType);
                case BrokerageName.TEB:
                    return new TEBBrokerageModel(accountType);
                default:
                    throw new ArgumentOutOfRangeException("brokerage", brokerage, null);
            }
        }
    }
}
