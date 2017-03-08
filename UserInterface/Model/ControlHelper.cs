using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuantConnect.Views.Model
{
    public static class ControlHelper
    {
        public static void LoadChartDurations(this ToolStripComboBox cbx, Resolution resolution)
        {
            cbx.Items.Clear();

            switch (resolution)
            {
                case Resolution.Tick:
                case Resolution.Second:
                case Resolution.Minute:
                    cbx.Items.Add(new ToolStripStatusLabel() { Text = "5" });
                    cbx.Items.Add(new ToolStripStatusLabel() { Text = "15" });
                    cbx.Items.Add(new ToolStripStatusLabel() { Text = "30" });
                    cbx.Items.Add(new ToolStripStatusLabel() { Text = "45" });
                    cbx.Items.Add(new ToolStripStatusLabel() { Text = "60" });
                    break;
                case Resolution.Hour:
                    for (int i = 1; i < 25; i++)
                    {
                        cbx.Items.Add(new ToolStripStatusLabel() { Text = i.ToString() });
                    }
                    break;
                case Resolution.Daily:
                    for (int i = 1; i < 31; i++)
                    {
                        cbx.Items.Add(new ToolStripStatusLabel() { Text = i.ToString() });
                    }
                    break;
                default:
                    break;
            }
            cbx.SelectedIndex = 0;
        }
         
        public static void LoadChartResolutions(this ToolStripComboBox cbx )
        {
            cbx.Items.Clear();
            cbx.Items.Add(Resolution.Second);
            cbx.Items.Add(Resolution.Minute);
            cbx.Items.Add(Resolution.Hour);
            cbx.Items.Add(Resolution.Daily);

            cbx.SelectedIndex = 0;
        }

    }
}
