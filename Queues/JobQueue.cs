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
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Queues
{
    /// <summary>
    /// Implementation of local/desktop job request:
    /// </summary>
    public class JobQueue : IJobQueueHandler
    {
        // The type name of the QuantConnect.Brokerages.Paper.PaperBrokerage
        private const string PaperBrokerageTypeName = "PaperBrokerage";
        private bool _liveMode = Config.GetBool("live-mode");
        private static readonly string Channel = Config.Get("job-channel");
        private static readonly int UserId = Config.GetInt("job-user-id", int.MaxValue);
        private static readonly int ProjectId = Config.GetInt("job-project-id", int.MaxValue);
        private static readonly string AlgorithmTypeName = Config.Get("algorithm-type-name");
        private readonly Language Language = (Language)Enum.Parse(typeof(Language), Config.Get("algorithm-language"));

        /// <summary>
        /// Physical location of Algorithm DLL.
        /// </summary>
        private string AlgorithmLocation
        {
            get
            {
                // we expect this dll to be copied into the output directory
                return Config.Get("algorithm-location", "QuantConnect.Algorithm.CSharp.dll"); 
            }
        }

        /// <summary>
        /// Initialize the job queue:
        /// </summary>
        public void Initialize()
        {
            //
        }
        
        /// <summary>
        /// Desktop/Local Get Next Task - Get task from the Algorithm folder of VS Solution.
        /// </summary>
        /// <returns></returns>
        public AlgorithmNodePacket NextJob(out string location)
        {
            location = AlgorithmLocation;
            Log.Trace("JobQueue.NextJob(): Selected " + location);

            // check for parameters in the config
            var parameters = new Dictionary<string, string>();
            var parametersConfigString = Config.Get("parameters");
            if (parametersConfigString != string.Empty)
            {
                parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(parametersConfigString);
            }

            //If this isn't a backtesting mode/request, attempt a live job.
            if (_liveMode)
            {
                var liveJob = new LiveNodePacket
                {
                    Type = PacketType.LiveNode,
                    Algorithm = File.ReadAllBytes(AlgorithmLocation),
                    Brokerage = Config.Get("live-mode-brokerage", PaperBrokerageTypeName),
                    Channel = Channel,
                    UserId = UserId,
                    ProjectId = ProjectId,
                    Version = Constants.Version,
                    DeployId = AlgorithmTypeName,
                    RamAllocation = int.MaxValue,
                    Parameters = parameters,
                    Language = Language,
                };

                try
                { 
                    // import the brokerage data for the configured brokerage
                    var brokerageFactory = Composer.Instance.Single<IBrokerageFactory>(factory => factory.BrokerageType.MatchesTypeName(liveJob.Brokerage));
                    liveJob.BrokerageData = brokerageFactory.BrokerageData;
                }
                catch (Exception err)
                {
                    Log.Error(err, string.Format("Error resolving BrokerageData for live job for brokerage {0}:", liveJob.Brokerage));
                }

                return liveJob;
            }

            //Default run a backtesting job.
            var backtestJob = new BacktestNodePacket(0, 0, "", new byte[] {}, 10000, "local")
            {
                Type = PacketType.BacktestNode,
                Algorithm = File.ReadAllBytes(AlgorithmLocation),
                Channel = Channel,
                UserId = UserId,
                ProjectId = ProjectId,
                Version = Constants.Version,
                BacktestId = AlgorithmTypeName,
                RamAllocation = int.MaxValue,
                Language = Language,
                Parameters = parameters
            };

            return backtestJob;
        }

        /// <summary>
        /// Desktop/Local acknowledge the task processed. Nothing to do.
        /// </summary>
        /// <param name="job"></param>
        public void AcknowledgeJob(AlgorithmNodePacket job)
        {
            // Make the console window pause so we can read log output before exiting and killing the application completely
            Log.Trace("Engine.Main(): Analysis Complete. Press any key to continue.");
            Console.Read();
        }
    }

}
