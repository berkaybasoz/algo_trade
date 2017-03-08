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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    public abstract class CommonIndicatorTests<T> 
        where T : BaseData, new()
    {
        [Test]
        public virtual void ComparesAgainstExternalData()
        {
            var indicator = CreateIndicator();
            RunTestIndicator(indicator);
        }

        [Test]
        public virtual void ComparesAgainstExternalDataAfterReset()
        {
            var indicator = CreateIndicator();
            RunTestIndicator(indicator);
            indicator.Reset();
            RunTestIndicator(indicator);
        }

        [Test]
        public virtual void ResetsProperly()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<IndicatorDataPoint>)
                TestHelper.TestIndicatorReset(indicator as IndicatorBase<IndicatorDataPoint>, TestFileName);
            else if (indicator is IndicatorBase<TradeBar>)
                TestHelper.TestIndicatorReset(indicator as IndicatorBase<TradeBar>, TestFileName);
            else
                throw new NotSupportedException("ResetsProperly: Unsupported indicator data type: " + typeof(T));
        }

        /// <summary>
        /// Executes a test of the specified indicator
        /// </summary>
        protected virtual void RunTestIndicator(IndicatorBase<T> indicator)
        {
            if (indicator is IndicatorBase<IndicatorDataPoint>)
                TestHelper.TestIndicator(indicator as IndicatorBase<IndicatorDataPoint>, TestFileName, TestColumnName, Assertion as Action<IndicatorBase<IndicatorDataPoint>, double>);
            else if (indicator is IndicatorBase<TradeBar>)
                TestHelper.TestIndicator(indicator as IndicatorBase<TradeBar>, TestFileName, TestColumnName, Assertion as Action<IndicatorBase<TradeBar>, double>);
            else
                throw new NotSupportedException("RunTestIndicator: Unsupported indicator data type: " + typeof(T));
        }

        /// <summary>
        /// Returns a custom assertion function, parameters are the indicator and the expected value from the file
        /// </summary>
        protected virtual Action<IndicatorBase<T>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double) indicator.Current.Value, 1e-3); }
        }

        /// <summary>
        /// Returns a new instance of the indicator to test
        /// </summary>
        protected abstract IndicatorBase<T> CreateIndicator();

        /// <summary>
        /// Returns the CSV file name containing test data for the indicator
        /// </summary>
        protected abstract string TestFileName { get; }

        /// <summary>
        /// Returns the name of the column of the CSV file corresponding to the precalculated data for the indicator
        /// </summary>
        protected abstract string TestColumnName { get; }
    }
}
