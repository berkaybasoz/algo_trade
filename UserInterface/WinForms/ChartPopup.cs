using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QuantConnect.Views.Model;

namespace QuantConnect.Views.WinForms
{
    public partial class ChartPopup : BaseForm
    {
        private static ConcurrentDictionary<string, ChartPopup> Forms = new ConcurrentDictionary<string, ChartPopup>();
        private int chartHeight = 200;
        private int chartWidth = 300;
        private Resolution resolution = Resolution.Second;
        private bool filtersEnable = false;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartControl;
        private long maxX = 0;
        private static string DATE_FORMAT = "HH:mm:ss";

        public string FormName
        {
            get;
            private set;
        }

        private ChartPopup()
        {
            InitializeComponent();
        }

        private void ChartPopup_Load(object sender, EventArgs e)
        {
            //ShowInTaskbar = false;
            FormClosed += Form_Closed;

            //tsDurations.Drop

            if (filtersEnable)
            {
                DisplayDurations();
                DisplayResolutions();
                tsCbxResolutions.SelectedIndexChanged += tsCbxResolutions_SelectedIndexChanged;
            }
            else
            {
                tsMain.Visible = false;
            }

            //pnlCharts.Visible = false;
        }

        private void Form_Closed(object sender, FormClosedEventArgs e)
        {
            //bilerek unregister etmiyoruz. Algoritma calisirken form kapandıktan sonra yeniden aciliyor yoksa
            //UnregisterForm();
        }

        private static ChartPopup RegisterForm(string name)
        {
            ChartPopup popup;

            if (!Forms.TryGetValue(name, out popup))
            {
                popup = new ChartPopup();
                popup.FormName = name;
                popup.Text = name;

                Forms.AddOrUpdate(name, popup, (k, v) => { return popup; });
                popup.Show();
                popup.BringToFront();
                popup.CreateChart(name);
            }
            return popup;
        }
        public static ChartPopup GetOrCreate(string name)
        {
            ChartPopup popup = RegisterForm(name);
            return popup;
        }

        private void UnregisterForm()
        {
            ChartPopup tmp;
            Forms.TryRemove(FormName, out tmp);
        }

        public void CreateChart(string name)
        {
            Run(() =>
            {
                ControlInvoke(pnlCharts, new Action(() => pnlCharts.Controls.Clear()));

                chartControl = new System.Windows.Forms.DataVisualization.Charting.Chart();
                chartControl.Name = name;
                chartControl.Titles.Add(name);

                chartControl.Height = chartHeight;
                chartControl.Width = chartWidth;
                chartControl.Visible = true;
                ChartArea chartArea = new ChartArea();
                chartArea.Area3DStyle.Enable3D = false;

                chartControl.ChartAreas.Add(chartArea);
                chartControl.Dock = DockStyle.Fill;
                chartControl.Invalidate();

                ControlInvoke(pnlCharts, new Action(() =>
                {
                    pnlCharts.Controls.Add(chartControl);
                }));
            });
        }

        public void DisplayChart(QuantConnect.Chart algoChart)
        {
            Run(() =>
            {
                string chartName = algoChart.Name;


                if (chartControl.Name != algoChart.Name)
                {
                    chartControl.Name = algoChart.Name;
                    chartControl.Titles.Add(chartName);
                }

                foreach (var algoSeri in algoChart.Series.Values)
                {

                    string algoSeriName = algoSeri.Name;

                    System.Windows.Forms.DataVisualization.Charting.Series seriControl = chartControl.Series.Where(s => s.Name == algoSeriName).FirstOrDefault();
                    if (seriControl == null)
                    {
                        chartControl.Series.Add(algoSeriName);
                        seriControl = chartControl.Series[algoSeriName];

                        seriControl.Color = algoSeri.Color;
                        seriControl.IsValueShownAsLabel = true;
                        //ctrlSeri.Label = "label "+seriName; 
                        //ctrlSeri.ToolTip = "tooltip " + seriName;
                        seriControl.ToolTip = "Name #SERIESNAME : X - #VALX{F2} , Y - #VALY{F2}";

                        Legend legent = new Legend("Legend " + algoSeriName);
                        chartControl.Legends.Add(legent);

                        //// Set Docking of the Legend chart to the Default Chart Area.
                        //legent. DockToChartArea = "Default";

                        //// Assign the legend to Series1.l
                        seriControl.Legend = "Legend " + algoSeriName;
                        seriControl.IsVisibleInLegend = true;
                        seriControl.LegendText = algoSeriName;
                        //ctrlSeri.IsXValueIndexed = true;

                        switch (algoSeri.SeriesType)
                        {
                            case SeriesType.Line:
                                seriControl.ChartType = SeriesChartType.Line;
                                break;
                            case SeriesType.Scatter:
                                seriControl.ChartType = SeriesChartType.Line;
                                break;
                            case SeriesType.Candle:
                                seriControl.ChartType = SeriesChartType.Candlestick;
                                break;
                            case SeriesType.Bar:
                                seriControl.ChartType = SeriesChartType.Bar;
                                break;
                            case SeriesType.Flag:
                                seriControl.ChartType = SeriesChartType.Line;
                                break;
                            default:
                                break;
                        }
                    }

                    //ctrlSeri.Points.DataBindXY(seri.Values.Select(s => s.x), seri.Values.Select(s => s.y));

                    if (filtersEnable)
                    {
                        DateTime minTime = GetMinDateTime();

                        foreach (var point in algoSeri.Values.ToList())
                        {
                            DateTime x = Time.UnixTimeStampToDateTime(point.x);
                            if (x > minTime)
                            {
                                seriControl.Points.AddXY(x.ToString(DATE_FORMAT), point.y);
                            }
                        }
                    }
                    else
                    {
                        seriControl.Points.Clear();
                        foreach (var point in algoSeri.Values.ToList().OrderByDescending(o => o.x).Take(10))
                        {
                            DateTime x = Time.UnixTimeStampToDateTime(point.x);
                             
                            seriControl.Points.AddXY(x.ToString(DATE_FORMAT), point.y);
                        }

                        //foreach (var point in algoSeri.Values.ToList().OrderByDescending(o => o.x))
                        //{
                        //    if (point.x > maxX)
                        //    {
                        //        maxX = point.x;
                        //        DateTime x = Time.UnixTimeStampToDateTime(point.x);
                        //        seriControl.Points.AddXY(x.ToString(DATE_FORMAT), point.y);

                        //    }
                        //}
                    }
                }

            });

        }

        private void DisplayChartFromSeries(QuantConnect.Chart chart)
        {
            string chartName = chart.Name;
            ControlInvoke(pnlCharts, new Action(() => pnlCharts.Controls.Clear()));

            foreach (var seri in chart.Series.Values)
            {
                System.Windows.Forms.DataVisualization.Charting.Chart
               ctrlChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
                ctrlChart.Name = chartName;
                ctrlChart.Titles.Add(chartName);

                string seriName = seri.Name;
                ctrlChart.Series.Add(seriName);
                System.Windows.Forms.DataVisualization.Charting.Series ctrlSeri = ctrlChart.Series[seriName];
                ctrlSeri.Color = seri.Color;

                ctrlSeri.IsValueShownAsLabel = true;
                //ctrlSeri.Label = "label "+seriName; 
                //ctrlSeri.ToolTip = "tooltip " + seriName;
                ctrlSeri.ToolTip = "Name #SERIESNAME : X - #VALX{F2} , Y - #VALY{F2}";

                //ctrlSeri.Legend= "label " + seriName;
                //ctrlSeri.LegendToolTip = "Name #SERIESNAME : X - #VALX{F2} , Y - #VALY{F2}";

                Legend legend = new Legend("Legend " + seriName);
                ctrlChart.Legends.Add(legend);

                //// Set Docking of the Legend chart to the Default Chart Area.
                //legent. DockToChartArea = "Default";

                //// Assign the legend to Series1.l
                ctrlSeri.Legend = "Legend " + seriName;
                ctrlSeri.IsVisibleInLegend = true;

                ctrlSeri.LegendText = seriName;
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


                //ctrlSeri.Points.DataBindXY(seri.Values.Select(s => s.x), seri.Values.Select(s => s.y));

                foreach (var point in seri.Values.ToList().OrderByDescending(o => o.x))
                {
                    ctrlSeri.Points.AddXY(Time.UnixTimeStampToDateTime(point.x).ToString("o"), point.y);

                }

                ctrlChart.Height = chartHeight;
                ctrlChart.Width = chartWidth;
                ctrlChart.Visible = true;
                ChartArea chartArea = new ChartArea();
                chartArea.Area3DStyle.Enable3D = false;

                ctrlChart.ChartAreas.Add(chartArea);
                ctrlChart.Invalidate();

                ControlInvoke(pnlCharts, new Action(() => pnlCharts.Controls.Add(ctrlChart)));

            }

        }

        private DateTime GetMinDateTime()
        {
            DateTime time = DateTime.UtcNow;
            int i;

            int.TryParse(tsCbxDurations.SelectedItem.ToString(), out i);

            switch (resolution)
            {
                case Resolution.Tick:
                    break;
                case Resolution.Second:
                    time = time.AddSeconds(-i);
                    break;
                case Resolution.Minute:
                    time = time.AddMinutes(-i);
                    break;
                case Resolution.Hour:
                    time = time.AddHours(-i);
                    break;
                case Resolution.Daily:
                    time = time.AddDays(-i);
                    break;
                default:
                    time = DateTime.MinValue;
                    break;
            }

            return time;
        }

        private void tsCbxResolutions_SelectedIndexChanged(object sender, EventArgs e)
        {

            Enum.TryParse<Resolution>(tsCbxResolutions.SelectedItem.ToString(), out resolution);

            DisplayDurations();
        }

        private void DisplayDurations()
        {
            tsCbxDurations.LoadChartDurations(resolution);

        }

        private void DisplayResolutions()
        {
            tsCbxResolutions.LoadChartResolutions();
        }

        public static void CloseForms()
        {
            foreach (var form in Forms.Values)
            {
                Run(new Action(() =>
                {
                    form.Close();
                }));

            }
        }

        ~ChartPopup()
        {
            UnregisterForm();
        }


    }
}
