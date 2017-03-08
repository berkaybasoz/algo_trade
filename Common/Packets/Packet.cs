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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Base class for packet messaging system
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// Packet type defined by a string enum
        /// </summary>
        [JsonProperty(PropertyName = "eType")]
        public PacketType Type = PacketType.None;

        /// <summary>
        /// User unique specific channel endpoint to send the packets
        /// </summary>
        [JsonProperty(PropertyName = "sChannel")]
        public string Channel = "";

        /// <summary>
        /// Initialize the base class and setup the packet type.
        /// </summary>
        /// <param name="type">PacketType for the class.</param>
        public Packet(PacketType type)
        {
            Channel = "";
            Type = type;
        }
    }

    /// <summary>
    /// Classifications of internal packet system
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PacketType
    {
        /// Default, unset:
        None,

        /// Base type for backtest and live work
        AlgorithmNode,

        /// Autocomplete Work Packet
        AutocompleteWork,

        /// Result of the Autocomplete Job:
        AutocompleteResult,

        /// Controller->Backtest Node Packet:
        BacktestNode,

        /// Packet out of backtest node:
        BacktestResult,

        /// API-> Controller Work Packet:
        BacktestWork,

        /// Controller -> Live Node Packet:
        LiveNode,

        /// Live Node -> User Packet:
        LiveResult,

        /// API -> Controller Packet:
        LiveWork,

        /// Node -> User Algo Security Types
        SecurityTypes,

        /// Controller -> User Error in Backtest Settings:
        BacktestError,

        /// Nodes -> User Algorithm Status Packet:
        AlgorithmStatus,

        /// API -> Compiler Work Packet:
        BuildWork,

        /// Compiler -> User Build Success
        BuildSuccess,

        /// Compiler -> User, Compile Error
        BuildError,

        /// Node -> User Algorithm Runtime Error
        RuntimeError,

        /// Error is an internal handled error packet inside users algorithm
        HandledError,

        /// Nodes -> User Log Message
        Log,

        /// Nodes -> User Debug Message
        Debug,

        /// Nodes -> User, Order Update Event
        OrderEvent,

        /// Boolean true/false success
        Success,

        /// History live job packets
        History,

        /// Result from a command
        CommandResult,

        /// Hook from git hub
        GitHubHook
    }
}
