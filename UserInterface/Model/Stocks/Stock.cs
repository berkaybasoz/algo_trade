using QuantConnect.Views.Controls.ActiveGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Views.Model.Stocks
{
    public class Stock : INotifyPropertyChanged
    {
        public const string TIME_FORMAT = "HH:mm:ss.fff";

        public const string TIME = "Time";
        public const string SYMBOL = "Symbol";
        public const string HIGH = "High";
        public const string LOW = "Low";
        public const string CLOSE = "Close";
        public const string VOLUME = "Volume";
        public const string PRICE = "Price";
        public const string VALUE = "Value";
        public const string ENDTIME = "EndTime";
        public const string DATATYPE = "DataType";
        public const string PERIOD = "Period";


        private DateTime time;
        private string symbol;
        private decimal high;
        private decimal low;
        private decimal close;
        private decimal volume;
        private decimal price;
        private decimal value;
        private DateTime endTime;
        private string dataType;
        private string period;
        private int rowIndex;


        public event OnCellUpdateHandler OnCellUpdate;
        public delegate void OnCellUpdateHandler(object sender, CellUpdateEventArgs e);


        public Stock()
        {

        }

        public Stock(DateTime time, string symbol, decimal high, decimal low, decimal close, decimal volume,
           decimal price, decimal value, DateTime endTime, string dataType, string period)
        {
            Change(time, symbol, high, low, close, volume, price, value, endTime, dataType, period);
        }

        public void Change(DateTime time, string symbol, decimal high, decimal low, decimal close, decimal volume,
           decimal price, decimal value, DateTime endTime, string dataType, string period)
        {
            Time = time;
            Symbol = symbol;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            Price = price;
            Value = value;
            EndTime = endTime;
            DataType = dataType;
            Period = period;
        }

        public DateTime Time
        {
            get { return this.time; }
            set
            { 
                if (this.time != value)
                {
                    this.time = value;
                    NotifyPropertyChanged("Time");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.time.ToString(TIME_FORMAT), String.Format("{0}_{1}", symbol, Stock.TIME)));
                }

            }
        }

        public string Symbol
        {
            get { return this.symbol; }
            set
            { 
                if (this.symbol != value)
                {
                    this.symbol = value;
                    NotifyPropertyChanged("Symbol");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.symbol, String.Format("{0}_{1}", symbol, Stock.SYMBOL)));
                }
            }
        }

        public decimal High
        {
            get { return this.high; }
            set
            { 
                if (this.high != value)
                {
                    this.high = value;
                    NotifyPropertyChanged("High");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.high, String.Format("{0}_{1}", symbol, Stock.HIGH)));
                }
            }
        }

        public decimal Low
        {
            get { return this.low; }
            set
            { 
                if (this.low != value)
                {
                    this.low = value;
                    NotifyPropertyChanged("Low");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.low, String.Format("{0}_{1}", symbol, Stock.LOW)));
                }
            }
        }

        public decimal Close
        {
            get { return this.close; }
            set
            { 
                if (this.close != value)
                {
                    this.close = value;
                    NotifyPropertyChanged("Close");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.close, String.Format("{0}_{1}", symbol, Stock.CLOSE)));
                }
            }
        }

        public decimal Volume
        {
            get { return this.volume; }
            set
            { 
                if (this.volume != value)
                {
                    this.volume = value;
                    NotifyPropertyChanged("Volume");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.volume, String.Format("{0}_{1}", symbol, Stock.VOLUME)));
                }
            }
        }

        public decimal Price
        {
            get { return this.price; }
            set
            { 
                if (this.price != value)
                {
                    this.price = value;
                    NotifyPropertyChanged("Price");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.price, String.Format("{0}_{1}", symbol, Stock.PRICE)));
                }
            }
        }

        public decimal Value
        {
            get { return this.value; }
            set
            { 
                if (this.value != value)
                {
                    this.value = value;
                    NotifyPropertyChanged("Value");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.value, String.Format("{0}_{1}", symbol, Stock.VALUE)));
                }
            }
        }

        public DateTime EndTime
        {
            get { return this.endTime; }
            set
            { 
                if (this.endTime != value)
                {
                    this.endTime = value;
                    NotifyPropertyChanged("EndTime");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.endTime.ToString(TIME_FORMAT), String.Format("{0}_{1}", symbol, Stock.ENDTIME)));
                }
            }
        }

        public string DataType
        {
            get { return this.dataType; }
            set
            {
                if (this.dataType != value)
                {
                    this.dataType = value;
                    NotifyPropertyChanged("DataType");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.dataType, String.Format("{0}_{1}", symbol, Stock.DATATYPE)));
                }
            }
        }

        public string Period
        {
            get { return this.period; }
            set
            {
                if (this.period != value)
                {
                    this.period = value;
                    NotifyPropertyChanged("Period");
                    NotifyCellUpdated(new CellUpdateEventArgs(this.period, String.Format("{0}_{1}", symbol, Stock.PERIOD)));
                } 
            }
        }

        public int RowIndex
        {
            get { return this.rowIndex; }
            set { this.rowIndex = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

        public void NotifyCellUpdated(CellUpdateEventArgs e)
        {
            if (OnCellUpdate != null)
                OnCellUpdate(this, e);

        }
    }

}
