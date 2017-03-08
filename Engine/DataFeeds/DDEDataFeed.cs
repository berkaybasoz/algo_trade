using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDde.Client;

namespace QuantConnect.Lean.Engine.DataFeeds.Queues
{
    class DDEDataFeed : IDisposable
    {
        #region --- Class Data -------------------------------
        private object syncRoot = new Object();
        private EventWaitHandle _eventStopped;
        private DdeClient client;
        public DdeClient Client
        {
            get { return client; }
            set { client = value; }
        }
        #endregion

        #region --- Delegates --------------------------------

        public event OnCellUpdateHandler OnCellUpdate;
        public delegate void OnCellUpdateHandler(object sender, CellUpdateEventArgs e);

        //public event OnStartedHandler OnStarted;
        //public delegate void OnStartedHandler(object sender, EventArgs e);

        //public event OnStoppedHandler OnStopped;
        //public delegate void OnStoppedHandler(object sender, EventArgs e);

        #endregion

        public static string[] UpdateFields = { "ALIS", "SATIS", "SON", "TAVAN", "TABAN", "AMIKTAR1", "SMIKTAR1", "PIY.DEG" };

        public DDEDataFeed(string code, string data)
        {
            this._eventStopped = new ManualResetEvent(false);
            //client = new DdeClient("MTX", "DATA");
            client = new DdeClient(code, data);
        }

        private bool disposed = false;

        ~DDEDataFeed()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                }
            }

            disposed = true;
        }

        public void StopQuotes()
        {
            if (client.IsConnected == true)
            {
                client.Disconnect();
                client.Dispose();

            }

        }

        void client_Disconnected(object sender, DdeDisconnectedEventArgs e)
        {

        }

        public void StartQuotesIMKB()
        {
            try
            {
                if (client.IsConnected == true)
                {
                    client.Disconnect();
                    client = new DdeClient("MTX", "DATA");
                }

                client.Advise += OnAdvise;
                client.Disconnected += new EventHandler<DdeDisconnectedEventArgs>(client_Disconnected);

                client.Connect();
                client.StartAdvise("XU030.SON", 1, true, 60000);
            }
            catch
            { }
        }




        public void BeginStartQuotes(IEnumerable<Symbol> symbols)
        {
            try
            {
                if (client.IsConnected == true)
                {
                    client.Disconnect();
                    client = new DdeClient("MTX", "DATA");
                }
                
                client.Advise += OnAdvise;
                client.Disconnected += new EventHandler<DdeDisconnectedEventArgs>(client_Disconnected);
                client.Connect();

                foreach (var item in symbols)
                {
                    for (int j = 0; j < UpdateFields.Length; ++j)
                    {
                        client.BeginStartAdvise(item.Value.ToUpper() + "." + UpdateFields[j], 1, true, null, null);
                    }
                }
            }
            catch
            { }
        }

        private void OnAdvise(object sender, DdeAdviseEventArgs args)
        {
            Task task = Task.Factory.StartNew(() =>
            {
                OnCellUpdateHandler onCellUpdate = OnCellUpdate;
                if (onCellUpdate != null)
                    onCellUpdate(null, new CellUpdateEventArgs(args.Item, Convert.ToDecimal(args.Text)));

            });
        }

        public decimal Request(string item)
        {
            try
            {
                if (client.IsConnected == true)
                {
                    string result = client.Request(item, 60000);
                    return Convert.ToDecimal(result);
                }
                else
                {
                    return decimal.Zero;
                }
            }
            catch
            {
                return decimal.Zero;
            }

        }

        public void StockInfo(string item, out string hata)
        {
            try
            {
                hata = "";
                if (client.IsConnected == true)
                {
                    try
                    {
                        client.StopAdvise(item, 1000);
                    }
                    catch
                    { }
                    client.StartAdvise(item, 1, true, 60000);
                }
                else
                {
                    client.Connect();
                    client.StartAdvise(item, 1, true, 60000);
                }
            }
            catch (Exception ex)
            {
                hata = ex.Message;
            }

        }
    }

    public class CellUpdateEventArgs : System.EventArgs
    {
        #region --- Class Data ------------------------

        private string _column;
        private string _row;
        private decimal _increment = 0;
        private string _key;
        private decimal _value;

        #endregion

        #region --- Constructor -----------------------

        public CellUpdateEventArgs(string key, decimal value)
        {
            int ind = key.IndexOf('.');
            this._row = key.Substring(0, ind);
            this._column = key.Substring(ind + 1, key.Length - (ind + 1));
            this._value = value;
            this._key = key;
        }

        #endregion

        #region --- Properties ------------------------

        public decimal Value
        {
            get { return _value; }
        }

        public string Column
        {
            get { return this._column; }
        }

        public string Row
        {
            get { return this._row; }
        }

        public decimal Increment
        {
            get { return this._increment; }
        }

        public string Key
        {
            get { return this._key; }
        }

        #endregion
    }
}
