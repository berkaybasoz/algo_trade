using QuantConnect.Views.Controls.ActiveGrid;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;


namespace QuantConnect.Views.DataFeeds
{
    /// <summary>
    /// Class to generate random events to simulate a real-time data feed.
    /// </summary>
    class RandomDataFeed : IDisposable
    {
        #region --- Class Data -------------------------------

        private Random rand;
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

        public RandomDataFeed(int numRows, int numColumns)
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
                if (this.rand == null)
                    this.rand = new Random((int)DateTime.Now.Ticks);

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

            if (this.rand == null)
                this.rand = new Random((int)DateTime.Now.Ticks);

            Thread td = new Thread(new ParameterizedThreadStart(BurstProc));
            td.Start(delay);
        }

        public void RandomBurst()
        {
            if (this.rand == null)
                this.rand = new Random((int)DateTime.Now.Ticks);

            Thread td = new Thread(new ThreadStart(RandomBurstProc));
            td.Start();
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
            int delay = RandomDelay();

            this._isRunning = true;

            OnStartedHandler onStarted = OnStarted;
            if (onStarted != null)
                onStarted(this, EventArgs.Empty);

            try
            {
                while (Thread.CurrentThread.ThreadState != ThreadState.AbortRequested)
                {
                    OnCellUpdateHandler onCellUpdate = OnCellUpdate;
                    if (onCellUpdate != null)
                        onCellUpdate(null, new CellUpdateEventArgs(RandomRow(), RandomColumn(), RandomIncrement()));

                    Thread.Sleep(delay);

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

        private int RandomDelay()
        {
            return rand.Next(180, 220);
        }

        private decimal RandomIncrement()
        {
            int increment = rand.Next(0, 50);
            decimal val = decimal.Divide(increment, 100M);

            if (increment % 2 == 0)
                val = Decimal.Negate(val);

            return val;
        }

        private int RandomRow()
        {
            return rand.Next(0, this._numRows);
        }

        private int RandomColumn()
        {
            return rand.Next(1, this._numColumns);
        }

        private void BurstProc(object param)
        {
            int delay = (int)param;

            Thread.Sleep(delay);

            int minRow = 0;
            int maxRow = this._numRows - 1;

            int minColumn = 1;
            int maxColumn = this._numColumns - 1;

            // Draw left-to-right
            for (int i = minColumn; i <= maxColumn; i++)
            {
                // Draw top-to-bottom
                for (int j = minRow; j <= maxRow; j++)
                    PopulateCell(j, i);

                minColumn++;

                // Draw left-to-right
                for (int k = minColumn; k <= maxColumn; k++)
                    PopulateCell(maxRow, k);

                maxRow--;

                // Draw bottom-to-top
                for (int l = maxRow; l >= minRow; l--)
                    PopulateCell(l, maxColumn);

                maxColumn--;

                // Draw right-to-left
                for (int m = maxColumn; m >= minColumn; m--)
                    PopulateCell(minRow, m);

                minRow++;
            }
        }

        private void RandomBurstProc()
        {
            for (int i = 0; i < this._numColumns; i++)
                PopulateCell(RandomRow(), RandomColumn());
        }


        private void PopulateCell(int row, int column)
        {
            int intVal = rand.Next(-9999, 9999);
            decimal decVal = decimal.Divide(intVal, 100M);

            if (intVal % 2 == 0)
                decVal = Decimal.Negate(decVal);

            OnCellUpdateHandler onCellUpdate = OnCellUpdate;
            if (onCellUpdate != null)
                onCellUpdate(null, new CellUpdateEventArgs(row, column, decVal));
        }

        #endregion

        #region --- IDisposable-------------------------------

        private bool disposed = false;

        ~RandomDataFeed()
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
