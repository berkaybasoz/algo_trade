using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.TEB.Data
{
    public delegate void ParserFunc(string str);
    public class ParserArg
    {
        public ParserFunc Func { get; set; }
        public string Parameter { get; set; }
    }
}
