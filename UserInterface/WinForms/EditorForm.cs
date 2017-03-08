using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FarsiLibrary.Win;
using FastColoredTextBoxNS;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Drawing.Drawing2D;
using QuantConnect.Views.Util;
using System.Diagnostics;
using System.Threading.Tasks;
using QuantConnect.Packets;
using QuantConnect.Lean.Engine.Results;
using System.Windows.Forms.DataVisualization.Charting;
using QuantConnect.Interfaces;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using QuantConnect.Views.Model;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Logging;
using QuantConnect.Debug;
using System.Collections.Concurrent;
using QuantConnect.Views.UserControls;
using QuantConnect.Views.Model.Stocks;
using QuantConnect.Views.Controls.ActiveGrid;
using QuantConnect.Views.DataFeeds;
using QuantConnect.Views.Model.UI;
using QuantConnect.Views.Model.Algo;

namespace QuantConnect.Views.WinForms
{
    public partial class EditorForm : BaseForm
    {
        public delegate void PolulateGridRowEventHandler(string symbol);
        public delegate void CellUpdateEventHandler(object sender, CellUpdateEventArgs e);
        private object syncRoot = new object();
        private string[] MarketDataGridColumnNames = {Stock.TIME,Stock.SYMBOL,Stock.HIGH, Stock.LOW, Stock.CLOSE,Stock.VOLUME, 
                                                         Stock.PRICE, Stock.VALUE, Stock.ENDTIME,Stock.DATATYPE,Stock.PERIOD };
        //StockList stockList = new StockList();
        //private SafeBindingSource sbStocks;
        private ConcurrentDictionary<string, Stock> stocks;
        private bool isGridColorful = false;
        //private List<RandomDataFeed> dataFeeds = new List<RandomDataFeed>(5);
        UiQueue uiQueue;
        public EditorForm()
        {
            InitializeComponent();

        }

        private void EditorForm_Load(object sender, EventArgs e)
        {
            try
            {
                OnException += EditorForm_OnException;
                //init menu images
                InitControls();

                DebugHelper.Run(() =>
                {
                    Log.DebuggingEnabled = true;
                    Log.DebuggingLevel = 2;
                });

                //#if DEBUG
                //                Log.DebuggingEnabled = true;
                //                Log.DebuggingLevel = 2;
                //#endif

                InitAlgorithms();

                uiQueue = new UiQueue();
                uiQueue.OnDequeue += uiQueue_OnDequeue;
                uiQueue.OnException += uiQueue_OnException;
                //new ChartPopup().Show();

            }
            catch (Exception ex)
            {
                Alert(ex.ToString());
            }
        }

        private void uiQueue_OnException(object arg1, Teb.Infra.Collection.QueueEventArgs<Action> arg2)
        {
            WriteLog(new ErrorLbxMessageItem(arg2.Exception.ToString()));
        }

        private void uiQueue_OnDequeue(object arg1, Teb.Infra.Collection.QueueEventArgs<Action> arg)
        {
            arg.Entry();
        }

        private void EditorForm_OnException(ItemEventArgs<Exception> arg)
        {
            WriteLog(new ErrorLbxMessageItem(arg.Item.ToString()));
        }

        private void InitControls()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditorForm));
            copyToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripButton.Image")));
            cutToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("cutToolStripButton.Image")));
            pasteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("pasteToolStripButton.Image")));

            lbxError.DrawMode = DrawMode.OwnerDrawFixed;
            lbxLog.DrawMode = DrawMode.OwnerDrawFixed;

            lbxError.DrawItem += new DrawItemEventHandler(ListBox_DrawItem);
            lbxLog.DrawItem += new DrawItemEventHandler(ListBox_DrawItem);

            toolStripContainer.TopToolStripPanel.Controls.Add(tsActions);
            //toolStripContainer.TopToolStripPanel.Controls.Add(tsMain);

            lbxLog.Tag = tpLogs;
            lbxError.Tag = tpErrors;

            Closed += OnClosed;

            PopulateMarketDataGridColumns();

            //RandomDataFeed rdf = new RandomDataFeed(this.agStocks.Items.Count, this.agStocks.Columns.Count);
            //rdf.OnCellUpdate += new RandomDataFeed.OnCellUpdateHandler(rdf_OnCellUpdate);
            //rdf.OnStarted += new RandomDataFeed.OnStartedHandler(rdf_OnStarted);
            //rdf.OnStopped += new RandomDataFeed.OnStoppedHandler(rdf_OnStopped);
            //dataFeeds.Add(rdf);

            agStocks.UseAlternateRowColors = true;
            agStocks.AllowFlashing = true;
            //this.dataFeeds[0].RandomBurst();
            //PopulateMarketDataGridRows();
        }

        #region Algo Env
        private const string TAB = "----------- \t";
        private AlgoEngineBuilder _algoEnv;
        private readonly string releaseAlgoDirectory = "AlgoTemplates";
        private readonly string debugAlgoDirectory = "AlgoTemplates/Debug";
        private Dictionary<string, string> debugAlgorithms = new Dictionary<string, string>() {
        { "Test Algorithm 1","TEBBasicAlgo1"  },
        { "Test Algorithm 2" ,"TEBBasicAlgo2"},
        { "Test Algorithm 3","TEBBasicAlgo3"  },
        {  "Test Algorithm 4" ,"TEBBasicAlgo4" } ,
        { "Test Algorithm 5","TEBBasicAlgo5"  },
        { "MACD Algorithm" ,"TEBMACDAlgo"  },
        { "Chart Algorithm" ,"TEBChartTestAlgo"  }};
        private System.Windows.Forms.Timer _polling;
        private object lockObj = new object();
        private FastColoredTextBox selectedTextBox;

        private void InitAlgorithms()
        {
            if (IsDebugMode)
                debugToolStripMenuItem.Visible = true;
            else
            {
                debugToolStripMenuItem.Visible = false;
            }


            DebugHelper.Run(() => { InitDebugAlgorithms(); });
            InitReleaseAlgorithms();
        }


        private void InitDebugAlgorithms()
        {
            foreach (var keyValue in debugAlgorithms)
            {
                string displayName = keyValue.Key;
                string algoName = keyValue.Value;
                string fileName = algoName + ".cs";

                ToolStripMenuItem subMenu = CreateToolStrip(displayName, algoName, fileName, true);
                subMenu.Click += debugTemplatesToolStripMenuItem_Click;
                this.debugToolStripMenuItem.DropDownItems.Add(subMenu);
            }
        }

        private void InitReleaseAlgorithms()
        {
            string path = Path.Combine(BaseDirectory, releaseAlgoDirectory);
            string[] fileList = Directory.GetFiles(path, "*.cs");

            if (fileList != null)
            {
                foreach (var filePath in fileList)
                {
                    string fileName = Path.GetFileName(filePath);
                    string algoName = fileName.Replace(".cs", "");
                    string displayName = algoName;

                    ToolStripMenuItem subMenu = CreateToolStrip(displayName, algoName, fileName, false, filePath);
                    subMenu.Click += releaseTemplatesToolStripMenuItem_Click;
                    this.templatesToolStripMenuItem.DropDownItems.Add(subMenu);

                }
            }
        }

        private void debugTemplatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
                if (tsmi.Tag != null)
                {
                    AlgoCode algo = tsmi.Tag as AlgoCode;
                    string code = "";
                    string fileName = algo.FileName;
                    string path = Path.Combine(BaseDirectory, debugAlgoDirectory, fileName);

                    if (File.Exists(path))
                        code = File.ReadAllText(path);

                    //debugAlgorithms.TryGetValue(algoName, out code);
                    algo.Code = code;
                    CreateTabWithCode(algo);

                }
            }
            catch (Exception ex)
            {
                Alert(ex.ToString());
            }
        }

        private void releaseTemplatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
                if (tsmi.Tag != null)
                {
                    AlgoCode algo = tsmi.Tag as AlgoCode;
                    string code = "";
                    string fileName = algo.FileName;
                    string displayName = algo.DisplayName;
                    string path = Path.Combine(BaseDirectory, releaseAlgoDirectory, fileName);

                    if (File.Exists(path))
                        code = File.ReadAllText(path);

                    algo.Code = code;
                    algo.FilePath = path;
                    algo.FileName = fileName;
                    algo.DisplayName = displayName;
                    //algo.isDebugable = false;
                    CreateTabWithCode(algo);

                }
            }
            catch (Exception ex)
            {
                Alert(ex.ToString());
            }
        }

        public ToolStripMenuItem CreateToolStrip(string displayName, string name, string fileName, bool isDebugable, string algoFilePath = null)
        {
            ToolStripMenuItem subMenu = new System.Windows.Forms.ToolStripMenuItem();

            subMenu.Name = "tsmi" + name;
            subMenu.Text = displayName;
            subMenu.Tag = new AlgoCode(displayName, name, fileName, isDebugable, algoFilePath);
            return subMenu;
        }

        public FastColoredTextBox GetSelectedTextBox()
        {

            FastColoredTextBox tb = null;

            if (tsFiles.Items.Count > 0 && tsFiles.SelectedItem != null)
            {
                if (tsFiles.SelectedItem is FATabStripItem)
                {
                    FATabStripItem tab = tsFiles.SelectedItem as FATabStripItem;

                    if (tab.Controls[0] is FastColoredTextBox)
                    {
                        tb = tab.Controls[0] as FastColoredTextBox;

                    }

                }
            }
            return tb;
        }

        private void InitFields()
        {
            #region MyRegion
            //_console = new RichTextBox();
            //_console.Parent = this;
            //_console.ReadOnly = true;
            //_console.Multiline = true;
            //_console.Location = new Point(0, 0);
            //_console.Dock = DockStyle.Fill; 
            #endregion

            #region Chart
            //System.Windows.Forms.DataVisualization.Charting.Chart chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();

            //var series1 = new System.Windows.Forms.DataVisualization.Charting.Series
            //{
            //    Name = "Finance",
            //    Color = System.Drawing.Color.Green,
            //    IsVisibleInLegend = false,
            //    IsXValueIndexed = true,
            //    ChartType = SeriesChartType.Line
            //};


            //// Frist parameter is X-Axis and Second is Collection of Y- Axis
            //series1.Points.DataBindXY(new[] { 2001, 2002, 2003, 2004 }, new[] { 100, 200, 90, 150 });

            //chart1.Series.Add(series1);
            ////chart1.Series.Add(series1);
            ////Random rnd = new Random();
            ////for (int i = 0; i < 100; i++)
            ////{
            ////    int first = rnd.Next(0, 10);
            ////    int second = rnd.Next(0, 10); 
            ////    series1.Points.AddXY(first, second);
            ////}
            ////chart1.Invalidate();
            //chart1.Height = 300;
            //chart1.Width = 300;
            //chart1.Visible = true;
            //series1.SetDefault(true);
            //series1.Enabled = true;

            //chart1.ChartAreas.Add(new ChartArea());
            //chart1.ChartAreas[0].Area3DStyle.Enable3D = false;

            //pnlCharts.Controls.Add(chart1);  
            #endregion

        }

        private void algoEnv_OnLog(string msg)
        {
            WriteLog(new InfoLbxMessageItem(msg));
        }

        private void OnClosed(object sender, EventArgs eventArgs)
        {
            if (_algoEnv != null)
            {
                _algoEnv.Dispose();

            }
            Environment.Exit(0);
        }


        private void OnStart()
        {
            //stockList.Clear();
            //stockList.AllowNew = true;
            //stockList.AllowRemove = true;
            //stockList.AllowEdit = true;

            //sbStocks = new SafeBindingSource(true);
            //sbStocks.DataSource = stockList; 
            //dsStocks.DataSource = sbStocks;
            stocks = new ConcurrentDictionary<string, Stock>();

        }

        private void OnStop()
        {

        }
        //private void WriteLog(string message, bool isError = false)
        //{
        //    //message = DateTime.Now.ToString("u") + " " + message + Environment.NewLine;

        //    if (isError)
        //    {
        //        AddToLbx(lbxError, message);
        //        ChangeText(tpErrors, String.Format("Errors ({0})", lbxError.Items.Count));
        //    }
        //    else
        //    {
        //        AddToLbx(lbxLog, message);
        //        ChangeText(tpLogs, String.Format("Logs ({0})", lbxLog.Items.Count));
        //    }

        //}

        private void WriteLog(LbxMessageItem msg)
        {
            //message = DateTime.Now.ToString("u") + " " + message + Environment.NewLine;
            uiQueue.Enqueue(() =>
            {
                if (msg is ErrorLbxMessageItem)
                {
                    AddToLbx(lbxError, msg);
                    ChangeText(tpErrors, String.Format("Errors ({0})", lbxError.Items.Count));
                }
                else
                {
                    AddToLbx(lbxLog, msg);
                    ChangeText(tpLogs, String.Format("Logs ({0})", lbxLog.Items.Count));
                }
            });


        }

        public void LockCodeEdit(FastColoredTextBox ctrl, bool enabled)
        {
            //ctrl.Enabled = enabled;
            ctrl.ReadOnly = !enabled;
        }

        private void tsbStart_Click(object sender, EventArgs e)
        {
            try
            {
                ClearLbx(lbxError);
                ClearLbx(lbxLog);

                ChartPopup.CloseForms();

                bool isAlgorithmSelected = false;

                AlgoCode algo = null;
                if ((selectedTextBox = GetSelectedTextBox()) != null)
                {

                    isAlgorithmSelected = true;
                    //selectedTextBox.Enabled = false;
                    LockCodeEdit(selectedTextBox, false);

                }

                if (isAlgorithmSelected == false)
                {
                    Alert("Choose an algorithm");
                    return;
                }
                else
                {
                    algo = (selectedTextBox.Tag as TbInfo).AlgoCode;


                    ChangeText(tpErrors, "Errors");
                    ChangeText(tpLogs, "Logs");

                    if (algo.IsDebugable == false)
                    {
                        algo.Code = selectedTextBox.Text;


                        CompileFunc compiler = new CompileFunc(BaseDirectory);
                        IAlgorithm algorithm = compiler.RuntimeCompile(algo.Code);
                        QuantConnect.AlgorithmFactory.Loader.TEBAlgorithm = algorithm;
                    }
                    else
                    {
                        //selectedTextBox.Enabled = false;    
                        LockCodeEdit(selectedTextBox, false);
                        QuantConnect.AlgorithmFactory.Loader.TEBAlgorithm = null;
                    }



                    _algoEnv = new AlgoEngineBuilder();
                    _algoEnv.OnLog += algoEnv_OnLog;
                    _algoEnv.BuildEngine(algo.ClassName);  //_algoEnv.BuildEngine(algorithm, CurrentAssemblyLocation);

                    #region Polling

                    _polling = new System.Windows.Forms.Timer();
                    _polling.Interval = 1000;
                    _polling.Tick += PollingOnTick;
                    _polling.Start();



                    #endregion


                    tsbStart.Enabled = false;
                    tsbStop.Enabled = true;

                    OnStart();
                }

            }
            catch (Exception ex)
            {
                Alert(ex.ToString());
            }
        }


        private void tsbStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (_algoEnv != null)
                {
                    _algoEnv.Stop();
                }

                if (selectedTextBox != null)
                {
                    AlgoCode algo = (selectedTextBox.Tag as TbInfo).AlgoCode;

                    if (algo.IsDebugable == false)
                    {
                        //selectedTextBox.Enabled = true;
                        LockCodeEdit(selectedTextBox, true);
                    }
                }

                tsbStop.Enabled = false;
                tsbStart.Enabled = true;

                OnStop();
            }
            catch (Exception ex)
            {
                Alert(ex.ToString());
            }
        }


        private void PollingOnTick(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(() =>
            {
                Run(() =>
                {
                    lock (lockObj)
                    {
                        Packet message;
                        if (_algoEnv == null || _algoEnv.ResultsHandler == null)
                            return;


                        while (_algoEnv.ResultsHandler.Messages.TryDequeue(out message))
                        {
                            //WriteLog(new InfoLbxMessageItem("OnMessage :" + message.Type.ToString()));

                            OnMessage(message);


                        }


                        DisplayChart();

                        //if (_algoEnv.AlgorithmHandlers.Results is DesktopResultHandler)
                        //{
                        //    var resultHandler = (DesktopResultHandler)_algoEnv.AlgorithmHandlers.Results;
                        //    DisplayStatistics(resultHandler.FinalStatistics);
                        //}

                        if (!_algoEnv.IsActive)
                        {
                            WriteLog(new WarnLbxMessageItem("Backtest bitti"));
                            _polling.Stop();
                            _polling.Dispose();
                            _polling = null;

                        }
                    }
                });
            });
        }

        private void DisplayStatistics(Dictionary<string, string> dictionary)
        {
            ControlInvoke(flpStatistics, () =>
            {
                flpStatistics.Controls.Clear();
                foreach (var kvp in dictionary)
                {
                    flpStatistics.Controls.Add(new KeyValueLabel(kvp.Key, kvp.Value));
                }
            });
        }

        private void OnMessage(Packet message)
        {
            switch (message.Type)
            {
                case PacketType.BacktestResult:
                    //Draw chart

                    var backtestingResultHandler = (BacktestingResultHandler)_algoEnv.AlgorithmHandlers.Results;
                    var backTeststatistics = new Dictionary<string, string>();
                    backTeststatistics = backtestingResultHandler.FinalStatistics;

                    WriteLog(new WarnLbxMessageItem(TAB + " " + message.Type.ToString() + " Backtesting Result FinalStatistics " + TAB));
                    foreach (var sta in backTeststatistics)
                    {
                        WriteLog(new WarnLbxMessageItem(string.Format("{0} {1}", sta.Key, sta.Value)));
                    }


                    var br = (BacktestResultPacket)message;

                    if (br.Progress == 1)
                    {
                        foreach (var pair in br.Results.Statistics)
                        {
                            WriteLog(new WarnLbxMessageItem("STATISTICS:: " + pair.Key + " " + pair.Value));
                            //Console.WriteLine("\t\t\t\t{{\"{0}\",\"{1}\"}},", pair.Key, pair.Value);
                        }
                        //Console.WriteLine("\t\t\t});");

                        //foreach (var pair in statisticsResults.RollingPerformances)
                        //{
                        //    Log.Trace("ROLLINGSTATS:: " + pair.Key + " SharpeRatio: " + Math.Round(pair.Value.PortfolioStatistics.SharpeRatio, 3));
                        //}
                    }
                    break;

                case PacketType.LiveResult:

                    var liveTradingResultHandler = (LiveTradingResultHandler)_algoEnv.AlgorithmHandlers.Results;

                    var lr = (LiveResultPacket)message;

                    WriteLog(new WarnLbxMessageItem(TAB + " " + message.Type.ToString() + " Live Trading Result FinalStatistics " + TAB));

                    foreach (var pair in lr.Results.Statistics)
                    {
                        WriteLog(new WarnLbxMessageItem("STATISTICS:: " + pair.Key + " " + pair.Value));
                    }

                    break;
                case PacketType.AlgorithmStatus:
                    WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;

                case PacketType.RuntimeError:

                    if (message is RuntimeErrorPacket)
                    {
                        var runError = message as RuntimeErrorPacket;
                        if (runError != null)
                            WriteLog(new ErrorLbxMessageItem(TAB + " " + message.Type.ToString() + " " + runError.Message));
                    }
                    break;

                case PacketType.HandledError:
                    if (message is HandledErrorPacket)
                    {
                        var handledError = message as HandledErrorPacket;
                        if (handledError != null)
                            WriteLog(new ErrorLbxMessageItem(TAB + " " + message.Type.ToString() + " " + handledError.Message));
                    }
                    break;

                case PacketType.Log:
                    if (message is LogPacket)
                    {
                        var log = message as LogPacket;
                        if (log != null)
                            WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + log.Message));

                        if (log.Message.StartsWith("$$|"))
                        {
                            OnCustomMessage(log.Message);
                        }
                    }
                    break;

                case PacketType.Debug:
                    if (message is DebugPacket)
                    {
                        var debug = message as DebugPacket;
                        if (debug != null)
                            WriteLog(new DebugLbxMessageItem(TAB + " " + message.Type.ToString() + " " + debug.Message));
                    }
                    break;

                case PacketType.OrderEvent:
                    if (message is OrderEventPacket)
                    {
                        var oe = message as OrderEventPacket;
                        if (oe != null)
                            WriteLog(new WarnLbxMessageItem(TAB + " " + message.Type.ToString() + " " + oe.Event.ToString()));
                    }
                    break;
                case PacketType.None:
                    WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.AlgorithmNode:
                    if (message is AlgorithmNodePacket)
                    {
                        var an = message as AlgorithmNodePacket;
                        if (an != null)
                            WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + an.ToString()));
                    }
                    break;
                case PacketType.AutocompleteWork:
                    WriteLog(new WarnLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.AutocompleteResult:
                    WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.BacktestNode:
                    if (message is BacktestNodePacket)
                    {
                        var bn = message as BacktestNodePacket;
                        if (bn != null)
                            WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + bn.ToString()));
                    }
                    break;
                case PacketType.BacktestWork:
                    WriteLog(new WarnLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.LiveNode:
                    if (message is LiveNodePacket)
                    {
                        var ln = message as LiveNodePacket;
                        if (ln != null)
                            WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + ln.ToString()));
                    }
                    break;
                case PacketType.LiveWork:
                    WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.SecurityTypes:
                    WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.BacktestError:
                    WriteLog(new ErrorLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.BuildWork:
                    WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.BuildSuccess:
                    WriteLog(new WarnLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.BuildError:
                    WriteLog(new ErrorLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.Success:
                    WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.History:
                    WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + message.ToString()));
                    break;
                case PacketType.CommandResult:
                    if (message is CommandResultPacket)
                    {
                        var cr = message as CommandResultPacket;
                        if (cr != null)
                            WriteLog(new InfoLbxMessageItem(TAB + " " + message.Type.ToString() + " " + cr.ToString()));
                    }
                    break;
                case PacketType.GitHubHook:

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnCustomMessage(string message)
        {
            uiQueue.Enqueue(() =>
            {
                string[] strList = message.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                if (strList.Length == 13)
                {
                    if (strList[1] == "1")
                    {
                        DateTime time = DateTime.MinValue;
                        string symbol = null;
                        decimal high = 0;
                        decimal low = 0;
                        decimal close = 0;
                        decimal volume = 0;
                        decimal price = 0;
                        decimal value = 0;
                        DateTime endTime = DateTime.MinValue;
                        string dataType = null;
                        string period = null;



                        DateTime.TryParse(strList[2], out time);
                        symbol = strList[3];
                        Decimal.TryParse(strList[4], out high);
                        Decimal.TryParse(strList[5], out low);
                        Decimal.TryParse(strList[6], out close);
                        Decimal.TryParse(strList[7], out volume);
                        Decimal.TryParse(strList[8], out price);
                        Decimal.TryParse(strList[9], out value);
                        DateTime.TryParse(strList[10], out endTime);
                        dataType = strList[11];
                        period = strList[12];
                        Stock stock;

                        if (!stocks.TryGetValue(symbol, out stock))
                        {
                            stock = new Stock();
                            stock.OnCellUpdate += stock_OnCellUpdate;
                            if (stocks.TryAdd(symbol, stock))
                            {
                                stock.RowIndex = stocks.Values.Count - 1;
                                PopulateMarketDataGridRow(symbol);

                            }
                        }

                        stock.Change(time, symbol, high, low, close, volume, price, value, endTime, dataType, period);
                        //uiQueue.Enqueue(() =>
                        //{

                        //    QuantConnect.Views.Controls.ActiveGrid.ActiveRow.ActiveCell cell = this.agStocks.FindCell(String.Format("{0}_{1}", symbol, Stock.HIGH));
                        //    stock_OnCellUpdate(null, new CellUpdateEventArgs(0, 3, high, String.Format("{0}_{1}", symbol, Stock.HIGH)));
                        //    stock_OnCellUpdate(null, new CellUpdateEventArgs(0, 4, low, String.Format("{0}_{1}", symbol, Stock.LOW)));
                        //    stock_OnCellUpdate(null, new CellUpdateEventArgs(0, 5, close, String.Format("{0}_{1}", symbol, Stock.CLOSE)));
                        //    stock_OnCellUpdate(null, new CellUpdateEventArgs(0, 6, volume, String.Format("{0}_{1}", symbol, Stock.VOLUME)));
                        //    stock_OnCellUpdate(null, new CellUpdateEventArgs(0, 7, price, String.Format("{0}_{1}", symbol, Stock.PRICE)));
                        //    stock_OnCellUpdate(null, new CellUpdateEventArgs(0, 8, value, String.Format("{0}_{1}", symbol, Stock.VALUE)));
                        //});

                    }
                }
            });
        }

        private void stock_OnCellUpdate(object sender, CellUpdateEventArgs e)
        {

            if (this.InvokeRequired)
            {
                this.Invoke(new CellUpdateEventHandler(this.stock_OnCellUpdate), new object[] { sender, e });
                return;
            }
            //uiQueue.Enqueue(() =>
            //{
            CellUpdate(e);
            //});
        }

        private void PopulateMarketDataGridColumns()
        {
            this.agStocks.SuspendLayout();

            // Add the row header column
            QuantConnect.Views.Controls.ActiveGrid.ActiveColumnHeader column = new QuantConnect.Views.Controls.ActiveGrid.ActiveColumnHeader();
            column.Text = "";
            column.Name = "CCY";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = QuantConnect.Views.Controls.ActiveGrid.SortOrderEnum.Unsorted;
            column.Text = "";
            column.Width = 70;
            this.agStocks.Columns.Add(column);

            foreach (string ccy in this.MarketDataGridColumnNames)
            {
                column = new QuantConnect.Views.Controls.ActiveGrid.ActiveColumnHeader();
                column.Text = ccy;
                column.Name = ccy;
                column.CellFormat = "N";
                column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
                column.DisplayZeroValues = false;
                column.SortOrder = QuantConnect.Views.Controls.ActiveGrid.SortOrderEnum.Unsorted;
                column.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
                column.Width = 55;
                this.agStocks.Columns.Add(column);
            }

            this.agStocks.ResumeLayout();
        }

        private void PopulateMarketDataGridRow(string symbol)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new PolulateGridRowEventHandler(this.PopulateMarketDataGridRow), symbol);
                return;
            }
            // This is essential as it will prevent the 'flashing' functionality
            // from being triggered while the grid is being initialised.
            this.agStocks.SuspendLayout();

            // Create a list to hold all of the row items.
            List<QuantConnect.Views.Controls.ActiveGrid.ActiveRow>
                items = new List<QuantConnect.Views.Controls.ActiveGrid.ActiveRow>(20);


            // Create a new row.
            QuantConnect.Views.Controls.ActiveGrid.ActiveRow item = new QuantConnect.Views.Controls.ActiveGrid.ActiveRow();
            item.Text = symbol;
            item.Name = symbol;

            // Add the cells to the row, one for each column.
            // N.B. Starting from column ONE not column ZERO as this is
            //      reserved for the row header.
            for (int i = 1; i < this.agStocks.Columns.Count; i++)
            {
                QuantConnect.Views.Controls.ActiveGrid.ActiveRow.ActiveCell cell =
                    new QuantConnect.Views.Controls.ActiveGrid.ActiveRow.ActiveCell(item, String.Empty);
                cell.Name = String.Format("{0}_{1}", symbol, this.agStocks.Columns[i].Name);
                cell.DecimalValue = Decimal.Zero;
                cell.PreTextFont = new Font("Arial", cell.Font.Size);
                cell.PostTextFont = new Font("Arial", cell.Font.Size);
                item.SubItems.Add(cell);
            }

            items.Add(item);


            // Add all of the rows to the list view.
            this.agStocks.Items.AddRange(items.ToArray());

            this.agStocks.ResumeLayout();
        }

        private void DisplayChart()
        {

            if (_algoEnv == null || _algoEnv.ResultsHandler == null)
                return;
            Run(() =>
            {
                foreach (Chart chart in _algoEnv.ResultsHandler.Charts.Values)
                {
                    ChartPopup popup = null;
                    BeginInvoke(new Action(() =>
                    {
                        string chartName = chart.Name;

                        popup = ChartPopup.GetOrCreate(chartName);

                        popup.DisplayChart(chart);
                    }));

                }

            });
        }


        #endregion

        #region UI Editor
        string[] keywords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "add", "alias", "ascending", "descending", "dynamic", "from", "get", "global", "group", "into", "join", "let", "orderby", "partial", "remove", "select", "set", "value", "var", "where", "yield" };
        string[] methods = { "Equals()", "GetHashCode()", "GetType()", "ToString()" };
        string[] snippets = { "if(^)\n{\n;\n}", "if(^)\n{\n;\n}\nelse\n{\n;\n}", "for(^;;)\n{\n;\n}", "while(^)\n{\n;\n}", "do\n{\n^;\n}while();", "switch(^)\n{\ncase : break;\n}" };
        string[] declarationSnippets = {
               "public class ^\n{\n}", "private class ^\n{\n}", "internal class ^\n{\n}",
               "public struct ^\n{\n;\n}", "private struct ^\n{\n;\n}", "internal struct ^\n{\n;\n}",
               "public void ^()\n{\n;\n}", "private void ^()\n{\n;\n}", "internal void ^()\n{\n;\n}", "protected void ^()\n{\n;\n}",
               "public ^{ get; set; }", "private ^{ get; set; }", "internal ^{ get; set; }", "protected ^{ get; set; }"
               };
        Style invisibleCharsStyle = new InvisibleCharsRenderer(Pens.Gray);
        Color currentLineColor = Color.FromArgb(100, 210, 210, 255);
        Color changedLineColor = Color.FromArgb(255, 230, 230, 255);

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateTab(null);
        }

        private Style sameWordsStyle = new FastColoredTextBoxNS.MarkerStyle(new SolidBrush(Color.FromArgb(50, Color.Gray)));

        private void CreateTab(string fileName)
        {
            try
            {
                var tb = new FastColoredTextBox();
                tb.Font = new Font("Consolas", 9.75f);
                tb.ContextMenuStrip = cmMain;
                tb.Dock = DockStyle.Fill;
                tb.BorderStyle = BorderStyle.Fixed3D;
                //tb.VirtualSpace = true;
                tb.LeftPadding = 17;
                tb.Language = FastColoredTextBoxNS.Language.CSharp;
                tb.AddStyle(sameWordsStyle);//same words style
                var tab = new FATabStripItem(fileName != null ? Path.GetFileName(fileName) : "[new]", tb);
                tab.Tag = fileName;
                if (fileName != null)
                    tb.OpenFile(fileName);
                tb.Tag = new TbInfo();
                tsFiles.AddTab(tab);
                tsFiles.SelectedItem = tab;
                tb.Focus();
                tb.DelayedTextChangedInterval = 1000;
                tb.DelayedEventsInterval = 500;
                tb.TextChangedDelayed += new EventHandler<TextChangedEventArgs>(tb_TextChangedDelayed);
                tb.SelectionChangedDelayed += new EventHandler(tb_SelectionChangedDelayed);
                tb.KeyDown += new KeyEventHandler(tb_KeyDown);
                tb.MouseMove += new MouseEventHandler(tb_MouseMove);
                tb.ChangedLineColor = changedLineColor;
                if (btHighlightCurrentLine.Checked)
                    tb.CurrentLineColor = currentLineColor;
                tb.ShowFoldingLines = btShowFoldingLines.Checked;
                tb.HighlightingRangeType = HighlightingRangeType.VisibleRange;
                //create autocomplete popup menu
                AutocompleteMenu popupMenu = new AutocompleteMenu(tb);
                popupMenu.Items.ImageList = ilAutocomplete;
                popupMenu.Opening += new EventHandler<CancelEventArgs>(popupMenu_Opening);
                BuildAutocompleteMenu(popupMenu);
                (tb.Tag as TbInfo).popupMenu = popupMenu;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Retry)
                    CreateTab(fileName);
            }
        }

        private void CreateTabWithCode(AlgoCode code)
        {
            try
            {
                var tb = new FastColoredTextBox();
                tb.Font = new Font("Consolas", 9.75f);
                tb.ContextMenuStrip = cmMain;
                tb.Dock = DockStyle.Fill;
                tb.BorderStyle = BorderStyle.Fixed3D;
                //tb.VirtualSpace = true;
                tb.LeftPadding = 17;
                tb.Language = FastColoredTextBoxNS.Language.CSharp;
                tb.AddStyle(sameWordsStyle);//same words style
                var tab = new FATabStripItem(code.DisplayName != null ? Path.GetFileName(code.DisplayName) : "[new]", tb);
                tab.Tag = code.DisplayName;
                //if (fileName != null)
                //    tb.OpenFile(fileName);

                if (code.Code != null)
                    tb.Text = code.Code;

                if (code.IsDebugable)
                    LockCodeEdit(tb, false);
                //tb.Enabled = false;


                tb.Tag = new TbInfo() { AlgoCode = code };

                tsFiles.AddTab(tab);
                tsFiles.SelectedItem = tab;
                tb.Focus();
                tb.DelayedTextChangedInterval = 1000;
                tb.DelayedEventsInterval = 500;
                tb.TextChangedDelayed += new EventHandler<TextChangedEventArgs>(tb_TextChangedDelayed);
                tb.SelectionChangedDelayed += new EventHandler(tb_SelectionChangedDelayed);
                tb.KeyDown += new KeyEventHandler(tb_KeyDown);
                tb.MouseMove += new MouseEventHandler(tb_MouseMove);
                tb.ChangedLineColor = changedLineColor;
                if (btHighlightCurrentLine.Checked)
                    tb.CurrentLineColor = currentLineColor;
                tb.ShowFoldingLines = btShowFoldingLines.Checked;
                tb.HighlightingRangeType = HighlightingRangeType.VisibleRange;
                //create autocomplete popup menu
                AutocompleteMenu popupMenu = new AutocompleteMenu(tb);
                popupMenu.Items.ImageList = ilAutocomplete;
                popupMenu.Opening += new EventHandler<CancelEventArgs>(popupMenu_Opening);
                BuildAutocompleteMenu(popupMenu);
                (tb.Tag as TbInfo).popupMenu = popupMenu;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Retry)
                    CreateTab(code.DisplayName);
            }
        }

        private void popupMenu_Opening(object sender, CancelEventArgs e)
        {
            //---block autocomplete menu for comments
            //get index of green style (used for comments)
            var iGreenStyle = CurrentTB.GetStyleIndex(CurrentTB.SyntaxHighlighter.GreenStyle);
            if (iGreenStyle >= 0)
                if (CurrentTB.Selection.Start.iChar > 0)
                {
                    //current char (before caret)
                    var c = CurrentTB[CurrentTB.Selection.Start.iLine][CurrentTB.Selection.Start.iChar - 1];
                    //green Style
                    var greenStyleIndex = Range.ToStyleIndex(iGreenStyle);
                    //if char contains green style then block popup menu
                    if ((c.style & greenStyleIndex) != 0)
                        e.Cancel = true;
                }
        }

        private void BuildAutocompleteMenu(AutocompleteMenu popupMenu)
        {
            List<AutocompleteItem> items = new List<AutocompleteItem>();

            foreach (var item in snippets)
                items.Add(new SnippetAutocompleteItem(item) { ImageIndex = 1 });
            foreach (var item in declarationSnippets)
                items.Add(new DeclarationSnippet(item) { ImageIndex = 0 });
            foreach (var item in methods)
                items.Add(new MethodAutocompleteItem(item) { ImageIndex = 2 });
            foreach (var item in keywords)
                items.Add(new AutocompleteItem(item));

            items.Add(new InsertSpaceSnippet());
            items.Add(new InsertSpaceSnippet(@"^(\w+)([=<>!:]+)(\w+)$"));
            items.Add(new InsertEnterSnippet());

            //set as autocomplete source
            popupMenu.Items.SetAutocompleteItems(items);
            popupMenu.SearchPattern = @"[\w\.:=!<>]";
        }

        void tb_MouseMove(object sender, MouseEventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            var place = tb.PointToPlace(e.Location);
            var r = new Range(tb, place, place);

            string text = r.GetFragment("[a-zA-Z]").Text;
            lbWordUnderMouse.Text = text;
        }

        void tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.OemMinus)
            {
                NavigateBackward();
                e.Handled = true;
            }

            if (e.Modifiers == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.OemMinus)
            {
                NavigateForward();
                e.Handled = true;
            }

            if (e.KeyData == (Keys.K | Keys.Control))
            {
                //forced show (MinFragmentLength will be ignored)
                (CurrentTB.Tag as TbInfo).popupMenu.Show(true);
                e.Handled = true;
            }
        }

        void tb_SelectionChangedDelayed(object sender, EventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            //remember last visit time
            if (tb.Selection.IsEmpty && tb.Selection.Start.iLine < tb.LinesCount)
            {
                if (lastNavigatedDateTime != tb[tb.Selection.Start.iLine].LastVisit)
                {
                    tb[tb.Selection.Start.iLine].LastVisit = DateTime.Now;
                    lastNavigatedDateTime = tb[tb.Selection.Start.iLine].LastVisit;
                }
            }

            //highlight same words
            tb.VisibleRange.ClearStyle(sameWordsStyle);
            if (!tb.Selection.IsEmpty)
                return;//user selected diapason
            //get fragment around caret
            var fragment = tb.Selection.GetFragment(@"\w");
            string text = fragment.Text;
            if (text.Length == 0)
                return;
            //highlight same words
            Range[] ranges = tb.VisibleRange.GetRanges("\\b" + text + "\\b").ToArray();

            if (ranges.Length > 1)
                foreach (var r in ranges)
                    r.SetStyle(sameWordsStyle);
        }

        void tb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            FastColoredTextBox tb = (sender as FastColoredTextBox);
            //rebuild object explorer
            string text = (sender as FastColoredTextBox).Text;
            ThreadPool.QueueUserWorkItem(
                (o) => ReBuildObjectExplorer(text)
            );

            //show invisible chars
            HighlightInvisibleChars(e.ChangedRange);
        }

        private void HighlightInvisibleChars(Range range)
        {
            range.ClearStyle(invisibleCharsStyle);
            if (btInvisibleChars.Checked)
                range.SetStyle(invisibleCharsStyle, @".$|.\r\n|\s");
        }

        List<ExplorerItem> explorerList = new List<ExplorerItem>();

        private void ReBuildObjectExplorer(string text)
        {
            try
            {
                List<ExplorerItem> list = new List<ExplorerItem>();
                int lastClassIndex = -1;
                //find classes, methods and properties
                Regex regex = new Regex(@"^(?<range>[\w\s]+\b(class|struct|enum|interface)\s+[\w<>,\s]+)|^\s*(public|private|internal|protected)[^\n]+(\n?\s*{|;)?", RegexOptions.Multiline);
                foreach (Match r in regex.Matches(text))
                    try
                    {
                        string s = r.Value;
                        int i = s.IndexOfAny(new char[] { '=', '{', ';' });
                        if (i >= 0)
                            s = s.Substring(0, i);
                        s = s.Trim();

                        var item = new ExplorerItem() { title = s, position = r.Index };
                        if (Regex.IsMatch(item.title, @"\b(class|struct|enum|interface)\b"))
                        {
                            item.title = item.title.Substring(item.title.LastIndexOf(' ')).Trim();
                            item.type = ExplorerItemType.Class;
                            list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), new ExplorerItemComparer());
                            lastClassIndex = list.Count;
                        }
                        else
                            if (item.title.Contains(" event "))
                            {
                                int ii = item.title.LastIndexOf(' ');
                                item.title = item.title.Substring(ii).Trim();
                                item.type = ExplorerItemType.Event;
                            }
                            else
                                if (item.title.Contains("("))
                                {
                                    var parts = item.title.Split('(');
                                    item.title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "(" + parts[1];
                                    item.type = ExplorerItemType.Method;
                                }
                                else
                                    if (item.title.EndsWith("]"))
                                    {
                                        var parts = item.title.Split('[');
                                        if (parts.Length < 2) continue;
                                        item.title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "[" + parts[1];
                                        item.type = ExplorerItemType.Method;
                                    }
                                    else
                                    {
                                        int ii = item.title.LastIndexOf(' ');
                                        item.title = item.title.Substring(ii).Trim();
                                        item.type = ExplorerItemType.Property;
                                    }
                        list.Add(item);
                    }
                    catch { ; }

                list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), new ExplorerItemComparer());

                BeginInvoke(
                    new Action(() =>
                    {
                        explorerList = list;
                        dgvObjectExplorer.RowCount = explorerList.Count;
                        dgvObjectExplorer.Invalidate();
                    })
                );
            }
            catch { ; }
        }

        enum ExplorerItemType
        {
            Class, Method, Property, Event
        }

        class ExplorerItem
        {
            public ExplorerItemType type;
            public string title;
            public int position;
        }

        class ExplorerItemComparer : IComparer<ExplorerItem>
        {
            public int Compare(ExplorerItem x, ExplorerItem y)
            {
                return x.title.CompareTo(y.title);
            }
        }

        private void tsFiles_TabStripItemClosing(TabStripItemClosingEventArgs e)
        {
            if ((e.Item.Controls[0] as FastColoredTextBox).IsChanged)
            {
                switch (MessageBox.Show("Do you want save " + e.Item.Title + " ?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information))
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        if (!Save(e.Item))
                            e.Cancel = true;
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private bool Save(FATabStripItem tab)
        {
            //var tb = (tab.Controls[0] as FastColoredTextBox);

            var tb = GetSelectedTextBox();
            AlgoCode algo = (tb.Tag as TbInfo).AlgoCode;

            if (tab.Tag == null)
            {
                if (sfdMain.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return false;
                tab.Title = Path.GetFileName(sfdMain.FileName);
                tab.Tag = sfdMain.FileName;
            }

            try
            {
                //File.WriteAllText(tab.Tag as string, tb.Text);
                File.WriteAllText(algo.FilePath, tb.Text);
                tb.IsChanged = false;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    return Save(tab);
                else
                    return false;
            }

            tb.Invalidate();

            return true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
                Save(tsFiles.SelectedItem);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
            {
                string oldFile = tsFiles.SelectedItem.Tag as string;
                tsFiles.SelectedItem.Tag = null;
                if (!Save(tsFiles.SelectedItem))
                    if (oldFile != null)
                    {
                        tsFiles.SelectedItem.Tag = oldFile;
                        tsFiles.SelectedItem.Title = Path.GetFileName(oldFile);
                    }
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofdMain.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                CreateTab(ofdMain.FileName);
        }

        FastColoredTextBox CurrentTB
        {
            get
            {
                if (tsFiles.SelectedItem == null)
                    return null;
                return (tsFiles.SelectedItem.Controls[0] as FastColoredTextBox);
            }

            set
            {
                tsFiles.SelectedItem = (value.Parent as FATabStripItem);
                value.Focus();
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Selection.SelectAll();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB.UndoEnabled)
                CurrentTB.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB.RedoEnabled)
                CurrentTB.Redo();
        }

        private void tmUpdateInterface_Tick(object sender, EventArgs e)
        {
            try
            {
                if (CurrentTB != null && tsFiles.Items.Count > 0)
                {
                    var tb = CurrentTB;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = tb.UndoEnabled;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = tb.RedoEnabled;
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = tb.IsChanged;
                    saveAsToolStripMenuItem.Enabled = true;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = true;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                    copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = !tb.Selection.IsEmpty;
                    printToolStripButton.Enabled = true;
                }
                else
                {
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = false;
                    saveAsToolStripMenuItem.Enabled = false;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                    copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = false;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = false;
                    printToolStripButton.Enabled = false;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = false;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = false;
                    dgvObjectExplorer.RowCount = 0;
                }
            }
            catch (Exception ex)
            {
                WriteLog(new ErrorLbxMessageItem(ex.ToString()));
            }
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
            {
                var settings = new PrintDialogSettings();
                settings.Title = tsFiles.SelectedItem.Title;
                settings.Header = "&b&w&b";
                settings.Footer = "&b&p";
                CurrentTB.Print(settings);
            }
        }

        bool tbFindChanged = false;

        private void tbFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && CurrentTB != null)
            {
                Range r = tbFindChanged ? CurrentTB.Range.Clone() : CurrentTB.Selection.Clone();
                tbFindChanged = false;
                r.End = new Place(CurrentTB[CurrentTB.LinesCount - 1].Count, CurrentTB.LinesCount - 1);
                var pattern = Regex.Escape(tbFind.Text);
                foreach (var found in r.GetRanges(pattern))
                {
                    found.Inverse();
                    CurrentTB.Selection = found;
                    CurrentTB.DoSelectionVisible();
                    return;
                }
                MessageBox.Show("Not found.");
            }
            else
                tbFindChanged = true;
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.ShowFindDialog();
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.ShowReplaceDialog();
        }

        private void PowerfulCSharpEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            List<FATabStripItem> list = new List<FATabStripItem>();
            foreach (FATabStripItem tab in tsFiles.Items)
                list.Add(tab);
            foreach (var tab in list)
            {
                TabStripItemClosingEventArgs args = new TabStripItemClosingEventArgs(tab);
                tsFiles_TabStripItemClosing(args);
                if (args.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                tsFiles.RemoveTab(tab);
            }
        }

        private void dgvObjectExplorer_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (CurrentTB != null)
            {
                var item = explorerList[e.RowIndex];
                CurrentTB.GoEnd();
                CurrentTB.SelectionStart = item.position;
                CurrentTB.DoSelectionVisible();
                CurrentTB.Focus();
            }
        }

        private void dgvObjectExplorer_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                ExplorerItem item = explorerList[e.RowIndex];
                if (e.ColumnIndex == 1)
                    e.Value = item.title;
                else
                    switch (item.type)
                    {

                        case ExplorerItemType.Class:
                            e.Value = global::QuantConnect.Views.Properties.Resources.class_libraries;
                            return;
                        case ExplorerItemType.Method:
                            e.Value = global::QuantConnect.Views.Properties.Resources.box;
                            return;
                        case ExplorerItemType.Event:
                            e.Value = global::QuantConnect.Views.Properties.Resources.lightning;
                            return;
                        case ExplorerItemType.Property:
                            e.Value = global::QuantConnect.Views.Properties.Resources.property;
                            return;
                    }
            }
            catch { ; }
        }

        private void tsFiles_TabStripItemSelectionChanged(TabStripItemChangedEventArgs e)
        {
            if (CurrentTB != null)
            {
                CurrentTB.Focus();
                string text = CurrentTB.Text;
                ThreadPool.QueueUserWorkItem(
                    (o) => ReBuildObjectExplorer(text)
                );
            }
        }

        private void backStripButton_Click(object sender, EventArgs e)
        {
            NavigateBackward();
        }

        private void forwardStripButton_Click(object sender, EventArgs e)
        {
            NavigateForward();
        }

        DateTime lastNavigatedDateTime = DateTime.Now;

        private bool NavigateBackward()
        {
            DateTime max = new DateTime();
            int iLine = -1;
            FastColoredTextBox tb = null;
            for (int iTab = 0; iTab < tsFiles.Items.Count; iTab++)
            {
                var t = (tsFiles.Items[iTab].Controls[0] as FastColoredTextBox);
                for (int i = 0; i < t.LinesCount; i++)
                    if (t[i].LastVisit < lastNavigatedDateTime && t[i].LastVisit > max)
                    {
                        max = t[i].LastVisit;
                        iLine = i;
                        tb = t;
                    }
            }
            if (iLine >= 0)
            {
                tsFiles.SelectedItem = (tb.Parent as FATabStripItem);
                tb.Navigate(iLine);
                lastNavigatedDateTime = tb[iLine].LastVisit;
                //WriteLog("Backward: " + lastNavigatedDateTime, false);
                tb.Focus();
                tb.Invalidate();
                return true;
            }
            else
                return false;
        }

        private bool NavigateForward()
        {
            DateTime min = DateTime.Now;
            int iLine = -1;
            FastColoredTextBox tb = null;
            for (int iTab = 0; iTab < tsFiles.Items.Count; iTab++)
            {
                var t = (tsFiles.Items[iTab].Controls[0] as FastColoredTextBox);
                for (int i = 0; i < t.LinesCount; i++)
                    if (t[i].LastVisit > lastNavigatedDateTime && t[i].LastVisit < min)
                    {
                        min = t[i].LastVisit;
                        iLine = i;
                        tb = t;
                    }
            }
            if (iLine >= 0)
            {
                tsFiles.SelectedItem = (tb.Parent as FATabStripItem);
                tb.Navigate(iLine);
                lastNavigatedDateTime = tb[iLine].LastVisit;
                //WriteLog("Forward: " + lastNavigatedDateTime, false);
                tb.Focus();
                tb.Invalidate();
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// This item appears when any part of snippet text is typed
        /// </summary>
        class DeclarationSnippet : SnippetAutocompleteItem
        {
            public DeclarationSnippet(string snippet)
                : base(snippet)
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var pattern = Regex.Escape(fragmentText);
                if (Regex.IsMatch(Text, "\\b" + pattern, RegexOptions.IgnoreCase))
                    return CompareResult.Visible;
                return CompareResult.Hidden;
            }
        }

        /// <summary>
        /// Divides numbers and words: "123AND456" -> "123 AND 456"
        /// Or "i=2" -> "i = 2"
        /// </summary>
        class InsertSpaceSnippet : AutocompleteItem
        {
            string pattern;

            public InsertSpaceSnippet(string pattern)
                : base("")
            {
                this.pattern = pattern;
            }

            public InsertSpaceSnippet()
                : this(@"^(\d+)([a-zA-Z_]+)(\d*)$")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                if (Regex.IsMatch(fragmentText, pattern))
                {
                    Text = InsertSpaces(fragmentText);
                    if (Text != fragmentText)
                        return CompareResult.Visible;
                }
                return CompareResult.Hidden;
            }

            public string InsertSpaces(string fragment)
            {
                var m = Regex.Match(fragment, pattern);
                if (m == null)
                    return fragment;
                if (m.Groups[1].Value == "" && m.Groups[3].Value == "")
                    return fragment;
                return (m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value).Trim();
            }

            public override string ToolTipTitle
            {
                get
                {
                    return Text;
                }
            }
        }

        /// <summary>
        /// Inerts line break after '}'
        /// </summary>
        class InsertEnterSnippet : AutocompleteItem
        {
            Place enterPlace = Place.Empty;

            public InsertEnterSnippet()
                : base("[Line break]")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var r = Parent.Fragment.Clone();
                while (r.Start.iChar > 0)
                {
                    if (r.CharBeforeStart == '}')
                    {
                        enterPlace = r.Start;
                        return CompareResult.Visible;
                    }

                    r.GoLeftThroughFolded();
                }

                return CompareResult.Hidden;
            }

            public override string GetTextForReplace()
            {
                //extend range
                Range r = Parent.Fragment;
                Place end = r.End;
                r.Start = enterPlace;
                r.End = r.End;
                //insert line break
                return Environment.NewLine + r.Text;
            }

            public override void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e)
            {
                base.OnSelected(popupMenu, e);
                if (Parent.Fragment.tb.AutoIndent)
                    Parent.Fragment.tb.DoAutoIndent();
            }

            public override string ToolTipTitle
            {
                get
                {
                    return "Insert line break after '}'";
                }
            }
        }

        private void autoIndentSelectedTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.DoAutoIndent();
        }

        private void btInvisibleChars_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
                HighlightInvisibleChars((tab.Controls[0] as FastColoredTextBox).Range);
            if (CurrentTB != null)
                CurrentTB.Invalidate();
        }

        private void btHighlightCurrentLine_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
            {
                if (btHighlightCurrentLine.Checked)
                    (tab.Controls[0] as FastColoredTextBox).CurrentLineColor = currentLineColor;
                else
                    (tab.Controls[0] as FastColoredTextBox).CurrentLineColor = Color.Transparent;
            }
            if (CurrentTB != null)
                CurrentTB.Invalidate();
        }

        private void commentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.InsertLinePrefix("//");
        }

        private void uncommentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.RemoveLinePrefix("//");
        }

        private void cloneLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //expand selection
            CurrentTB.Selection.Expand();
            //get text of selected lines
            string text = Environment.NewLine + CurrentTB.Selection.Text;
            //move caret to end of selected lines
            CurrentTB.Selection.Start = CurrentTB.Selection.End;
            //insert text
            CurrentTB.InsertText(text);
        }

        private void cloneLinesAndCommentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //start autoUndo block
            CurrentTB.BeginAutoUndo();
            //expand selection
            CurrentTB.Selection.Expand();
            //get text of selected lines
            string text = Environment.NewLine + CurrentTB.Selection.Text;
            //comment lines
            CurrentTB.InsertLinePrefix("//");
            //move caret to end of selected lines
            CurrentTB.Selection.Start = CurrentTB.Selection.End;
            //insert text
            CurrentTB.InsertText(text);
            //end of autoUndo block
            CurrentTB.EndAutoUndo();
        }

        private void bookmarkPlusButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB == null)
                return;
            CurrentTB.BookmarkLine(CurrentTB.Selection.Start.iLine);
        }

        private void bookmarkMinusButton_Click(object sender, EventArgs e)
        {
            if (CurrentTB == null)
                return;
            CurrentTB.UnbookmarkLine(CurrentTB.Selection.Start.iLine);
        }

        private void gotoButton_DropDownOpening(object sender, EventArgs e)
        {
            gotoButton.DropDownItems.Clear();
            foreach (Control tab in tsFiles.Items)
            {
                FastColoredTextBox tb = tab.Controls[0] as FastColoredTextBox;
                foreach (var bookmark in tb.Bookmarks)
                {
                    var item = gotoButton.DropDownItems.Add(bookmark.Name + " [" + Path.GetFileNameWithoutExtension(tab.Tag as String) + "]");
                    item.Tag = bookmark;
                    item.Click += (o, a) =>
                    {
                        var b = (Bookmark)(o as ToolStripItem).Tag;
                        try
                        {
                            CurrentTB = b.TB;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            return;
                        }
                        b.DoVisible();
                    };
                }
            }
        }

        private void btShowFoldingLines_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
                (tab.Controls[0] as FastColoredTextBox).ShowFoldingLines = btShowFoldingLines.Checked;
            if (CurrentTB != null)
                CurrentTB.Invalidate();
        }

        private void Zoom_click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.Zoom = int.Parse((sender as ToolStripItem).Tag.ToString());
        }

        private void temizleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            object tmp = ((sender as ToolStripMenuItem).Owner as ContextMenuStrip).SourceControl;
            if (tmp is ListBox)
            {
                ListBox tmpLbx = tmp as ListBox;
                Clear(tmpLbx);

                //TODO : asagisinin dinamik olmasi gerekiyor

                if (tmpLbx.Tag != null)
                {
                    if (tmpLbx.Tag is TabPage)
                    {
                        TabPage tmpPage = tmpLbx.Tag as TabPage;

                        if (tmpPage.Tag != null)
                        {
                            ChangeText(tmpPage, String.Format(tmpPage.Tag.ToString(), 0));
                        }
                    }
                }

            }
        }
        #endregion

        #region --- Data Feed Event Handlers -----------------

        /// <summary>
        /// Handles changes to an individual cell within the grid
        /// </summary>
        /// <param name="e"></param>
        private void CellUpdate(CellUpdateEventArgs e)
        {

            Run(() =>
            {
                lock (syncRoot)
                {

                    #region usage

                    // There are three options for locating the cell we wish to update.

                    // OPTION 1
                    // In this simulation, the data-feed generates a random cell based on 
                    // column-index and row-index. This makes life very easy when locating
                    // the cell we need to update but it's not very realistic in a real-world
                    // application.
                    //
                    //  QuantConnect.Views.Controls.ActiveGrid.ActiveRow.ActiveCell cell = this.lvBalances[e.Row, e.Column];


                    // OPTION 2
                    // In the real world it's unlikely that we'll know the cell indices;
                    // we're more likely to know the row key and the column-header key values which
                    // means that we have to actually find the cell instead of going straight
                    // to it.
                    // string keyRow = currencies[e.Row];
                    // string keyColumn = currencies[e.Column-1];
                    //  QuantConnect.Views.Controls.ActiveGrid.ActiveRow.ActiveCell cell = this.lvBalances.FindCell(keyRow, keyColumn);


                    // OPTION 3
                    // If every cell is assigned a unique name then we can use it to locate
                    // the cell within the grid. In this example, the unique key was simply generated
                    // as a composite of the row name and column name but it could just as well have
                    // been an integer or a guid.
                    //string keyCell = String.Format("{0}_{1}", MarketDataGridColumnNames[e.Row], MarketDataGridColumnNames[e.Column - 1]);//berkay

                    //string keyCell = String.Format("{0}_{1}", stocks, MarketDataGridColumnNames[e.Column - 1]); 
                    #endregion

                    string keyCell = e.CellKey;
                    QuantConnect.Views.Controls.ActiveGrid.ActiveRow.ActiveCell cell = this.agStocks.FindCell(keyCell);

                    if (cell != null)
                    {

                        //change text
                        if (!string.IsNullOrEmpty(e.NewText))
                        {
                            cell.Text = e.NewText;
                            return;
                        }

                        // Create a new value for the cell.
                        decimal newValue = e.NewDecimalValue;

                        // Has the value been reduced, increased, or left unchanged?
                        if (newValue < cell.DecimalValue)
                        {
                            // Reduced
                            cell.FlashBackColor = isGridColorful ? Color.LightGreen : Color.Yellow;
                            cell.FlashPreTextForeColor = Color.Red;
                            cell.FlashPostTextForeColor = Color.Red;
                            cell.FlashPreText = "▼";
                            cell.FlashPostText = String.Empty;
                        }
                        else if (newValue > cell.DecimalValue)
                        {
                            // Increased
                            cell.FlashBackColor = isGridColorful ? Color.PowderBlue : Color.Yellow;
                            cell.FlashPreTextForeColor = Color.Blue;
                            cell.FlashPostTextForeColor = Color.Blue;
                            cell.FlashPreText = "▲";
                            cell.FlashPostText = String.Empty;
                        }
                        else
                        {
                            // Unchanged
                            cell.FlashBackColor = Color.Yellow;
                            cell.FlashPreText = String.Empty;
                            cell.FlashPostText = String.Empty;
                        }
                        cell.DecimalValue = newValue;
                    }
                }
            });
        }

        #endregion



    }

}
