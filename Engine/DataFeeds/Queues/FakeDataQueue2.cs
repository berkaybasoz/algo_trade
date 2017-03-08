/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using Timer = System.Timers.Timer;

namespace QuantConnect.Lean.Engine.DataFeeds.Queues
{
    /// <summary>
    /// This is an implementation of <see cref="IDataQueueHandler"/> used for testing
    /// </summary>
    public class FakeDataQueue2 : IDataQueueHandler
    {
        private int count;
        private readonly Random _random = new Random();

        private readonly Timer _timer;
        private readonly ConcurrentQueue<Tick> _ticks;
        private readonly HashSet<Symbol> _symbols;
        private readonly object _sync = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeDataQueue"/> class to randomly emit data for each symbol
        /// </summary>
        public FakeDataQueue2()
        {
            _ticks = new ConcurrentQueue<Tick>();
            _symbols = new HashSet<Symbol>();

            // load it up to start
            PopulateQueue();
            PopulateQueue();
            PopulateQueue();
            PopulateQueue();

            _timer = new Timer
            {
                AutoReset = true,
                Enabled = true,
                Interval = 1000,
            };

            var lastCount = 0;
            var lastTime = DateTime.Now;
            _timer.Elapsed += (sender, args) =>
            {
                var elapsed = (DateTime.Now - lastTime);
                var ticksPerSecond = (count - lastCount) / elapsed.TotalSeconds;
                Console.WriteLine("TICKS PER SECOND:: " + ticksPerSecond.ToString("000000.0") + " ITEMS IN QUEUE:: " + _ticks.Count);
                lastCount = count;
                lastTime = DateTime.Now;
                PopulateQueue();
            };
        }

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            Tick tick;
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
            foreach (var symbol in symbols)
            {
                lock (_sync)
                {
                    _symbols.Add(symbol);
                }
            }
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                lock (_sync)
                {
                    _symbols.Remove(symbol);
                }
            }
        }

        /// <summary>
        /// Pumps a bunch of ticks into the queue
        /// </summary>
        private void PopulateQueue()
        {
            List<Symbol> symbols;
            lock (_sync)
            {
                symbols = _symbols.ToList();
            }

            foreach (var symbol in symbols)
            {
                if (symbol.Equals("GARAN.E") == false)
                    return;

                // emits 500 per second
                for (int i = 0; i < 50; i++)
                {
                    decimal tmp = 10 + (decimal)Math.Abs(Math.Sin(DateTime.Now.TimeOfDay.TotalMinutes));
                    decimal tmpBid = tmp + 0.01M;
                    decimal tmpAsk = tmp + 0.01M;
                  
                    //_ticks.Enqueue(new Tick(DateTime.Now, symbol, tmp, tmpBid, tmpAsk)
                    //{
                    //    AskSize = (long)tmpAsk * 1000,
                    //    BidSize = (long)tmpBid * 1000
                    //});
                    _ticks.Enqueue(new Tick
                    {
                        Time = DateTime.Now,
                        Symbol = symbol,
                        Value = 10 + (decimal)Math.Abs(Math.Sin(DateTime.Now.TimeOfDay.TotalMinutes)),
                        TickType = TickType.Trade,
                        Quantity = _random.Next(10, (int)_timer.Interval), 
                    }); 
                    
                    //_ticks.Enqueue(new Tick
                    //{
                    //    Time = DateTime.Now,
                    //    Symbol = symbol,
                    //    Value = 10 + (decimal)Math.Abs(Math.Sin(DateTime.Now.TimeOfDay.TotalMinutes)),
                    //    TickType = TickType.Trade,
                    //    Quantity = _random.Next(10, (int)_timer.Interval),
                    //    DataType=MarketDataType.Tick
                    //}); 
                    
                    //_ticks.Enqueue(new Tick
                    //{
                    //    Time = DateTime.Now,
                    //    Symbol = symbol,
                    //    Value = 10 + (decimal)Math.Abs(Math.Sin(DateTime.Now.TimeOfDay.TotalMinutes)),
                    //    TickType = TickType.Quote,
                    //    Quantity = _random.Next(10, (int)_timer.Interval), 
                    //});
                }
            }
        }
    }
}
