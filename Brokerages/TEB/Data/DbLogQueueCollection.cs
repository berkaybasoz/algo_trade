using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.DAL;
using Teb.Infra.Collection;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class DbLogQueueCollection : ConcurrentDictionary<string, DbLogQueue>
    {
    }
    public class DbLogQueue : IQueue<SecurityDbLog>
    {
        public string Key { get; set; }
        public DbQueryManager DbManager { get; set; }
    }

    public class DbNewsQueue : IQueue<NewsDbLog>
    {
        public DbQueryManager DbManager { get; set; }
    }

    public class NewsDbLog : DbLog
    {
        public DbQueryManager DbManager { get; set; }
    } 

    public class SecurityDbLog : DbLog
    {
        public DbQueryManager DbManager { get; set; }
    }
}
