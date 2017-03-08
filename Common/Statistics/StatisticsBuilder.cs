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
using System.Globalization;
using System.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// The <see cref="StatisticsBuilder"/> class creates summary and rolling statistics from trades, equity and benchmark points
    /// </summary>
    public static class StatisticsBuilder
    {
        /// <summary>
        /// Generates the statistics and returns the results
        /// </summary>
        /// <param name="trades">The list of closed trades</param>
        /// <param name="profitLoss">Trade record of profits and losses</param>
        /// <param name="pointsEquity">The list of daily equity values</param>
        /// <param name="pointsPerformance">The list of algorithm performance values</param>
        /// <param name="pointsBenchmark">The list of benchmark values</param>
        /// <param name="startingCapital">The algorithm starting capital</param>
        /// <param name="totalFees">The total fees</param>
        /// <param name="totalTransactions">The total number of transactions</param>
        /// <returns>Returns a <see cref="StatisticsResults"/> object</returns>
        public static StatisticsResults Generate(
            List<Trade> trades, 
            SortedDictionary<DateTime, decimal> profitLoss,
            List<ChartPoint> pointsEquity, 
            List<ChartPoint> pointsPerformance, 
            List<ChartPoint> pointsBenchmark, 
            decimal startingCapital, 
            decimal totalFees, 
            int totalTransactions)
        {
            var equity = ChartPointToDictionary(pointsEquity);

            var firstDate = equity.Keys.FirstOrDefault().Date;
            var lastDate = equity.Keys.LastOrDefault().Date;

            var totalPerformance = GetAlgorithmPerformance(firstDate, lastDate, trades, profitLoss, equity, pointsPerformance, pointsBenchmark, startingCapital);
            var rollingPerformances = GetRollingPerformances(firstDate, lastDate, trades, profitLoss, equity, pointsPerformance, pointsBenchmark, startingCapital);
            var summary = GetSummary(totalPerformance, totalFees, totalTransactions);

            return new StatisticsResults(totalPerformance, rollingPerformances, summary);
        }

        /// <summary>
        /// Returns the performance of the algorithm in the specified date range
        /// </summary>
        /// <param name="fromDate">The initial date of the range</param>
        /// <param name="toDate">The final date of the range</param>
        /// <param name="trades">The list of closed trades</param>
        /// <param name="profitLoss">Trade record of profits and losses</param>
        /// <param name="equity">The list of daily equity values</param>
        /// <param name="pointsPerformance">The list of algorithm performance values</param>
        /// <param name="pointsBenchmark">The list of benchmark values</param>
        /// <param name="startingCapital">The algorithm starting capital</param>
        /// <returns>The algorithm performance</returns>
        private static AlgorithmPerformance GetAlgorithmPerformance(
            DateTime fromDate, 
            DateTime toDate, 
            List<Trade> trades, 
            SortedDictionary<DateTime, decimal> profitLoss, 
            SortedDictionary<DateTime, decimal> equity, 
            List<ChartPoint> pointsPerformance, 
            List<ChartPoint> pointsBenchmark, 
            decimal startingCapital)
        {
            var periodTrades = trades.Where(x => x.ExitTime.Date >= fromDate && x.ExitTime < toDate.AddDays(1)).ToList();
            var periodProfitLoss = new SortedDictionary<DateTime, decimal>(profitLoss.Where(x => x.Key >= fromDate && x.Key.Date < toDate.AddDays(1)).ToDictionary(x => x.Key, y => y.Value));
            var periodEquity = new SortedDictionary<DateTime, decimal>(equity.Where(x => x.Key.Date >= fromDate && x.Key.Date < toDate.AddDays(1)).ToDictionary(x => x.Key, y => y.Value));

            var listPerformance = new List<double>();
            var performance = ChartPointToDictionary(pointsPerformance, fromDate, toDate);
            performance.Values.ToList().ForEach(i => listPerformance.Add((double)(i / 100)));

            var benchmark = ChartPointToDictionary(pointsBenchmark, fromDate, toDate);
            var listBenchmark = CreateBenchmarkDifferences(benchmark, periodEquity);
            EnsureSameLength(listPerformance, listBenchmark);

            var runningCapital = equity.Count == periodEquity.Count ? startingCapital : periodEquity.Values.FirstOrDefault();

            return new AlgorithmPerformance(periodTrades, periodProfitLoss, periodEquity, listPerformance, listBenchmark, runningCapital);
        }

        /// <summary>
        /// Returns the rolling performances of the algorithm
        /// </summary>
        /// <param name="firstDate">The first date of the total period</param>
        /// <param name="lastDate">The last date of the total period</param>
        /// <param name="trades">The list of closed trades</param>
        /// <param name="profitLoss">Trade record of profits and losses</param>
        /// <param name="equity">The list of daily equity values</param>
        /// <param name="pointsPerformance">The list of algorithm performance values</param>
        /// <param name="pointsBenchmark">The list of benchmark values</param>
        /// <param name="startingCapital">The algorithm starting capital</param>
        /// <returns>A dictionary with the rolling performances</returns>
        private static Dictionary<string, AlgorithmPerformance> GetRollingPerformances(
            DateTime firstDate, 
            DateTime lastDate, 
            List<Trade> trades, 
            SortedDictionary<DateTime, decimal> profitLoss, 
            SortedDictionary<DateTime, decimal> equity, 
            List<ChartPoint> pointsPerformance, 
            List<ChartPoint> pointsBenchmark, 
            decimal startingCapital)
        {
            var rollingPerformances = new Dictionary<string, AlgorithmPerformance>();
            
            var monthPeriods = new[] { 1, 3, 6, 12 };
            foreach (var monthPeriod in monthPeriods)
            {
                var ranges = GetPeriodRanges(monthPeriod, firstDate, lastDate);

                foreach (var period in ranges)
                {
                    var key = "M" + monthPeriod + "_" + period.EndDate.ToString("yyyyMMdd");
                    var periodPerformance = GetAlgorithmPerformance(period.StartDate, period.EndDate, trades, profitLoss, equity, pointsPerformance, pointsBenchmark, startingCapital);
                    rollingPerformances[key] = periodPerformance;
                }
            }

            return rollingPerformances;
        }

        /// <summary>
        /// Returns a summary of the algorithm performance as a dictionary
        /// </summary>
        private static Dictionary<string, string> GetSummary(AlgorithmPerformance totalPerformance, decimal totalFees, int totalTransactions)
        {
            return new Dictionary<string, string> 
            { 
                { "Total Trades", totalTransactions.ToString(CultureInfo.InvariantCulture) },
                { "Average Win", Math.Round(totalPerformance.PortfolioStatistics.AverageWinRate * 100, 2) + "%"  },
                { "Average Loss", Math.Round(totalPerformance.PortfolioStatistics.AverageLossRate * 100, 2) + "%" },
                { "Compounding Annual Return", Math.Round(totalPerformance.PortfolioStatistics.CompoundingAnnualReturn * 100, 3) + "%" },
                { "Drawdown", (Math.Round(totalPerformance.PortfolioStatistics.Drawdown * 100, 3)) + "%" },
                { "Expectancy", Math.Round(totalPerformance.PortfolioStatistics.Expectancy, 3).ToString(CultureInfo.InvariantCulture) },
                { "Net Profit", Math.Round(totalPerformance.PortfolioStatistics.TotalNetProfit * 100, 3) + "%"},
                { "Sharpe Ratio", Math.Round((double)totalPerformance.PortfolioStatistics.SharpeRatio, 3).ToString(CultureInfo.InvariantCulture) },
                { "Loss Rate", Math.Round(totalPerformance.PortfolioStatistics.LossRate * 100) + "%" },
                { "Win Rate", Math.Round(totalPerformance.PortfolioStatistics.WinRate * 100) + "%" }, 
                { "Profit-Loss Ratio", Math.Round(totalPerformance.PortfolioStatistics.ProfitLossRatio, 2).ToString(CultureInfo.InvariantCulture) },
                { "Alpha", Math.Round((double)totalPerformance.PortfolioStatistics.Alpha, 3).ToString(CultureInfo.InvariantCulture) },
                { "Beta", Math.Round((double)totalPerformance.PortfolioStatistics.Beta, 3).ToString(CultureInfo.InvariantCulture) },
                { "Annual Standard Deviation", Math.Round((double)totalPerformance.PortfolioStatistics.AnnualStandardDeviation, 3).ToString(CultureInfo.InvariantCulture) },
                { "Annual Variance", Math.Round((double)totalPerformance.PortfolioStatistics.AnnualVariance, 3).ToString(CultureInfo.InvariantCulture) },
                { "Information Ratio", Math.Round((double)totalPerformance.PortfolioStatistics.InformationRatio, 3).ToString(CultureInfo.InvariantCulture) },
                { "Tracking Error", Math.Round((double)totalPerformance.PortfolioStatistics.TrackingError, 3).ToString(CultureInfo.InvariantCulture) },
                { "Treynor Ratio", Math.Round((double)totalPerformance.PortfolioStatistics.TreynorRatio, 3).ToString(CultureInfo.InvariantCulture) },
                { "Total Fees", "$" + totalFees.ToString("0.00") }
            };
        }

        /// <summary>
        /// Helper class for rolling statistics
        /// </summary>
        private class PeriodRange
        {
            internal DateTime StartDate { get; set; }
            internal DateTime EndDate { get; set; }
        }

        // 
        /// <summary>
        /// Gets a list of date ranges for the requested monthly period
        /// </summary>
        /// <remarks>The first and last ranges created are partial periods</remarks>
        /// <param name="periodMonths">The number of months in the period (valid inputs are [1, 3, 6, 12])</param>
        /// <param name="firstDate">The first date of the total period</param>
        /// <param name="lastDate">The last date of the total period</param>
        /// <returns>The list of date ranges</returns>
        private static IEnumerable<PeriodRange> GetPeriodRanges(int periodMonths, DateTime firstDate, DateTime lastDate)
        {
            // get end dates
            var date = lastDate.Date;
            var endDates = new List<DateTime>();
            do
            {
                endDates.Add(date);
                date = new DateTime(date.Year, date.Month, 1).AddDays(-1);
            } while (date >= firstDate);

            // build period ranges
            var ranges = new List<PeriodRange> { new PeriodRange { StartDate = firstDate, EndDate = endDates[endDates.Count - 1] } };
            for (var i = endDates.Count - 2; i >= 0; i--)
            {
                var startDate = ranges[ranges.Count - 1].EndDate.AddDays(1).AddMonths(1 - periodMonths);
                if (startDate < firstDate) startDate = firstDate;

                ranges.Add(new PeriodRange
                {
                    StartDate = startDate,
                    EndDate = endDates[i]
                });
            }

            return ranges;
        }

        /// <summary>
        /// Convert the charting data into an equity array.
        /// </summary>
        /// <remarks>This is required to convert the equity plot into a usable form for the statistics calculation</remarks>
        /// <param name="points">ChartPoints Array</param>
        /// <param name="fromDate">An optional starting date</param>
        /// <param name="toDate">An optional ending date</param>
        /// <returns>SortedDictionary of the equity decimal values ordered in time</returns>
        private static SortedDictionary<DateTime, decimal> ChartPointToDictionary(IEnumerable<ChartPoint> points, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var dictionary = new SortedDictionary<DateTime, decimal>();

            foreach (var point in points)
            {
                var x = Time.UnixTimeStampToDateTime(point.x);

                if (fromDate != null && x.Date < fromDate) continue;
                if (toDate != null && x.Date >= ((DateTime)toDate).AddDays(1)) break;
                    
                dictionary[x] = point.y;
            }

            return dictionary;
        }

        /// <summary>
        /// Creates a list of benchmark differences for the period
        /// </summary>
        /// <param name="benchmark">The benchmark values</param>
        /// <param name="equity">The equity values</param>
        /// <returns>The list of benchmark differences</returns>
        private static List<double> CreateBenchmarkDifferences(SortedDictionary<DateTime, decimal> benchmark, SortedDictionary<DateTime, decimal> equity)
        {
            // to find the delta in benchmark for first day, we need to know the price at the opening
            // moment of the day, but since we cannot find this, we cannot find the first benchmark's delta,
            // so we pad it with Zero. If running a short backtest this will skew results, longer backtests
            // will not be affected much
            var listBenchmark = new List<double> { 0 };

            var minDate = equity.Keys.FirstOrDefault().AddDays(-1);
            var maxDate = equity.Keys.LastOrDefault();

            // Get benchmark performance array for same period:
            var dtPrevious = new DateTime();
            benchmark.Keys.ToList().ForEach(dt =>
            {
                if (dt >= minDate && dt < maxDate)
                {
                    decimal previous;
                    if (benchmark.TryGetValue(dtPrevious, out previous) && previous != 0)
                    {
                        var deltaBenchmark = (benchmark[dt] - previous) / previous;
                        listBenchmark.Add((double)deltaBenchmark);
                    }
                    else
                    {
                        listBenchmark.Add(0);
                    }
                    dtPrevious = dt;
                }
            });

            return listBenchmark;
        }

        /// <summary>
        /// Ensures the performance list and benchmark list have the same length, padding with trailing zeros
        /// </summary>
        /// <param name="listPerformance">The performance list</param>
        /// <param name="listBenchmark">The benchmark list</param>
        private static void EnsureSameLength(List<double> listPerformance, List<double> listBenchmark)
        {
            // THIS SHOULD NEVER HAPPEN --> But if it does, log it and fail silently.
            while (listPerformance.Count < listBenchmark.Count)
            {
                listPerformance.Add(0);
                Log.Trace("StatisticsBuilder.EnsureSameLength(): Padded Performance");
            }
            while (listPerformance.Count > listBenchmark.Count)
            {
                listBenchmark.Add(0);
                Log.Trace("StatisticsBuilder.EnsureSameLength(): Padded Benchmark");
            }
        }

    }
}
