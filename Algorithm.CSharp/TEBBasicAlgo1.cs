
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
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
    public class TEBBasicAlgo1 : QCAlgorithm
    {
        private volatile bool isSent = false;
        //public const string SECURITY_NAME = "GARAN.E";
       
        //private readonly List<OrderTicket> _openLimitOrders = new List<OrderTicket>();

        public const string BIST_SECURITY_NAME = "GARAN.E";
        Security security;


        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(2500000);
            SetCash("TRY", 111000, 1);             //Set Strategy Cash 
            security = AddSecurity(SecurityType.Equity, BIST_SECURITY_NAME, Resolution.Second);


            Schedule.On(DateRules.On(2016, 05,11), TimeRules.At(11, 38), () =>
            {
                Log("SpecificTime: Fired at : " + Time);
            });

            // schedule an event to fire every trading day for a security
            // the time rule here tells it to fire 10 minutes after SPY's market open
            Schedule.On(DateRules.EveryDay(BIST_SECURITY_NAME), TimeRules.AfterMarketOpen(BIST_SECURITY_NAME, 10), () =>
            {
                Log("EveryDay.GARAN 10 min after open: Fired at: " + Time);
            });

            // schedule an event to fire every trading day for a security
            // the time rule here tells it to fire 10 minutes before SPY's market close
            Schedule.On(DateRules.EveryDay(BIST_SECURITY_NAME), TimeRules.BeforeMarketClose(BIST_SECURITY_NAME, 10), () =>
            {
                Log("EveryDay.GARAN 10 min before close: Fired at: " + Time);
            });

            // schedule an event to fire on certain days of the week
            Schedule.On(DateRules.Every(DayOfWeek.Monday, DayOfWeek.Friday), TimeRules.At(12, 0), () =>
            {
                Log("Mon/Fri at 12pm: Fired at: " + Time);
            });


            // the scheduling methods return the ScheduledEvent object which can be used for other things
            // here I set the event up to check the portfolio value every 10 minutes, and liquidate if we have too many losses
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromMinutes(10)), () =>
            {
                Log("EveryDay 10 min Fired at: " + Time);
                // if we have over 1000 dollars in unrealized losses, liquidate
                if (Portfolio.TotalUnrealizedProfit < -1000)
                {
                    Log("Liquidated due to unrealized losses at: " + Time);
                    Liquidate();
                }
            });

            //SetBrokerageModel(BrokerageName.TEB);

            //QuantConnect.Securities.Security sec = AddSecurity(SecurityType.Equity, SECURITY_NAME, Resolution.Second);

            //security.DataFilter = new CustomDataFilter(); //Securities[securityName].DataFilter = new CustomDataFilter();
            //sec.FeeModel = new QuantConnect.Orders.Fees.TEBFeeModel(0);
            //sec.FillModel = new QuantConnect.Orders.Fills.TEBFillModel();
            //sec.MarginModel = new QuantConnect.Securities.TEBSecurityMarginModel(1m);
            //sec.SlippageModel = new QuantConnect.Orders.Slippage.TEBSlippageModel(0m);
        }

        //Tick Data Event Handler
        public void OnData(Ticks data)
        {
            // A "Ticks" object is a string indexed, collection of LISTS. Each list 
            // contains all the ticks which occurred in that second. 
            //
            // In backtesting they are all timestamped to the previous second, in 
            // live trading they are realtime and stream in one at a time.
           
            if (!isSent)
                return;

            List<Tick> spyTicks = data[BIST_SECURITY_NAME];
            if (spyTicks[0].DataType != MarketDataType.Tick)
                return;
             
            if (!Portfolio.HoldStock)
            {
                int quantity = (int)Math.Floor(Portfolio.Cash / spyTicks[0].Price);
                Order(BIST_SECURITY_NAME, quantity);
                Debug("Purchased " + BIST_SECURITY_NAME + " on " + Time.ToShortDateString());
            }
        }

        public override void OnData(Slice data)
        {
            if (!isSent)
                return;

 
            if (!Portfolio.Invested)
            {
                if (!security.Exchange.ExchangeOpen)
                    return;

                if (!data.Bars.ContainsKey(BIST_SECURITY_NAME))
                    return;

                Debug("............ " + Time.ToString("HH:mm:ss.ffffff") + " Purchase Start.............. ");
                //int quantity = (int)Math.Floor(Portfolio.Cash / data["AAPL"].Close);


                TradeBar bar = data.Bars[BIST_SECURITY_NAME];
                if (bar.DataType != MarketDataType.TradeBar)
                    return;

                SendLimitOrder(bar);


                Debug("............ " + Time.ToString("HH:mm:ss.ffffff") + " Purchase End.............. ");
            }
            else
            {
                Debug("Portfolio.Invested " + Time.ToString("HH:mm:ss.ffffff"));

                Debug("Transactions.OrdersCount : " + Transactions.OrdersCount + Time.ToString("HH:mm:ss.ffffff"));
                
                var openOrders = Transactions.GetOpenOrders(BIST_SECURITY_NAME);
                
                if (openOrders.Count != 0)
                {
                    Debug("openOrders.Count : " + openOrders.Count + Time.ToString("HH:mm:ss.ffffff"));
                }

                //var quantity = CalculateOrderQuantity(BIST_SECURITY_NAME, -.5m);
              
                //MarketOrder("SPY", quantity, asynchronous: true); // async needed for partial fill market orders
            }
            //}
        }

        private void SendLimitOrder( TradeBar bar )
        {
             
            int quantity =  1;
            decimal price = 9; //bar.Price;


            OrderTicket ticket = LimitOrder(BIST_SECURITY_NAME, quantity, price);      //int code = LimitOrder(BIST_SECURITY_NAME, 1, 9);        
            int code = ticket;  //int code = Order(BIST_SECURITY_NAME, 1,OrderType.Limit);//SetHoldings("SPY", 1); 

            //_openLimitOrders.Add(ticket);

            if (code >= 0)
            {
                Debug("Purchased complete " + Time.ToString("HH:mm:ss.ffffff"));
                //Notify.Email("myemail@gmail.com", "Test Subject", "Test Body: " + Time.ToString("u"), "Test Attachment");

                WriteTicketInfo(ticket);

                WriteBarInfo(bar);
                WritePortfolioInfo();


                if (CheckOrdersForFill(ticket))
                {
                    Debug(ticket.OrderType + " order is filled.");
                    //_openLimitOrders.Clear();
                    //return;
                }
                else
                {
                    var newLimitPrice = ticket.Get(OrderField.LimitPrice) + 0.1m;

                    Debug("Updating limits : " + newLimitPrice.ToString("0.00"));

                    var response = ticket.Update(new UpdateOrderFields
                    {
                        LimitPrice = newLimitPrice,
                        Tag = "Update #" + (ticket.UpdateRequests.Count + 1)
                    });

                    if (response.IsSuccess)
                    {
                        Log("Successfully updated async limit order: " + ticket.OrderId);
                    }
                    else
                    {
                        Log("Unable to updated async limit order: " + response.ErrorCode);
                    }

                    response = ticket.Cancel("Attempt to cancel async order");
                    if (response.IsSuccess)
                    {
                        Log("Successfully canceled async limit order: " + ticket.OrderId);
                    }
                    else
                    {
                        Log("Unable to cancel async limit order: " + response.ErrorCode);
                    }
                }

            }
            else
            {
                Debug("Purchased failed " + Time.ToString("HH:mm:ss.ffffff"));
                OrderError errorCode = (OrderError)code;
                WriteOrderStatus(errorCode);
            }
        }

        public override void OnOrderEvent(OrderEvent fill)
        {
            var order = Transactions.GetOrderById(fill.OrderId);
            Debug(Time + " - " + order.Type + " - " + fill.Status + ":: " + fill);

            if (fill.Status == OrderStatus.Submitted)
            {
                Debug(String.Format("{0} : Submitted: {1}", Time, Transactions.GetOrderById(fill.OrderId)));
            }

            if (fill.Status.IsFill())
            {
                Debug(String.Format("{0} : Filled: {1}", Time, Transactions.GetOrderById(fill.OrderId)));
            }
        }

        /// <summary>
        /// Margin call event handler. This method is called right before the margin call orders are placed in the market.
        /// </summary>
        /// <param name="requests">The orders to be executed to bring this algorithm within margin limits</param>
        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            // this code gets called BEFORE the orders are placed, so we can try to liquidate some of our positions
            // before we get the margin call orders executed. We could also modify these orders by changing their
            // quantities
            foreach (var order in requests)
            {
                // liquidate an extra 10% each time we get a margin call to give us more padding
                var newQuantity = (int)(Math.Sign(order.Quantity) * order.Quantity * 1.1m);
                requests.Remove(order);
                requests.Add(new SubmitOrderRequest(order.OrderType, order.SecurityType, order.Symbol, newQuantity, order.StopPrice, order.LimitPrice, Time, "OnMarginCall"));
            }
        }

        /// <summary>
        /// Margin call warning event handler. This method is called when Portoflio.MarginRemaining is under 5% of your Portfolio.TotalPortfolioValue
        /// </summary>
        public override void OnMarginCallWarning()
        {
            // this code gets called when the margin remaining drops below 5% of our total portfolio value, it gives the algorithm
            // a chance to prevent a margin call from occurring

            // prevent margin calls by responding to the warning and increasing margin remaining
            var spyHoldings = Securities[BIST_SECURITY_NAME].Holdings.Quantity;
            var shares = (int)(-spyHoldings * .005m);
            Error(string.Format("{0} - OnMarginCallWarning(): Liquidating {1} shares of SPY to avoid margin call.", Time, shares));
            MarketOrder(BIST_SECURITY_NAME, shares);
        }

        public override void OnEndOfAlgorithm()
        {	//Ignore non-tradeable symbols
            if (Securities[BIST_SECURITY_NAME].Type == SecurityType.Base) return;

            //if (Portfolio.Invested)
            //{
            //    Liquidate(securityName);
            //}

            //WritePortfolioInfo();

            base.OnEndOfAlgorithm();
        }

        private void WriteOrderStatus(OrderError errorCode)
        {
            if (errorCode != OrderError.None)
            {
                Debug(String.Format("{0} Order status {1} ", Time.ToString("HH:mm:ss.ffffff"), errorCode.ToString()));
            }

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

        private bool CheckOrdersForFill(OrderTicket longOrder)
        {
            if (longOrder.Status == OrderStatus.Filled)
            {
                return true;
            }

            return false;
        }

        private bool TimeIs(int day, int hour, int minute)
        {
            return Time.Day == day && Time.Hour == hour && Time.Minute == minute;
        }

        public class CustomFillModelTemp : ImmediateFillModel
        {
            private readonly QCAlgorithm _algorithm;
            private readonly Random _random = new Random(387510346); // seed it for reproducibility
            private readonly Dictionary<long, decimal> _absoluteRemainingByOrderId = new Dictionary<long, decimal>();

            public CustomFillModelTemp(QCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                // this model randomly fills market orders

                decimal absoluteRemaining;
                if (!_absoluteRemainingByOrderId.TryGetValue(order.Id, out absoluteRemaining))
                {
                    absoluteRemaining = order.AbsoluteQuantity;
                    _absoluteRemainingByOrderId.Add(order.Id, order.AbsoluteQuantity);
                }

                var fill = base.MarketFill(asset, order);
                var absoluteFillQuantity = (int)(Math.Min(absoluteRemaining, _random.Next(0, 2 * (int)order.AbsoluteQuantity)));
                fill.FillQuantity = Math.Sign(order.Quantity) * absoluteFillQuantity;

                if (absoluteRemaining == absoluteFillQuantity)
                {
                    fill.Status = OrderStatus.Filled;
                    _absoluteRemainingByOrderId.Remove(order.Id);
                }
                else
                {
                    absoluteRemaining = absoluteRemaining - absoluteFillQuantity;
                    _absoluteRemainingByOrderId[order.Id] = absoluteRemaining;
                    fill.Status = OrderStatus.PartiallyFilled;
                }

                _algorithm.Log("CustomFillModel: " + fill);

                return fill;
            }
        }

    }
}