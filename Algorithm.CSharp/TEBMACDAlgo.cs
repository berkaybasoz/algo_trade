
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;


namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class TEBMACDAlgo : QCAlgorithm
    {

        private readonly List<OrderTicket> _openLimitOrders = new List<OrderTicket>();
        private readonly List<OrderTicket> _openMarketOrders = new List<OrderTicket>();

        private const string BIST_SYMBOL = "GARAN.E";
        private const int quantity = 1;
        private const decimal price = 9; //bar.Price;
        private Security security;
        private volatile bool isSent = false;


        //MACD fields
        private MovingAverageConvergenceDivergence macd;
        private DateTime previous;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(2500000);
            SetCash("TRY", 111000, 1);             //Set Strategy Cash 
            security = AddSecurity(SecurityType.Equity, BIST_SYMBOL, Resolution.Second);

            macd = MACD(BIST_SYMBOL, 9, 26, 9, MovingAverageType.Exponential, Resolution.Second);
        }

        public override void OnData(Slice data)
        {
            //if (!isSent)
            //    return;

           

            if (!data.Bars.ContainsKey(Symbol(BIST_SYMBOL)))
            { 
                return;
            }

            if (!security.Exchange.ExchangeOpen)
            {
                Debug("----------- security exchange is not open"); 
                return;
            }

            TradeBar bar = data.Bars[BIST_SYMBOL];
            if (bar.DataType != MarketDataType.TradeBar)
            {
                Debug("............ bar is not TradeBar.............. "); 
                return;
            }


            // only once per day if (previous.Date == Time.Date)

            //only once per second
            if (previous.ToString("yyyyMMdd-HH:mm:ss") == Time.ToString("yyyyMMdd-HH:mm:ss"))
            { 
                return;
            }

            if (!macd.IsReady)
            { 
                return;
            }

            var holding = Portfolio[BIST_SYMBOL];

            decimal signalDeltaPercent = (macd - macd.Signal) / macd.Fast;
            var tolerance = 0.0025m;

            bool isSent = false;
            // if our macd is greater than our signal, then let's go long
            if (holding.Quantity <= 0 && signalDeltaPercent > tolerance) // 0.01%
            {
                // longterm says buy as well
                SetHoldings(BIST_SYMBOL, 1.0);
                isSent = true;
            }
            // of our macd is less than our signal, then let's go short
            else if (holding.Quantity >= 0 && signalDeltaPercent < -tolerance)
            {
                Liquidate(BIST_SYMBOL);
                isSent = true;
            }

            // plot both lines
            Plot("MACD", macd, macd.Signal);
            Plot(BIST_SYMBOL, "Open", data[BIST_SYMBOL].Open);
            Plot(BIST_SYMBOL, macd.Fast, macd.Slow);

            previous = Time;

            if (isSent) { 
            decimal holdingQty = Portfolio.GetHoldingsQuantity(Symbol(BIST_SYMBOL));




            Debug("----------- Portfolio is invested? : " + Portfolio.Invested);
            Debug("----------- Portfolio.Count : " + Portfolio.Count);
            Debug("----------- Holding Quantity : " + holdingQty);
            Debug("----------- HasOpenPosition? : " + TradeBuilder.HasOpenPosition(Symbol(BIST_SYMBOL)));

            //int quantity = (int)Math.Floor(Portfolio.Cash / data["AAPL"].Close);





            DisplayBarInfo(bar);

            //OrderTicket marketTicket = PlaceMarketOrder(bar);

            //DisplayTicketInfo(marketTicket);

            //OrderTicket limitTicket = PlaceLimitOrder(bar);

            //DisplayTicketInfo(limitTicket);

            DisplayPortfolioInfo();

            //UpdateOrder(ticket);

            //CancelOrder(ticket);

            DisplayOpenOrders();

                
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



        private OrderTicket PlaceMarketOrder(TradeBar bar)
        {
            Debug("Sending market order");
            var marketTicket = MarketOrder(Symbol(BIST_SYMBOL), 10, asynchronous: false);
            if (marketTicket.Status != OrderStatus.Filled)
            {
                Log("Synchronous market order was not filled synchronously!");
                //Quit();
            }

            int code = marketTicket;  //int code = Order(BIST_SECURITY_NAME, 1,OrderType.Limit);//SetHoldings("SPY", 1); 

            _openMarketOrders.Add(marketTicket);


            if (code >= 0)
            {   //Notify.Email("myemail@gmail.com", "Test Subject", "Test Body: " + Time.ToString("u"), "Test Attachment");
                Debug("Market Order sent" + Time.ToString("HH:mm:ss.ffffff"));
            }
            else
            {
                Debug("Market Order send failed " + Time.ToString("HH:mm:ss.ffffff"));
                OrderError errorCode = (OrderError)code;
                DisplayOrderStatus(errorCode);
            }
            return marketTicket;

        }

        private OrderTicket PlaceLimitOrder(TradeBar bar)
        {

            Debug("Sending limit order");
            OrderTicket limitTicket = LimitOrder(Symbol(BIST_SYMBOL), quantity, price);      //int code = LimitOrder(BIST_SECURITY_NAME, 1, 9);        
            int code = limitTicket;  //int code = Order(BIST_SECURITY_NAME, 1,OrderType.Limit);//SetHoldings("SPY", 1); 

            _openLimitOrders.Add(limitTicket);


            if (code >= 0)
            {   //Notify.Email("myemail@gmail.com", "Test Subject", "Test Body: " + Time.ToString("u"), "Test Attachment");
                Debug("Limit Order sent" + Time.ToString("HH:mm:ss.ffffff"));
            }
            else
            {
                Debug("Limit Order send failed " + Time.ToString("HH:mm:ss.ffffff"));
                OrderError errorCode = (OrderError)code;
                DisplayOrderStatus(errorCode);
            }
            return limitTicket;

        }

        private void UpdateOrder(OrderTicket limitTicket)
        {
            int code = limitTicket;

            if (code >= 0)
            {
                Debug("Updating order");
                if (CheckOrdersForFill(limitTicket))
                {
                    Debug(limitTicket.OrderType + " order is already filled.");
                    //_openLimitOrders.Clear();
                    //return;
                }
                else
                {
                    var newLimitPrice = limitTicket.Get(OrderField.LimitPrice) + 0.1m;

                    Debug("Updating limits : " + newLimitPrice.ToString("0.00"));

                    var response = limitTicket.Update(new UpdateOrderFields
                    {
                        LimitPrice = newLimitPrice,
                        Tag = "Update #" + (limitTicket.UpdateRequests.Count + 1)
                    });

                    if (response.IsSuccess)
                    {
                        Log("Successfully updated async limit order: " + limitTicket.OrderId);
                    }
                    else
                    {
                        Log("Unable to updated async limit order: " + response.ErrorCode);
                    }

                }

            }
            else
            {
                Debug("Order update failed ");
                OrderError errorCode = (OrderError)code;
                DisplayOrderStatus(errorCode);
            }
        }

        private void CancelOrder(OrderTicket limitTicket)
        {
            int code = limitTicket;

            if (code >= 0)
            {
                Debug("Canceling order");
                if (CheckOrdersForFill(limitTicket))
                {
                    Debug(limitTicket.OrderType + " order is already filled.");
                    //_openLimitOrders.Clear();
                    //return;
                }
                else
                {

                    var response = limitTicket.Cancel("Attempt to cancel async order");
                    if (response.IsSuccess)
                    {
                        Log("Successfully canceled async limit order: " + limitTicket.OrderId);
                    }
                    else
                    {
                        Log("Unable to cancel async limit order: " + response.ErrorCode);
                    }
                }

            }
            else
            {
                Debug("Order cancel failed");
                OrderError errorCode = (OrderError)code;
                DisplayOrderStatus(errorCode);
            }
        }




        private void DisplayOpenOrders()
        {
            Debug("Transactions.OrdersCount : " + Transactions.OrdersCount);



            var openOrders = Transactions.GetOpenOrders(Symbol(BIST_SYMBOL));

            if (openOrders.Count != 0)
            {
                Debug("openOrders.Count : " + openOrders.Count);
                Debug("transactions.OrdersCount : " + Transactions.OrdersCount);
            }

            Debug("............ " + Time.ToString("HH:mm:ss.ffffff") + " Purchase End.............. ");
        }

        private void DisplayOrderStatus(OrderError errorCode)
        {
            if (errorCode != OrderError.None)
            {
                Debug(String.Format("{0} Order status {1} ", Time.ToString("HH:mm:ss.ffffff"), errorCode.ToString()));
            }

        }

        private void DisplayTicketInfo(OrderTicket ticket)
        {

            Debug("-------------------OrderTicket Info-------------------");

            Debug("OrderId: " + ticket.OrderId);
            Debug("Symbol: " + ticket.Symbol);
            Debug("OrderType: " + ticket.OrderType);
            Debug("Quantity: " + ticket.Quantity);
            Debug("QuantityFilled: " + ticket.QuantityFilled);
            Debug("SecurityType: " + ticket.SecurityType);
            Debug("Status: " + ticket.Status);
            Debug("AverageFillPrice: " + ticket.AverageFillPrice);

            //decimal price = ticket.AverageFillPrice;
            //Plot("Trade Plotter", "Asset Price", price); //Save price once per day
            //Plot("Trade Plotter", "Buy Orders", price); //Save price when place Buy order
            //Plot("Trade Plotter", "Sell Orders", price); //Save price when place Sell order
        }

        private void DisplayBarInfo(TradeBar bar)
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

        private void DisplayPortfolioInfo()
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

        private bool CheckOrdersForFill(OrderTicket ticket)
        {
            if (ticket.Status == OrderStatus.Filled)
            {
                return true;
            }

            return false;
        }
    }
}