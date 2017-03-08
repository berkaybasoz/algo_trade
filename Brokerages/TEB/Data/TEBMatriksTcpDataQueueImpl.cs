using QuantConnect.Brokerages.TEB.Data;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Teb.Infra;
using Teb.Infra.Model;
using Teb.Infra.Extension;
using Teb.Infra.Collection;
using Teb.DAL;
using System.Data;

namespace QuantConnect.Brokerages.TEB.Data
{


    /// <summary>
    /// TEB brokerage - implementation of IDataQueueHandler interface
    /// </summary>
    public partial class TEBMatriksTcpDataQueue : IDataQueueHandler
    {
        #region Private Fields


        private object logCollLock = new object();
        private object errorCollLock = new object();
        private object logUICollLock = new object();
        private object errorUICollLock = new object();


        private string logFileNamePrefix = "MatriksDataLogs";
        private string seanceInfo;
        private string connString;
        private string strLetters = "ABCDEFGHIJKLMNOPRSQTUVYZXW";


        private SecurityDistributeOption distOption = new SecurityDistributeOption();


        private List<string> DistExchangeIDList = new List<string>();


        private DbNewsQueue dbQNews;
        private DbLogQueueCollection dbLogQueueCollection;
        private ConcurrentQueue<string> errors;
        private StreamQueue distStreamQueue;

        private bool enableFirstLetterForDbQueue = true;
        private bool waitForDBDequeuWhenClosing = true;
        private ConcurrentDictionary<string, string> letterKeys = new ConcurrentDictionary<string, string>();



        private MatriksTcpDataProvider dataProvider;
        private string matriksStreamServerIP;
        private int matriksStreamServerPort;
        private string matriksDistExchangeIDList;

        #endregion

        #region Properties



        //public DataBroadcaster Broadcaster
        //{
        //    get { return broadcaster; }
        //    set { broadcaster = value; }
        //}

        public SecurityDistributeOption DistOption
        {
            get { return distOption; }
            set { distOption = value; }
        }


        public string SeanceInfo
        {
            get { return seanceInfo; }
            set
            {
                seanceInfo = value;
            }
        }

        public StreamMessageQueue StreamMessageQueue
        {
            get
            {
                if (dataProvider != null)
                    return dataProvider.StreamMessageQueue;
                else
                    return null;
            }
        }

        public ParserQueue Parser001Queue
        {
            get
            {
                if (dataProvider != null)
                    return dataProvider.Parser001Queue;
                else
                    return null;
            }
        }

        public ParserQueue Parser002Queue
        {
            get
            {
                if (dataProvider != null)
                    return dataProvider.Parser002Queue;
                else
                    return null;
            }
        }

        public ParserQueue Parser012Queue
        {
            get
            {
                if (dataProvider != null)
                    return dataProvider.Parser012Queue;
                else
                    return null;
            }
        }

        public ParserQueue Parser020Queue
        {
            get
            {
                if (dataProvider != null)
                    return dataProvider.Parser020Queue;
                else
                    return null;
            }
        }

        public ParserQueue Parser060Queue
        {
            get
            {
                if (dataProvider != null)
                    return dataProvider.Parser060Queue;
                else
                    return null;
            }
        }

        public ParserQueue Parser064Queue
        {
            get
            {
                if (dataProvider != null)
                    return dataProvider.Parser064Queue;
                else
                    return null;
            }
        }

        public ParserQueue ParserUnkownQueue
        {
            get
            {
                if (dataProvider != null)
                    return dataProvider.ParserUnkownQueue;
                else
                    return null;
            }
        }

        public StreamQueue DistStreamQueue
        {
            get { return distStreamQueue; }
            set
            {
                distStreamQueue = value;
            }
        }

        public ConcurrentQueue<string> Errors
        {
            get
            {
                if (errors == null)
                    errors = new ConcurrentQueue<string>();

                return errors;
            }
            set { errors = value; }
        }

        public DbLogQueueCollection DbLogQueueCollection
        {
            get { return dbLogQueueCollection; }
            set
            {
                dbLogQueueCollection = value;
            }
        }

        public bool EnableFirstLetterForDbQueue
        {
            get { return enableFirstLetterForDbQueue; }
            set
            {
                enableFirstLetterForDbQueue = value;
            }
        }

        public bool WaitForDBDequeuWhenClosing
        {
            get { return waitForDBDequeuWhenClosing; }
            set
            {
                waitForDBDequeuWhenClosing = value;
            }
        }

        public DbNewsQueue DbQNews
        {
            get { return dbQNews; }
            set
            {
                dbQNews = value;
            }
        }



        #endregion




        #region Start Stop Watch Save Clear


        private void Start()
        {

            try
            {
                StartLogger(logFileNamePrefix);

                DistExchangeIDList.Clear();
                if (string.IsNullOrEmpty(matriksDistExchangeIDList) == false)
                {
                    string[] tmpExchangeIDs = matriksDistExchangeIDList.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var tmpExchangeID in tmpExchangeIDs)
                    {
                        DistExchangeIDList.Add(tmpExchangeID);
                    }
                }


                DbLogQueueCollection = new DbLogQueueCollection();


                letterKeys.Clear();
                List<string> letterValues = new List<string>();

                if (EnableFirstLetterForDbQueue)
                {
                    foreach (var item in strLetters)
                    {
                        letterValues.Add(item.ToString());
                    }
                }
                else
                {
                    letterValues.Add("ABCDEF");
                    letterValues.Add("GHIJKL");
                    letterValues.Add("MNOPRSQ");
                    letterValues.Add("TUVYZXW");
                }

                foreach (var l in strLetters)
                {
                    string key = l.ToString();
                    string value;

                    value = letterValues.FirstOrDefault(f => f.Contains(key));

                    letterKeys.AddOrUpdate(key, value, (k, v) => { return value; });
                }


                foreach (var l in letterValues)
                {
                    DbLogQueue dbQ = new DbLogQueue();
                    dbQ.Key = l;//yani ABCDEF
                    dbQ.DbManager = new DbQueryManager(connString);
                    dbQ.OnDequeue += DbLogQueue_OnDequeue;
                    dbQ.OnException += DbLogQueue_OnException;
                    DbLogQueueCollection.AddOrUpdate(l, dbQ);
                }

                DbQNews = new DbNewsQueue();
                DbQNews.DbManager = new DbQueryManager(connString);
                DbQNews.OnDequeue += DbNewsQueue_OnDequeue;
                DbQNews.OnException += DbNewsQueue_OnException;


                DistStreamQueue = new StreamQueue();
                DistStreamQueue.OnDequeue += DistStreamQueue_OnDequeue;
                DistStreamQueue.OnException += DistStreamQueue_OnException;




                dataProvider = new MatriksTcpDataProvider(matriksStreamServerIP, matriksStreamServerPort);
                dataProvider.OnMaxTimeStampChanged += DataProvider_OnMaxTimeStampChanged;
                dataProvider.OnSecurityChanged += dataProvider_OnSecurityChanged;
                dataProvider.OnSecurityDepthChanged += dataProvider_OnSecurityDepthChanged;
                dataProvider.OnBistTimeChanged += dataProvider_OnBistTimeChanged;
                dataProvider.OnHandleNewsChanged += dataProvider_OnHandleNewsChanged;
                dataProvider.OnHandleUknownFeed += DataProvider_OnHandleUknownFeed;
                dataProvider.OnLog += DataProvider_OnLog;
                dataProvider.OnException += DataProvider_OnException;

                SetSecurities();

                //Broadcaster = new DataBroadcaster(notIP, NotPort); 
                //Broadcaster.OnLog += NotSocketServer_OnLog;
                //Broadcaster.OnException += NotSocketServer_OnException; 
                //Broadcaster.Start();
                dataProvider.Start();

            }
            catch (Exception ex)
            {
                TBYException tex = new TBYConfigurationException(MethodInfo.GetCurrentMethod().GetFullName(), "Veri yayın başlatılırken hata oluştu", ex);
                ProcessException(tex);
            }

        }



        private void Stop()
        {
            try
            {



                dataProvider.Stop();

                //Broadcaster.Stop();


                if (WaitForDBDequeuWhenClosing == false)
                {
                    DbQNews.Stop();
                    DbQNews.Dispose();

                    if (DbLogQueueCollection != null)
                    {
                        foreach (var item in DbLogQueueCollection.Values)
                        {
                            item.Stop();
                            item.Dispose();
                        }
                    }
                }



                StopLogger();
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Kapanırken hata oluştu", ex);
                ProcessException(tex);
            }

        }




        #endregion


        #region Log & Exception

        void ProcessException(Exception tex)
        {

            Error(tex);
        }



        #endregion





        private void DataProvider_OnException(Exception ex)
        {
            TBYException tex = new TBYDBException(MethodBase.GetCurrentMethod().GetFullName(), "Veri yayın veritabanında hata oluştu.", ex);
            ProcessException(tex);
        }

        private void DataProvider_OnLog(string log)
        {

            try
            {
                Log(log);

            }
            catch (Exception pException)
            {
                Log(MethodInfo.GetCurrentMethod().GetFullName() + " Message :" + pException.Message);
            }
            //Log lg = new Log(log);
            //LogHandler.Handle(lg);
        }

        private void dataProvider_OnSecurityDepthChanged(SecurityDepthEventArgs arg)
        {
            //SecurityDepth secDepth = arg.SecurityDepth;

            //if (DistOption.DistributeSecurityDepth)
            //{
            //    DistStreamQueue.Enqueue(() =>
            //    {
            //        //Broadcaster.BroadCastQuoteDepth(secDepth);//TODO bunucuda scs ile gonder
            //    });

            //}
        }

        private void dataProvider_OnBistTimeChanged(BistTimeEventArgs arg)
        {

            //BistTime bistTime = arg.BistTime;

            //if (DistOption.DistributeBistTime)
            //{
            //    DistStreamQueue.Enqueue(() =>
            //    {
            //        BroadCastBistTime(bistTime);
            //    });

            //}
        }

        private void dataProvider_OnSecurityChanged(SecurityEventArgs arg)
        {
            Security sec = arg.Security;
            if (sec != null)
            {
                string feedMsg = arg.Message;

                string fl = sec.OrgSecurity.FirstOrDefault().ToString();
                DbLogQueue dbQ = FindQueue(fl.ToUpper());
                if (dbQ != null)
                {
                    SecurityDbLog log = new SecurityDbLogAdapter(sec, feedMsg);
                    log.DbManager = dbQ.DbManager;
                    dbQ.Enqueue(log);
                }
                else
                {
                    ProcessException(new ApplicationException(String.Format("{0} senedi için db kuyruğu bulunamadı", sec.OrgSecurity)));
                }




                #region MyRegion
                //if (DistOption.DistributeLimitControl && DistExchangeIDList.Contains(sec.ExchangeID))
                //{
                //    DistStreamQueue.Enqueue(() =>
                //    {
                //        LimitControl lc = new LimitControl();
                //        lc.OrgSecurity = sec.OrgSecurity;
                //        lc.Symbol = sec.Symbol;
                //        lc.SymbolSfx = sec.SymbolSfx;
                //        lc.LastUpdate = sec.LastUpdate;
                //        lc.IsDeleted = sec.IsDeleted;
                //        lc.Last = sec.Last;
                //        lc.IsBist30 = sec.IsBist30;
                //        lc.IsBist50 = sec.IsBist50;
                //        lc.IsBist100 = sec.IsBist100;
                //        lc.ExchangeID = sec.ExchangeID;
                //        //Broadcaster.BroadCastLimitControl(lc);//TODO bunucuda scs ile gonder
                //    });
                //}

                #endregion
                if (DistOption.DistributeSecurity && DistExchangeIDList.Contains(sec.ExchangeID))
                {
                    DistStreamQueue.Enqueue(() =>
                    {
                        BroadcastSecurity(sec);
                    });
                }
            }

        }

        private void dataProvider_OnHandleNewsChanged(NewsEventArgs arg)
        {
            News news = arg.News;
            string feedMsg = arg.Message;
            if (DbQNews != null)
            {
                NewsDbLog log = new NewsDbAdapter(news, feedMsg);
                log.DbManager = DbQNews.DbManager;
                DbQNews.Enqueue(log);
            }
            else
            {
                ProcessException(new ApplicationException(String.Format("{0} haberi için db kuyruğu bulunamadı", feedMsg)));
            }
        }

        private void BroadcastSecurity(Security security)
        {
            //if (si.Symbol.Contains("GARAN"))
            //{
            //}
            string symbol = security.Symbol;
            if (_symbols.ContainsKey(symbol))
            {
                //_ticks.Enqueue(new Tick(DateTime.Now, symbol, security.Last, security.Bid, security.Ask)
                //{
                //    AskSize=security.AskSize,
                //    BidSize=security.BidSize
                //});


                //_ticks.Enqueue(new Tick()
                //         {
                //             Time = DateTime.Now,
                //             Symbol = symbol,
                //             Value = security.Last,
                //             TickType = TickType.Trade,
                //             Quantity = security.LastSize
                //         });
                _ticks.Enqueue(new Tick(DateTime.Now, symbol, security.Last, security.Bid, security.Ask)
                        {
                            AskSize = security.AskSize,
                            BidSize = security.BidSize,
                            Quantity = security.LastSize
                        });

            }
        }

        private void NotSocketServer_OnException(Exception ex)
        {
            TBYException tex = new TBYSocketException(MethodBase.GetCurrentMethod().GetFullName(), "Veri yayın socketinde hata oluştu.", ex);
            ProcessException(tex);
        }

        private void NotSocketServer_OnLog(string log)
        {
            Log(log);
        }

        private void DistStreamQueue_OnDequeue(object arg1, QueueEventArgs<Action> arg)
        {
            arg.Entry();
        }

        private void DistStreamQueue_OnException(object arg1, QueueEventArgs<Action> arg2)
        {
            TBYException tex = new TBYQueueException(MethodInfo.GetCurrentMethod().GetFullName(), "Veri yayın DistStreamQueue_OnException hatası oluştu", arg2.Exception);
            ProcessException(tex);
        }



        private void DataProvider_OnHandleUknownFeed(FeedEventArgs arg)
        {
            Log("Unknown message " + arg.Message);
        }

        private void DataProvider_OnMaxTimeStampChanged(string time)
        {
            //MaxTimeStampStr = time;
        }

        #region Db


        public DbLogQueue FindQueue(string key)
        {
            DbLogQueue dbQ = null;
            string value;
            if (letterKeys.TryGetValue(key, out value))
            {
                if (DbLogQueueCollection != null)
                {
                    DbLogQueueCollection.TryGetValue(value, out dbQ);
                }
            }
            return dbQ;

        }

        private void AddDbLog(QueueEventArgs<SecurityDbLog> arg)
        {
            try
            {
                SecurityDbLog log = arg.Entry;
                log.DbManager.ExecuteNForget(log.Query, log.IsStoreProcedure, log.Parameters);
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYDBException(MethodBase.GetCurrentMethod().GetFullName(), "Veri yayın veritabanında hata oluştu.", ex);
                ProcessException(tex);
            }
        }
        private void AddNews(QueueEventArgs<NewsDbLog> arg)
        {
            try
            {
                NewsDbLog log = arg.Entry;
                log.DbManager.ExecuteNForget(log.Query, log.IsStoreProcedure, log.Parameters);
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYDBException(MethodBase.GetCurrentMethod().GetFullName(), "Veri yayın veritabanında hata oluştu.", ex);
                ProcessException(tex);
            }
        }

        private void SetSecurities()
        {

            try
            {
                DbQueryManager dbManager = new DbQueryManager(connString);
                IDataReader queryResult = dbManager.ToExecuteReader("sel_SecurityStream", true, new Dictionary<string, object>() { { "Kriter", 1 } });
                while (queryResult.Read())
                {
                    SecurityBuilder secBuilder = new SecurityBuilder();
                    Security sec = secBuilder.Build(queryResult);

                    dataProvider.SecurityValues.AddOrUpdate(sec.OrgSecurity, sec, (k, v) => { return sec; });
                }
                queryResult.Close();
                queryResult.Dispose();

            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Veritabanından semboller getirilirken hata oluştu", ex);
                ProcessException(tex);
            }

        }

        private void DbLogQueue_OnDequeue(object arg1, QueueEventArgs<SecurityDbLog> arg)
        {
            AddDbLog(arg);
        }
        private void DbLogQueue_OnException(object arg1, QueueEventArgs<SecurityDbLog> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "DbLogQueue_OnException hatası oluştu", arg2.Exception);
            ProcessException(tex);
        }


        private void DbNewsQueue_OnDequeue(object arg1, QueueEventArgs<NewsDbLog> arg)
        {
            AddNews(arg);
        }
        private void DbNewsQueue_OnException(object arg1, QueueEventArgs<NewsDbLog> arg2)
        {
            TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "DbNewsQueue_OnException hatası oluştu", arg2.Exception);
            ProcessException(tex);
        }



        #endregion

        #region Logging
        private StringQueue logQueue;
        public StringQueue LogQueue
        {
            get { return logQueue; }
            set
            {
                logQueue = value;
            }
        }

        public void StartLogger(string fileName)
        {
            try
            {

                LogQueue = new StringQueue();
                LogQueue.OnException += LogQueue_OnException;
                LogQueue.OnDequeue += LogQueue_OnDequeue;
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Logger açılırken hata oluştu", ex);
                QuantConnect.Logging.Log.Error(tex.ToString());
            }
        }
        public void StopLogger()
        {
            try
            {
                LogQueue.Stop();
                LogQueue.Dispose();
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Logger kapatılırken hata oluştu", ex);
                QuantConnect.Logging.Log.Error(tex.ToString());
            }
        }
        public void Log(string msg)
        {
            string txt = "";
            if (LogQueue != null)
            {
                txt = String.Format("{0} {1}", DateTime.Now.ToString("hh:mm:ss.fff"), msg);
                LogQueue.Enqueue(txt);
            }
            else
            {
                txt = String.Format("{0} LogQueue is null!!!.Log {1}", DateTime.Now.ToString("hh:mm:ss.fff"), msg);
                QuantConnect.Logging.Log.Error(txt);

            }
        }
        public void Error(Exception exp)
        {
            string str = "Error!!! " + exp.ToString();
            Log(str);
        }
        private void LogQueue_OnException(object arg1, QueueEventArgs<string> arg)
        {
            TBYException tex = new TBYAppContextTCPRecieveException(MethodBase.GetCurrentMethod().GetFullName(), "Kritik Hata!!! Log kuyruğunda log yazılırken hata oluştu!!!", arg.Exception);

            QuantConnect.Logging.Log.Error(tex.ToString());
        }

        private void LogQueue_OnDequeue(object arg1, QueueEventArgs<string> arg2)
        {
            try
            {
                QuantConnect.Logging.Log.Debug(arg2.Entry);
            }
            catch (Exception ex)
            {
                TBYException tex = new TBYException(MethodInfo.GetCurrentMethod().GetFullName(), "Log kuyruğunda log yazılırken hata oluştu", ex);
                QuantConnect.Logging.Log.Error(tex.ToString());
            }
        }
        #endregion

    }


}
