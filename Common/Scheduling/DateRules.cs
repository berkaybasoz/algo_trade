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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Helper class used to provide better syntax when defining date rules
    /// </summary>
    public class DateRules
    {
        private readonly SecurityManager _securities;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateRules"/> helper class
        /// </summary>
        /// <param name="securities">The security manager</param>
        public DateRules(SecurityManager securities)
        {
            _securities = securities;
        }

        /// <summary>
        /// Specifies an event should fire only on the specified day
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month</param>
        /// <param name="day">The day</param>
        /// <returns></returns>
        public IDateRule On(int year, int month, int day)
        {
            // make sure they're date objects
            var dates = new[] {new DateTime(year, month, day)};
            return new FuncDateRule(string.Join(",", dates.Select(x => x.ToShortDateString())), (start, end) => dates);
        }

        /// <summary>
        /// Specifies an event should fire only on the specified days
        /// </summary>
        /// <param name="dates">The dates the event should fire</param>
        /// <returns></returns>
        public IDateRule On(params DateTime[] dates)
        {
            // make sure they're date objects
            dates = dates.Select(x => x.Date).ToArray();
            return new FuncDateRule(string.Join(",", dates.Select(x => x.ToShortDateString())), (start, end) => dates);
        }

        /// <summary>
        /// Specifies an event should fire on each of the specified days of week
        /// </summary>
        /// <param name="days">The days the event shouls fire</param>
        /// <returns>A date rule that fires on every specified day of week</returns>
        public IDateRule Every(params DayOfWeek[] days)
        {
            var hash = days.ToHashSet();
            return new FuncDateRule(string.Join(",", days), (start, end) => Time.EachDay(start, end).Where(date => hash.Contains(date.DayOfWeek)));
        }

        /// <summary>
        /// Specifies an event should fire every day
        /// </summary>
        /// <returns>A date rule that fires every day</returns>
        public IDateRule EveryDay()
        {
            return new FuncDateRule("EveryDay", Time.EachDay);
        }

        /// <summary>
        /// Specifies an event should fire every day the symbol is trading
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine tradeable dates</param>
        /// <returns>A date rule that fires every day the specified symbol trades</returns>
        public IDateRule EveryDay(Symbol symbol)
        {
            var security = GetSecurity(symbol);
            return new FuncDateRule(symbol.ToString() + ": EveryDay", (start, end) => Time.EachTradeableDay(security, start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the first of each month
        /// </summary>
        /// <returns>A date rule that fires on the first of each month</returns>
        public IDateRule MonthStart()
        {
            return new FuncDateRule("MonthStart", (start, end) => MonthStartIterator(null, start, end));
        }

        /// <summary>
        /// Specifies an event should fire on the first tradeable date for the specified
        /// symbol of each month
        /// </summary>
        /// <param name="symbol">The symbol whose exchange is used to determine the first 
        /// tradeable date of the month</param>
        /// <returns>A date rule that fires on the first tradeable date for the specified security each month</returns>
        public IDateRule MonthStart(Symbol symbol)
        {
            return new FuncDateRule(symbol.ToString() + ": MonthStart", (start, end) => MonthStartIterator(GetSecurity(symbol), start, end));
        }

        /// <summary>
        /// Gets the security with the specified symbol, or throws an exception if the symbol is not found
        /// </summary>
        /// <param name="symbol">The security's symbol to search for</param>
        /// <returns>The security object matching the given symbol</returns>
        private Security GetSecurity(Symbol symbol)
        {
            Security security;
            if (!_securities.TryGetValue(symbol, out security))
            {
                throw new Exception(symbol.ToString() + " not found in portfolio. Request this data when initializing the algorithm.");
            }
            return security;
        }

        private static IEnumerable<DateTime> MonthStartIterator(Security security, DateTime start, DateTime end)
        {
            if (security == null)
            {
                foreach (var date in Time.EachDay(start, end))
                {
                    // fire on the first of each month
                    if (date.Day == 1) yield return date;
                }
                yield break;
            }

            // start a month back so we can properly resolve the first event (we may have passed it)
            var aMonthBeforeStart = start.AddMonths(-1);
            int lastMonth = aMonthBeforeStart.Month;
            foreach (var date in Time.EachTradeableDay(security, aMonthBeforeStart, end))
            {
                if (date.Month != lastMonth)
                {
                    if (date >= start)
                    {
                        // only emit if the date is on or after the start
                        // the date may be before here because we backed up a month
                        // to properly resolve the first tradeable date
                        yield return date;
                    }
                    lastMonth = date.Month;
                }
            }
        }
    }
}
