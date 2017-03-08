 using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Debug
{
    public class DebugHelper
    {
        [Conditional("DEBUG")]
        public static void Log(string msg)
        {
            QuantConnect.Logging.Log.Debug(msg,2);
        }

        [Conditional("DEBUG")]
        public static void Run(Action action)
        {
            action();
        }

        /*
         //#if DEBUG
             Log.DebuggingEnabled = true;
               Log.DebuggingLevel = 2;
            #endif
         */
    }
}