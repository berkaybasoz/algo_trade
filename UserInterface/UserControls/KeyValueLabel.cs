using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuantConnect.Views.UserControls
{
    public partial class KeyValueLabel : UserControl
    {
        public KeyValueLabel(string key, string value)
        {
            InitializeComponent();

            Key = key;

            Value = value;
        }

        public string Key
        {
            get
            {
                return lblKey.Text;
            }
            set
            {
                lblKey.Text = value;
            }
        }

        public string Value
        {
            get
            {
                return lblValue.Text;
            }
            set
            {
                lblValue.Text = value;
            }
        }
    }
}
