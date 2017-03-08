﻿/*
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
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Logging;

namespace QuantConnect 
{
    /// <summary>
    /// Isolator class - create a new instance of the algorithm and ensure it doesn't 
    /// exceed memory or time execution limits.
    /// </summary>
    public class Isolator
    {
        /// <summary>
        /// Algo cancellation controls - cancel source.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource
        {
            get; private set;
        }

        /// <summary>
        /// Algo cancellation controls - cancellation token for algorithm thread.
        /// </summary>
        public CancellationToken CancellationToken
        {
            get { return CancellationTokenSource.Token; }
        }

        /// <summary>
        /// Check if this task isolator is cancelled, and exit the analysis
        /// </summary>
        public bool IsCancellationRequested
        {
            get { return CancellationTokenSource.IsCancellationRequested; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Isolator"/> class
        /// </summary>
        public Isolator()
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Execute a code block with a maximum limit on time and memory.
        /// </summary>
        /// <param name="timeSpan">Timeout in timespan</param>
        /// <param name="withinCustomLimits">Function used to determine if the codeBlock is within custom limits, such as with algorithm manager
        /// timing individual time loops, return a non-null and non-empty string with a message indicating the error/reason for stoppage</param>
        /// <param name="codeBlock">Action codeblock to execute</param>
        /// <param name="memoryCap">Maximum memory allocation, default 1024Mb</param>
        /// <returns>True if algorithm exited successfully, false if cancelled because it exceeded limits.</returns>
        public bool ExecuteWithTimeLimit(TimeSpan timeSpan, Func<string> withinCustomLimits, Action codeBlock, long memoryCap = 1024)
        {
            // default to always within custom limits
            withinCustomLimits = withinCustomLimits ?? (() => null);

            var message = "";
            var end = DateTime.Now + timeSpan;
            var memoryLogger = DateTime.Now + TimeSpan.FromMinutes(1);

            //Convert to bytes
            memoryCap *= 1024 * 1024;

            //Launch task
            var task = Task.Factory.StartNew(codeBlock, CancellationTokenSource.Token);

            while (!task.IsCompleted && DateTime.Now < end)
            {
                var memoryUsed = GC.GetTotalMemory(false);

                if (memoryUsed > memoryCap)
                {
                    if (GC.GetTotalMemory(true) > memoryCap)
                    {
                        message = "Execution Security Error: Memory Usage Maxed Out - " + Math.Round(Convert.ToDouble(memoryCap / (1024 * 1024))) + "MB max.";
                        break;
                    }
                }

                if (DateTime.Now > memoryLogger)
                {
                    if (memoryUsed > (memoryCap * 0.8))
                    {
                        memoryUsed = GC.GetTotalMemory(true);
                        Log.Error("Execution Security Error: Memory usage over 80% capacity.");
                    }
                    Log.Trace(DateTime.Now.ToString("u") + " Isolator.ExecuteWithTimeLimit(): Used: " + Math.Round(Convert.ToDouble(memoryUsed / (1024 * 1024))));
                    memoryLogger = DateTime.Now.AddMinutes(1);
                }

                // check to see if we're within other custom limits defined by the caller
                var possibleMessage = withinCustomLimits();
                if (!string.IsNullOrEmpty(possibleMessage))
                {
                    message = possibleMessage;
                    break;
                }

                Thread.Sleep(100);
            }

            if (task.IsCompleted == false && message == "")
            {
                message = "Execution Security Error: Operation timed out - " + timeSpan.TotalMinutes + " minutes max. Check for recursive loops.";
                Log.Trace("Isolator.ExecuteWithTimeLimit(): " + message);
            }

            if (message != "")
            {
                CancellationTokenSource.Cancel();
                Log.Error("Security.ExecuteWithTimeLimit(): " + message);
                throw new Exception(message);
            }
            return task.IsCompleted;
        }

        /// <summary>
        /// Execute a code block with a maximum limit on time and memory.
        /// </summary>
        /// <param name="timeSpan">Timeout in timespan</param>
        /// <param name="codeBlock">Action codeblock to execute</param>
        /// <param name="memoryCap">Maximum memory allocation, default 1024Mb</param>
        /// <returns>True if algorithm exited successfully, false if cancelled because it exceeded limits.</returns>
        public bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock, long memoryCap = 1024)
        {
            return ExecuteWithTimeLimit(timeSpan, null, codeBlock, memoryCap);
        }
    }
}
