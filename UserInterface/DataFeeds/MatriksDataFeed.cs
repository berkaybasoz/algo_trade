using QuantConnect.Views.Controls.ActiveGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Views.DataFeeds
{
    class MatriksDataFeed: IDisposable
    {
        #region --- Class Data -------------------------------

        //private Random rand;
        private int _numRows = 0;
        private int _numColumns = 0;
        private object syncRoot = new Object();
        private bool _isRunning = false;
        private EventWaitHandle _eventStopped;

        #endregion

        #region --- Delegates --------------------------------

        public event OnCellUpdateHandler OnCellUpdate;
        public delegate void OnCellUpdateHandler(object sender, CellUpdateEventArgs e);

        public event OnStartedHandler OnStarted;
        public delegate void OnStartedHandler(object sender, EventArgs e);

        public event OnStoppedHandler OnStopped;
        public delegate void OnStoppedHandler(object sender, EventArgs e);

        #endregion

        #region --- Constructor ------------------------------

        public MatriksDataFeed(int numRows, int numColumns)
        {
            this._numRows = numRows;
            this._numColumns = numColumns;
            this._eventStopped = new ManualResetEvent(false);
        }

        #endregion

        #region --- Public Methods ---------------------------

        public void Start()
        {
            lock (this.syncRoot)
            {
                //if (this.rand == null)
                //    this.rand = new Random((int)DateTime.Now.Ticks);

                if (!this._isRunning)
                {
                    // Set the event to non-signalled
                    this._eventStopped.Reset();

                    ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc));
                }
            }
        }

        public void StopAsync()
        {
            ThreadStart ts = new ThreadStart(Stop);
            Thread thd = new Thread(ts);
            thd.Start();
        }

        public void Burst(int delay)
        {

        }



        #endregion

        #region --- Private Methods --------------------------

        private void Stop()
        {
            lock (this.syncRoot)
            {
                if (this._isRunning)
                {
                    // Tell the worker thread that it needs to abort.
                    this._isRunning = false;

                    // Wait for the worker thread to terminate.
                    this._eventStopped.WaitOne();

                    OnStoppedHandler onStopped = OnStopped;
                    if (onStopped != null)
                        onStopped(this, EventArgs.Empty);
                }
            }
        }

        private void ThreadProc(Object stateInfo)
        {
 

            this._isRunning = true;

            OnStartedHandler onStarted = OnStarted;
            if (onStarted != null)
                onStarted(this, EventArgs.Empty);

            try
            {
                while (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    //OnCellUpdateHandler onCellUpdate = OnCellUpdate;
                    //if (onCellUpdate != null)
                    //    onCellUpdate(null, new CellUpdateEventArgs(RandomRow(), RandomColumn(), RandomIncrement()));

                    //Thread.Sleep(delay);

                    if (!this._isRunning)
                        Thread.CurrentThread.Abort();
                }
            }
            finally
            {
                // Set the event to signalled
                this._eventStopped.Set();
            }
        }
 

        private void PopulateCell(int row, int column)
        {
            

            //OnCellUpdateHandler onCellUpdate = OnCellUpdate;
            //if (onCellUpdate != null)
            //    onCellUpdate(null, new CellUpdateEventArgs(row, column, decVal));
        }

        #endregion

        #region --- IDisposable-------------------------------

        private bool disposed = false;

        ~MatriksDataFeed()
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
                    // Clean up all managed resources
                    Stop();
                }
            }

            // Clean up all native resources

            disposed = true;
        }

        #endregion

        #region --- Properties -------------------------------

        public bool IsRunning
        {
            get { return this._isRunning; }
        }

        public int Columns
        {
            get { return this._numColumns; }
            set { this._numColumns = value; }
        }

        public int Rows
        {
            get { return this._numRows; }
            set { this._numRows = value; }
        }

        #endregion
    }
}
