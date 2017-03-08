using QuantConnect.Views.Model;
using QuantConnect.Views.Model.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuantConnect.Views.WinForms
{
    public class BaseForm : Form
    {
        public static event Action<ItemEventArgs<Exception>> OnException;

        bool isDebugMode = false;

        public bool IsDebugMode
        {
            get { return isDebugMode; }
            //private set { isDebugMode = value; }
        }

        public string CurrentAssemblyLocation
        {
            get
            {
                string fullPath = Assembly.GetExecutingAssembly().Location;
                return fullPath;

            }
        }

        public string BaseDirectory
        {
            get
            {
                string fullPath = AppDomain.CurrentDomain.BaseDirectory;
                return fullPath;

            }
        }

        public void Alert(string message)
        {
            MessageBox.Show(message);
        }

        public BaseForm()
        {

#if DEBUG
            isDebugMode = true;
#endif
        }

        public void ControlInvoke(Control ctrl, Action action)
        {
            if (ctrl.InvokeRequired)
            {
                ctrl.Invoke(action);
            }
            else
            {
                action();
            }
        }

        public void ChangeText(Control ctrl, string msg)
        {
            ControlInvoke(ctrl, new Action(() => ctrl.Text = msg));
        }

        public void AddToLbx(ListBox lbx, LbxMessageItem message)
        {
            ControlInvoke(lbx, new Action(() =>
            {
                //lbx.Items.Insert(0, message);
                lbx.Items.Add(message);
                lbx.SelectedIndex = lbx.Items.Count - 1;
            })); 
        }


        public void Clear(object sender)
        {
            if (sender is ListBox)
            {
                ClearLbx((sender as ListBox));
            }

        }

        public void ClearLbx(ListBox lbx)
        {
            ControlInvoke(lbx, new Action(() => lbx.Items.Clear())); 
        }

        public static void Exception( Exception ex)
        {
            if (OnException != null)
            {
                OnException( ex);
            }
        }

        protected void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {

            int idx = e.Index;
            if (idx != -1)
            {
                ListBox lbx = sender as ListBox;
                Graphics g = e.Graphics;
                LbxMessageItem lbxItem = lbx.Items[idx] as LbxMessageItem;
                SolidBrush backgroundBrush = new SolidBrush(lbxItem.BackColor != null ? lbxItem.BackColor : e.BackColor);
                SolidBrush foregroundBrush = new SolidBrush(lbxItem.ForeColor != null ? lbxItem.ForeColor : e.ForeColor);
                Font textFont = lbxItem.Font != null ? lbxItem.Font : e.Font;
                string text = lbxItem.Text != null ? lbxItem.Text : string.Empty;
                RectangleF rectangle = new RectangleF(new PointF(e.Bounds.X, e.Bounds.Y), new SizeF(e.Bounds.Width, g.MeasureString(text, textFont).Height));

                g.FillRectangle(backgroundBrush, rectangle);
                g.DrawString(text, textFont, foregroundBrush, rectangle);

                backgroundBrush.Dispose();
                foregroundBrush.Dispose();
                g.Dispose();
            }

        }
      
        public static void Run( Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Exception( ex);
            }
        }
    }
}
