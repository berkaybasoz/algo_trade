using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.Infra.Model;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class SecurityCollection : ConcurrentDictionary<string, Security>
    {

    }
}
