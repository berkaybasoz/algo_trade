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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityTests
    {
        [Test]
        public void SimplePropertiesTests()
        {
            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            var config = CreateTradeBarConfig();
            var security = new Security(exchangeHours, config, new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));

            Assert.AreEqual(config, security.SubscriptionDataConfig);
            Assert.AreEqual(config.Symbol, security.Symbol);
            Assert.AreEqual(config.SecurityType, security.Type);
            Assert.AreEqual(config.Resolution, security.Resolution);
            Assert.AreEqual(config.FillDataForward, security.IsFillDataForward);
            Assert.AreEqual(exchangeHours, security.Exchange.Hours);
        }

        [Test]
        public void ConstructorTests()
        {
            var security = GetSecurity();

            Assert.IsNotNull(security.Exchange);
            Assert.IsInstanceOf<SecurityExchange>(security.Exchange);
            Assert.IsNotNull(security.Cache);
            Assert.IsInstanceOf<SecurityCache>(security.Cache);
            Assert.IsNotNull(security.PortfolioModel);
            Assert.IsInstanceOf<SecurityPortfolioModel>(security.PortfolioModel);
            Assert.IsNotNull(security.FillModel);
            Assert.IsInstanceOf<ImmediateFillModel>(security.FillModel);
            Assert.IsNotNull(security.PortfolioModel);
            Assert.IsInstanceOf<InteractiveBrokersFeeModel>(security.FeeModel);
            Assert.IsNotNull(security.SlippageModel);
            Assert.IsInstanceOf<SpreadSlippageModel>(security.SlippageModel);
            Assert.IsNotNull(security.SettlementModel);
            Assert.IsInstanceOf<ImmediateSettlementModel>(security.SettlementModel);
            Assert.IsNotNull(security.MarginModel);
            Assert.IsInstanceOf<SecurityMarginModel>(security.MarginModel);
            Assert.IsNotNull(security.DataFilter);
            Assert.IsInstanceOf<SecurityDataFilter>(security.DataFilter);
        }

        [Test]
        public void HoldingsTests()
        {
            var security = GetSecurity();
            
            // Long 100 stocks test
            security.Holdings.SetHoldings(100m, 100);
            
            Assert.AreEqual(100m, security.Holdings.AveragePrice);
            Assert.AreEqual(100, security.Holdings.Quantity);
            Assert.IsTrue(security.HoldStock);
            Assert.IsTrue(security.Invested);
            Assert.IsTrue(security.Holdings.IsLong);
            Assert.IsFalse(security.Holdings.IsShort);

            // Short 100 stocks test
            security.Holdings.SetHoldings(100m, -100);

            Assert.AreEqual(100m, security.Holdings.AveragePrice);
            Assert.AreEqual(-100, security.Holdings.Quantity);
            Assert.IsTrue(security.HoldStock);
            Assert.IsTrue(security.Invested);
            Assert.IsFalse(security.Holdings.IsLong);
            Assert.IsTrue(security.Holdings.IsShort);

            // Flat test
            security.Holdings.SetHoldings(100m, 0);

            Assert.AreEqual(100m, security.Holdings.AveragePrice);
            Assert.AreEqual(0, security.Holdings.Quantity);
            Assert.IsFalse(security.HoldStock);
            Assert.IsFalse(security.Invested);
            Assert.IsFalse(security.Holdings.IsLong);
            Assert.IsFalse(security.Holdings.IsShort);

        }

        [Test]
        public void UpdatingSecurityPriceTests()
        {
            var security = GetSecurity();

            // Update securuty price with a TradeBar
            security.SetMarketPrice(new TradeBar(DateTime.Now, Symbols.SPY, 101m, 103m, 100m, 102m, 100000));

            Assert.AreEqual(101m, security.Open);
            Assert.AreEqual(103m, security.High);
            Assert.AreEqual(100m, security.Low);
            Assert.AreEqual(102m, security.Close);
            Assert.AreEqual(100000, security.Volume);

            // Update security price with a tick with higher prices
            security.SetMarketPrice(new Tick(DateTime.Now, Symbols.SPY, 104m, 104m, 104m));
            Assert.AreEqual(104m, security.High);
            Assert.AreEqual(104m, security.Close);

            // Update security price with a tick with lower prices
            security.SetMarketPrice(new Tick(DateTime.Now, Symbols.SPY, 99m, 99m, 99m));
            Assert.AreEqual(99m, security.Low);
            Assert.AreEqual(99m, security.Close);
        }

        [Test]
        public void SetLeverageTest()
        {
            var security = GetSecurity();

            security.SetLeverage(4m);
            Assert.AreEqual(4m,security.Leverage);

            security.SetLeverage(5m);
            Assert.AreEqual(5m, security.Leverage);

            Assert.That(() => security.SetLeverage(0.1m),
                Throws.TypeOf<ArgumentException>().With.Message.EqualTo("Leverage must be greater than or equal to 1."));
        }
        private Security GetSecurity()
        {
            return new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), CreateTradeBarConfig(), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
        }

        private static SubscriptionDataConfig CreateTradeBarConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
