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

    public class TEBBasicAlgo4 : QCAlgorithm
    {
        public const string BIST_SECURITY_NAME = "GARAN.E";

        public override void Initialize()
        {
            SetCash(2500000);
            SetCash("TRY", 111000, 1);             //Set Strategy Cash 
            //AddSecurity(SecurityType.Equity, BIST_SECURITY_NAME, Resolution.Second);
           
            
            AddSecurity(SecurityType.Equity, BIST_SECURITY_NAME, Resolution.Tick);
            Debug(this.GetType().Name + " initialized");


            //Chart assets = new Chart("Assets", ChartType.Stacked);
            //assets.AddSeries(new Series(BIST_SECURITY_NAME, SeriesType.Candle));
            //AddChart(assets);
        }

        public override void OnData(Slice data)
        {
            TradeBar bar = data.Bars[BIST_SECURITY_NAME];
            if (bar.DataType == MarketDataType.Tick)
            {
                Log(String.Format("$$|1|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}",
                            bar.Time, bar.Symbol, bar.High, bar.Low, bar.Close, bar.Volume,
                            bar.Price, bar.Value, bar.EndTime, bar.DataType, bar.Period));

            }
            else
            {
                Log(String.Format("$$|1|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}",
                              bar.Time, bar.Symbol, bar.High, bar.Low, bar.Close, bar.Volume,
                              bar.Price, bar.Value, bar.EndTime, bar.DataType, bar.Period));


               // LimitOrder(BIST_SECURITY_NAME, -10, 12);
               // LimitOrder(BIST_SECURITY_NAME, 50, 12);

               // OrderTicket marketOrder = Order(BIST_SECURITY_NAME, 10000);

               //marketOrder.Cancel();



               // Plot("Limit Plot", BIST_SECURITY_NAME, bar.Close);
               // Plot("Assets", BIST_SECURITY_NAME, bar.Price);

               // Debug("Purchased Stock");
            }

        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {

            var order = Transactions.GetOrderById(orderEvent.OrderId);

            Debug(String.Format("{0}: {1}: {2} {3}", Time, order.Type, orderEvent.Status, orderEvent));

            if (orderEvent.Status == OrderStatus.Submitted)
            {
                Debug(String.Format("{0} : submitted: {1}", Time, Transactions.GetOrderById(orderEvent.OrderId)));
            }

            if (orderEvent.Status.IsOpen())
            {
                Debug(String.Format("{0} : open: {1}", Time, Transactions.GetOrderById(orderEvent.OrderId)));
            }
            if (orderEvent.Status.IsFill())
            {
                Debug(String.Format("{0} : fill: {1}", Time, Transactions.GetOrderById(orderEvent.OrderId)));
            }
            if (orderEvent.Status.IsClosed())
            {
                Debug(String.Format("{0} : closed: {1}", Time, Transactions.GetOrderById(orderEvent.OrderId)));
            }
        }

    }
}
