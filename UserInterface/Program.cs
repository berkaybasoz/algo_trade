using QuantConnect.Views.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuantConnect.Views
{
    class Program
    {
        [STAThread]
        static public void Main()
        {
            Application.Run(new EditorForm());
        }
         
    }
}
