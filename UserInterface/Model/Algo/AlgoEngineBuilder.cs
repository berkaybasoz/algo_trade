using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Views.Model.Algo
{

    public class AlgoEngineBuilder : IDisposable
    {
        private string environment = "desktop";
        private LeanEngineAlgorithmHandlers _algorithmHandlers;
        private IResultHandler _resultsHandler;
        private Thread _leanEngineThread;
        private Engine _engine;
        public event Action<string> OnLog;
        AlgorithmNodePacket job;

        public IResultHandler ResultsHandler
        {
            get { return _resultsHandler; }
            set { _resultsHandler = value; }
        }

        public LeanEngineAlgorithmHandlers AlgorithmHandlers
        {
            get { return _algorithmHandlers; }
            set { _algorithmHandlers = value; }
        }

        public bool IsActive
        {
            get
            {
                return ResultsHandler == null ? false : ResultsHandler.IsActive;
            }
        }

        public void BuildEngine(string algorithm)
        {
            //BuildForBackTesting(algorithm);
            BuildForLive(algorithm);
        }
        public void Start(string algorithm, string algorithmLocation)
        {
            //BuildForBackTesting(algorithm);
            BuildForLive(algorithm, algorithmLocation);
        }

        public void Stop()
        {
            try
            {
                if (_algorithmHandlers != null)
                {
                    _algorithmHandlers.Setup.Dispose();
                    _algorithmHandlers.Dispose();
                }
            }
            catch (Exception ex)
            {

            }

            try
            {

                if (_leanEngineThread != null)
                {
                    _leanEngineThread.Abort();
                }

                Dispose();


            }
            catch (Exception ex)
            {

            }
        }

        private void BuildForLive(string algorithm)
        {
            BuildForLive(algorithm, "QuantConnect.Algorithm.CSharp.dll");
        }

        private void BuildForLive(string algorithm, string algorithmLocation)
        {
            /*string algorithm = "TEBBasicAlgo1";
                   string environment = "teb-algo";
              */

            Log("Running " + algorithm + "...");

            Config.Set("algorithm-type-name", algorithm);
            Config.Set("live-mode", "true");
            Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
            Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
            Config.Set("api-handler", "QuantConnect.Api.Api");

            Config.Set("environment", environment);

            Config.Set("algorithm-language", "CSharp");
            Config.Set("algorithm-location", algorithmLocation);
            Config.Set("map-file-provider", "LocalDiskMapFileProvider");
            Config.Set("ironpython-location", "../ironpython/Lib");



            Config.Set("live-mode-brokerage", "TEBBrokerage");
            Config.Set("TEB-account-id", "22208");//Config.Set("TEB-account-id", "7041580");
            Config.Set("TEB-connection-string", "data source=xx;initial catalog=xxdb;user id=xxuser;password=xxpass;MultipleActiveResultSets=True;Asynchronous Processing=true;Application Name=NotificationDataApp;Max Pool Size=1024;Pooling=true;");
            Config.Set("TEB-fix-app-name", "FIXALGOCASHCLIENT");//FIXALGOFUTURECLIENT
            Config.Set("TEB-fix-connect-timeout", "60000");
            Config.Set("TEB-fix-sender-sub-id", "accountsubid");

            Config.Set("setup-handler", "BrokerageSetupHandler");
            Config.Set("transaction-handler", "QuantConnect.Lean.Engine.TransactionHandlers.BrokerageTransactionHandler");
            Config.Set("data-feed-handler", "QuantConnect.Lean.Engine.DataFeeds.LiveTradingDataFeed");
            Config.Set("real-time-handler", "QuantConnect.Lean.Engine.RealTime.LiveTradingRealTimeHandler");
            //Config.Set("data-queue-handler", "QuantConnect.Lean.Engine.DataFeeds.Queues.DDEDataQueue");
            //Config.Set("data-queue-handler", "QuantConnect.Lean.Engine.DataFeeds.Queues.LiveDataQueue");
           //Config.Set("data-queue-handler", "QuantConnect.Lean.Engine.DataFeeds.Queues.FakeDataQueue2");//20160831 MatriksTcp test et
            Config.Set("data-queue-handler", "QuantConnect.Brokerages.TEB.Data.TEBMatriksTcpDataQueue");
            Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.DesktopResultHandler");
            //QuantConnect.Lean.Engine.Results.LiveTradingResultHandler
            Config.Set("force-exchange-always-open", "true");

            Config.Set("TEB-matriks-stream-server-ip", "10.72.238.131");//Matriks Stream Server IP
            Config.Set("TEB-matriks-stream-server-port", "443");
            Config.Set("TEB-matriks-stream-exhangeid-list", "4");//4 for equity, 9 for VIOP

            _engine = CreateEngine();

            _resultsHandler = _engine.AlgorithmHandlers.Results;
        }

        private void BuildForBackTesting(string algorithm)
        {
            /*string algorithm = "TEBBasicAlgo1";
                   string environment = "teb-algo";
              */



            Log("Running " + algorithm + "...");


            Config.Set("algorithm-type-name", algorithm);
            Config.Set("live-mode", "false");
            Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
            Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
            Config.Set("api-handler", "QuantConnect.Api.Api");
            Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.DesktopResultHandler");
            Config.Set("environment", environment);

            Config.Set("algorithm-language", "CSharp");
            Config.Set("algorithm-location", "QuantConnect.Algorithm.CSharp.dll");
            Config.Set("map-file-provider", "LocalDiskMapFileProvider");
            Config.Set("ironpython-location", "../ironpython/Lib");



            Config.Set("live-mode-brokerage", "TEBBrokerage");
            Config.Set("TEB-account-id", "22208");
            Config.Set("TEB-connection-string", "data source=172.22.1.81;initial catalog=tradedb;user id=sa;password=AlabaliK321;MultipleActiveResultSets=True;Asynchronous Processing=true;Application Name=NotificationDataApp;Max Pool Size=1024;Pooling=true;");
            Config.Set("TEB-fix-app-name", "FIXHOSTCASHCLIENT");
            Config.Set("TEB-fix-connect-timeout", "60000");
            Config.Set("ironpython-location", "bbasoz");

            Config.Set("setup-handler", "ConsoleSetupHandler");
            Config.Set("transaction-handler", "QuantConnect.Lean.Engine.TransactionHandlers.BacktestingTransactionHandler");

            /* 
            Engine.cs defaults
               var setupHandlerTypeName = Config.Get("setup-handler", "ConsoleSetupHandler");
               var transactionHandlerTypeName = Config.Get("transaction-handler", "BacktestingTransactionHandler");
               var realTimeHandlerTypeName = Config.Get("real-time-handler", "BacktestingRealTimeHandler");
               var dataFeedHandlerTypeName = Config.Get("data-feed-handler", "FileSystemDataFeed");
               var resultHandlerTypeName = Config.Get("result-handler", "BacktestingResultHandler");
               var historyProviderTypeName = Config.Get("history-provider", "SubscriptionDataReaderHistoryProvider");
               var commandQueueHandlerTypeName = Config.Get("command-queue-handler", "EmptyCommandQueueHandler");
               var mapFileProviderTypeName = Config.Get("map-file-provider", "LocalDiskMapFileProvider");
               var factorFileProviderTypeName = Config.Get("factor-file-provider", "LocalDiskFactorFileProvider");
            */

            _engine = CreateEngine();

            _resultsHandler = _engine.AlgorithmHandlers.Results;
        }

        public Engine CreateEngine()
        {
            //Launch the Lean Engine in another thread: this will run the algorithm specified above.
            // TODO > This should only be launched when clicking a backtest/trade live button provided in the UX.
            Composer v = Composer.Instance;

            //Log.LogHandler = new CompositeLogHandler(new ILogHandler[]
            //    {
            //        new ConsoleLogHandler(),
            //        new FileLogHandler("regression.log")
            //    });

            var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            _algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            var engine = new Engine(systemHandlers, _algorithmHandlers, Config.GetBool("live-mode"));


            Log("Engine.Main(): Memory " + OS.ApplicationMemoryUsed + "Mb-App  " + +OS.TotalPhysicalMemoryUsed + "Mb-Used  " + OS.TotalPhysicalMemory + "Mb-Total");

            // log the job endpoints
            Log("JOB HANDLERS: ");
            Log("         DataFeed:     " + _algorithmHandlers.DataFeed.GetType().FullName);
            Log("         Setup:        " + _algorithmHandlers.Setup.GetType().FullName);
            Log("         RealTime:     " + _algorithmHandlers.RealTime.GetType().FullName);
            Log("         Results:      " + _algorithmHandlers.Results.GetType().FullName);
            Log("         Transactions: " + _algorithmHandlers.Transactions.GetType().FullName);
            Log("         History:      " + _algorithmHandlers.HistoryProvider.GetType().FullName);
            Log("         Commands:     " + _algorithmHandlers.CommandQueue.GetType().FullName);
            Log("         FactorFileProvider:     " + _algorithmHandlers.FactorFileProvider.GetType().FullName);
            Log("         HistoryProvider:     " + _algorithmHandlers.HistoryProvider.GetType().FullName);
            Log("         MapFileProvider:     " + _algorithmHandlers.MapFileProvider.GetType().FullName);
            Log("         Transactions:     " + _algorithmHandlers.Transactions.GetType().FullName);


            _leanEngineThread = new Thread(() =>
            {
                string algorithmPath;
                job = systemHandlers.JobQueue.NextJob(out algorithmPath);

                if (job is LiveNodePacket)
                    Log("         Brokerage:    " + ((LiveNodePacket)job).Brokerage);

                engine.Run(job, algorithmPath);
                systemHandlers.JobQueue.AcknowledgeJob(job);
            });
            _leanEngineThread.Start();

            return engine;

        }

        public void Log(string message)
        {
            if (OnLog != null)
                OnLog(message);
        }

        public void Dispose()
        {
            if (job != null)
            {
                _engine.SystemHandlers.JobQueue.AcknowledgeJob(job);
                Log("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);
            }
            if (_engine != null)
            {
                _engine.SystemHandlers.Dispose();
                _engine.AlgorithmHandlers.Dispose();
            }

        }
    }
}
