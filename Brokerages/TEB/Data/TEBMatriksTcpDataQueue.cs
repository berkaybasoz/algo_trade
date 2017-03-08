using QuantConnect.Brokerages.TEB.Data;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.TEB.Data
{    /// <summary>
    /// TEB brokerage - implementation of IDataQueueHandler interface
    /// </summary>
    public partial class TEBMatriksTcpDataQueue : IDataQueueHandler
    {
        private int count;
        private readonly object _sync = new object();
        private readonly ConcurrentQueue<BaseData> _ticks;
        private readonly ConcurrentDictionary<string, Symbol> _symbols;
        //private readonly HashSet<Symbol> _symbols;

        #region IDataQueueHandler implementation



        public TEBMatriksTcpDataQueue()
        {
            _ticks = new ConcurrentQueue<BaseData>();
            _symbols = new ConcurrentDictionary<string, Symbol>();
            //_symbols = new HashSet<Symbol>();
        

            matriksStreamServerIP = Config.Get("TEB-matriks-stream-server-ip");
            matriksStreamServerPort = Config.GetInt("TEB-matriks-stream-server-port");
            matriksDistExchangeIDList = Config.Get("TEB-matriks-stream-exhangeid-list");


            connString = Config.Get("TEB-connection-string");

            Start();
        }

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {

            BaseData tick;
            while (_ticks.TryDequeue(out tick))
            {
                yield return tick;
                Interlocked.Increment(ref count);
            }
            //yada
            //lock (_ticks)
            //{
            //    var copy = _ticks.ToArray();
            //    _ticks.Clear();
            //    return copy;
            //}


        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var tmpSymbol in symbols.ToList())
            {
                _symbols.AddOrUpdate(tmpSymbol, tmpSymbol, (k, v) => { return tmpSymbol; });
                QuantConnect.Logging.Log.Trace("TEBBrokerage.Subscribe(): {0}", tmpSymbol);
            }

            //yada
            //foreach (var symbol in symbols)
            //{
            //    lock (_sync)
            //    {
            //        _symbols.Add(symbol);
            //        QuantConnect.Logging.Log.Trace("TEBBrokerage.Subscribe(): {0}", symbol);
            //    }
            //}

        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {

            foreach (var tmpSymbol in symbols.ToList())
            {
                Symbol tmp;
                if (_symbols.TryRemove(tmpSymbol, out tmp))
                {
                    QuantConnect.Logging.Log.Trace("TEBBrokerage.Unsubscribe(): {0}", tmpSymbol);
                }

            }

            //yada
            //foreach (var symbol in symbols)
            //{
            //    lock (_sync)
            //    {
            //        _symbols.Remove(symbol);
            //        QuantConnect.Logging.Log.Trace("TEBBrokerage.Unsubscribe(): {0}", symbol);
            //    }
            //}
        }



        #endregion
    }
}
