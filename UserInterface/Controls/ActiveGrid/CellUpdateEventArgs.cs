using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Views.Controls.ActiveGrid
{
    /// <summary>
    /// Class to contain the arguments for a cell update event 
    /// </summary>
    public class CellUpdateEventArgs : System.EventArgs
    {
        #region --- Class Data ------------------------

        private int _column;
        private int _row;
        private decimal _newDecimalValue;
        private string _key;
        private string _cellKey;
        private string _newText;

     

        #endregion

        #region --- Constructor -----------------------

        public CellUpdateEventArgs(int row, int column, decimal newDecimalValue)
        {
            this._row = row;
            this._column = column;
            this._newDecimalValue = newDecimalValue;
            this._key = String.Format("{0}_{1}", row, column);
        }

        public CellUpdateEventArgs(  decimal newDecimalValue, string cellKey) 
        {
            this._newDecimalValue = newDecimalValue;
            this._cellKey = cellKey;
        }
        public CellUpdateEventArgs(string newText, string cellKey)
        {
            this._newText = newText;
            this._cellKey = cellKey;
        }
       
        #endregion

        #region --- Properties ------------------------

        public int Column
        {
            get { return this._column; }
        }

        public int Row
        {
            get { return this._row; }
        }

        public decimal NewDecimalValue
        {
            get { return this._newDecimalValue; }
        }

        public string Key
        {
            get { return this._key; }
        }

        public string NewText
        {
            get { return _newText; }
            set { _newText = value; }
        }

        public string CellKey
        {
            get { return _cellKey; }
        }
        #endregion
    }
}
