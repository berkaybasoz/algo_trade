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
using System.IO;

namespace QuantConnect.Logging
{
    /// <summary>
    /// Provides an implementation of <see cref="ILogHandler"/> that writes all log messages to a file on disk.
    /// </summary>
    public class FileLogHandler : ILogHandler
    {
        private bool _disposed;

        // we need to control synchronization to our stream writer since it's not inherently thread-safe
        private readonly object _lock = new object();
        private readonly Lazy<TextWriter> _writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogHandler"/> class to write messages to the specified file path.
        /// The file will be opened using <see cref="FileMode.Append"/>
        /// </summary>
        /// <param name="filepath">The file path use to save the log messages</param>
        public FileLogHandler(string filepath)
        {
            _writer = new Lazy<TextWriter>(
                () => new StreamWriter(File.Open(filepath, FileMode.Append, FileAccess.Write, FileShare.Read))
                );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogHandler"/> class using 'log.txt' for the filepath.
        /// </summary>
        public FileLogHandler()
            : this("log.txt")
        {
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public void Error(string text)
        {
            WriteMessage(text, "ERROR");
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public void Debug(string text)
        {
            WriteMessage(text, "DEBUG");
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public void Trace(string text)
        {
            WriteMessage(text, "TRACE");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_writer.IsValueCreated)
                {
                    _disposed = true;
                    _writer.Value.Dispose();
                }
            }
        }

        /// <summary>
        /// Writes the message to the writer
        /// </summary>
        private void WriteMessage(string text, string level)
        {
            lock (_lock)
            {
                if (_disposed) return;
                _writer.Value.WriteLine("{0} {1}:: {2}", DateTime.UtcNow.ToString("o"), level, text);
                _writer.Value.Flush();
            }
        }
    }
}
