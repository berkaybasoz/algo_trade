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
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;
using QuantConnect.Brokerages.TEB.Data;

namespace QuantConnect.Brokerages.TEB
{
    /// <summary>
    /// Provides an implementations of IBrokerageFactory that produces a TEBBrokerage
    /// </summary>
    public class TEBBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Gets TEB values from configuration
        /// </summary>
        public static class Configuration
        {
            /// <summary>
            /// 
            /// </summary>
            public static int QuantConnectUserID
            {
                get { return Config.GetInt("qc-user-id"); }
            }
            /// <summary>
            /// 
            /// </summary>
            public static long AccountID
            {
                get { return Config.GetInt("TEB-account-id"); }
            }
            /// <summary>
            /// 
            /// </summary>
            public static string ConnectionString
            {
                get { return Config.Get("TEB-connection-string"); }
            }
            
            /// <summary>
            /// 
            /// </summary>
            public static string SenderSubID
            {
                get { return Config.Get("TEB-fix-sender-sub-id"); }
            }
            
            /// <summary>
            /// 
            /// </summary>
            public static string FixApplicationName
            {
                get { return Config.Get("TEB-fix-app-name"); }
            }

            /// <summary>
            /// 
            /// </summary>
            public static int FixConnectionTimeout
            {
                get { return  Config.GetInt("TEB-fix-connect-timeout",2500); }
            }
        }



        /// <summary>
        /// Initializes a new instance of he TEBBrokerageFactory class
        /// </summary>
        public TEBBrokerageFactory()
            : base(typeof(TEBBrokerage))
        {
        }

        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration/disk
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData
        {
            get
            {


                // always need to grab account ID from configuration


                var data = new Dictionary<string, string>();
                data.Add("TEB-account-id", Configuration.AccountID.ToString());
                data.Add("TEB-connection-string", Configuration.ConnectionString );
                data.Add("TEB-sender-sub-id", Configuration.SenderSubID ); 
                data.Add("TEB-fix-app-name", Configuration.FixApplicationName);
                data.Add("TEB-fix-connect-timeout", Configuration.FixConnectionTimeout.ToString());
                return data;
            }
        }


        public override IBrokerageModel BrokerageModel
        {
            get { return new TEBBrokerageModel(); }
        }


        TEBBrokerage brokerage;//TODO : bunu local olarak kullan şimdilik dispose için global tanımladık
        /// <summary>
        /// Creates a new IBrokerage instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();
         
            //var accountID = Read<string>(job.BrokerageData, "TEB-account-id", errors);
            //var connectionString = Read<string>(job.BrokerageData, "TEB-connection-string", errors);
            //var applicationName = Read<string>(job.BrokerageData, "TEB-fix-app-name", errors);
            //var senderSubId = Read<string>(job.BrokerageData, "TEB-fix-sender-sub-id", errors);

            var accountID = Read<long>(BrokerageData,"TEB-account-id", errors).ToString();
            var connectionString = Read<string>(BrokerageData, "TEB-connection-string", errors);
            var senderSubId = Read<string>(BrokerageData, "TEB-fix-sender-sub-id", errors);
            var applicationName = Read<string>(BrokerageData, "TEB-fix-app-name", errors);
            var timeout = Read<int>(BrokerageData, "TEB-fix-connect-timeout", errors);


              brokerage = new TEBBrokerage(algorithm, algorithm.Transactions, algorithm.Portfolio,
                accountID, connectionString, applicationName, senderSubId, timeout);
              TEBMatriksTcpDataQueue dataQueue = new TEBMatriksTcpDataQueue();
              Composer.Instance.AddPart<IDataQueueHandler>(dataQueue);

            return brokerage;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override void Dispose()
        {
            if (brokerage != null)
                brokerage.Dispose();
        }



    }
}
