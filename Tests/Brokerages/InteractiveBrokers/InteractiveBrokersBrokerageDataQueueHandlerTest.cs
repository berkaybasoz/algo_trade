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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    [Ignore("These tests require the IBController and IB TraderWorkstation to be installed.")]
    public class InteractiveBrokersBrokerageDataQueueHandlerTest
    {
        [Test]
        public void GetsTickData()
        {
            InteractiveBrokersGatewayRunner.StartFromConfiguration();
            
            var ib = new InteractiveBrokersBrokerage(new OrderProvider(), new SecurityProvider());
            ib.Connect();

            ib.Subscribe(null, new List<Symbol> {Symbols.USDJPY, Symbols.EURGBP});
            
            Thread.Sleep(1000);

            for (int i = 0; i < 10; i++)
            {
                foreach (var tick in ib.GetNextTicks())
                {
                    Console.WriteLine("{0}: {1} - {2} @ {3}", tick.Time, tick.Symbol, tick.Price, ((Tick)tick).Quantity);
                }
            }

            InteractiveBrokersGatewayRunner.Stop();
        }
    }
}
