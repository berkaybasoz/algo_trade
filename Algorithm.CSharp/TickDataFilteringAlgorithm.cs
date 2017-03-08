﻿using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// Tick Filter Example
    /// </summary>
    public class TickDataFilteringAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialize the tick filtering example algorithm
        /// </summary>
        public override void Initialize()
        {
            SetCash(25000);
            SetStartDate(2014, 6, 1);
            SetEndDate(2014, 7, 1);
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Tick);

            //Add our custom data filter.
            Securities["SPY"].DataFilter = new ExchangeDataFilter(this);
        }

        /// <summary>
        /// Data arriving here will now be filtered.
        /// </summary>
        /// <param name="data">Ticks data array</param>
        public void OnData(Ticks data)
        {
            if (!data.ContainsKey("SPY")) return;
            var spyTickList = data["SPY"];

            //Ticks return a list of ticks this second
            foreach (var tick in spyTickList)
            {
                Log(tick.Exchange);
            }

            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
            }
        }
    }
    /// <summary>
    /// Exchange filter class 
    /// </summary>
    public class ExchangeDataFilter : ISecurityDataFilter
    {
        private IAlgorithm _algo;

        /// <summary>
        /// Save instance of the algorithm namespace
        /// </summary>
        /// <param name="algo"></param>
        public ExchangeDataFilter(IAlgorithm algo)
        {
            _algo = algo;
        }

        /// <summary>
        /// Global Market Short Codes and their full versions: (used in tick objects)
        /// https://github.com/QuantConnect/QCAlgorithm/blob/master/QuantConnect.Common/Global.cs
        /// </summary>
        public static class MarketCodesFilter
        {
            /// US Market Codes
            public static Dictionary<string, string> US = new Dictionary<string, string>() 
            {
                {"A", "American Stock Exchange"},
                {"B", "Boston Stock Exchange"},
                {"C", "National Stock Exchange"},
                {"D", "FINRA ADF"},
                {"I", "International Securities Exchange"},
                {"J", "Direct Edge A"},
                {"K", "Direct Edge X"},
                {"M", "Chicago Stock Exchange"},
                {"N", "New York Stock Exchange"},
                {"P", "Nyse Arca Exchange"},
                {"Q", "NASDAQ OMX"},
                {"T", "NASDAQ OMX"},
                {"U", "OTC Bulletin Board"},
                {"u", "Over-the-Counter trade in Non-NASDAQ issue"},
                {"W", "Chicago Board Options Exchange"},
                {"X", "Philadelphia Stock Exchange"},
                {"Y", "BATS Y-Exchange, Inc"},
                {"Z", "BATS Exchange, Inc"}
            };

            /// Canada Market Short Codes:
            public static Dictionary<string, string> Canada = new Dictionary<string, string>() 
            {
                {"T", "Toronto"},
                {"V", "Venture"}
            };

            /// <summary>
            /// Select allowed exchanges for this filter: e.g. top 4
            /// </summary>
            public static List<string> AllowedExchanges = new List<string>() { 
                "P",    //NYSE ARCA - SPY PRIMARY EXCHANGE
                        //https://www.google.com/finance?q=NYSEARCA%3ASPY&ei=XcA2VKCSLs228waMhYCIBg
            };
        }


        /// <summary>
        /// Filter out a tick from this vehicle, with this new data:
        /// </summary>
        /// <param name="data">New data packet:</param>
        /// <param name="asset">Vehicle of this filter.</param>
        public bool Filter(Security asset, BaseData data)
        {
            // TRUE -->  Accept Tick
            // FALSE --> Reject Tick
            var tick = data as Tick;

            // This is a tick bar
            if (tick != null)
            {
                if (tick.Exchange == "P") //MarketCodesFilter.AllowedExchanges.Contains()
                {
                    return true;
                }
            }

            //Only allow those exchanges through.
            return false;
        }

    }
}