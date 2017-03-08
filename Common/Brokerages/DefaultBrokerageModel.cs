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
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides a default implementation of <see cref="IBrokerageModel"/> that allows all orders and uses
    /// the default transaction models
    /// </summary>
    public class DefaultBrokerageModel : IBrokerageModel
    {
        /// <summary>
        /// The default markets for the backtesting brokerage
        /// </summary>
        public static readonly IReadOnlyDictionary<SecurityType, string> DefaultMarketMap = new Dictionary<SecurityType, string>
        {
            {SecurityType.Base, Market.USA},
            {SecurityType.Equity, Market.USA},
            {SecurityType.Option, Market.USA},
            {SecurityType.Forex, Market.FXCM},
            {SecurityType.Cfd, Market.FXCM}
        }.ToReadOnlyDictionary();

        /// <summary>
        /// Gets or sets the account type used by this model
        /// </summary>
        public virtual AccountType AccountType
        {
            get; 
            private set;
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public virtual IReadOnlyDictionary<SecurityType, string> DefaultMarkets
        {
            get { return DefaultMarketMap; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to 
        /// <see cref="QuantConnect.AccountType.Margin"/></param>
        public DefaultBrokerageModel(AccountType accountType = AccountType.Margin)
        {
            AccountType = accountType;
        }

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
        public virtual bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public virtual bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the 
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security">The security being traded</param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public virtual bool CanExecuteOrder(Security security, Order order)
        {
            return true;
        }

        /// <summary>
        /// Applies the split to the specified order ticket
        /// </summary>
        /// <remarks>
        /// This default implementation will update the orders to maintain a similar market value
        /// </remarks>
        /// <param name="tickets">The open tickets matching the split event</param>
        /// <param name="split">The split event data</param>
        public virtual void ApplySplit(List<OrderTicket> tickets, Split split)
        {
            // by default we'll just update the orders to have the same notional value
            var splitFactor = split.SplitFactor;
            tickets.ForEach(ticket => ticket.Update(new UpdateOrderFields
            {
                Quantity = (int?) (ticket.Quantity/splitFactor),
                LimitPrice = ticket.OrderType.IsLimitOrder() ? ticket.Get(OrderField.LimitPrice)*splitFactor : (decimal?) null,
                StopPrice = ticket.OrderType.IsStopOrder() ? ticket.Get(OrderField.StopPrice)*splitFactor : (decimal?) null
            }));
        }

        /// <summary>
        /// Gets the brokerage's leverage for the specified security
        /// </summary>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        public decimal GetLeverage(Security security)
        {
            switch (security.Type)
            {
                case SecurityType.Equity:
                    return 1m;

                case SecurityType.Forex:
                case SecurityType.Cfd:
                    return 50m;

                case SecurityType.Base:
                case SecurityType.Commodity:
                case SecurityType.Option:
                case SecurityType.Future:
                default:
                    return 1m;
            }
        }

        /// <summary>
        /// Gets a new fill model that represents this brokerage's fill behavior
        /// </summary>
        /// <param name="security">The security to get fill model for</param>
        /// <returns>The new fill model for this brokerage</returns>
        public virtual IFillModel GetFillModel(Security security)
        {
            return new ImmediateFillModel();
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public virtual IFeeModel GetFeeModel(Security security)
        {
            switch (security.Type)
            {
                case SecurityType.Base:
                    return new ConstantFeeModel(0m);

                case SecurityType.Forex:
                case SecurityType.Equity:
                    return new InteractiveBrokersFeeModel();

                case SecurityType.Commodity:
                case SecurityType.Option:
                case SecurityType.Future:
                case SecurityType.Cfd:
                default:
                    return new ConstantFeeModel(0m);
            }
        }

        /// <summary>
        /// Gets a new slippage model that represents this brokerage's fill slippage behavior
        /// </summary>
        /// <param name="security">The security to get a slippage model for</param>
        /// <returns>The new slippage model for this brokerage</returns>
        public virtual ISlippageModel GetSlippageModel(Security security)
        {
            switch (security.Type)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                    return new ConstantSlippageModel(0);

                case SecurityType.Forex:
                case SecurityType.Cfd:
                    return new SpreadSlippageModel();

                case SecurityType.Commodity:
                case SecurityType.Option:
                case SecurityType.Future:
                default:
                    return new ConstantSlippageModel(0);
            }
        }

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The settlement model for this brokerage</returns>
        public virtual ISettlementModel GetSettlementModel(Security security, AccountType accountType)
        {
            if (security.Type == SecurityType.Equity && accountType == AccountType.Cash)
                return new DelayedSettlementModel(Equity.DefaultSettlementDays, Equity.DefaultSettlementTime);
            
            return new ImmediateSettlementModel();
        }

    }
}