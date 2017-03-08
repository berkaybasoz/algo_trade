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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators.CandlestickPatterns;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Provides helpers for using candlestick patterns
    /// </summary>
    public class CandlestickPatterns
    {
        private readonly QCAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="CandlestickPatterns"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public CandlestickPatterns(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.TwoCrows"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public TwoCrows TwoCrows(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "TWOCROWS", resolution);
            var pattern = new TwoCrows(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.ThreeBlackCrows"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeBlackCrows ThreeBlackCrows(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREEBLACKCROWS", resolution);
            var pattern = new ThreeBlackCrows(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.ThreeInside"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeInside ThreeInside(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREEINSIDE", resolution);
            var pattern = new ThreeInside(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.ThreeLineStrike"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeLineStrike ThreeLineStrike(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREELINESTRIKE", resolution);
            var pattern = new ThreeLineStrike(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.ThreeOutside"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeOutside ThreeOutside(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREEOUTSIDE", resolution);
            var pattern = new ThreeOutside(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.ThreeStarsInSouth"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeStarsInSouth ThreeStarsInSouth(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREESTARSINSOUTH", resolution);
            var pattern = new ThreeStarsInSouth(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.ThreeWhiteSoldiers"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeWhiteSoldiers ThreeWhiteSoldiers(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREEWHITESOLDIERS", resolution);
            var pattern = new ThreeWhiteSoldiers(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.AbandonedBaby"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public AbandonedBaby AbandonedBaby(Symbol symbol, decimal penetration = 0.3m, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "ABANDONEDBABY", resolution);
            var pattern = new AbandonedBaby(name, penetration);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.AdvanceBlock"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public AdvanceBlock AdvanceBlock(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "ADVANCEBLOCK", resolution);
            var pattern = new AdvanceBlock(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.BeltHold"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public BeltHold BeltHold(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "BELTHOLD", resolution);
            var pattern = new BeltHold(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.Breakaway"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public Breakaway Breakaway(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "BREAKAWAY", resolution);
            var pattern = new Breakaway(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.ClosingMarubozu"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ClosingMarubozu ClosingMarubozu(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "CLOSINGMARUBOZU", resolution);
            var pattern = new ClosingMarubozu(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.ConcealedBabySwallow"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ConcealedBabySwallow ConcealedBabySwallow(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "CONCEALEDBABYSWALLOW", resolution);
            var pattern = new ConcealedBabySwallow(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.Counterattack"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public Counterattack Counterattack(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "COUNTERATTACK", resolution);
            var pattern = new Counterattack(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.DarkCloudCover"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public DarkCloudCover DarkCloudCover(Symbol symbol, decimal penetration = 0.5m, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "DARKCLOUDCOVER", resolution);
            var pattern = new DarkCloudCover(name, penetration);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.Doji"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public Doji Doji(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "DOJI", resolution);
            var pattern = new Doji(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.DojiStar"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public DojiStar DojiStar(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "DOJISTAR", resolution);
            var pattern = new DojiStar(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.DragonflyDoji"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public DragonflyDoji DragonflyDoji(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "DRAGONFLYDOJI", resolution);
            var pattern = new DragonflyDoji(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.Engulfing"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public Engulfing Engulfing(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "ENGULFING", resolution);
            var pattern = new Engulfing(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.EveningDojiStar"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public EveningDojiStar EveningDojiStar(Symbol symbol, decimal penetration = 0.3m, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "EVENINGDOJISTAR", resolution);
            var pattern = new EveningDojiStar(name, penetration);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.EveningStar"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public EveningStar EveningStar(Symbol symbol, decimal penetration = 0.3m, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "EVENINGSTAR", resolution);
            var pattern = new EveningStar(name, penetration);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.GapSideBySideWhite"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public GapSideBySideWhite GapSideBySideWhite(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "GAPSIDEBYSIDEWHITE", resolution);
            var pattern = new GapSideBySideWhite(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.GravestoneDoji"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public GravestoneDoji GravestoneDoji(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "GRAVESTONEDOJI", resolution);
            var pattern = new GravestoneDoji(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.Hammer"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public Hammer Hammer(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "HAMMER", resolution);
            var pattern = new Hammer(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.HangingMan"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public HangingMan HangingMan(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "HANGINGMAN", resolution);
            var pattern = new HangingMan(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.Harami"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public Harami Harami(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "HARAMI", resolution);
            var pattern = new Harami(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.HaramiCross"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public HaramiCross HaramiCross(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "HARAMICROSS", resolution);
            var pattern = new HaramiCross(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.HighWaveCandle"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public HighWaveCandle HighWaveCandle(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "HIGHWAVECANDLE", resolution);
            var pattern = new HighWaveCandle(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new <see cref="Indicators.CandlestickPatterns.Hikkake"/> pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public Hikkake Hikkake(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "HIKKAKE", resolution);
            var pattern = new Hikkake(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }
    }
}
