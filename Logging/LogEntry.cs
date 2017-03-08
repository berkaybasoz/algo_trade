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
 *
*/

using System;

namespace QuantConnect.Logging
{
    /// <summary>
    /// Log entry wrapper to make logging simpler:
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Time of the log entry
        /// </summary>
        public DateTime Time;

        /// <summary>
        /// Message of the log entry
        /// </summary>
        public string Message;

        /// <summary>
        /// Create a default log message with the current time.
        /// </summary>
        /// <param name="message"></param>
        public LogEntry(string message)
        {
            Time = DateTime.UtcNow;
            Message = message;
        }

        /// <summary>
        /// Create a log entry at a specific time in the analysis (for a backtest).
        /// </summary>
        /// <param name="message">Message for log</param>
        /// <param name="time">Time of the message</param>
        public LogEntry(string message, DateTime time)
        {
            Time = time.ToUniversalTime();
            Message = message;
        }
    }
}
