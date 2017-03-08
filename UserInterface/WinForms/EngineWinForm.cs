/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Util;
using Timer = System.Windows.Forms.Timer;
using System.Collections.Generic;
using QuantConnect.Logging;
using System.Windows.Forms.DataVisualization.Charting;
using System.Globalization;
using QuantConnect.ToolBox.GoogleDownloader;
using QuantConnect.ToolBox;
using System.Linq;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Text;
using System.Reflection;
using System.IO;
using QuantConnect.Algorithm;
using QuantConnect.Interfaces;
using QuantConnect.AlgorithmFactory;
using System.Threading.Tasks;
using QuantConnect.Views.Model;

namespace QuantConnect.Views.WinForms
{
    public partial class EngineWinForm : BaseForm
    {
        #region Fields

        //Form Controls:  
        private Timer _polling;
        private Button btnStart;
        private TabControl tabControl1;
        private TabPage tpLogs;
        private ListBox lbxLog;
        private TabPage tpErrors;
        private ListBox lbxError;
        private Button button1;
        private ComboBox cbAlgoName;
        //Form Business Logic:
        AlgoEngineBuilder _algoEnv;

        //public string[] algoNames = new string[] { "QuantConnect.Algorithm.CSharp.TEBBasicAlgo",
        //                                            "QuantConnect.Algorithm.CSharp.TEBBasicAlgo1", 
        //                                            "QuantConnect.Algorithm.CSharp.TEBBasicAlgo2", 
        //                                            "QuantConnect.Algorithm.CSharp.BasicTemplateAlgorithm" };
        public string[] algoNames = new string[] { "TEBBasicAlgo1", 
                                                    "TEBBasicAlgo2", 
                                                    "TEBBasicAlgo3"  };
        #endregion


        public string CurrentAssemblyLocation
        {
            get
            {
                string fullPath = Assembly.GetExecutingAssembly().Location;
                return fullPath;

            }
        }

        private bool isCustomAlgo = false;

        public readonly string customAlgoDirectory = "AlgoTemplate";

        private string customAlgoFileName = "Template1.cs";

        private object lockObj = new object();

        public EngineWinForm()
        {
            InitializeComponent();
        }

        private void EngineWinForm_Load(object sender, EventArgs e)
        {

            #region Screen

            Text = "TEB Algorithmic Trading Engine: v" + Constants.Version;
            //Size = new Size(1024, 768);
            //MinimumSize = new Size(1024, 768);
            //CenterToScreen();
            //WindowState = FormWindowState.Maximized;
            Icon = new Icon("../../../lean.ico");
            //Form Events:

            Closed += OnClosed;
            #endregion

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

            foreach (var algoName in algoNames)
            {
                cbAlgoName.Items.Add(algoName);
            }

            cbAlgoName.SelectedIndex = 0;
            cbAlgoName.SelectedIndexChanged += cbAlgoName_SelectedIndexChanged;


            //RuntimeCompileFromFile();
        }

        private void cbAlgoName_SelectedIndexChanged(object sender, EventArgs e)
        {
            //cbAlgoName
        }

        private void ConsoleOnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {

        }

        private void OnClosed(object sender, EventArgs eventArgs)
        {
            if (_algoEnv != null)
            {
                _algoEnv.Dispose();

            }
            Environment.Exit(0);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            ClearLbx(lbxError);
            ClearLbx(lbxLog);


            if (cbAlgoName.SelectedItem == null || string.IsNullOrWhiteSpace(cbAlgoName.SelectedItem.ToString()))
            {
                Alert("Choose an algorithm");
                return;
            }
            else
            {

                ChangeText(tpErrors, "Errors");
                ChangeText(tpLogs, "Logs");

                string algorithmName = "";

                if (isCustomAlgo)
                {
                    string code = rtbAlgo.Text;
                    IAlgorithm algorithm = RuntimeCompile(code);
                    QuantConnect.AlgorithmFactory.Loader.TEBAlgorithm = algorithm;
                }
                else
                {
                    algorithmName = cbAlgoName.SelectedItem.ToString();
                    QuantConnect.AlgorithmFactory.Loader.TEBAlgorithm = null;
                }

               
             
                _algoEnv = new AlgoEngineBuilder();
                _algoEnv.OnLog += algoEnv_OnLog;
                _algoEnv.BuildEngine(algorithmName);  //_algoEnv.BuildEngine(algorithm, CurrentAssemblyLocation);

                #region Polling

                _polling = new Timer();
                _polling.Interval = 1000;
                _polling.Tick += PollingOnTick;
                _polling.Start();

                tabControl1.SelectedIndex = 1;
             
                #endregion
            }

        }

        private void algoEnv_OnLog(string msg)
        {
            AppendConsole(msg);
        }

        private void PollingOnTick(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(() =>
            {
                lock (lockObj)
                {
                    Packet message;
                    if (_algoEnv == null || _algoEnv.ResultsHandler == null)
                        return;


                    while (_algoEnv.ResultsHandler.Messages.TryDequeue(out message))
                    {
                        #region MyRegion
                        switch (message.Type)
                        {
                            case PacketType.BacktestResult:
                                //Draw chart

                                var backtestingResultHandler = (BacktestingResultHandler)_algoEnv.AlgorithmHandlers.Results;
                                var statistics = new Dictionary<string, string>();
                                statistics = backtestingResultHandler.FinalStatistics;

                                break;

                            case PacketType.LiveResult:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.AlgorithmStatus:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;

                            case PacketType.RuntimeError:
                                var runError = message as RuntimeErrorPacket;
                                if (runError != null)
                                    AppendConsole(runError.Message, true);
                                break;

                            case PacketType.HandledError:
                                var handledError = message as HandledErrorPacket;
                                if (handledError != null)
                                    AppendConsole(handledError.Message, true);
                                break;

                            case PacketType.Log:
                                var log = message as LogPacket;
                                if (log != null)
                                    AppendConsole(log.Message);
                                break;

                            case PacketType.Debug:
                                var debug = message as DebugPacket;
                                if (debug != null)
                                    AppendConsole(debug.Message);
                                break;

                            case PacketType.OrderEvent:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.None:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.AlgorithmNode:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.AutocompleteWork:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.AutocompleteResult:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.BacktestNode:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.BacktestWork:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.LiveNode:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.LiveWork:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.SecurityTypes:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.BacktestError:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.BuildWork:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.BuildSuccess:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.BuildError:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.Success:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.History:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.CommandResult:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            case PacketType.GitHubHook:
                                AppendConsole("PollingOnTick :" + message.Type.ToString());
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        #endregion

                    }

                    if (!_algoEnv.IsActive)
                    {
                        AppendConsole("Backtest bitti");
                        _polling.Stop();
                        _polling.Dispose();
                        _polling = null;

                        DisplayChart();
                    }
                }
               
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = ofd.FileName;
                    // Load settings from command line
                    var symbols = "GARAN".Split(',');
                    var resolution = (Resolution)Enum.Parse(typeof(Resolution), "Minute");
                    var startDate = DateTime.ParseExact("20150101", "yyyyMMdd", CultureInfo.InvariantCulture);
                    var endDate = DateTime.ParseExact("20160328", "yyyyMMdd", CultureInfo.InvariantCulture);

                    // Load settings from config.json
                    var dataDirectory = Config.Get("data-directory", "../../../Data");

                    // Create an instance of the downloader
                    const string market = Market.USA;
                    GoogleDataDownloader downloader = new GoogleDataDownloader();

                    foreach (var symbol in symbols)
                    {
                        // Download the data
                        var symbolObject = Symbol.Create(symbol, SecurityType.Equity, market);

                        var data = downloader.Parse(filePath, symbolObject, resolution, startDate, endDate).OrderBy(o => o.Time);


                        // Save the data
                        var writer = new LeanDataWriter(resolution, symbolObject, dataDirectory);
                        writer.Write(data);
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        private void RuntimeCompile2()
        {
            string code = @"
    using System;

    namespace First
    {
        public class Program
        {
            public static void Main()
            {
            " +
                "Console.WriteLine(\"Hello, world!\");"
                + @"
            }
        }
    }
";

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            // Reference to System.Drawing library
            parameters.ReferencedAssemblies.Add("System.Drawing.dll");
            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;
            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = true;

            CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);

            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }

                throw new InvalidOperationException(sb.ToString());
            }


            Assembly assembly = results.CompiledAssembly;
            Type program = assembly.GetType("First.Program");
            MethodInfo main = program.GetMethod("Main");

            main.Invoke(null, null);
        }

        private void RuntimeCompile1()
        {
            string code = @"using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class BasicTemplateAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash 
            AddSecurity(SecurityType.Equity, ""SPY"", Resolution.Second);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name=""data"">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(""SPY"", 1);
                Debug(""Purchased Stock"");
            }
        }
    }
}
";

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            // Reference to System.Drawing library
            parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Common\bin\Debug\QuantConnect.Common.dll");
            parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Algorithm\bin\Debug\QuantConnect.Algorithm.dll");
            parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Indicators\bin\Debug\QuantConnect.Indicators.dll");
            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;
            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = true;

            CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);

            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }

                throw new InvalidOperationException(sb.ToString());
            }


            Assembly assembly = results.CompiledAssembly;
            Type program = assembly.GetType("First.Program");
            MethodInfo main = program.GetMethod("Main");

            main.Invoke(null, null);
        }

        private IAlgorithm RuntimeCompile(string code)
        {
            Assembly assembly = CompileSource(code);

            //List<string> types = Loader.GetExtendedTypeNames(assembly);

            Type typeOfAlgo = assembly.GetType("QuantConnect.Algorithm.CSharp.TEBDynamicBasicAlgo1");
            var algorithm = (IAlgorithm)Activator.CreateInstance(typeOfAlgo);

            MethodInfo method = typeOfAlgo.GetMethod("Initialize");

            if (method.IsStatic)
                method.Invoke(null, null);
            else
                method.Invoke(algorithm, null);

            return algorithm;
            //yada
            //CurrentAssemblyLocation=  assembly.Location;
            //Config.Set("algorithm-type-name",String.Format(@"C:\Aktarım\Lean-master\UserInterface\bin\Debug\",currentDLL));


        }

        private static Assembly CompileSource(string code)
        {


            CSharpCodeProvider provider = new CSharpCodeProvider();

            CompilerParameters parameters = new CompilerParameters();
            // Reference to System.Drawing library
            parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Common\bin\Debug\QuantConnect.Common.dll");
            parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Algorithm\bin\Debug\QuantConnect.Algorithm.dll");
            parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Indicators\bin\Debug\QuantConnect.Indicators.dll");
            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;
            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = false;
            //parameters.TreatWarningsAsErrors = false;
            //parameters.WarningLevel = 4; 
            //parameters.TempFiles.KeepFiles = false;


            CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);

            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }

                throw new InvalidOperationException(sb.ToString());
            }


            Assembly assembly = results.CompiledAssembly;
            return assembly;
        }


        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/ff650674.aspx
        /// </summary>
        /// 
        private void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = ofd.FileName;
                    // Load settings from command line
                    var symbols = "GARAN".Split(',');
                    var resolution = (Resolution)Enum.Parse(typeof(Resolution), "Minute");
                    var startDate = DateTime.ParseExact("20150101", "yyyyMMdd", CultureInfo.InvariantCulture);
                    var endDate = DateTime.ParseExact("20160328", "yyyyMMdd", CultureInfo.InvariantCulture);

                    // Load settings from config.json
                    var dataDirectory = Config.Get("data-directory", "../../../Data");

                    // Create an instance of the downloader
                    const string market = Market.USA;
                    GoogleDataDownloader downloader = new GoogleDataDownloader();

                    foreach (var symbol in symbols)
                    {
                        // Download the data
                        var symbolObject = Symbol.Create(symbol, SecurityType.Equity, market);

                        var data = downloader.Parse(filePath, symbolObject, resolution, startDate, endDate).OrderBy(o => o.Time);


                        // Save the data
                        var writer = new LeanDataWriter(resolution, symbolObject, dataDirectory);
                        writer.Write(data);
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        public static void RunTest()
        {
            int iterations = 3;

            // Call the object and methods to JIT before the test run
            QueryPerfCounter myTimer = new QueryPerfCounter();
            myTimer.Start();
            myTimer.Stop();

            // Time the overall test duration
            //

            // Use QueryPerfCounters to get the average time per iteration
            myTimer.Start();

            for (int i = 0; i < iterations; i++)
            {
                // Method to time
                //System.Threading.Thread.Sleep(1000);
            }
            myTimer.Stop();

            // Calculate time per iteration in nanoseconds
            double result = myTimer.Duration(iterations);

            // Show the average time per iteration results
            //Console.WriteLine("Iterations: {0}", iterations);
            //Console.WriteLine("Average time per iteration: ");
            //Console.WriteLine(result / 1000000000 + " seconds");
            //Console.WriteLine(result / 1000000 + " milliseconds");
            Console.WriteLine(result + " nanoseconds");
            //DateTime dtStartTime = DateTime.Now;
            //            // Show the overall test duration results
            //            DateTime dtEndTime = DateTime.Now;
            //            Double duration = ((TimeSpan)(dtEndTime - dtStartTime)).TotalMilliseconds;
            //            Console.WriteLine();
            //            Console.WriteLine("Duration of test run: ");
            //            Console.WriteLine(duration / 1000 + " seconds");
            //            Console.WriteLine(duration + " milliseconds");
            Console.ReadLine();
        }

        #region UI
         

        private void DisplayChart()
        {

            if (_algoEnv == null || _algoEnv.ResultsHandler == null)
                return;


            foreach (Chart chart in _algoEnv.ResultsHandler.Charts.Values)
            {
                System.Windows.Forms.DataVisualization.Charting.Chart ctrlChart = new System.Windows.Forms.DataVisualization.Charting.Chart();

                ctrlChart.Name = chart.Name;

                foreach (var seri in chart.Series.Values)
                {
                    string seriName = seri.Name;
                    ctrlChart.Series.Add(seriName);
                    System.Windows.Forms.DataVisualization.Charting.Series ctrlSeri = ctrlChart.Series[seriName];
                    ctrlSeri.Color = seri.Color;

                    //ctrlSeri.IsVisibleInLegend = false;
                    //ctrlSeri.IsXValueIndexed = true;

                    switch (seri.SeriesType)
                    {
                        case SeriesType.Line:
                            ctrlSeri.ChartType = SeriesChartType.Line;
                            break;
                        case SeriesType.Scatter:
                            ctrlSeri.ChartType = SeriesChartType.Line;
                            break;
                        case SeriesType.Candle:
                            ctrlSeri.ChartType = SeriesChartType.Candlestick;
                            break;
                        case SeriesType.Bar:
                            ctrlSeri.ChartType = SeriesChartType.Bar;
                            break;
                        case SeriesType.Flag:
                            ctrlSeri.ChartType = SeriesChartType.Line;
                            break;
                        default:
                            break;
                    }



                    foreach (var point in seri.Values)
                    {
                        ctrlSeri.Points.AddXY(point.x, point.y);

                    }

                }

                ctrlChart.Height = 400;
                ctrlChart.Width = 600;
                ctrlChart.Visible = true;
                ctrlChart.ChartAreas.Add(new ChartArea());
                ctrlChart.ChartAreas[0].Area3DStyle.Enable3D = false;
                ctrlChart.Invalidate();

                pnlCharts.Controls.Add(ctrlChart);
            }
        }


        private void AppendConsole(string message, bool isError = false)
        {
            message = DateTime.Now.ToString("u") + " " + message + Environment.NewLine;
            //Add to console:
            if (isError)
            {
                AddToLbx(lbxError, message);
                ChangeText(tpErrors, String.Format("Errors ({0})", lbxError.Items.Count));
            }
            else
            {
                AddToLbx(lbxLog, message);
                ChangeText(tpLogs, String.Format("Logs ({0})", lbxLog.Items.Count));
            }

            //Log.Trace(message);
        }

         
        #endregion


        private void rbExistingAlgo_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Checked)
            {
                isCustomAlgo = false;
                DisplayAlgoType();
            }
            DisplayCustomAlgo();
        }

        private void rbCustomAlgo_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Checked)
            {
                isCustomAlgo = true;
                DisplayAlgoType();
            }
            DisplayCustomAlgo();
        }

        public void DisplayAlgoType()
        {
            if (isCustomAlgo)
            {
                cbAlgoName.Enabled = false;
            }
            else
            {
                cbAlgoName.Enabled = true;
            }

        }

        public void DisplayCustomAlgo()
        {
            if (isCustomAlgo)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, customAlgoDirectory, customAlgoFileName);

                string code = File.ReadAllText(path);

                rtbAlgo.Text = code;
            }
            else
            {
                rtbAlgo.Text = "";
            }
        }


    }



}

