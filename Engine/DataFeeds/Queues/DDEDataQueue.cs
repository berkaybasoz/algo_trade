using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds.Queues
{
    /// <summary>
    /// This is an implementation of <see cref="IDataQueueHandler"/> used for DDE
    /// </summary>
    public class DDEDataQueue : IDataQueueHandler
    {
        private int count;
        private readonly Random _random = new Random();
        private DDEDataFeed feed = new DDEDataFeed("MTX", "DATA");
        private readonly ConcurrentQueue<BaseData> _ticks;
        private readonly HashSet<Symbol> _symbols;
        private static ConcurrentDictionary<string, Tick> history = new ConcurrentDictionary<string, Tick>();
 
        /// <summary>
        /// Initializes a new instance of the <see cref="FakeDataQueue"/> class to randomly emit data for each symbol
        /// </summary>
        public DDEDataQueue()
        {
            _ticks = new ConcurrentQueue<BaseData>();
            _symbols = new HashSet<Symbol>();
            feed.OnCellUpdate += new DDEDataFeed.OnCellUpdateHandler(feed_OnCellUpdate);
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
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            feed.BeginStartQuotes(symbols);
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            feed.StopQuotes();
        }

        private void feed_OnCellUpdate(object sender, CellUpdateEventArgs e)
        {
             var keys = e.Key.Split('.');
             Tick tick;
             if (!history.TryGetValue(keys[0], out tick))
             {


                 tick = new Tick
                 {
                     Time = DateTime.Now,
                     Symbol = new Symbol(SecurityIdentifier.GenerateEquity(keys[0], Market.USA), keys[0]),
                     Value = e.Value,
                     TickType = TickType.Quote,
                     Quantity = 0
                 };

                 history.AddOrUpdate(keys[0], tick);
             }

             tick.Update(keys[0], keys[1], e.Value);

             _ticks.Enqueue(tick);
        }
    }
}
