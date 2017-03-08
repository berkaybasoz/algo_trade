using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
  
    public class TEBBasicAlgo3 : QCAlgorithm
    {
        public string securityName = "SPY";
        public readonly Resolution resolution = Resolution.Second;
        public readonly SecurityType securityType = SecurityType.Equity;
        //DateTime startDate = DateTime.ParseExact("20160208", "yyyyMMdd", CultureInfo.InvariantCulture);
        //DateTime endDate = DateTime.ParseExact("20160328", "yyyyMMdd", CultureInfo.InvariantCulture);

 
        public override void Initialize()
        {


            //SetStartDate(startDate);
            //SetEndDate(endDate);
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            //SetEndDate(DateTime.Today.AddDays(-1));
            SetCash(100000);             //Set Strategy Cash

            AddSecurity(securityType, securityName, resolution);


            //Securities[securityName].DataFilter = new CustomDataFilter();

            //var plotter = new Chart("Trade Plotter", ChartType.Overlay);
            //var assetPrice = new Series("Asset Price", SeriesType.Line);
            //var buyOrders = new Series("Buy Orders", SeriesType.Scatter);
            //var sellOrders = new Series("Sell Orders", SeriesType.Scatter);


            //plotter.AddSeries(assetPrice);
            //plotter.AddSeries(buyOrders);
            //plotter.AddSeries(sellOrders);

            //AddChart(plotter);
        }


        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {

            //foreach (string symbol in Securities.Keys)
            //{
            //    if (Securities[symbol].Holdings.UnrealizedProfit > 0)
            //    {
            //        Order(symbol, -Securities[symbol].Holdings.Quantity);
            //    }
            //}

            //if (Portfolio["SPY"].UnrealizedProfit > 0)
            //{
            //}
            if (!data.ContainsKey(securityName))
                return;

            TradeBar bar = data.Bars[securityName];

            if (!Portfolio.Invested)
            {
                //SetHoldings(securityName, 10);
                OrderTicket ticket = Order(securityName, 100);
                WriteTicketInfo(ticket);
                Debug("Purchased Stock");
                WriteBarInfo(bar);
                WritePortfolioInfo();



            }


        }

        public override void OnEndOfAlgorithm()
        {	//Ignore non-tradeable symbols
            if (Securities[securityName].Type == SecurityType.Base) 
                return;

            //if (Portfolio.Invested)
            //{
            //    Liquidate(securityName);
            //}

            //WritePortfolioInfo();

            base.OnEndOfAlgorithm();
        }

        private void WriteTicketInfo(OrderTicket ticket)
        {

            Debug("-------------------Order Info-------------------");
            Debug("OrderId: " + ticket.OrderId);
            Debug("OrderType: " + ticket.OrderType);
            Debug("Quantity: " + ticket.Quantity);
            Debug("QuantityFilled: " + ticket.QuantityFilled);
            Debug("SecurityType: " + ticket.SecurityType);
            Debug("Status: " + ticket.Status);
            Debug("AverageFillPrice: " + ticket.AverageFillPrice);

            decimal price = ticket.AverageFillPrice;
            Plot("Trade Plotter", "Asset Price", price); //Save price once per day
            Plot("Trade Plotter", "Buy Orders", price); //Save price when place Buy order
            Plot("Trade Plotter", "Sell Orders", price); //Save price when place Sell order
        }

        private void WriteBarInfo(TradeBar bar)
        {
            Debug("-------------------TradeBar Info-------------------");
            Debug("DataType: " + bar.DataType);
            Debug("EndTime: " + bar.EndTime);
            Debug("IsFillForward: " + bar.IsFillForward);
            Debug("Open: " + bar.Open);
            Debug("Close: " + bar.Close);
            Debug("High: " + bar.High);
            Debug("Low: " + bar.Low);
            Debug("Period: " + bar.Period);
            Debug("Price: " + bar.Price);
            Debug("Time: " + bar.Time);
            Debug("Volume: " + bar.Volume);
            Debug("Value: " + bar.Value);
        }

        private void WritePortfolioInfo()
        {
            Debug("-------------------Portfolio Info-------------------");
            Debug("Cash: " + Portfolio.Cash);
            Debug("MarginRemaining: " + Portfolio.MarginRemaining);
            Debug("TotalAbsoluteHoldingsCost: " + Portfolio.TotalAbsoluteHoldingsCost);
            Debug("TotalMarginUsed: " + Portfolio.TotalMarginUsed);
            Debug("TotalPortfolioValue: " + Portfolio.TotalPortfolioValue);
            Debug("TotalProfit: " + Portfolio.TotalProfit);
            Debug("TotalSaleVolume: " + Portfolio.TotalSaleVolume);
            Debug("TotalUnleveredAbsoluteHoldingsCost: " + Portfolio.TotalUnleveredAbsoluteHoldingsCost);
            Debug("TotalUnrealizedProfit: " + Portfolio.TotalUnrealizedProfit);
            Debug("UnsettledCash: " + Portfolio.UnsettledCash);
            Debug("UnsettledCashBook: " + Portfolio.UnsettledCashBook);
        }

    }

    public class CustomDataFilter : ISecurityDataFilter
    {
        /// <summary>
        /// Filter out a tick from this vehicle, with this new data:
        /// </summary>
        /// <param name="data">New data packet:</param>
        /// <param name="vehicle">Vehicle of this filter.</param>
        public bool Filter(Security asset, BaseData data)
        {
            // TRUE -->  Accept Tick
            // FALSE --> Reject Tick

            //Example:
            if (data.Time.DayOfWeek != DayOfWeek.Saturday)
            {
                return false;
            }

            return true;
        }
    }
}
