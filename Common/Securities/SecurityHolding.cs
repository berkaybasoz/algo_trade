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
using QuantConnect.Orders;

namespace QuantConnect.Securities 
{
    /// <summary>
    /// SecurityHolding is a base class for purchasing and holding a market item which manages the asset portfolio
    /// </summary>
    public class SecurityHolding
    {
        //Working Variables
        private decimal _averagePrice;
        private int     _quantity;
        private decimal _price;
        private decimal _totalSaleVolume;
        private decimal _profit;
        private decimal _lastTradeProfit;
        private decimal _totalFees;
        private readonly Security _security;

        /// <summary>
        /// Create a new holding class instance setting the initial properties to $0.
        /// </summary>
        /// <param name="security">The security being held</param>
        public SecurityHolding(Security security)
        {
            _security = security;
            //Total Sales Volume for the day
            _totalSaleVolume = 0;
            _lastTradeProfit = 0;
        }

        /// <summary>
        /// Average price of the security holdings.
        /// </summary>
        public decimal AveragePrice
        {
            get
            {
                return _averagePrice;
            }
        }

        /// <summary>
        /// Quantity of the security held.
        /// </summary>
        /// <remarks>Positive indicates long holdings, negative quantity indicates a short holding</remarks>
        /// <seealso cref="AbsoluteQuantity"/>
        public int Quantity
        {
            get
            {
                return _quantity;
            }
        }

        /// <summary>
        /// Symbol identifier of the underlying security.
        /// </summary>
        public Symbol Symbol
        {
            get
            {
                return _security.Symbol;
            }
        }

        /// <summary>
        /// The security type of the symbol
        /// </summary>
        public SecurityType Type
        {
            get
            {
                return _security.Type;
            }
        }

        /// <summary>
        /// Leverage of the underlying security.
        /// </summary>
        public virtual decimal Leverage
        {
            get
            {
                return _security.MarginModel.GetLeverage(_security);
            }
        }
        

        /// <summary>
        /// Acquisition cost of the security total holdings.
        /// </summary>
        public virtual decimal HoldingsCost 
        {
            get 
            {
                return AveragePrice * Convert.ToDecimal(Quantity) * _security.QuoteCurrency.ConversionRate * _security.SymbolProperties.ContractMultiplier;
            }
        }

        /// <summary>
        /// Unlevered Acquisition cost of the security total holdings.
        /// </summary>
        public virtual decimal UnleveredHoldingsCost
        {
            get { return HoldingsCost/Leverage; }
        }

        /// <summary>
        /// Current market price of the security.
        /// </summary>
        public virtual decimal Price
        {
            get
            {
                return _price;
            }
        }

        /// <summary>
        /// Absolute holdings cost for current holdings in units of the account's currency
        /// </summary>
        /// <seealso cref="HoldingsCost"/>
        public virtual decimal AbsoluteHoldingsCost 
        {
            get 
            {
                return Math.Abs(HoldingsCost);
            }
        }

        /// <summary>
        /// Unlevered absolute acquisition cost of the security total holdings.
        /// </summary>
        public virtual decimal UnleveredAbsoluteHoldingsCost
        {
            get
            {
                return Math.Abs(UnleveredHoldingsCost);
            }
        }

        /// <summary>
        /// Market value of our holdings.
        /// </summary>
        public virtual decimal HoldingsValue
        {
            get { return _price*Convert.ToDecimal(Quantity)*_security.QuoteCurrency.ConversionRate*_security.SymbolProperties.ContractMultiplier; }
        }

        /// <summary>
        /// Absolute of the market value of our holdings.
        /// </summary>
        /// <seealso cref="HoldingsValue"/>
        public virtual decimal AbsoluteHoldingsValue
        {
            get { return Math.Abs(HoldingsValue); }
        }

        /// <summary>
        /// Boolean flat indicating if we hold any of the security
        /// </summary>
        public virtual bool HoldStock 
        {
            get 
            {
                return (AbsoluteQuantity > 0);
            }
        }

        /// <summary>
        /// Boolean flat indicating if we hold any of the security
        /// </summary>
        /// <remarks>Alias of HoldStock</remarks>
        /// <seealso cref="HoldStock"/>
        public virtual bool Invested
        {
            get
            {
                return HoldStock;
            }
        }

        /// <summary>
        /// The total transaction volume for this security since the algorithm started.
        /// </summary>
        public virtual decimal TotalSaleVolume
        {
            get { return _totalSaleVolume; }
        }

        /// <summary>
        /// Total fees for this company since the algorithm started.
        /// </summary>
        public virtual decimal TotalFees
        {
            get { return _totalFees; }
        }

        /// <summary>
        /// Boolean flag indicating we have a net positive holding of the security.
        /// </summary>
        /// <seealso cref="IsShort"/>
        public virtual bool IsLong 
        {
            get 
            {
                return Quantity > 0;
            }
        }

        /// <summary>
        /// BBoolean flag indicating we have a net negative holding of the security.
        /// </summary>
        /// <seealso cref="IsLong"/>
        public virtual bool IsShort 
        {
            get 
            {
                return Quantity < 0;
            }
        }

        /// <summary>
        /// Absolute quantity of holdings of this security
        /// </summary>
        /// <seealso cref="Quantity"/>
        public virtual decimal AbsoluteQuantity 
        {
            get 
            {
                return Math.Abs(Quantity);
            }
        }

        /// <summary>
        /// Record of the closing profit from the last trade conducted.
        /// </summary>
        public virtual decimal LastTradeProfit 
        {
            get 
            {
                return _lastTradeProfit;
            }
        }

        /// <summary>
        /// Calculate the total profit for this security.
        /// </summary>
        /// <seealso cref="NetProfit"/>
        public virtual decimal Profit
        {
            get { return _profit; }
        }

        /// <summary>
        /// Return the net for this company measured by the profit less fees.
        /// </summary>
        /// <seealso cref="Profit"/>
        /// <seealso cref="TotalFees"/>
        public virtual decimal NetProfit 
        {
            get 
            {
                return Profit - TotalFees;
            }
        }

        /// <summary>
        /// Gets the unrealized profit as a percenage of holdings cost
        /// </summary>
        public decimal UnrealizedProfitPercent
        {
            get
            {
                if (AbsoluteHoldingsCost == 0) return 0m;
                return UnrealizedProfit/AbsoluteHoldingsCost;
            }
        }

        /// <summary>
        /// Unrealized profit of this security when absolute quantity held is more than zero.
        /// </summary>
        public virtual decimal UnrealizedProfit
        {
            get { return TotalCloseProfit(); }
        }

        /// <summary>
        /// Adds a fee to the running total of total fees.
        /// </summary>
        /// <param name="newFee"></param>
        public void AddNewFee(decimal newFee) 
        {
            _totalFees += newFee;
        }

        /// <summary>
        /// Adds a profit record to the running total of profit.
        /// </summary>
        /// <param name="profitLoss">The cash change in portfolio from closing a position</param>
        public void AddNewProfit(decimal profitLoss) 
        {
            _profit += profitLoss;
        }

        /// <summary>
        /// Adds a new sale value to the running total trading volume in terms of the account currency
        /// </summary>
        /// <param name="saleValue"></param>
        public void AddNewSale(decimal saleValue)
        {
            _totalSaleVolume += saleValue;
        }

        /// <summary>
        /// Set the last trade profit for this security from a Portfolio.ProcessFill call.
        /// </summary>
        /// <param name="lastTradeProfit">Value of the last trade profit</param>
        public void SetLastTradeProfit(decimal lastTradeProfit) 
        {
            _lastTradeProfit = lastTradeProfit;
        }
            
        /// <summary>
        /// Set the quantity of holdings and their average price after processing a portfolio fill.
        /// </summary>
        public virtual void SetHoldings(decimal averagePrice, int quantity) 
        {
            _averagePrice = averagePrice;
            _quantity = quantity;
        }

        /// <summary>
        /// Update local copy of closing price value.
        /// </summary>
        /// <param name="closingPrice">Price of the underlying asset to be used for calculating market price / portfolio value</param>
        public virtual void UpdateMarketPrice(decimal closingPrice)
        {
            _price = closingPrice;
        }

        /// <summary>
        /// Profit if we closed the holdings right now including the approximate fees.
        /// </summary>
        /// <remarks>Does not use the transaction model for market fills but should.</remarks>
        public virtual decimal TotalCloseProfit() 
        {
            if (AbsoluteQuantity == 0)
            {
                return 0;
            }

            // this is in the account currency
            var marketOrder = new MarketOrder(_security.Symbol, -Quantity, _security.LocalTime.ConvertToUtc(_security.Exchange.TimeZone));
            var orderFee = _security.FeeModel.GetOrderFee(_security, marketOrder);

            return (Price - AveragePrice)*Quantity*_security.QuoteCurrency.ConversionRate*_security.SymbolProperties.ContractMultiplier - orderFee;
        }
    }
}