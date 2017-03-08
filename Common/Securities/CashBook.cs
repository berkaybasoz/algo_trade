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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuantConnect.Data;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides a means of keeping track of the different cash holdings of an algorithm
    /// </summary>
    public class CashBook : IDictionary<string, Cash>
    {

        /// <summary>
        /// Gets the base currency used
        /// </summary>
        public const string AccountCurrency = "USD";

        private readonly Dictionary<string, Cash> _currencies;

        /// <summary>
        /// Gets the total value of the cash book in units of the base currency
        /// </summary>
        public decimal TotalValueInAccountCurrency
        {
            get { return _currencies.Values.Sum(x => x.ValueInAccountCurrency); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CashBook"/> class.
        /// </summary>
        public CashBook()
        {
            _currencies = new Dictionary<string, Cash>();
            _currencies.Add(AccountCurrency, new Cash(AccountCurrency, 0, 1.0m));
        }

        /// <summary>
        /// Adds a new cash of the specified symbol and quantity
        /// </summary>
        /// <param name="symbol">The symbol used to reference the new cash</param>
        /// <param name="quantity">The amount of new cash to start</param>
        /// <param name="conversionRate">The conversion rate used to determine the initial
        /// portfolio value/starting capital impact caused by this currency position.</param>
        public void Add(string symbol, decimal quantity, decimal conversionRate)
        {
            var cash = new Cash(symbol, quantity, conversionRate);
            _currencies.Add(symbol, cash);
        }

        /// <summary>
        /// Checks the current subscriptions and adds necessary currency pair feeds to provide real time conversion data
        /// </summary>
        /// <param name="securities">The SecurityManager for the algorithm</param>
        /// <param name="subscriptions">The SubscriptionManager for the algorithm</param>
        /// <param name="marketHoursDatabase">A security exchange hours provider instance used to resolve exchange hours for new subscriptions</param>
        /// <param name="symbolPropertiesDatabase">A symbol properties database instance</param>
        /// <param name="marketMap">The market map that decides which market the new security should be in</param>
        /// <returns>Returns a list of added currency securities</returns>
        public List<Security> EnsureCurrencyDataFeeds(SecurityManager securities, SubscriptionManager subscriptions, MarketHoursDatabase marketHoursDatabase, SymbolPropertiesDatabase symbolPropertiesDatabase, IReadOnlyDictionary<SecurityType, string> marketMap)
        {
            var addedSecurities = new List<Security>();
            foreach (var cash in _currencies.Values)
            {
                var security = cash.EnsureCurrencyDataFeed(securities, subscriptions, marketHoursDatabase, symbolPropertiesDatabase, marketMap, this);
                if (security != null)
                {
                    addedSecurities.Add(security);
                }
            }
            return addedSecurities;
        }

        /// <summary>
        /// Converts a quantity of source currency units into the specified destination currency
        /// </summary>
        /// <param name="sourceQuantity">The quantity of source currency to be converted</param>
        /// <param name="sourceCurrency">The source currency symbol</param>
        /// <param name="destinationCurrency">The destination currency symbol</param>
        /// <returns>The converted value</returns>
        public decimal Convert(decimal sourceQuantity, string sourceCurrency, string destinationCurrency)
        {
            var source = this[sourceCurrency];
            var destination = this[destinationCurrency];
            var conversionRate = source.ConversionRate*destination.ConversionRate;
            return sourceQuantity*conversionRate;
        }

        /// <summary>
        /// Converts a quantity of source currency units into the account currency
        /// </summary>
        /// <param name="sourceQuantity">The quantity of source currency to be converted</param>
        /// <param name="sourceCurrency">The source currency symbol</param>
        /// <returns>The converted value</returns>
        public decimal ConvertToAccountCurrency(decimal sourceQuantity, string sourceCurrency)
        {
            return Convert(sourceQuantity, sourceCurrency, AccountCurrency);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("{0} {1,13}    {2,10} = {3}", "Symbol", "Quantity", "Conversion", "Value in " + AccountCurrency));
            foreach (var value in Values)
            {
                sb.AppendLine(value.ToString());
            }
            sb.AppendLine("-------------------------------------------------");
            sb.AppendLine(string.Format("CashBook Total Value:                {0}{1}", 
                Currencies.CurrencySymbols[AccountCurrency], 
                Math.Round(TotalValueInAccountCurrency, 2))
                );

            return sb.ToString();
        }

        #region IDictionary Implementation

        /// <summary>
        /// Gets the count of Cash items in this CashBook.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get { return _currencies.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        public bool IsReadOnly
        {
            get { return ((IDictionary<string, Cash>) _currencies).IsReadOnly; }
        }

        /// <summary>
        /// Add the specified item to this CashBook.
        /// </summary>
        /// <param name="item">KeyValuePair of symbol -> Cash item</param>
        public void Add(KeyValuePair<string, Cash> item)
        {
            _currencies.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Add the specified key and value.
        /// </summary>
        /// <param name="symbol">The symbol of the Cash value.</param>
        /// <param name="value">Value.</param>
        public void Add(string symbol, Cash value)
        {
            _currencies.Add(symbol, value);
        }

        /// <summary>
        /// Clear this instance of all Cash entries.
        /// </summary>
        public void Clear()
        {
            _currencies.Clear();
        }

        /// <summary>
        /// Remove the Cash item corresponding to the specified symbol
        /// </summary>
        /// <param name="symbol">The symbolto be removed</param>
        public bool Remove(string symbol)
        {
            return _currencies.Remove (symbol);
        }

        /// <summary>
        /// Remove the specified item.
        /// </summary>
        /// <param name="item">Item.</param>
        public bool Remove(KeyValuePair<string, Cash> item)
        {
            return _currencies.Remove(item.Key);
        }

        /// <summary>
        /// Determines whether the current instance contains an entry with the specified symbol.
        /// </summary>
        /// <returns><c>true</c>, if key was contained, <c>false</c> otherwise.</returns>
        /// <param name="symbol">Key.</param>
        public bool ContainsKey(string symbol)
        {
            return _currencies.ContainsKey(symbol);
        }

        /// <summary>
        /// Try to get the value.
        /// </summary>
        /// <remarks>To be added.</remarks>
        /// <returns><c>true</c>, if get value was tryed, <c>false</c> otherwise.</returns>
        /// <param name="symbol">The symbol.</param>
        /// <param name="value">Value.</param>
        public bool TryGetValue(string symbol, out Cash value)
        {
            return _currencies.TryGetValue(symbol, out value);
        }

        /// <summary>
        /// Determines whether the current collection contains the specified value.
        /// </summary>
        /// <param name="item">Item.</param>
        public bool Contains(KeyValuePair<string, Cash> item)
        {
            return _currencies.Contains(item);
        }

        /// <summary>
        /// Copies to the specified array.
        /// </summary>
        /// <param name="array">Array.</param>
        /// <param name="arrayIndex">Array index.</param>
        public void CopyTo(KeyValuePair<string, Cash>[] array, int arrayIndex)
        {
            ((IDictionary<string, Cash>) _currencies).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets or sets the <see cref="QuantConnect.Securities.Cash"/> with the specified symbol.
        /// </summary>
        /// <param name="symbol">Symbol.</param>
        public Cash this[string symbol]
        {
            get
            {
                Cash cash;
                if (!_currencies.TryGetValue(symbol, out cash))
                {
                    throw new Exception("This cash symbol (" + symbol + ") was not found in your cash book.");
                }
                return cash;
            }
            set { _currencies[symbol] = value; }
        }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection<string> Keys
        {
            get { return _currencies.Keys; }
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public ICollection<Cash> Values
        {
            get { return _currencies.Values; }
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<KeyValuePair<string, Cash>> GetEnumerator()
        {
            return _currencies.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _currencies).GetEnumerator();
        }

        #endregion
    }
}