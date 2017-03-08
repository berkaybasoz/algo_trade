using QuantConnect.Logging;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.FIX.Model;

namespace QuantConnect.Brokerages.TEB
{
 public   class ConvertHelper
    {
     public decimal RoundPrice(decimal input, decimal minTick)
        {
            if (minTick == 0) return minTick;
            return Math.Round(input / minTick) * minTick;
        }

        public int ConvertToQuantity(Teb.FIX.Model.Order tebOrder)
        {
            switch (tebOrder.Core.Side)
            {
                case CashDefinition.SIDE_BUY:
                    return tebOrder.OrderQty.HasValue ? tebOrder.OrderQty.Value : 0;

                case CashDefinition.SIDE_SELL:
                    return -(tebOrder.OrderQty.HasValue ? tebOrder.OrderQty.Value : 0);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
      
        public string ConvertToSide(int quantity)
        {
            if (quantity >= 0)
            {
                return CashDefinition.SIDE_BUY;
            }
            else
            {
                return CashDefinition.SIDE_SELL;
            }
        }

        //public string Convert2Side(Orders.OrderDirection direction)
        //{
        //    switch (direction)
        //    {
        //        case OrderDirection.Buy:
        //            return CashDefinition.SIDE_BUY;
        //        case OrderDirection.Sell:
        //            return CashDefinition.SIDE_SELL;
        //        default:
        //            throw new InvalidEnumArgumentException("order.OrderDirection", (int)direction, typeof(Orders.OrderDirection));
        //    }
        //}

        public QuantConnect.Orders.OrderStatus ConvertToStatus(Teb.FIX.Model.Order fo)
        {
            switch (fo.ServerStatus)
            {
                case ContentServerStatus.BusinessReject:
                case ContentServerStatus.CancelReject:
                case ContentServerStatus.CancelReplaceReject:
                case ContentServerStatus.Rejected:
                case ContentServerStatus.SessionRejected:
                case ContentServerStatus.Expired:
                case ContentServerStatus.Canceled:
                    return OrderStatus.Canceled;
                case ContentServerStatus.Fill:
                    return OrderStatus.Filled;
                case ContentServerStatus.New:
                case ContentServerStatus.NewActivated:
                    return OrderStatus.Submitted;
                case ContentServerStatus.NewNotActivated:
                    return OrderStatus.New;
                case ContentServerStatus.PartialFill:
                    return OrderStatus.PartiallyFilled;
                case ContentServerStatus.Waiting:
                    Log.Error("TEBBrokersBrokerage.ConvertOrderStatus(): ContentServerStatus.Waiting order");
                    return OrderStatus.None;
                default:
                    throw new InvalidEnumArgumentException("status", (int)fo.ServerStatus, typeof(ContentServerStatus));
            }


        }
        public string ConvertToTimeInForce(Orders.OrderType orderType)
        {
            switch (orderType)
            {
                case QuantConnect.Orders.OrderType.Market:
                    return CashDefinition.TIF_IOC;
                case QuantConnect.Orders.OrderType.Limit:
                    return CashDefinition.TIF_DAY;

                default:
                    throw new InvalidEnumArgumentException("order.OrderType", (int)orderType, typeof(Orders.OrderType));
            }
        }

        public string ConvertToTimeInForce(OrderDuration orderDuration)
        {
            switch (orderDuration)
            {
                case OrderDuration.GTC:
                    return CashDefinition.TIF_GTC;
                case OrderDuration.Custom:
                    return CashDefinition.TIF_DAY;
                case OrderDuration.Day:
                    return CashDefinition.TIF_DAY;
                default:
                    throw new InvalidEnumArgumentException("order.OrderType", (int)orderDuration, typeof(Orders.OrderDuration));
            }
        }

        public QuantConnect.Orders.OrderDuration ConvertToTimeInForce(string orderDuration)
        {
            switch (orderDuration)
            {
                case CashDefinition.TIF_GTC:
                    return OrderDuration.GTC;
                case CashDefinition.TIF_DAY:
                    return OrderDuration.Day;
                default:
                    return OrderDuration.Day;
            }
        }

        public QuantConnect.Orders.OrderType ConvertToOrderType(string orderType)
        {
            switch (orderType)
            {
                case CashDefinition.ORDER_ENTRY_TYPE_LIMIT: return QuantConnect.Orders.OrderType.Limit;
                case CashDefinition.ORDER_ENTRY_TYPE_MARKET: return QuantConnect.Orders.OrderType.Market;
                default:
                    throw new InvalidCastException("orderType");
            }
        }

        public string ConvertToOrdType(Orders.OrderType orderType)
        {
            switch (orderType)
            {
                case QuantConnect.Orders.OrderType.Market:
                    return CashDefinition.ORDTYPE_MARKET;
                case QuantConnect.Orders.OrderType.Limit:
                    return CashDefinition.ORDTYPE_LIMIT;

                default:
                    throw new InvalidEnumArgumentException("order.OrderType", (int)orderType, typeof(Orders.OrderType));
            }

        }


        public QuantConnect.Orders.Order ConvertToOrder(Teb.FIX.Model.Order tebOrder)
        {
            QuantConnect.Orders.Order o = null;

            if (tebOrder.OrdType == CashDefinition.ORDTYPE_MARKET)
            {
                o = new QuantConnect.Orders.MarketOrder();
            }
            else
            {
                o = new QuantConnect.Orders.LimitOrder();
                (o as LimitOrder).LimitPrice = tebOrder.Price.HasValue ? tebOrder.Price.Value : 0;
            }

            o.Duration = ConvertToTimeInForce(tebOrder.Core.TimeInForce); ;
            o.Id = int.Parse(tebOrder.ClOrdID.Replace("CS", ""));
            o.Price = tebOrder.Price.HasValue ? tebOrder.Price.Value : 0;
            o.Quantity = ConvertToQuantity(tebOrder);
            o.Status = ConvertToStatus(tebOrder);
            o.Time = tebOrder.TransactTime.HasValue ? tebOrder.TransactTime.Value : DateTime.Now;
            o.Symbol = tebOrder.Symbol;
            o.BrokerId.Add(tebOrder.ConnectionClOrdID);
            return o;
        }

        public QuantConnect.Holding ConvertToHolding(TEBPosition position)
        {
            //string currencySymbol;
            //if (!Currencies.CurrencySymbols.TryGetValue(currency, out currencySymbol))
            //{
            //    currencySymbol = "$";
            //}
            string currencySymbol = "$";
            string symbol = position.Symbol;
            return new Holding
            {
                Symbol = ConvertToSymbol(symbol),
                Type = SecurityType.Equity,
                Quantity = position.NetLotCount,
                //Quantity = position.Side == CashDefinition.SIDE_SELL ? -position.Units : position.Units,
                AveragePrice = position.AvgNetPrice,
                //MarketPrice = marketPrice,
                ConversionRate = 1m, // this will be overwritten when GetAccountHoldings is called to ensure fresh values
                CurrencySymbol = currencySymbol
            };

            //return new Holding
            //{
            //    Symbol = Symbol.Create(position.Symbol, SecurityType.Equity, Market.USA),
            //    Type = SecurityType.Equity,
            //    AveragePrice = position.CostBasis / position.Quantity,
            //    ConversionRate = 1.0m,
            //    CurrencySymbol = "$",
            //    MarketPrice = 0m, //--> GetAccountHoldings does a call to GetQuotes to fill this data in
            //    Quantity = position.Quantity
            //};
        }

        public QuantConnect.Symbol ConvertToSymbol(string symbol)
        {
            var securityType = SecurityType.Equity;
            var market = Market.USA;

            return Symbol.Create(symbol, securityType, market);
        }
    }
}
