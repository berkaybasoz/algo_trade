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

    public class TEBChartTestAlgo : QCAlgorithm
    {
        public string securityName = "GARAN.E";
        public readonly Resolution resolution = Resolution.Second;
        public readonly SecurityType securityType = SecurityType.Equity;
        //DateTime startDate = DateTime.ParseExact("20160208", "yyyyMMdd", CultureInfo.InvariantCulture);
        //DateTime endDate = DateTime.ParseExact("20160328", "yyyyMMdd", CultureInfo.InvariantCulture);


        public override void Initialize()
        {



            SetCash(10000000);             //Set Strategy Cash

            AddSecurity(securityType, securityName, resolution);


      
            //#5 - Stock Plotter with Trades
            Chart plotter = new Chart("Plotter", ChartType.Overlay);
            plotter.AddSeries(new Series(securityName, SeriesType.Line));
            plotter.AddSeries(new Series("Buy", SeriesType.Scatter));
            plotter.AddSeries(new Series("Sell", SeriesType.Scatter));
            AddChart(plotter);

            //#6 - Asset Pricing, Stacked:
            Chart assets = new Chart("Assets", ChartType.Stacked);
        
            assets.AddSeries(new Series(securityName, SeriesType.Candle)); 
            AddChart(assets);   //Don't forget to add the chart to the algorithm.

            AddChart(plotter);
        }

        int i = 0;
        public override void OnData(Slice data)
        {
             
            if (!data.ContainsKey(securityName))
                return;

            TradeBar bar = data.Bars[securityName];
            WriteBarInfo(bar);
           
            Plot("Custom chart","Custom Series", i++);


            Plot("Plotter", securityName, bar.Price);

            Plot("Assets", securityName, bar.Price);
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

        
    }
 
}
