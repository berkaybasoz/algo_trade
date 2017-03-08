using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Views.Model.UI
{
    public class LbxMessageItem
    {
        public string Text { get; set; }
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public Font Font { get; set; }
        //string text="";
        //Color backColor = Color.White;
        //Color foreColor = Color.Black;
        //Font font = new Font(new Font("Arial", 9), FontStyle.Bold); 
        public LbxMessageItem(string text)
        {
            Text = text;
        }
    }

    public class ErrorLbxMessageItem : LbxMessageItem
    {
        public ErrorLbxMessageItem(string text)
            : base(text)
        {
            BackColor = Color.Red;
            ForeColor = Color.White;
            Font = new Font(new Font("Arial", 9), FontStyle.Bold);
        }
    }

    public class DebugLbxMessageItem : LbxMessageItem
    {
        public DebugLbxMessageItem(string text)
            : base(text)
        {
            BackColor = Color.White;
            ForeColor = Color.Gray;
            Font = new Font(new Font("Arial", 9), FontStyle.Regular);
        }
    }

    public class InfoLbxMessageItem : LbxMessageItem
    {
        public InfoLbxMessageItem(string text)
            : base(text)
        {
            BackColor = Color.White;
            ForeColor = Color.Black;
            Font = new Font(new Font("Arial", 9), FontStyle.Regular);
        }
    }

    public class WarnLbxMessageItem : LbxMessageItem
    {
        public WarnLbxMessageItem(string text)
            : base(text)
        {
            BackColor = Color.Yellow;
            ForeColor = Color.White;
            Font = new Font(new Font("Arial", 9), FontStyle.Regular);
        }
    }

}
