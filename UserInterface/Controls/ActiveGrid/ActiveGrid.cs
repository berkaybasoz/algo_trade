using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace QuantConnect.Views.Controls.ActiveGrid
//namespace SKACERO
{
    #region --- Enumerations -----------------------------------------

    public enum SortOrderEnum
    {
        Unsorted = 0,
        Ascending,
        Descending
    }

    #endregion

    #region --- Class ActiveColumnHeader -----------------------------

    /// <summary>
    /// Displays a single column header in a ActiveGrid control
    /// </summary>
    public class ActiveColumnHeader : ColumnHeader
    {
        #region --- Class Data ----------------------------------

        private StringAlignment fldVerticalAlignment;
        private StringAlignment fldHorizontallAlignment;
        private String fldFormat;
        private SortOrderEnum _sortOrder;
        private bool _displayZeroValue;

        #endregion

        #region --- Constructor ---------------------------------

        public ActiveColumnHeader()
            : base()
        {
            // Set the default values
            Initialize();
        }

        public ActiveColumnHeader(int imageIndex)
            : base(imageIndex)
        {
            // Set the default values
            Initialize();
        }

        public ActiveColumnHeader(string imageKey)
            : base(imageKey)
        {
            // Set the default values
            Initialize();
        }

        private void Initialize()
        {
            this.fldHorizontallAlignment = StringAlignment.Center;
            this.fldVerticalAlignment = StringAlignment.Center;
            this.fldFormat = String.Empty;
            this._sortOrder = SortOrderEnum.Unsorted;
            this._displayZeroValue = true;
        }

        #endregion

        #region --- Public Methods ------------------------------

        /// <summary>
        /// Toggle the sort order of this column between ascending and descending.
        /// </summary>
        /// <returns>The new sort order for this column</returns>
        public SortOrderEnum SwitchSortOrder()
        {
            this._sortOrder = (this._sortOrder == SortOrderEnum.Ascending) ? SortOrderEnum.Descending : SortOrderEnum.Ascending;
            return this._sortOrder;
        }

        /// <summary>
        /// Reset this column to its default values.
        /// </summary>
        public void Reset()
        {
            this._sortOrder = SortOrderEnum.Ascending;
        }

        #endregion

        #region --- Cell Appearance Properties ------------------

        [Category("Cell Appearance"),
        DefaultValue(StringAlignment.Center),
        Description("Vertical alignment of the text in all cells belonging to this column"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public StringAlignment CellVerticalAlignment
        {
            get { return this.fldVerticalAlignment;  }
            set { this.fldVerticalAlignment = value; }
        }

        [Category("Cell Appearance"),
        DefaultValue(StringAlignment.Center),
        Description("Horizontal alignment of the text in all cells belonging to this column"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public StringAlignment CellHorizontalAlignment
        {
            get { return this.fldHorizontallAlignment;  }
            set { this.fldHorizontallAlignment = value; }
        }

        [Category("Cell Appearance"),
        DefaultValue(null),
        Description("Format specifier for the text in all cells belonging to this column"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public String CellFormat
        {
            get { return String.IsNullOrEmpty(this.fldFormat) ? String.Empty : this.fldFormat; }
            set { this.fldFormat = value; }
        }



        [Category("Cell Appearance"),
        DefaultValue(true),
        Description("Show or hide the value of zero"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Boolean DisplayZeroValues
        {
            get { return this._displayZeroValue;  }
            set { this._displayZeroValue = value; }

        }

        #endregion

        #region --- Properties ----------------------------------

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ActiveGrid Grid
        {
            get { return base.ListView as ActiveGrid; }
        }

        public SortOrderEnum SortOrder
        {
            get { return this._sortOrder;  }
            set { this._sortOrder = value; }
        }

        #endregion
    }

    #endregion

    #region --- Class ActiveGrid -------------------------------------

    /// <summary>
    /// Represents a lightweight grid control that supports real-time notification
    /// of cell changes.
    /// ( Based in a standard ListView control in 'Detail View' mode )
    /// </summary>
    public class ActiveGrid : ListView
    {
        #region --- Class Data -----------------------------------------

        private System.ComponentModel.IContainer components = null;
        private ActiveColumnHeaderCollection _lvColumns;
        private ActiveRowCollection _lvRows;
        private bool _layoutSuspended;
        private int _groupIndex;
        private object syncRoot = new object();
        private Dictionary<String, ActiveRow.ActiveCell> _cells = new Dictionary<string, ActiveRow.ActiveCell>();
        private string _mouseOverRowKey = String.Empty;
        private bool _rowHeaderLikeButton = false;

        #region --- Row Values ----------------------------

        private bool fldUseAlternateRow;
        private bool fldUseGradient;

        #endregion

        #region --- Flash Values --------------------------

        private bool fldAllowFlashing;
        private bool fldFlashUseGradient;
        private bool fldFlashFadeEffect;
        private int fldFlashDuration;
        private int _flashCount;
        private Font fldFlashFont;

        #endregion

        #endregion

        #region --- Delegates ------------------------------------------

        public event OnRowHeaderLeftMouseClickHandler OnRowHeaderLeftMouseClick;
        public delegate void OnRowHeaderLeftMouseClickHandler(object sender, RowHeaderEventArgs e);

        public event OnRowHeaderRightMouseClickHandler OnRowHeaderRightMouseClick;
        public delegate void OnRowHeaderRightMouseClickHandler(object sender, RowHeaderEventArgs e);

        #endregion

        #region --- Constructor ----------------------------------------

        public ActiveGrid()
            : base()
        {
            this.components = new System.ComponentModel.Container();

            // This control is only ever intended to be used in the 'Details' view mode
            base.View = System.Windows.Forms.View.Details;

            this._lvColumns = new ActiveColumnHeaderCollection(this);
            this._lvRows = new ActiveRowCollection(this);
            this._layoutSuspended = false;
            this._groupIndex = 0;
            this._flashCount = 0;


            // NON-FLASH Defaults
            this.fldUseAlternateRow = true;
            this.fldUseGradient = false;
            this.fldFlashFadeEffect = false;


            // FLASH Defaults
            this.fldAllowFlashing = false;
            this.fldFlashUseGradient = false;
            this.fldFlashDuration = ActiveRow.ActiveCell.DEFAULT_FLASH_DURATION;
            this.fldFlashFont = this.Font;

            // Activate double buffering
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // Enable the OnNotifyMessage event so we get a chance to filter out 
            // Windows messages before they get to the form's WndProc
            this.SetStyle(ControlStyles.EnableNotifyMessage, true);

            this.OwnerDraw = true;

        }

        public ActiveGrid(IContainer container)
            : this()
        {
            this.components = container;
        }

        #endregion

        #region --- Public Methods -------------------------------------

        /// <summary>
        /// Adds a new column with the specified name to the grid
        /// </summary>
        /// <param name="name">The name associated with the new column</param>
        /// <param name="text">The text to be displayed in the column header</param>
        /// <param name="width">The width of the column</param>
        /// <param name="hAlign">The horizontal alignment of the text in the column header</param>
        /// <param name="value">Object containing data associated with the column header</param>
        /// <returns>The newly created column if successful, otherwise null</returns>
        public ActiveColumnHeader AddColumn(string name, string text, int width, HorizontalAlignment hAlign, object value)
        {
            ActiveColumnHeader newColumn = null;

            // Make sure that a column with this name doesn't already exist.
            if (this.Columns.ContainsKey(name))
                throw new DuplicateColumnNameException(String.Format("{0} : A column with this name already exists", name));

            // Column doesn't already exist so we can add it.
            newColumn = new ActiveColumnHeader();
            if (newColumn != null)
            {
                newColumn.Name = name;
                newColumn.Text = text;
                newColumn.Width = width;
                newColumn.TextAlign = hAlign;
                newColumn.Tag = value;

                this.Columns.Add(newColumn);
            }

            return newColumn;
        }

        /// <summary>
        /// Returns the position of the first row that has a value equal to or greater than 'name'
        /// This is the first position where an element with value 'name' could get inserted 
        /// without breaking the actual sorting of the range.
        /// </summary>
        /// <param name="name">Value to compare</param>
        /// <returns>The position of the first row that has a value equal to or greater than 'name'</returns>
        public int LowerBound(string name)
        {
            int index = 0;
            foreach (ActiveRow itm in this.Items)
            {
                if (String.CompareOrdinal(itm.Text, name) >= 0)
                {
                    index = itm.Index;
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// Returns the position of the first row that has a value greater than 'name'
        /// This is the last position where an element with value 'name' could get inserted 
        /// without breaking the actual sorting of the range.
        /// </summary>
        /// <param name="name">Value to compare</param>
        /// <returns>The position of the first row that has a value greater than 'name'</returns>
        public int UpperBound(string name)
        {
            int index = 0;
            foreach (ActiveRow itm in this.Items)
            {
                if (String.CompareOrdinal(itm.Text.ToUpper(), name.ToUpper()) > 0)
                {
                    index = itm.Index;
                    break;
                }

                index++;
            }

            return index;
        }

        /// <summary>
        /// Removes the column with the specified name from the grid
        /// </summary>
        /// <param name="name"></param>
        public void RemoveColumn(string name)
        {
            if (this.Columns.ContainsKey(name))
                this.Columns.RemoveByKey(name);
        }

        /// <summary>
        /// Removes the row with the specified name from the grid.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveRow(string name)
        {
            if (this.Items.ContainsKey(name))
                this.Items.RemoveByKey(name);
        }

        /// <summary>
        /// Checks whether a row with the given name exists in the grid.
        /// </summary>
        /// <param name="rowname">The name of the row</param>
        /// <returns>true if the row exists; otherwise, false.</returns>
        public bool RowExists(string rowname)
        {
            return (!String.IsNullOrEmpty(rowname) && this.Items.ContainsKey(rowname));
        }

        /// <summary>
        /// Shows the column with the given name by setting the width to the value provided.
        /// </summary>
        /// <param name="columnname"></param>
        /// <param name="width">The name of the column to show</param>
        public void ShowColumn(string columnname, int width)
        {
            if (width <= 0)
            {
                HideColumn(columnname);
            }
            else if (this.Columns.ContainsKey(columnname))
            {
                ColumnHeader column = this.Columns[this.Columns.IndexOfKey(columnname)];
                if (column != null)
                    column.Width = width;
            }
        }

        /// <summary>
        /// Hides the column with the given name by setting the width to zero.
        /// </summary>
        /// <param name="columnname">The name of the column to hide</param>
        public void HideColumn(string columnname)
        {
            if (this.Columns.ContainsKey(columnname))
            {
                ColumnHeader column = this.Columns[this.Columns.IndexOfKey(columnname)];
                if (column != null)
                    column.Width = 0;
            }
        }

        /// <summary>
        /// Temporarily suspends the layout logic for the grid
        /// </summary>
        public new void SuspendLayout()
        {
            lock (syncRoot)
            {
                this._layoutSuspended = true;

                base.SuspendLayout();

                base.BeginUpdate();
            }
        }

        /// <summary>
        /// Resumes usual layout logic, optionally forcing an immediate layout
        /// of pending layout requests.
        /// </summary>
        /// <param name="performLayout">true to execute bending layout requests; otherwise false</param>
        public new void ResumeLayout(bool performLayout)
        {
            lock (syncRoot)
            {
                this._layoutSuspended = false;

                base.EndUpdate();

                base.ResumeLayout(performLayout);
            }
        }

        /// <summary>
        /// Resumes usual layout logic, optionally forcing an immediate layout
        /// of pending layout requests.
        /// </summary>
        public new void ResumeLayout()
        {
            lock (syncRoot)
            {
                this._layoutSuspended = false;

                base.EndUpdate();

                base.ResumeLayout();
            }
        }

        /// <summary>
        /// Increment the number of cells that are currently in a flashed state
        /// </summary>
        public void IncrementFlashCount()
        {
           Interlocked.Increment(ref this._flashCount);
        }

        /// <summary>
        /// Decrement the number of cells that are currently in a flashed state
        /// </summary>
        public void DecrementFlashCount()
        {
           Interlocked.Decrement(ref this._flashCount);
        }

        public ActiveRow.ActiveCell FindCell(string keyRow, string keyColumn)
        {
            ActiveRow.ActiveCell cell = null;

            if (this._lvRows.ContainsKey(keyRow) && this.Columns.ContainsKey(keyColumn))
            {
                ActiveRow row = this._lvRows[keyRow];
                if (row != null )
                    cell = row.SubItems[this.Columns.IndexOfKey(keyColumn)];
            }

            return cell;
        }

        public ActiveRow.ActiveCell FindCell(string key)
        {
            ActiveRow.ActiveCell cell = null;

            if (!this._cells.TryGetValue(key, out cell))
                cell = null;

            return cell;
        }


        public override string ToString()
        {
            return "ActiveGrid{}";
        }

        #endregion

        #region --- Protected Methods ----------------------------------

        /// <summary>
        /// Invalidates the specific region of the grid and causes a paint message to be sent to the grid.
        /// </summary>
        /// <param name="rc"></param>
        private delegate void InvalidateCallback(Rectangle rc);
        protected new void Invalidate(Rectangle rc)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new InvalidateCallback(Invalidate), new object[] { rc });
                return;
            }

            base.Invalidate(rc, false);
        }

        /// <summary>
        /// Invalidates the specific cell of the grid and causes a paint message to be sent to the grid.
        /// </summary>
        /// <param name="cell"></param>
        private delegate void InvalidateCellCallback(ActiveRow.ActiveCell cell);
        public void InvalidateCell(ActiveRow.ActiveCell cell)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new InvalidateCellCallback(InvalidateCell), new object[] { cell });
                return;
            }

            if (cell != null)
             cell.Draw(base.CreateGraphics());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        protected override void OnNotifyMessage(Message m)
        {
            //Filter out the WM_ERASEBKGND message
            if (m.Msg != 0x14)
              base.OnNotifyMessage(m);
        }

        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
            base.OnDrawColumnHeader(e);
        }

        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;
            ActiveRow row = e.Item as ActiveRow;
            if (row != null)
                row.Draw(e);
        }

        protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = false;
            ActiveRow.ActiveCell cell = e.SubItem as ActiveRow.ActiveCell;
            if (cell != null)
                cell.Draw(e.Graphics, e.Item.Selected);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            ListViewHitTestInfo lvh = this.HitTest(e.Location);
            if (lvh != null)
            {
                ActiveRow row = lvh.Item as ActiveRow;
                if (row != null)
                {
                    // Has the row changed
                    if (this._mouseOverRowKey != row.Name)
                    {
                        ActiveRow rowPrevious = this.Items[this._mouseOverRowKey];
                        if (rowPrevious != null)
                            rowPrevious.OnMouseMove(e);
                    }

                    this._mouseOverRowKey = row.Name;
                    row.OnMouseMove(e);
                }
                else
                {
                    OnRowLeave(e);
                }
            }
            else
            {
                    OnRowLeave(e);
            }
        }

        private void OnRowLeave(MouseEventArgs e)
        {
            if (!String.IsNullOrEmpty(this._mouseOverRowKey))
            {
                ActiveRow rowPrevious = this.Items[this._mouseOverRowKey];
                if (rowPrevious != null)
                    rowPrevious.OnMouseMove(e);
            }

            this._mouseOverRowKey = String.Empty;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            OnRowLeave(new MouseEventArgs( MouseButtons.None, 0, -1, -1, 0) );
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            bool handled = false;

            ListViewHitTestInfo lvh = this.HitTest(e.Location);
            if (lvh != null)
            {
                ActiveRow row = lvh.Item as ActiveRow;
                if (row != null)
                    handled = row.OnMouseClick(e);
            }

            if(!handled)
              base.OnMouseClick(e);
        }


        public void RowHeaderLeftClick(ActiveRow row)
        {
            if (row != null)
            {
                OnRowHeaderLeftMouseClickHandler onRowHeaderLeftMouseClick = OnRowHeaderLeftMouseClick;
                if (onRowHeaderLeftMouseClick != null)
                    onRowHeaderLeftMouseClick(this, new RowHeaderEventArgs(row.Index, row.Name, row.Tag));
            }
        }

        public void RowHeaderRightClick(ActiveRow row)
        {
            if (row != null)
            {
                OnRowHeaderRightMouseClickHandler onRowHeaderRightMouseClick = OnRowHeaderRightMouseClick;
                if (onRowHeaderRightMouseClick != null)
                    onRowHeaderRightMouseClick(this, new RowHeaderEventArgs(row.Index, row.Name, row.Tag));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region --- Appearance Alternating Row Properties --------------

        [Category("Appearance Alternating Row"),
        DefaultValue(true),
        Description("Draw alternating backgrounds"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Boolean UseAlternateRowColors
        {
            get { return this.fldUseAlternateRow; }
            set { this.fldUseAlternateRow = value; }
        }

        [Category("Appearance Alternating Row"),
        DefaultValue(null),
        Description("Background color of alternate rows in the list"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color AlternatingBackColor
        {
            get { return ActiveGrid.Paintbox.AlternateBackgroundColor;  }
            set { ActiveGrid.Paintbox.AlternateBackgroundColor = value; }
        }

        [Category("Appearance Alternating Row"),
        DefaultValue(false),
        Description("Draw backgrounds using a gradient"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Boolean UseGradient
        {
            get { return this.fldUseGradient;  }
            set { this.fldUseGradient = value; }
        }

        [Category("Appearance Alternating Row"),
        DefaultValue(LinearGradientMode.Vertical),
        Description("Direction of the background gradient"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public LinearGradientMode LinearGradientMode
        {
            get { return ActiveGrid.Paintbox.RowLinearGradientMode; }
            set { ActiveGrid.Paintbox.RowLinearGradientMode = value; }
        }

        [Category("Appearance Alternating Row"),
        DefaultValue(null),
        Description("Start Color of the alternating background gradient"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color AlternatingGradientStartColor
        {
            get { return ActiveGrid.Paintbox.GradientStartColor;  }
            set { ActiveGrid.Paintbox.GradientStartColor = value; }
        }

        [Category("Appearance Alternating Row"),
        DefaultValue(null),
        Description("End Color of the alternating background gradient"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color AlternatingGradientEndColor
        {
            get { return ActiveGrid.Paintbox.GradientEndColor;  }
            set { ActiveGrid.Paintbox.GradientEndColor = value; }
        }

        #endregion

        #region --- Flash Behaviour Properties -------------------------

        [Category("Flash Behavior"),
        DefaultValue(null),
        Description("Flash the cell when its contents are changed"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Boolean AllowFlashing
        {
            get { return this.fldAllowFlashing;  }
            set { this.fldAllowFlashing = value; }
        }

        [Category("Flash Behavior"),
        DefaultValue(false),
        Description("Draw gradient backgrounds for flashed cells"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Boolean UseFlashGradient
        {
            get { return this.fldFlashUseGradient;  }
            set { this.fldFlashUseGradient = value; }
        }

        [Category("Flash Behavior"),
        DefaultValue(false),
        Description("Fade-out effect for flashed cells"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Boolean UseFlashFadeOut
        {
            get { return this.fldFlashFadeEffect; }
            set { this.fldFlashFadeEffect = value; }
        }       

        [Category("Flash Behavior"),
        DefaultValue(null),
        Description("Length of time in milliseconds that a cell remains in the flashed state"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Int32 FlashDuration
        {
            get { return this.fldFlashDuration;  }
            set { this.fldFlashDuration = value; }
        }

        [Category("Flash Behavior"),
        DefaultValue(null),
        Description("Color of the text when a cell is in the flashed state"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color FlashForeColor
        {
            get { return ActiveGrid.Paintbox.FlashForeColor;  }
            set { ActiveGrid.Paintbox.FlashForeColor = value; }
        }

        [Category("Flash Behavior"),
        DefaultValue(null),
        Description("Color of the background when a cell is in the flashed state"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color FlashBackColor
        {
            get { return ActiveGrid.Paintbox.FlashBackgroundColor;  }
            set { ActiveGrid.Paintbox.FlashBackgroundColor = value; }
        }

        [Category("Flash Behavior"),
        DefaultValue(null),
        Description("Font used when a cell is in the flashed state"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Font FlashFont
        {
            get { return this.fldFlashFont;  }
            set { this.fldFlashFont = value; }
        }

        [Category("Flash Behavior"),
        DefaultValue(null),
        Description("Start Color of the background gradient when a cell is in the flashed state"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color FlashGradientStartColor
        {
            get { return ActiveGrid.Paintbox.FlashGradientStartColor;  }
            set { ActiveGrid.Paintbox.FlashGradientStartColor = value; }
        }

        [Category("Flash Behavior"),
        DefaultValue(null),
        Description("End Color of the background gradient when a cell is in the flashed state"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color FlashGradientEndColor
        {
            get { return ActiveGrid.Paintbox.FlashGradientEndColor;  }
            set { ActiveGrid.Paintbox.FlashGradientEndColor = value; }
        }

        [Category("Flash Behavior"),
        DefaultValue(LinearGradientMode.Vertical),
        Description("Direction of the Background gradient for flashed cells"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public LinearGradientMode FlashLinearGradientMode
        {
            get { return ActiveGrid.Paintbox.FlashLinearGradientMode;  }
            set { ActiveGrid.Paintbox.FlashLinearGradientMode = value; }
        }

        #endregion

        #region --- Public Properties ----------------------------------

        public new Color BackColor
        {
            get { return ActiveGrid.Paintbox.NormalBackgroundColor;  }
            set { ActiveGrid.Paintbox.NormalBackgroundColor = value; }
        }

        [Category("Appearance"),
        DefaultValue(null),
        Description("Fore color for negative numeric values"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color ForeColorNegativeValues
        {
            get { return ActiveGrid.Paintbox.NegativeValueColor;  }
            set { ActiveGrid.Paintbox.NegativeValueColor = value; }
        }

        [Category("Appearance"),
        DefaultValue(System.Windows.Forms.View.Details),
        Description("Forces the view-style to be set to Details"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public new System.Windows.Forms.View View
        {
            get {  return System.Windows.Forms.View.Details;  }
            set {  base.View = System.Windows.Forms.View.Details; }
        }

        /// <summary>
        /// Flag to indicate if the Row Header labels should behave like 
        /// a LinkLabel control.
        /// </summary>
        [Category("Behavior"),
        DefaultValue(false),
        Description("The ActiveRow labels behave like a linkLabel control"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Boolean UseRowHeaderButtons
        {
            get { return this._rowHeaderLikeButton;  }
            set { this._rowHeaderLikeButton = value; }
        }

        /// <summary>
        /// An ActiveColumnCollection representing the collection of 
        /// ActiveColumn contained within the ActiveGrid
        /// </summary>
        [Category("Behavior"),
        DefaultValue(null),
        Description("The ActiveColumnHeader controls contained in the ActiveGrid"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public new ActiveColumnHeaderCollection Columns
        {
            get { return this._lvColumns; }
        }

        /// <summary>
        /// An ActiveRowCollection representing the collection of 
        /// ActiveRow objectss contained within the ActiveGrid
        /// </summary>
        [Category("Behavior"),
        DefaultValue(null),
        Description("The ActiveRow items contained in the ActiveGrid"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public new ActiveRowCollection Items
        {
            get { return this._lvRows; }
        }

        public Int32 GroupIndex
        {
            get { return this._groupIndex; }
            set { this._groupIndex = value; }
        }

        /// <summary>
        /// Fetches the cell at the given coordinates
        /// </summary>
        /// <param name="row">Row number index</param>
        /// <param name="column">Column number index</param>
        /// <returns>The ActiveCell object representing the selected cell if successful, otherwise null</returns>
        public ActiveRow.ActiveCell this[int row, int column]
        {
            get { return this[row][column]; }
        }

        /// <summary>
        /// Fetches the row at the given index
        /// </summary>
        /// <param name="index">Row number index</param>
        /// <returns>The ActiveRow object representing the selected row if successful, otherwise null</returns>
        public ActiveRow this[int index]
        {
            get
            {
                if (index >= 0 && index < this.Items.Count)
                    return this.Items[index] as ActiveRow;
                else
                    throw (new IndexOutOfRangeException(String.Format("The row index [{0}] is out of range", index)));
            }
        }

        public Boolean LayoutSuspended
        {
            get { return this._layoutSuspended; }
        }

        public Int32 FlashedCellCount
        {
            get { lock (syncRoot) { return this._flashCount; } }
        }

        #endregion

        #region --- Nested Class ActiveRowCollection -------------------

        /// <summary>
        /// Represents the collection of items in a ActiveGrid control or assigned to a ListViewGroup. 
        /// </summary>
        public class ActiveRowCollection : ListView.ListViewItemCollection
        {
            #region --- Class Data ---------------------------------

            private ActiveGrid _owner;

            #endregion

            #region --- Constructor --------------------------------

            /// <summary>
            /// Initializes a new instance of the ActiveGrid.ActiveRowCollection class
            /// </summary>
            /// <param name="owner">An ActiveGrid representing the grid that owns 
            /// the Control collection</param>
            public ActiveRowCollection(ActiveGrid owner)
                : base(owner)
            {
                if (owner == null)
                    throw new NullReferenceException("ActiveRowCollection: Owner must not be a null value");

                this._owner = owner;
            }

            #endregion

            #region --- Public Methods -----------------------------

            /// <summary>
            /// Adds an existing ActiveRow to the collection.
            /// </summary>
            /// <param name="value">The ActiveRow to add to the collection</param>
            /// <returns>The ActiveRow that was added to the collection</returns>
            public ActiveRow Add(ActiveRow value)
            {
                ActiveRow row = base.Add(value) as ActiveRow;

                if (row != null)
                {
                    for (int i = 1; i < row.SubItems.Count; i++)
                    {
                        ActiveRow.ActiveCell cell = row[i];
                        if (!String.IsNullOrEmpty(cell.Name))
                            this._owner._cells.Add(cell.Name, cell);
                    }
                }

                return row;
            }

            /// <summary>
            /// Adds an item to the collection with the specified text.
            /// </summary>
            /// <param name="text">The text to display for the item.</param>
            /// <returns>The ActiveRow that was added to the collection</returns>
            public new ActiveRow Add(string text)
            {
                return base.Add(new ActiveRow(text)) as ActiveRow;
            }

            /// <summary>
            /// Adds an item to the collection with the specified text and image.
            /// </summary>
            /// <param name="text">The text of the item.</param>
            /// <param name="imageIndex">The index of the image to display for the item.</param>
            /// <returns></returns>
            public new ActiveRow Add(string text, int imageIndex)
            {
                return base.Add(new ActiveRow(text, imageIndex)) as ActiveRow;
            }

            /// <summary>
            /// Creates an item with the specified text and image and adds it to the collection. 
            /// </summary>
            /// <param name="text">The text of the item.</param>
            /// <param name="imageKey">The key of the image to display for the item.</param>
            /// <returns>The ActiveRow added to the collection.</returns>
            public new ActiveRow Add(string text, string imageKey)
            {
                return base.Add(new ActiveRow(text, imageKey)) as ActiveRow;
            }

            /// <summary>
            /// Creates an item with the specified key, text, and image and adds an item to the collection. 
            /// </summary>
            /// <param name="key">The name of the item</param>
            /// <param name="text">The text of the item</param>
            /// <param name="imageIndex">The index of the image to display for the item</param>
            /// <returns>The ListViewItem added to the collection</returns>
            public new ActiveRow Add(string key, string text, int imageIndex)
            {
                return base.Add(new ActiveRow(key, text, imageIndex)) as ActiveRow;
            }

            /// <summary>
            /// Creates and item with the specified key, text, and image, and adds it to the collection. 
            /// </summary>
            /// <param name="key">The name of the item</param>
            /// <param name="text">The text of the item</param>
            /// <param name="imageKey">The key of the image to display for the item</param>
            /// <returns>The ActiveRow added to the collection</returns>
            public new ActiveRow Add(string key, string text, string imageKey)
            {
                return base.Add(new ActiveRow(key, text, imageKey)) as ActiveRow;
            }

            /// <summary>
            /// Adds a collection of items to the collection.
            /// </summary>
            /// <param name="items">The ActiveGrid.ActiveRowCollection to add to the collection.</param>
            public void AddRange(ActiveRowCollection items)
            {
                base.AddRange(items);

                foreach (ActiveRow row in items)
                {
                    for (int i = 1; i < row.SubItems.Count; i++)
                    {
                        ActiveRow.ActiveCell cell = row[i];
                        if (!String.IsNullOrEmpty(cell.Name))
                            this._owner._cells.Add(cell.Name, cell);
                    }
                }
            }

            /// <summary>
            /// Adds an array of ActiveRow objects to the collection.
            /// </summary>
            /// <param name="values">An array of ActiveRow objects to add to the collection.</param>
            public void AddRange(ActiveRow[] items)
            {
                base.AddRange(items);

                foreach (ActiveRow row in items)
                {
                    for(int i=1; i< row.SubItems.Count; i++ )
                    {
                        ActiveRow.ActiveCell cell = row[i];
                        if(!String.IsNullOrEmpty(cell.Name))
                            this._owner._cells.Add(cell.Name, cell);
                    }
                }
            }

            /// <summary>
            /// Determines whether the specified item is located in the collection
            /// </summary>
            /// <param name="subItem">An ActiveRow representing the item to locate in the collection</param>
            /// <returns>true if the item is contained in the collection; otherwise, false</returns>
            public bool Contains(ActiveRow item)
            {
                return base.Contains(item);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key">The item name to search for</param>
            /// <param name="searchAllSubItems">true to search subitems; otherwise, false</param>
            /// <returns>An array of type ListViewItem</returns>
            public new ActiveRow[] Find(string key, bool searchAllSubItems)
            {
                return base.Find(key, searchAllSubItems) as ActiveRow[];
            }

            /// <summary>
            /// Inserts an existing ActiveRow into the collection at the specified index
            /// </summary>
            /// <param name="index">The zero-based index location where the item is inserted</param>
            /// <param name="item">The ActiveRow that represents the item to insert</param>
            /// <returns></returns>
            public ActiveRow Insert(int index, ActiveRow item)
            {
                return base.Insert(index, item) as ActiveRow;
            }

            /// <summary>
            /// Creates a new item and inserts it into the collection at the specified index
            /// </summary>
            /// <param name="index">The zero-based index location where the item is inserted</param>
            /// <param name="text">The text to display for the item</param>
            /// <returns>The ActiveRow that was inserted into the collection</returns>
            public new ActiveRow Insert(int index, string text)
            {
                return base.Insert(index, text) as ActiveRow;
            }

            /// <summary>
            /// Creates a new item with the specified image index and inserts it into the collection at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index location where the item is inserted</param>
            /// <param name="text">The text to display for the item</param>
            /// <param name="imageIndex">The index of the image to display for the item</param>
            /// <returns>The ActiveRow that was inserted into the collection</returns>
            public new ActiveRow Insert(int index, string text, int imageIndex)
            {
                return base.Insert(index, text, imageIndex) as ActiveRow;
            }

            /// <summary>
            /// Creates a new item with the specified text and image and inserts it in the collection at the specified index
            /// </summary>
            /// <param name="index">The zero-based index location where the item is inserted</param>
            /// <param name="text">The text of the ListViewItem</param>
            /// <param name="imageKey">The key of the image to display for the item</param>
            /// <returns>The ActiveRow added to the collection</returns>
            public new ActiveRow Insert(int index, string text, string imageKey)
            {
                return base.Insert(index, text, imageKey) as ActiveRow;
            }

            /// <summary>
            /// Creates a new item with the specified key, text, and image, and inserts it in the collection at the specified index
            /// </summary>
            /// <param name="index">The zero-based index location where the item is inserted</param>
            /// <param name="key">The Name of the item</param>
            /// <param name="text">The text of the item</param>
            /// <param name="imageIndex">The index of the image to display for the item</param>
            /// <returns>The ActiveRow added to the collection</returns>
            public new ActiveRow Insert(int index, string key, string text, int imageIndex)
            {
                return base.Insert(index, key, text, imageIndex) as ActiveRow;
            }

            /// <summary>
            /// Creates a new item with the specified key, text, and image, and adds it to the collection at the specified index
            /// </summary>
            /// <param name="index">The zero-based index location where the item is inserted</param>
            /// <param name="key">The Name of the item</param>
            /// <param name="text">The text of the item</param>
            /// <param name="imageKey">The key of the image to display for the item</param>
            /// <returns>The ListViewItem added to the collection</returns>
            public new ActiveRow Insert(int index, string key, string text, string imageKey)
            {
                return base.Insert(index, key, text, imageKey) as ActiveRow;
            }

            #endregion

            #region --- Public Properties --------------------------

            /// <summary>
            /// Gets or sets the item at the specified index within the collection.
            /// </summary>
            /// <param name="index">The index of the item in the collection to get or set</param>
            public new ActiveRow this[int index]
            {
                get { return base[index] as ActiveRow; }
                set { base[index] = value; }
            }

            /// <summary>
            /// Retrieves the item with the specified key.
            /// </summary>
            /// <param name="key">The name of the item to retrieve.</param>
            public new ActiveRow this[string key]
            {
                get { return base[key] as ActiveRow; }
            }

            #endregion
        }
        #endregion

        #region --- Nested Class ActiveColumnHeaderCollection ----------

        /// <summary>
        /// Represents the collection of column headers in an ActiveGrid control.
        /// </summary>
        public class ActiveColumnHeaderCollection : ListView.ColumnHeaderCollection
        {
            #region --- Class Data ---------------------------------

            private ActiveGrid _owner;

            #endregion

            #region --- Public Constructors ------------------------

            /// <summary>
            /// Initializes a new instance of the ActiveGrid.ActiveColumnHeaderCollection class
            /// </summary>
            /// <param name="owner">An ActiveGrid representing the grid that owns the Control collection</param>
            public ActiveColumnHeaderCollection(ActiveGrid owner)
                : base(owner)
            {
                if (owner == null)
                    throw new NullReferenceException("ActiveColumnHeaderCollection: Owner must not be a null value");

                this._owner = owner;
            }

            #endregion

            #region --- Public Methods -----------------------------

            /// <summary>
            /// Adds an existing ActiveColumnHeader to the collection
            /// </summary>
            /// <param name="value">The ActiveColumnHeader to add to the collection</param>
            /// <returns>The zero-based index into the collection where the item was added</returns>
            public int Add(ActiveColumnHeader value)
            {
                return base.Add(value);
            }

            /// <summary>
            /// Creates and adds a column with the specified text to the collection
            /// </summary>
            /// <param name="text">The text to display in the column header</param>
            /// <returns>The ActiveColumnHeader with the specified text that was added to the ActiveColumnHeaderCollection.</returns>
            public new ActiveColumnHeader Add(string text)
            {
                return base.Add(text) as ActiveColumnHeader;
            }

            /// <summary>
            /// Creates and adds a column with the specified text and width to the collection
            /// </summary>
            /// <param name="text">The text of the ActiveColumnHeader to add to the collection</param>
            /// <param name="width">The width of the ActiveColumnHeader to add to the collection</param>
            /// <returns>The ActiveColumnHeader with the specified text and width that was added to the ActiveColumnHeaderCollection. </returns>
            public new ActiveColumnHeader Add(string text, int width)
            {
                return base.Add(text, width) as ActiveColumnHeader;
            }

            /// <summary>
            /// Creates and adds a column with the specified text and key to the collection.
            /// </summary>
            /// <param name="key">The key of the ActiveColumnHeader to add to the collection</param>
            /// <param name="text">The text of the ActiveColumnHeader to add to the collection</param>
            /// <returns>The ActiveColumnHeader with the specified key and text that was added to the ActiveColumnHeaderCollection</returns>
            public new ActiveColumnHeader Add(string key, string text)
            {
                return base.Add(key, text) as ActiveColumnHeader;
            }

            /// <summary>
            /// Adds a column header to the collection with specified text, width, and alignment settings
            /// </summary>
            /// <param name="text">The text to display in the column header</param>
            /// <param name="width">The initial width of the column header</param>
            /// <param name="textAlign">One of the HorizontalAlignment values</param>
            /// <returns>The ColumnHeader that was created and added to the collection</returns>
            public new ActiveColumnHeader Add(string text, int width, HorizontalAlignment textAlign)
            {
                return base.Add(text, width, textAlign) as ActiveColumnHeader;
            }

            /// <summary>
            /// Creates and adds a column with the specified text, key, and width to the collection
            /// </summary>
            /// <param name="key">The key of the column header</param>
            /// <param name="text">The text to display in the column header</param>
            /// <param name="width">The initial width of the ActiveColumnHeader</param>
            /// <returns>The ActiveColumnHeader with the given text, key, and width that was added to the collection</returns>
            public new ActiveColumnHeader Add(string key, string text, int width)
            {
                return base.Add(key, text, width) as ActiveColumnHeader;
            }

            /// <summary>
            /// Creates and adds a column with the specified key, aligned text, width, and image index to the collection
            /// </summary>
            /// <param name="key">The key of the column header</param>
            /// <param name="text">The text to display in the column header</param>
            /// <param name="width">The initial width of the column header</param>
            /// <param name="textAlign">One of the HorizontalAlignment values</param>
            /// <param name="imageIndex">The index value of the image to display in the column</param>
            /// <returns>The ActiveColumnHeader with the specified key, aligned text, width, and image index that has been added to the collection</returns>
            public new ActiveColumnHeader Add(string key, string text, int width, HorizontalAlignment textAlign, int imageIndex)
            {
                return base.Add(key, text, width, textAlign, imageIndex) as ActiveColumnHeader;
            }

            /// <summary>
            /// Creates and adds a column with the specified key, aligned text, width, and image key to the collection
            /// </summary>
            /// <param name="key">The key of the column header</param>
            /// <param name="text">The text to display in the column header</param>
            /// <param name="width">The initial width of the column header</param>
            /// <param name="textAlign">One of the HorizontalAlignment values</param>
            /// <param name="imageKey">The key value of the image to display in the column header</param>
            /// <returns>The ActiveColumnHeader with the specified key, aligned text, width, and image key that has been added to the collection</returns>
            public new ActiveColumnHeader Add(string key, string text, int width, HorizontalAlignment textAlign, string imageKey)
            {
                return base.Add(key, text, width, textAlign, imageKey) as ActiveColumnHeader;
            }

            /// <summary>
            /// Adds an array of column headers to the collection
            /// </summary>
            /// <param name="values">An array of ActiveColumnHeader objects to add to the collection</param>
            public void AddRange(ActiveColumnHeader[] values)
            {
                base.AddRange(values);
            }

            /// <summary>
            /// Inserts an existing column header into the collection at the specified index
            /// </summary>
            /// <param name="index">The zero-based index location where the column header is inserted</param>
            /// <param name="value">The ActiveColumnHeader to insert into the collection</param>
            public void Insert(int index, ActiveColumnHeader value)
            {
                base.Insert(index, value);
            }

            /// <summary>
            /// Determines whether the specified column header is located in the collection.
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool Contains(ActiveColumnHeader value)
            {
                return base.Contains(value);
            }


            #endregion

            #region --- Public Properties --------------------------

            /// <summary>
            /// Gets the column header at the specified index within the collection.
            /// </summary>
            /// <param name="index">The index of the column header to retrieve from the collection.</param>
            public new ActiveColumnHeader this[int index]
            {
                get { return base[index] as ActiveColumnHeader; }
            }

            /// <summary>
            /// Gets the column header with the specified key from the collection.
            /// </summary>
            /// <param name="index">The name of the column header to retrieve from the collection</param>
            public new ActiveColumnHeader this[string key]
            {
                get { return base[key] as ActiveColumnHeader;  }
            }

            /// <summary>
            /// Gets the ActiveGrid to which this column belongs.
            /// </summary>
            public ActiveGrid Grid
            {
                get { return this._owner; }
            }

            #endregion
        }

        #endregion

        #region --- Nested Class Paintbox ------------------------------

        /// <summary>
        /// The ActiveGrid can be quite heavy on system resources, especially
        /// in its use of brushes. In an attempt to reduce the number of resources
        /// being created and destroyed, and to reduce the load on the Garbage Collector,
        /// this class is used as a central repository for all of the brushes and colours
        /// required by the control.
        /// </summary>
        public static class Paintbox
        {
            #region --- Custom Brushes ----------------------------

            private static object syncLock = new Object();
            private static Dictionary<System.Drawing.Color, System.Drawing.Brush> _brushes = new Dictionary<System.Drawing.Color, System.Drawing.Brush>(10);

            public static System.Drawing.Brush Brush(System.Drawing.Color color)
            {
                lock (syncLock)
                {
                    System.Drawing.Brush brush = null;

                    if (!_brushes.TryGetValue(color, out brush))
                    {
                        brush = new System.Drawing.SolidBrush(color);
                        _brushes.Add(color, brush);
                    }

                    return brush;
                }
            }

            #endregion

            #region --- Gradients ---------------------------------

            private static System.Drawing.Drawing2D.LinearGradientMode _flashLinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            public static System.Drawing.Drawing2D.LinearGradientMode FlashLinearGradientMode
            {
                get { return _flashLinearGradientMode; }
                set { _flashLinearGradientMode = value; }
            }

            private static System.Drawing.Drawing2D.LinearGradientMode _rowLinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            public static System.Drawing.Drawing2D.LinearGradientMode RowLinearGradientMode
            {
                get { return _rowLinearGradientMode; }
                set { _rowLinearGradientMode = value; }
            }

            #endregion

            #region --- Colours -----------------------------------

            private static System.Drawing.Color _clrNormalBackground = System.Drawing.Color.White;
            public static System.Drawing.Color NormalBackgroundColor
            {
                get { return _clrNormalBackground; }
                set { _clrNormalBackground = value; }
            }

            private static System.Drawing.Color _clrAlternateBackground = System.Drawing.Color.Gainsboro;
            public static System.Drawing.Color AlternateBackgroundColor
            {
                get { return _clrAlternateBackground; }
                set { _clrAlternateBackground = value; }
            }



            private static System.Drawing.Color _clrFlashBackground = System.Drawing.Color.Yellow;
            public static System.Drawing.Color FlashBackgroundColor
            {
                get { return _clrFlashBackground; }
                set { _clrFlashBackground = value; }
            }

            private static System.Drawing.Color _clrFlashForeground = System.Drawing.Color.Black;
            public static System.Drawing.Color FlashForeColor
            {
                get { return _clrFlashForeground; }
                set { _clrFlashForeground = value; }
            }

            private static System.Drawing.Color _clrFlashGradientStart = System.Drawing.Color.White;
            public static System.Drawing.Color FlashGradientStartColor
            {
                get { return _clrFlashGradientStart; }
                set { _clrFlashGradientStart = value; }
            }

            private static System.Drawing.Color _clrFlashGradientEnd = System.Drawing.Color.White;
            public static System.Drawing.Color FlashGradientEndColor
            {
                get { return _clrFlashGradientEnd; }
                set { _clrFlashGradientEnd = value; }
            }

            private static System.Drawing.Color _clrGradientStart = System.Drawing.Color.White;
            public static System.Drawing.Color GradientStartColor
            {
                get { return _clrGradientStart; }
                set { _clrGradientStart = value; }
            }

            private static System.Drawing.Color _clrGradientEnd = System.Drawing.Color.White;
            public static System.Drawing.Color GradientEndColor
            {
                get { return _clrGradientEnd; }
                set { _clrGradientEnd = value; }
            }

            private static System.Drawing.Color _clrNegativeForeground = System.Drawing.Color.Red;
            public static System.Drawing.Color NegativeValueColor
            {
                get { return _clrNegativeForeground; }
                set { _clrNegativeForeground = value; }
            }

            private static System.Drawing.Color _clrPreTextForeground = System.Drawing.Color.Black;
            public static System.Drawing.Color PreTextColor
            {
                get { return _clrPreTextForeground; }
                set { _clrPreTextForeground = value; }
            }


            private static System.Drawing.Color _clrPostTextForeground = System.Drawing.Color.Black;
            public static System.Drawing.Color PostTextColor
            {
                get { return _clrPostTextForeground; }
                set { _clrPostTextForeground = value; }
            }


            private static System.Drawing.Color _clrFlashPreTextForeground = System.Drawing.Color.Black;
            public static System.Drawing.Color FlashPreTextColor
            {
                get { return _clrFlashPreTextForeground; }
                set { _clrFlashPreTextForeground = value; }
            }


            private static System.Drawing.Color _clrFlashPostTextForeground = System.Drawing.Color.Black;
            public static System.Drawing.Color FlashPostTextColor
            {
                get { return _clrFlashPostTextForeground; }
                set { _clrFlashPostTextForeground = value; }
            }

            private static System.Drawing.Color _clrNormalTextForeground = System.Drawing.Color.Black;
            public static System.Drawing.Color NormalTextColor
            {
                get { return _clrNormalTextForeground; }
                set { _clrNormalTextForeground = value; }
            }

            #endregion

            #region --- Brushes -----------------------------------

            private static System.Drawing.SolidBrush _brushNormalBackground;
            public static System.Drawing.Brush NormalBackgroundBrush
            {
                get
                {
                    if (_brushNormalBackground == null)
                        _brushNormalBackground = new System.Drawing.SolidBrush(_clrNormalBackground);
                    else if (_brushNormalBackground.Color != _clrNormalBackground)
                    {
                        _brushNormalBackground.Color = _clrNormalBackground;
                    }

                    return _brushNormalBackground;
                }
            }

            private static System.Drawing.SolidBrush _brushAlternateBackground;
            public static System.Drawing.Brush AlternateBackgroundBrush
            {
                get
                {
                    if (_brushAlternateBackground == null)
                        _brushAlternateBackground = new System.Drawing.SolidBrush(_clrAlternateBackground);
                    else if (_brushAlternateBackground.Color != _clrAlternateBackground)
                    {
                        _brushAlternateBackground.Color = _clrAlternateBackground;
                    }

                    return _brushAlternateBackground;
                }
            }

            private static System.Drawing.SolidBrush _brushFlashBackground;
            public static System.Drawing.Brush FlashBackgroundBrush
            {
                get
                {
                    if (_brushFlashBackground == null)
                        _brushFlashBackground = new System.Drawing.SolidBrush(_clrFlashBackground);
                    else if (_brushFlashBackground.Color != _clrFlashBackground)
                    {
                        _brushFlashBackground.Color = _clrFlashBackground;
                    }

                    return _brushFlashBackground;
                }
            }

            private static System.Drawing.SolidBrush _brushFlashText;
            public static System.Drawing.Brush FlashTextBrush
            {
                get
                {
                    if (_brushFlashText == null)
                        _brushFlashText = new System.Drawing.SolidBrush(_clrFlashForeground);
                    else if (_brushFlashText.Color != _clrFlashForeground)
                    {
                        _brushFlashText.Color = _clrFlashForeground;
                    }

                    return _brushFlashText;
                }
            }


            private static System.Drawing.SolidBrush _brushNormalText;
            public static System.Drawing.Brush NormalTextBrush
            {
                get
                {
                    if (_brushNormalText == null)
                        _brushNormalText = new System.Drawing.SolidBrush(_clrNormalTextForeground);
                    else if (_brushNormalText.Color != _clrNormalTextForeground)
                    {
                        _brushNormalText.Color = _clrNormalTextForeground;
                    }

                    return _brushNormalText;
                }
            }


            private static System.Drawing.SolidBrush _brushNegativeText;
            public static System.Drawing.Brush NegativeValueBrush
            {
                get
                {
                    if (_brushNegativeText == null)
                        _brushNegativeText = new System.Drawing.SolidBrush(_clrNegativeForeground);
                    else if (_brushNegativeText.Color != _clrNegativeForeground)
                    {
                        _brushNegativeText.Color = _clrNegativeForeground;
                    }

                    return _brushNegativeText;
                }
            }


            private static System.Drawing.SolidBrush _brushPreText;
            public static System.Drawing.Brush PreTextBrush
            {
                get
                {
                    if (_brushPreText == null)
                        _brushPreText = new System.Drawing.SolidBrush(_clrPreTextForeground);
                    else if (_brushPreText.Color != _clrPreTextForeground)
                    {
                        _brushPreText.Color = _clrPreTextForeground;
                    }

                    return _brushPreText;
                }
            }


            private static System.Drawing.SolidBrush _brushPostText;
            public static System.Drawing.Brush PostTextBrush
            {
                get
                {
                    if (_brushPostText == null)
                        _brushPostText = new System.Drawing.SolidBrush(_clrPostTextForeground);
                    else if (_brushPostText.Color != _clrPostTextForeground)
                    {
                        _brushPostText.Color = _clrPostTextForeground;
                    }

                    return _brushPostText;
                }
            }

            private static System.Drawing.SolidBrush _brushFlashPreText;
            public static System.Drawing.Brush FlashPreTextBrush
            {
                get
                {
                    if (_brushFlashPreText == null)
                        _brushFlashPreText = new System.Drawing.SolidBrush(_clrFlashPreTextForeground);
                    else if (_brushFlashPreText.Color != _clrFlashPreTextForeground)
                    {
                        _brushFlashPreText.Color = _clrFlashPreTextForeground;
                    }

                    return _brushFlashPreText;
                }
            }


            private static System.Drawing.SolidBrush _brushFlashPostText;
            public static System.Drawing.Brush FlashPostTextBrush
            {
                get
                {
                    if (_brushFlashPostText == null)
                        _brushFlashPostText = new System.Drawing.SolidBrush(_clrFlashPostTextForeground);
                    else if (_brushFlashPostText.Color != _clrFlashPostTextForeground)
                    {
                        _brushFlashPostText.Color = _clrFlashPostTextForeground;
                    }

                    return _brushFlashPostText;
                }
            }


            #endregion
        }

        #endregion
    }

    #endregion

    #region --- Class ActiveRow --------------------------------------

    /// <summary>
    /// Class representing a row within the ActiveGrid
    /// </summary>
    public class ActiveRow : ListViewItem
    {
        #region --- Enumerations ----------------------------------

        public enum CellStyle
        {
            Plain = 0,
            Gradient
        }

        public enum CellState
        {
            Normal = 0,
            Highlighted,
            Flashed
        }

        #endregion

        #region --- Class Data ------------------------------------

        /// <summary>
        /// The collection of ActiveCells belonging to this row.
        /// </summary>
        private ActiveCellCollection _cells;
        /// <summary>
        /// Synchronization object
        /// </summary>
        private object syncRoot = new object();
        /// <summary>
        /// Row header LinkLabel
        /// </summary>
        private LinkLabelCell _linkLabel;

        #endregion

        #region --- Overloaded Constructors -----------------------

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with default values. 
        /// </summary>
        public ActiveRow()
            : base()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified item text. 
        /// </summary>
        /// <param name="text">The text to display for the item</param>
        public ActiveRow(string text)
            : base(text)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with an array of strings representing subitems.
        /// </summary>
        /// <param name="items">An array of strings that represent the subitems of the new item.</param>
        public ActiveRow(string[] items)
            : base(items)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with an array of ActiveCell objects and the image 
        /// index position of the item's icon. 
        /// </summary>
        /// <param name="subItems">An array of type ActiveCell that represents the subitems of the item. </param>
        /// <param name="imageIndex">The zero-based index of the image within the ImageList associated with the ListView that contains the item.</param>
        public ActiveRow(ActiveCell[] subItems, int imageIndex)
            : base(subItems, imageIndex)
        {
            Initialize();
        }


        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified item text and the image index position of the item's icon. 
        /// </summary>
        /// <param name="text">The text to display for the item.</param>
        /// <param name="imageIndex">The zero-based index of the image within the ImageList associated with the ListView that contains the item.</param>
        public ActiveRow(string text, int imageIndex)
            : base(text, imageIndex)
        {
            Initialize();
        }


        /// <summary>
        /// Initializes a new instance of the ActiveRow class with an array of strings representing subitems and 
        /// the image index position of the item's icon. 
        /// </summary>
        /// <param name="items">An array of strings that represent the subitems of the new item.</param>
        /// <param name="imageIndex">The zero-based index of the image within the ImageList associated with the ListView that contains the item.</param>
        public ActiveRow(string[] items, int imageIndex)
            : base(items, imageIndex)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the image index position of the 
        /// item's icon; the foreground color, background color, and font of the item; and an array 
        /// of strings representing subitems.
        /// </summary>
        /// <param name="items">An array of strings that represent the subitems of the new item.</param>
        /// <param name="imageIndex">The zero-based index of the image within the ImageList associated with the ListView that contains the item.</param>
        /// <param name="foreColor">A Color that represents the foreground color of the item.</param>
        /// <param name="backColor">A Color that represents the background color of the item.</param>
        /// <param name="font">A Font that represents the font to display the item's text in.</param>
        public ActiveRow(string[] items, int imageIndex, Color foreColor, Color backColor, Font font)
            : base(items, imageIndex, foreColor, backColor, font)
        {
            Initialize();
        }

        #endregion

        #region --- New Constructors ------------------------------

        /// <summary>
        /// Initializes a new instance of the ActiveRow class and assigns it to the specified group.
        /// </summary>
        /// <param name="group">The ListViewGroup to assign the item to.</param>
        public ActiveRow(ListViewGroup group)
            : base(group)
        {
            Initialize();
        }


        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified subitems and image. 
        /// </summary>
        /// <param name="subItems">An array of ListViewItem.ListViewSubItem objects.</param>
        /// <param name="imageKey">The name of the image within the ImageList of the owning ListView to display in the ListViewItem.</param>
        public ActiveRow(ActiveCell[] subItems, string imageKey)
            : base(subItems, imageKey)
        {
            Initialize();
        }


        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified serialization information and streaming context.
        /// </summary>
        /// <param name="info">A SerializationInfo containing information about the ListViewItem to be initialized.</param>
        /// <param name="context">A StreamingContext that indicates the source destination and context information of a serialized stream.</param>
        public ActiveRow(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified key, item text and the image index position of the item's icon.
        /// </summary>
        /// <param name="key">The name of the item used as a search key</param>
        /// <param name="text">The text to display for the item</param>
        /// <param name="imageIndex">The zero-based index of the image within the ImageList associated with the ListView that contains the item.</param>
        public ActiveRow(string key, string text, int imageIndex)
            : base(text, imageIndex)
        {
            base.Name = key;
            Initialize();

        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified key, item text and the image.
        /// </summary>
        /// <param name="key">The name of the item used as a search key</param>
        /// <param name="text">The text to display for the item</param>
        /// <param name="imageKey">The name of the image within the ImageList of the owning Grid to display in the ListViewItem.</param>
        public ActiveRow(string key, string text, string imageKey)
            : base(text, imageKey)
        {
            base.Name = key;
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified item text and assigns it to the specified group
        /// </summary>
        /// <param name="text">The text to display for the item.</param>
        /// <param name="group">The ListViewGroup to assign the item to</param>
        public ActiveRow(string text, ListViewGroup group)
            : base(text, group)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified text and image. 
        /// </summary>
        /// <param name="text">The text to display for the item.</param>
        /// <param name="imageKey">The name of the image within the ImageList of the owning ListView to display in the ListViewItem.</param>
        public ActiveRow(string text, string imageKey)
            : base(text, imageKey)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with an array of strings representing subitems, and assigns the item to the specified group. 
        /// </summary>
        /// <param name="items">An array of strings that represent the subitems of the new item.</param>
        /// <param name="group">The ListViewGroup to assign the item to.</param>
        public ActiveRow(string[] items, ListViewGroup group)
            : base(items, group)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified item and subitem text and image. 
        /// </summary>
        /// <param name="items">An array containing the text of the subitems of the ListViewItem.</param>
        /// <param name="imageKey">The name of the image within the ImageList of the owning ListView to display in the ListViewItem.</param>
        public ActiveRow(string[] items, string imageKey)
            : base(items, imageKey)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the image index position of the item's icon and an array of ListViewItem.ListViewSubItem objects, and assigns the item to the specified group.
        /// </summary>
        /// <param name="subItems">An array of type ListViewItem.ListViewSubItem that represents the subitems of the item.</param>
        /// <param name="imageIndex">The zero-based index of the image within the ImageList associated with the ListView that contains the item</param>
        /// <param name="group">The ListViewGroup to assign the item to.</param>
        public ActiveRow(ActiveCell[] subItems, int imageIndex, ListViewGroup group)
            : base(subItems, imageIndex, group)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified subitems, image, and group.
        /// </summary>
        /// <param name="subItems">An array of ListViewItem.ListViewSubItem objects that represent the subitems of the ListViewItem.</param>
        /// <param name="imageKey">The name of the image within the ImageList of the owning ListView to display in the item.</param>
        /// <param name="group">The ListViewGroup to assign the item to.</param>
        public ActiveRow(ActiveCell[] subItems, string imageKey, ListViewGroup group)
            : base(subItems, imageKey, group)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified item text and the image index position of the item's icon, and assigns the item to the specified group. 
        /// </summary>
        /// <param name="text">The text to display for the item.</param>
        /// <param name="imageIndex">The zero-based index of the image within the ImageList associated with the ListView that contains the item. </param>
        /// <param name="group">The ListViewGroup to assign the item to.</param>
        public ActiveRow(string text, int imageIndex, ListViewGroup group)
            : base(text, imageIndex, group)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the specified text, image, and group.
        /// </summary>
        /// <param name="text">The text to display for the item.</param>
        /// <param name="imageKey">The name of the image within the ImageList of the owning ListView to display in the item.</param>
        /// <param name="group">The ListViewGroup to assign the item to.</param>
        public ActiveRow(string text, string imageKey, ListViewGroup group)
            : base(text, imageKey, group)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the image index position of the item's icon and an array of strings representing subitems, and assigns the item to the specified group.
        /// </summary>
        /// <param name="items">An array of strings that represent the subitems of the new item</param>
        /// <param name="imageIndex">The zero-based index of the image within the ImageList associated with the ListView that contains the item. </param>
        /// <param name="group">The ListViewGroup to assign the item to.</param>
        public ActiveRow(string[] items, int imageIndex, ListViewGroup group)
            : base(items, imageIndex, group)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with subitems containing the specified text, image, and group.
        /// </summary>
        /// <param name="items">An array of strings that represents the text for subitems of the ListViewItem.</param>
        /// <param name="imageKey">The name of the image within the ImageList of the owning ListView to display in the item.</param>
        /// <param name="group">The ListViewGroup to assign the item to.</param>
        public ActiveRow(string[] items, string imageKey, ListViewGroup group)
            : base(items, imageKey, group)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the subitems containing the specified text, image, colors, and font. 
        /// </summary>
        /// <param name="items">An array of strings that represent the text of the subitems for the ListViewItem.</param>
        /// <param name="imageKey">The name of the image within the ImageList of the owning ListView to display in the item</param>
        /// <param name="foreColor">A Color that represents the foreground color of the item</param>
        /// <param name="backColor">A Color that represents the background color of the item</param>
        /// <param name="font">A Font to apply to the item text</param>
        public ActiveRow(string[] items, string imageKey, Color foreColor, Color backColor, Font font)
            : base(items, imageKey, foreColor, backColor, font)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the image index position of the 
        /// item's icon; the foreground color, background color, and font of the item; and an array of 
        /// strings representing subitems. Assigns the item to the specified group. 
        /// </summary>
        /// <param name="items">An array of strings that represent the subitems of the new item</param>
        /// <param name="imageIndex">The zero-based index of the image within the ImageList associated with the ListView that contains the item</param>
        /// <param name="foreColor">A Color that represents the foreground color of the item</param>
        /// <param name="backColor">A Color that represents the background color of the item</param>
        /// <param name="font">A Font that represents the font to display the item's text in</param>
        /// <param name="group">The ListViewGroup to assign the item to</param>
        public ActiveRow(string[] items, int imageIndex, Color foreColor, Color backColor, Font font, ListViewGroup group)
            : base(items, imageIndex, foreColor, backColor, font, group)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the ActiveRow class with the subitems containing the specified 
        /// text, image, colors, font, and group. 
        /// </summary>
        /// <param name="items">An array of strings that represents the text of the subitems for the ListViewItem</param>
        /// <param name="imageKey">The name of the image within the ImageList of the owning ListView to display in the item</param>
        /// <param name="foreColor">A Color that represents the foreground color of the item</param>
        /// <param name="backColor">A Color that represents the background color of the item</param>
        /// <param name="font">A Font to apply to the item text</param>
        /// <param name="group">The ListViewGroup to assign the item to</param>
        public ActiveRow(string[] items, string imageKey, Color foreColor, Color backColor, Font font, ListViewGroup group)
            : base(items, imageKey, foreColor, backColor, font, group)
        {
            Initialize();
        }

        #endregion

        #region --- Public Methods --------------------------------

        /// <summary>
        /// Invalidates the whole area of the row and causes it to be redrawn
        /// </summary>
        public void Invalidate()
        {
            if (this.Grid != null)
                this.Grid.Invalidate(this.Bounds);
        }

        /// <summary>
        /// Invalidates a specific area of the row and causes it to be redrawn
        /// </summary>
        public void Invalidate(Rectangle rc)
        {
            if (this.Grid != null)
                this.Grid.Invalidate(rc);
        }

        /// <summary>
        /// Paints the row
        /// </summary>
        /// <param name="g"></param>
        /// <param name="highlighted"></param>
        public void Draw(DrawListViewItemEventArgs e)
        {
            // Is the row Highlighted?
            this.Selected = ((e.State & ListViewItemStates.Selected) != 0);

            Draw(e.Graphics, e.ItemIndex);
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            if (this._linkLabel != null)
            {
                this.Grid.Cursor = this._linkLabel.MouseOver(e.Location) ? Cursors.Hand : Cursors.Default;

                if (this._linkLabel.Changed)
                    Draw(this.Grid.CreateGraphics(), this.Index);
            }
        }

        public bool OnMouseClick(MouseEventArgs e)
        {
            return (this._linkLabel==null ? false : this._linkLabel.OnMouseClick(e));
        }


        public override string ToString()
        {
            return "LVRow{}";
        }

        #endregion

        #region --- Private Methods -------------------------------

        /// <summary>
        /// Gets a rectangle representing the Pre-Cell area of the row.
        /// </summary>
        /// <returns>Rectangle representing the Pre-Cell area of the row</returns>
        private Rectangle PreCellBounds()
        {
            Rectangle labelBounds = this.GetBounds(ItemBoundsPortion.Label);
            return new Rectangle(0, this.Bounds.Y, labelBounds.X + labelBounds.Width, this.Bounds.Height - (this.Grid.GridLines ? 1 : 0));
        }

        private Rectangle PostCellBounds()
        {
            // Calculate the total width of all columns in the row.
            int totalHeaderWidth = 0;
            foreach (ActiveColumnHeader column in this.Grid.Columns)
                totalHeaderWidth += column.Width;

            // Create a rectangle representing the post-cell area of the row.
            return new Rectangle(totalHeaderWidth + this.Bounds.X + (this.Grid.GridLines ? 1 : 0), this.Bounds.Y, this.Grid.Width - totalHeaderWidth, this.Bounds.Height - (this.Grid.GridLines ? 1 : 0));
        }

        /// <summary>
        /// Initializes the class data.
        /// </summary>
        private void Initialize()
        {
            this._cells = new ActiveCellCollection(this);
            this._linkLabel = new LinkLabelCell(this.Bounds);

            this._linkLabel.OnLeftMouseClick += new LinkLabelCell.OnLeftMouseClickHandler(_linkLabel_OnLeftMouseClick);
            this._linkLabel.OnRightMouseClick += new LinkLabelCell.OnRightMouseClickHandler(_linkLabel_OnRightMouseClick);
        }

        private void _linkLabel_OnRightMouseClick(object sender, EventArgs e)
        {
            if(this.Grid != null)
              this.Grid.RowHeaderRightClick(this);
        }

        private void _linkLabel_OnLeftMouseClick(object sender, EventArgs e)
        {
            if(this.Grid != null)
              this.Grid.RowHeaderLeftClick(this);
        }


        private void Draw(Graphics g, int ItemIndex)
        {
            if (this.Grid != null)
            {
                // This bit needs some explanation....
                // Each row can be thought of as divided into three sections:
                // Section 1: The bit before the cells [PreCell]
                // Section 2: The cells
                // Section 3: The bit after the cells [PostCell]
                // In order to prevent any flickering of the cell content due to unnecessarly
                // invalidating the cells we need to handle each of the sections seperately.

                // Create a rectangle representing the pre-cell area of the row.
                Rectangle rcPre = PreCellBounds();

                // Create a rectangle representing the post-cell area of the row.
                Rectangle rcPost = PostCellBounds();

                if (this.Grid.UseGradient && AlternateRow )
                {
                    DrawAlternatingGradient(g, rcPre, rcPost);
                }
                else
                {
                    DrawPlain(g, ItemIndex, rcPre, rcPost);
                }


                // Invalidate the post-cell region
                // TODO: XP-Bug
                // Warning! This presents a problem in Windows-XP as it causes the mouse cursor
                // to flicker when it is positioned over this area. It also sends the processor
                // into overdrive. 
                //
                // if (this.Selected)
                //    this.Grid.Invalidate(rcPost);


                // Draw the row foreground
                DrawForeground(g);
            }
        }

        private void DrawPlain(Graphics g, int ItemIndex, Rectangle rcPre, Rectangle rcPost)
        {
            g.FillRectangle((this.Selected ? SystemBrushes.Highlight : (AlternateRow ? ActiveGrid.Paintbox.AlternateBackgroundBrush : ActiveGrid.Paintbox.NormalBackgroundBrush)), rcPre);

            // TODO: XP-Bug
            // g.FillRectangle((this.Selected ? SystemBrushes.Highlight : (AlternateRow ? ActiveGrid.Paintbox.AlternateBackgroundBrush : ActiveGrid.Paintbox.NormalBackgroundBrush)), rcPost);

        }

        private void DrawAlternatingGradient(Graphics g, Rectangle rcPre, Rectangle rcPost)
        {
            using (LinearGradientBrush bg = new LinearGradientBrush(rcPre, this.Grid.AlternatingGradientStartColor, this.Grid.AlternatingGradientEndColor, LinearGradientMode.Vertical))
            {
                g.FillRectangle((this.Selected ? SystemBrushes.Highlight : bg), rcPre);
            }

            // TODO: XP-Bug
            // using (LinearGradientBrush bg = new LinearGradientBrush(rcPost, this.Grid.AlternatingGradientStartColor, this.Grid.AlternatingGradientEndColor, LinearGradientMode.Vertical))
            // {
            //    g.FillRectangle((this.Selected ? SystemBrushes.Highlight : bg), rcPost);
            // }
 
        }

        /// <summary>
        /// Draws the row header text
        /// </summary>
        /// <param name="g"></param>
        /// <param name="highlighted"></param>
        private void DrawForeground(Graphics g)
        {
            using (StringFormat sf = new StringFormat())
            {
                // Fetch the column header of the row label.
                ActiveColumnHeader header = this.Grid.Columns[0];
                if (header != null)
                {
                    // Set the user-defined string format.
                    sf.Alignment = header.CellHorizontalAlignment;
                    sf.LineAlignment = header.CellVerticalAlignment;
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                }

                // Draw the contents of the row label.
                if (this.Selected)
                {
                    g.DrawString(this.Text, this.Font, SystemBrushes.HighlightText, this.GetBounds(ItemBoundsPortion.Label), sf);
                }
                else
                {
                    // Are we using LinkLabel-style headers for the rows?
                    if (this.Grid.UseRowHeaderButtons)
                    {
                        this._linkLabel.Draw(g, this.Text, this.Font, this.GetBounds(ItemBoundsPortion.Label), sf, this.Selected);
                    }
                    else
                    {
                        g.DrawString(this.Text, this.Font, ActiveGrid.Paintbox.Brush(this.ForeColor), this.GetBounds(ItemBoundsPortion.Label), sf);
                    }
                }
            }
        }

        #endregion

        #region --- Properties ------------------------------------

        /// <summary>
        /// Gets the background color of the item's text
        /// </summary>
        public new Color BackColor
        {
            get { return AlternateRow ? ActiveGrid.Paintbox.AlternateBackgroundColor : ActiveGrid.Paintbox.NormalBackgroundColor; }
        }

        public new Rectangle Bounds
        {
            get { return new Rectangle(base.Bounds.X, base.Bounds.Y, 230, base.Bounds.Height);  }
        }


        /// <summary>
        /// Gets a flag to indicate if the row is an alternate row. 
        /// </summary>
        public Boolean AlternateRow
        {
            get
            {
                bool alternate = false;

                if (this.Grid != null && this.Grid.UseAlternateRowColors)
                    alternate = (this.Index % 2 == 1);

                return alternate;
            }
        }

        /// <summary>
        /// Gets a collection containing all cells of the row
        /// </summary>
        [Category("Data"),
        DefaultValue(null),
        Description("The ActiveCell items contained in the ActiveRow"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public new ActiveCellCollection SubItems
        {
            get { return this._cells; }
        }

        /// <summary>
        /// Gets the ActiveGrid control that contains the row
        /// </summary>
        [Browsable(false)]
        public ActiveGrid Grid
        {
            get { return base.ListView as ActiveGrid; }
        }

        /// <summary>
        /// Gets the zero-based index of the cell within the row
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ActiveCell this[int index]
        {
            get
            {
                if (index >= 0 && index < base.SubItems.Count)
                    return base.SubItems[index] as ActiveCell;
                else
                    throw (new IndexOutOfRangeException(String.Format("The cell index [{0}] is out of range", index)));
            }
        }

        #endregion

        #region --- Nested Class ActiveCellCollection -------------

        /// <summary>
        /// Represents a collection of ActiveRow.ActiveCell objects stored in an ActiveRow.
        /// </summary>
        public class ActiveCellCollection : ListViewItem.ListViewSubItemCollection
        {
            #region --- Public Constructors ------------------------

            /// <summary>
            /// Initializes a new instance of the ActiveRow.ActiveCellCollection class
            /// </summary>
            /// <param name="owner">The ActiveRow that owns the collection</param>
            public ActiveCellCollection(ActiveRow owner)
                : base(owner)
            {
            }

            #endregion

            #region --- Public Methods -----------------------------

            /// <summary>
            /// Adds an existing ActiveRow.ActiveCell to the collection
            /// </summary>
            /// <param name="item">The ActiveRow.ActiveCell to add to the collection</param>
            /// <returns>The ActiveRow.ActiveCell that was added to the collection</returns>
            public ActiveCell Add(ActiveCell item)
            {
                return base.Add(item) as ActiveCell;
            }


            /// <summary>
            /// Adds a cell to the collection with specified text. 
            /// </summary>
            /// <param name="text">The text to display for the subitem</param>
            /// <returns>The ActiveRow.ActiveCell that was added to the collection</returns>
            public new ActiveCell Add(string text)
            {
                return base.Add(text) as ActiveCell;
            }

            /// <summary>
            /// Adds a cell to the collection with specified text, foreground color, background color, and font settings
            /// </summary>
            /// <param name="text">The text to display for the subitem</param>
            /// <param name="foreColor">A Color that represents the foreground color of the subitem</param>
            /// <param name="backColor">A Color that represents the background color of the subitem.</param>
            /// <param name="font">A Font that represents the typeface to display the subitem's text in</param>
            /// <returns>The ActiveRow.ActiveCell that was added to the collection</returns>
            public new ActiveCell Add(string text, Color foreColor, Color backColor, Font font)
            {
                return base.Add(text, foreColor, backColor, font) as ActiveCell;
            }

            /// <summary>
            /// Adds an array of ActiveRow.ActiveCell objects to the collection
            /// </summary>
            /// <param name="items">An array of ActiveRow.ActiveCell objects to add to the collection</param>
            public void AddRange(ActiveCell[] items)
            {
                base.AddRange(items);
            }

            /// <summary>
            /// Determines whether the specified cell is located in the collection
            /// </summary>
            /// <param name="subItem">An ActiveRow.ActiveCell representing the subitem to locate in the collection</param>
            /// <returns>true if the cell is contained in the collection; otherwise, false</returns>
            public bool Contains(ActiveCell subItem)
            {
                return base.Contains(subItem);
            }

            #endregion

            #region --- Public Properties --------------------------

            /// <summary>
            /// Gets or sets the cell at the specified index within the collection.
            /// </summary>
            /// <param name="index">The index of the cell in the collection to get or set</param>
            public new ActiveCell this[int index]
            {
                get { return base[index] as ActiveCell; }
                set { base[index] = value; }
            }

            /// <summary>
            /// Retrieves the cell with the specified key.
            /// </summary>
            /// <param name="index">The name of the cell to retrieve.</param>
            public new ActiveCell this[string key]
            {
                get { return base[key] as ActiveCell; }
            }

            #endregion
        }

        #endregion

        #region --- Nested Class Active Cell ----------------------

        /// <summary>
        /// Class representing the default cell of an ActiveRow.
        /// </summary>
        public class ActiveCell : ListViewItem.ListViewSubItem
        {
            #region --- Class Data ------------------------------------

            private readonly String DEFAULT_FORMAT_SPECIFIER = "{0}";
            public static Int32 DEFAULT_FLASH_DURATION = 2000;
            /// <summary>
            /// The row to which the cell belongs.
            /// </summary>
            private ActiveRow _row = null;
            /// <summary>
            /// The column to which the cell belongs.
            /// </summary>
            private ActiveColumnHeader _column = null;
            /// <summary>
            /// Synchronization object
            /// </summary>
            private Object synchRoot = new object();
            /// <summary>
            /// The current style enumeration of the cell
            /// </summary>
            private CellStyle _style;
            /// <summary>
            /// The decimal value associated with the cell
            /// </summary>
            private Decimal _decValue = Decimal.Zero;
            /// <summary>
            /// Indicates whether the contents of the cell are displaying a decimal value
            /// </summary>
            private bool _useDecimal = false;
            /// <summary>
            /// Reference to the callback method for the background timer.
            /// </summary>
            private System.Threading.TimerCallback callback;
            /// <summary>
            /// Enumeration for the current state of the cell.
            /// </summary>
            private volatile CellState _state;
            /// <summary>
            /// Indicates whether the cell is currently in a flashed state.
            /// </summary>
            private volatile bool _isFlashed = false;
            /// <summary>
            /// Handle the fading functionality of the cell.
            /// </summary>
            private ActiveCellFader _fader = new ActiveCellFader();
            /// <summary>
            /// The current number of itearations into a fade.
            /// </summary>
            private int _iterations;
            /// <summary>
            /// Indicates that the cell has been drawn at least once.
            /// </summary>
            private bool _firstTime = true;

            #region --- Fonts -----------------------------

            /// <summary>
            /// Font to use for the pre text
            /// </summary>
            private Font fldPreTextFont;
            /// <summary>
            /// Font to use for the post text
            /// </summary>
            private Font fldPostTextFont;
            /// <summary>
            /// Font used when the cell is in the flashed state
            /// </summary>
            private Font fldFlashFont;

            #endregion

            #region --- Colours ---------------------------

            /// <summary>
            /// Foreground color for the pre-text
            /// </summary>
            private Color fldPreTextForeColor;
            /// <summary>
            /// Foreground color for the post-text
            /// </summary>
            private Color fldPostTextForeColor;
            /// <summary>
            /// A color representing the starting color for the gradient
            /// </summary>
            private Color fldGradientStartColor;
            /// <summary>
            /// A color representing the ending color for the gradient
            /// </summary>
            private Color fldGradientEndColor;
            /// <summary>
            /// Background colour while cell is being flashed
            /// </summary>
            private Color fldFlashBackColor;
            /// <summary>
            /// Foreground color while the cell is being flashed
            /// </summary>
            private Color fldFlashForeColor;
            /// <summary>
            /// Foreground color for the pre-text while the cell is being flashed
            /// </summary>
            private Color fldFlashPreTextForeColor;
            /// <summary>
            /// Foreground color for the post-text while the cell is being flashed
            /// </summary>
            private Color fldFlashPostTextForeColor;
            /// <summary>
            /// A color representing the starting color for the gradient when the cell is flashed
            /// </summary>
            private Color fldFlashGradientStartColor;
            /// <summary>
            /// A color representing the ending color for the gradient when the cell is flashed
            /// </summary>
            private Color fldFlashGradientEndColor;

            #endregion

            #region --- Strings ---------------------------

            /// <summary>
            /// Text that is to appear at the near left of the cell
            /// </summary>
            private String fldPreText;
            /// <summary>
            /// Text that is to appear at the far right of the cell
            /// </summary>
            private String fldPostText;
            /// <summary>
            /// Text that is to appear at the near left of the cell when it is in the flashed state
            /// </summary>
            private String fldFlashPreText;
            /// <summary>
            /// Text that is to appear at the far right of the cell when it is in the flashed state
            /// </summary>
            private String fldFlashPostText;
            /// <summary>
            /// A String containing zero or more format items for the main text within the cell 
            /// </summary>
            private String fldFormat;

            #endregion

            #region --- Miscellaneous ---------------------

            /// <summary>
            /// Vertical alignment of the main text
            /// </summary>
            private StringAlignment fldVerticalAlignment;
            /// <summary>
            /// Horizontal alignment of the main text
            /// </summary>
            private StringAlignment fldHorizontalAlignment;
            /// <summary>
            /// Enumeration specifying the orientation of the gradient when in a flashed state
            /// </summary>
            private LinearGradientMode fldFlashLinearGradientMode;
            /// <summary>
            /// Flag to indicate that a different fore-color should be used for negative values
            /// </summary>
            private bool fldUseNegativeForeColor;
            /// <summary>
            /// Flag to indicate whether zero-values will be displayed or not.
            /// </summary>
            private bool fldDisplayZeroValues = false;

            #endregion

            #endregion

            #region --- Public Constructors ---------------------------

            /// <summary>
            /// Initializes a new instance of the ActiveCell class with default values
            /// </summary>
            public ActiveCell()
                : base()
            {
                Initialize();
            }

            /// <summary>
            /// Initializes a new instance of the ActiveCell class with the specified owner and text
            /// </summary>
            /// <param name="owner">A LVRow that represents the item that owns the subitem</param>
            /// <param name="text">The text to display for the subitem</param>
            public ActiveCell(ActiveRow owner, string text)
                : base(owner, text)
            {
                this._row = owner;

                Initialize();
            }

            /// <summary>
            /// Initializes a new instance of the ActiveCell class with 
            /// the specified owner, text, foreground color, background color, and font values. 
            /// </summary>
            /// <param name="owner">A LVRow that represents the item that owns the subitem</param>
            /// <param name="text">The text to display for the subitem</param>
            /// <param name="foreColor">A Color that represents the foreground color of the subitem</param>
            /// <param name="backColor">A Color that represents the background color of the subitem</param>
            /// <param name="font">A Font that represents the font to display the subitem's text in</param>
            public ActiveCell(ActiveRow owner, string text, Color foreColor, Color backColor, Font font)
                : base(owner, text, foreColor, backColor, font)
            {
                this._row = owner;

                Initialize();
            }

            /// <summary>
            /// Set the default values for all of the class data.
            /// </summary>
            private void Initialize()
            {
                // Set the timer callback method.
                callback = flashTimerTimeout;

                this._iterations = 0;

                // FONTS
                this.fldPreTextFont = base.Font;
                this.fldPostTextFont = base.Font;
                this.fldFlashFont = base.Font;

                // COLORS
                this.fldPreTextForeColor = ActiveGrid.Paintbox.PreTextColor;
                this.fldPostTextForeColor = ActiveGrid.Paintbox.PostTextColor;
                this.fldFlashForeColor = ActiveGrid.Paintbox.FlashForeColor;
                this.fldFlashBackColor = ActiveGrid.Paintbox.FlashBackgroundColor;
                this.fldFlashGradientStartColor = ActiveGrid.Paintbox.FlashGradientStartColor;
                this.fldFlashGradientEndColor = ActiveGrid.Paintbox.FlashGradientEndColor;
                this.fldFlashPreTextForeColor = ActiveGrid.Paintbox.FlashPreTextColor;
                this.fldFlashPostTextForeColor = ActiveGrid.Paintbox.FlashPostTextColor;

                // STRINGS
                this.fldPreText = String.Empty;
                this.fldPostText = String.Empty;
                this.fldFlashPreText = String.Empty;
                this.fldFlashPostText = String.Empty;
                this.fldFormat = DEFAULT_FORMAT_SPECIFIER;

                // MISCELLENEOUS
                this.fldVerticalAlignment = StringAlignment.Center;
                this.fldHorizontalAlignment = StringAlignment.Center;
                this.fldFlashLinearGradientMode = LinearGradientMode.Vertical;
                this.fldDisplayZeroValues = false;
                this.fldUseNegativeForeColor = false;
                this._state = CellState.Normal;
                this._style = CellStyle.Plain;
            }

            #endregion

            #region --- Public Methods --------------------------------

            /// <summary>
            /// Invalidates the entire surface of the cell and causes it to be redrawn
            /// N.B This method is not thread-safe.
            /// </summary>
            public void Invalidate()
            {
                if (this._row != null)
                    this._row.Invalidate(this.Bounds);
            }

            /// <summary>
            /// Invalidates the entire surface of the cell and causes it to be redrawn in
            /// a thread-safe manner.
            /// N.B. This request may originate from a different thread than the one
            ///      from which it originated. In the interest of thread-safety, we'll
            ///      ask the parent control to del with it accordingly.
            ///      (A reference to the whole cell is sent because any attempt to get
            ///      the Bounds will not be safe).
            /// </summary>
            public void InvalidateCell()
            {
                if (this._row != null && this._row.Grid != null)
                    this._row.Grid.InvalidateCell(this);
            }

            /// <summary>
            /// Paints the cell.
            /// </summary>
            /// <param name="g"></param>
            /// <param name="highlighted"></param>
            public void Draw(Graphics g, bool selected)
            {
                this.CellState = selected ? CellState.Highlighted : (this._isFlashed ? CellState.Flashed : CellState.Normal);

                Draw(g);
            }

            /// <summary>
            /// Paints the cell.
            /// </summary>
            /// <param name="g"></param>
            public void Draw(Graphics g)
            {
                SetDrawingOptions();

                DrawBackground(g);

                DrawForeground(g);
            }

            /// <summary>
            /// Changes the state of the cell into 'Flashed' mode
            /// </summary>
            public void Flash()
            {
                // Only ever flash the cell if the parent grid supports it and the layout
                // logic is not currently suspended.
                if (this._row.Grid.AllowFlashing && !this._row.Grid.LayoutSuspended)
                {
                    // Set the iteration level.
                    Interlocked.Exchange(ref this._iterations, this._row.Grid.UseFlashFadeOut ? ActiveCellFader.DEFAULT_TOTAL_ITERATIONS : 0);

                    // Is the cell already 'Flashed'?
                    if (!this._isFlashed || this._row.Grid.UseFlashFadeOut)
                        StartBackgroundTimer();
                }

                // Redraw the cell.
                Draw(this._row.Grid.CreateGraphics());
            }


            public override string ToString()
            {
                return "ActiveCell{}";
            }

            #endregion

            #region --- Private Methods -------------------------------

            /// <summary>
            /// Sets the initial drawing specifications of the cell prior to drawing for the first time.
            /// This only need to be called once when it is first drawn.
            /// </summary>
            private void SetDrawingOptions()
            {
                if (this._row != null && this._row.Grid != null && this._firstTime)
                {
                    // Set the Column-level drawing options for the cell.
                    // The values are defined in the Column Header object to which this cell belongs.
                    this._column = this._row.Grid.Columns[this._row.SubItems.IndexOf(this)];
                    if (this._column != null)
                    {
                        this.fldHorizontalAlignment = this._column.CellHorizontalAlignment;
                        this.fldVerticalAlignment = this._column.CellVerticalAlignment;
                        this.Format = this._column.CellFormat;
                        this.fldDisplayZeroValues = this._column.DisplayZeroValues;
                    }

                    // Set Grid-level drawing options for the cell.
                    // The values are defined in the Grid object to which this cell belongs.
                    if (this._isFlashed)
                        this._style = this._row.Grid.UseFlashGradient ? CellStyle.Gradient : CellStyle.Plain;
                    else
                        this._style = this._row.Grid.UseGradient ? CellStyle.Gradient : CellStyle.Plain;

                    // Set the Grid-level Flash settings.
                    this.fldFlashForeColor = ActiveGrid.Paintbox.FlashForeColor;
                    this.fldFlashBackColor = ActiveGrid.Paintbox.FlashBackgroundColor;
                    this.fldFlashGradientStartColor = ActiveGrid.Paintbox.FlashGradientStartColor;
                    this.fldFlashGradientEndColor = ActiveGrid.Paintbox.FlashGradientEndColor;
                    this.fldFlashLinearGradientMode = ActiveGrid.Paintbox.FlashLinearGradientMode;

                    // Set the background colour, taking into account the fact that the grid
                    // may be using an alternating background.
                    this.BackColor = this._row.AlternateRow ? ActiveGrid.Paintbox.AlternateBackgroundColor : ActiveGrid.Paintbox.NormalBackgroundColor;
                    this._fader.StartColor = ActiveGrid.Paintbox.FlashBackgroundColor;
                    this._fader.EndColor = this.BackColor;
                    this._fader.TotalIterations = ActiveCellFader.DEFAULT_TOTAL_ITERATIONS;

                    this._firstTime = false;
                }
            }

            /// <summary>
            /// Draws the background of the cell taking into account the current state.
            /// </summary>
            /// <param name="g"></param>
            private void DrawBackground(Graphics g)
            {
                // Set Grid-level drawing options for the cell.
                // The values are defined in the Grid object to which this cell belongs.
                if (this._isFlashed)
                    this._style = this._row.Grid.UseFlashGradient ? CellStyle.Gradient : CellStyle.Plain;
                else
                    this._style = this._row.Grid.UseGradient ? CellStyle.Gradient : CellStyle.Plain;

                switch (this._state)
                {
                    case CellState.Highlighted:
                        DrawHighlighted(g);
                        break;
                    case CellState.Flashed:
                        if (this._style == CellStyle.Gradient)
                            DrawGradient(g);
                        else
                            DrawPlain(g);
                        break;
                    default:
                        if (this._style == CellStyle.Gradient && this._row.AlternateRow)
                            DrawGradient(g);
                        else
                            DrawPlain(g);
                        break;
                }
            }

            /// <summary>
            /// Gets the bounding rectangle of the cell taking into account the grid lines.
            /// </summary>
            /// <returns></returns>
            private Rectangle CellBounds()
            {
                return new Rectangle(this.Bounds.X + (this._row.Grid.GridLines ? 1 : 0), this.Bounds.Y, this.Bounds.Width - (this._row.Grid.GridLines ? 1 : 0), this.Bounds.Height - (this._row.Grid.GridLines ? 1 : 0));
            }

            /// <summary>
            /// Set the current cell state to 'Flashed'
            /// </summary>
            private void FlashOn()
            {
                if (!this._isFlashed)
                {
                    if (this._row != null && this._row.Grid != null)
                        this._row.Grid.IncrementFlashCount();

                    this._isFlashed = true;

                    if (this.CellState != CellState.Highlighted)
                        this.CellState = CellState.Flashed;
                }
            }

            /// <summary>
            /// Set the current cell state to 'Not-Flashed'
            /// </summary>
            private void FlashOff()
            {
                if (this._isFlashed)
                {
                    this._isFlashed = false;

                    if (this._row != null && this._row.Grid != null)
                        this._row.Grid.DecrementFlashCount();

                    if (this.CellState != CellState.Highlighted)
                        this.CellState = CellState.Normal;
                }
            }

            #endregion

            #region --- Drawing Methods -------------------------------

            /// <summary>
            /// Draws the foreground of the cell taking into account the current state.
            /// </summary>
            /// <param name="g"></param>
            private void DrawForeground(Graphics g)
            {
                // Discover which of the three text areas we need to draw and which we can ignore.
                // This will prevent any unnecessary drawing from taking place and will boost performance.
                bool drawText = !String.IsNullOrEmpty(this.Text);
                bool drawPreText = (this._isFlashed) ? !String.IsNullOrEmpty(this.fldFlashPreText) : !String.IsNullOrEmpty(this.fldPreText);
                bool drawPostText = (this._isFlashed) ? !String.IsNullOrEmpty(this.fldFlashPostText) : !String.IsNullOrEmpty(this.fldPostText);

                // If there's nothing to draw then return.
                if (!drawText && !drawPreText && !drawPostText)
                    return;


                // Set the correct brushes to use when drawing each of the three text areas.
                // The choice of brush will be based mainly on the current state of the cell.
                Brush fgTextBrush = null;
                Brush fgPreTextBrush = null;
                Brush fgPostTextBrush = null;

                switch (this._state)
                {
                    case CellState.Highlighted:
                        if (drawText) fgTextBrush = SystemBrushes.HighlightText;
                        if (drawPreText) fgPreTextBrush = SystemBrushes.HighlightText;
                        if (drawPostText) fgPostTextBrush = SystemBrushes.HighlightText;
                        break;
                    case CellState.Flashed:
                        if (drawText) fgTextBrush = this.fldUseNegativeForeColor ? ActiveGrid.Paintbox.NegativeValueBrush : ActiveGrid.Paintbox.Brush(this.fldFlashForeColor);
                        if (drawPreText) fgPreTextBrush = ActiveGrid.Paintbox.Brush(this.fldFlashPreTextForeColor);
                        if (drawPostText) fgPostTextBrush = ActiveGrid.Paintbox.Brush(this.fldFlashPostTextForeColor);
                        break;
                    case CellState.Normal:
                    default:
                        if (drawText) fgTextBrush = this.fldUseNegativeForeColor ? ActiveGrid.Paintbox.NegativeValueBrush : ActiveGrid.Paintbox.Brush(base.ForeColor);
                        if (drawPreText) fgPreTextBrush = ActiveGrid.Paintbox.Brush(this.fldPreTextForeColor);
                        if (drawPostText) fgPostTextBrush = ActiveGrid.Paintbox.Brush(this.fldPostTextForeColor);
                        break;
                }

                // Draw the text contents of the cell.
                using (StringFormat sf = new StringFormat())
                {
                    sf.LineAlignment = this.fldVerticalAlignment;

                    // Draw the Pre-Text (if there is any)
                    SizeF szPreText = SizeF.Empty;
                    if (drawPreText)
                    {
                        sf.Alignment = StringAlignment.Near;
                        sf.Trimming = StringTrimming.Character;
                        sf.FormatFlags = StringFormatFlags.NoWrap;
                        szPreText = g.MeasureString((this._state == CellState.Flashed) ? this.fldFlashPreText : this.fldPreText, this.fldPreTextFont);
                        g.DrawString((this._state == CellState.Flashed) ? this.fldFlashPreText : this.fldPreText, this.fldPreTextFont, fgPreTextBrush, this.Bounds, sf);
                    }

                    // Draw the Post-Text (if there is any)
                    SizeF szPostText = SizeF.Empty;
                    if (drawPostText)
                    {
                        sf.Alignment = StringAlignment.Far;
                        sf.Trimming = StringTrimming.Character;
                        sf.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
                        szPostText = g.MeasureString((this._state == CellState.Flashed) ? this.fldFlashPostText : this.fldPostText, this.fldPostTextFont, (this.Bounds.Width - Convert.ToInt32(szPreText.Width)), sf);
                        g.DrawString((this._state == CellState.Flashed) ? this.fldFlashPostText : this.fldPostText, this.fldPostTextFont, fgPostTextBrush, this.Bounds, sf);
                    }

                    // Draw the main text (if there is any)
                    if (drawText)
                    {
                        sf.Alignment = this.HorizontalAlignment;
                        sf.Trimming = StringTrimming.EllipsisCharacter;
                        sf.FormatFlags = StringFormatFlags.NoWrap;

                        RectangleF rcMain = new RectangleF(this.Bounds.Left + (drawPreText ? szPreText.Width : 0),
                                                           this.Bounds.Top,
                                                           this.Bounds.Width - (drawPreText ? szPreText.Width : 0) - (drawPostText ? szPostText.Width : 0),
                                                           this.Bounds.Height);

                        g.DrawString(this.Text, ((this._state == CellState.Flashed) ? this.fldFlashFont : base.Font), fgTextBrush, rcMain, sf);
                    }
                }
            }


            /// <summary>
            /// Draw a solid background  
            /// </summary>
            /// <param name="g"></param>
            private void DrawPlain(Graphics g)
            {
                // Draw the backgroung based on the current state of the cell
                switch (this.CellState)
                {
                    case CellState.Flashed:
                        g.FillRectangle(ActiveGrid.Paintbox.Brush(this._row.Grid.UseFlashFadeOut ? this._fader.GetIterationColor(this._iterations) : this.fldFlashBackColor), CellBounds());
                        break;
                    case CellState.Highlighted:
                        DrawHighlighted(g);
                        break;
                    default:
                        g.FillRectangle(this._row.Grid.UseAlternateRowColors ? (this._row.AlternateRow ? ActiveGrid.Paintbox.AlternateBackgroundBrush : ActiveGrid.Paintbox.NormalBackgroundBrush) : ActiveGrid.Paintbox.NormalBackgroundBrush, CellBounds());
                        break;
                }
            }


            /// <summary>
            /// Draw the cell in the 'Highlighted' state.
            /// </summary>
            /// <param name="g"></param>
            private void DrawHighlighted(Graphics g)
            {
                g.FillRectangle(SystemBrushes.Highlight, CellBounds());
            }

            /// <summary>
            /// Draw the background using a gradient.
            /// </summary>
            /// <param name="g"></param>
            private void DrawGradient(Graphics g)
            {
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(CellBounds(),
                            (this.CellState == CellState.Normal) ? ActiveGrid.Paintbox.GradientStartColor : this.fldFlashGradientStartColor,
                            (this.CellState == CellState.Normal) ? ActiveGrid.Paintbox.GradientEndColor : this.fldFlashGradientEndColor,
                            (this.CellState == CellState.Normal) ? ActiveGrid.Paintbox.RowLinearGradientMode : this.fldFlashLinearGradientMode))
                {
                    g.FillRectangle(bgBrush, bgBrush.Rectangle);
                }
            }

            #endregion

            #region --- Background Timer ------------------------------

            /// <summary>
            /// Background timer used to handle the duration of the cell flash.
            /// </summary>
            private System.Threading.Timer _flashTimer = null;

            /// <summary>
            /// Creates a new background timer that will callback when the flash duration has expired.
            /// </summary>
            /// <returns>true if the timer was initialized successfully; otherwise, false</returns>
            private void StartBackgroundTimer()
            {
                // Do we have a callback method 
                if (this.callback != null)
                {
                    FlashOn();

                    ActiveCellState state = new ActiveCellState(this._row.Grid.UseFlashFadeOut, ActiveCellFader.DEFAULT_TOTAL_ITERATIONS, this._row.Grid.UseFlashFadeOut ? (this._row.Grid.FlashDuration / ActiveCellFader.DEFAULT_TOTAL_ITERATIONS) : this._row.Grid.FlashDuration);

                    // Start the timer with the required configuration.
                    this._flashTimer = new System.Threading.Timer(callback, state, state.Interval, Timeout.Infinite);
                }
            }

            /// <summary>
            /// Callback method for the cell's background timer.
            /// N.B. This method will be called on a different thread to the one that
            /// initialized the timer.
            /// </summary>
            /// <param name="state"></param>
            private void flashTimerTimeout(object state)
            {
                ActiveCellState cellState = state as ActiveCellState;

                if (cellState == null)
                {
                    // The state object should always be non-null but just in
                    // case this ever happens simply dispose of the timer.
                    if (this._flashTimer != null)
                    {
                        this._flashTimer.Dispose();
                        this._flashTimer = null;
                    }
                }
                else
                {
                    // Is the 'Fade-Out' effect enabled?
                    if (cellState.UseFadeOut)
                    {
                        // This bit of strange logic is to handle the situation when the contents
                        // of a cell are changed part-way through the fade effect.
                        // When this happens the fade cycle must be reset to the start and timing
                        // begins again from scratch.
                        cellState.IterationsRemaining = this._iterations;
                        cellState.DecrementIterations();
                        this._iterations = cellState.IterationsRemaining;

                        // Have we finished fading-out so that we can dispose of the timer?
                        if (cellState.CanDispose)
                        {
                            FlashOff();

                            cellState.Disposed = true;

                            if (this._flashTimer != null)
                            {
                                this._flashTimer.Dispose();
                                this._flashTimer = null;
                            }
                        }
                        else if (!cellState.Disposed)
                        {
                            // Schedule the next callback event.
                            if (this._flashTimer != null)
                                this._flashTimer.Change(cellState.Interval, Timeout.Infinite);
                        }
                    }
                    else
                    {
                        FlashOff();

                        cellState.Disposed = true;

                        if (this._flashTimer != null)
                        {
                            this._flashTimer.Dispose();
                            this._flashTimer = null;
                        }
                    }
                }

                // Redraw the cell in a thread-safe manner.
                InvalidateCell();
            }

            #endregion

            #region --- Public Properties -----------------------------

            #region --- Colours ---------------------------------------

            /// <summary>
            /// Gets or sets the foreground color for the pre-text string when it is in a normal state.
            /// </summary>
            public Color PreTextForeColor
            {
                get { return this.fldPreTextForeColor;  }
                set { this.fldPreTextForeColor = value; }
            }

            /// <summary>
            /// Gets or sets the foreground color for the post-text string when it is in a normal state.
            /// </summary>
            public Color PostTextForeColor
            {
                get { return this.fldPostTextForeColor;  }
                set { this.fldPostTextForeColor = value; }
            }

            /// <summary>
            /// Gets or sets the foreground color for the pre-text string when it is in a 'flashed' state.
            /// </summary>
            public Color FlashPreTextForeColor
            {
                get { return this.fldFlashPreTextForeColor;  }
                set { this.fldFlashPreTextForeColor = value; }
            }

            /// <summary>
            /// Gets or sets the foreground color for the post-text string when it is in a 'flashed' state.
            /// </summary>
            public Color FlashPostTextForeColor
            {
                get { return this.fldFlashPostTextForeColor;  }
                set { this.fldFlashPostTextForeColor = value; }
            }

            /// <summary>
            /// Gets or sets the foreground color of the cell while it is in a 'flashed' state.
            /// </summary>
            public Color FlashForeColor
            {
                get { return this.fldFlashForeColor;  }
                set { this.fldFlashForeColor = value; }
            }

            /// <summary>
            /// Gets or sets the background color of the cell while it is in a 'flashed' state.
            /// </summary>
            public Color FlashBackColor
            {
                get { return this.fldFlashBackColor;  }
                set 
                { 
                    this.fldFlashBackColor = value; 
                    if(this._fader!= null)
                        this._fader.StartColor = value; 
                }
            }

            /// <summary>
            /// Gets or sets the start colour of the background gradient of the cell when
            /// it is in the normal state
            /// </summary>
            public Color GradientStartColor
            {
                get { return this.fldGradientStartColor;  }
                set { this.fldGradientStartColor = value; }
            }

            /// <summary>
            /// Gets or sets the end colour of the background gradient of the cell when
            /// it is in the normal state
            /// </summary>
            public Color GradientEndColor
            {
                get { return this.fldGradientEndColor;  }
                set { this.fldGradientEndColor = value; }
            }

            /// <summary>
            /// Gets or sets the start colour of the background gradient of the cell when
            /// it is in the 'flashed' state
            /// </summary>
            public Color FlashGradientStartColor
            {
                get { return this.fldFlashGradientStartColor;  }
                set { this.fldFlashGradientStartColor = value; }
            }

            /// <summary>
            /// Gets or sets the end colour of the background gradient of the cell when
            /// it is in the 'flashed' state
            /// </summary>
            public Color FlashGradientEndColor
            {
                get { return this.fldFlashGradientEndColor;  }
                set { this.fldFlashGradientEndColor = value; }
            }

            #endregion

            #region --- Fonts -----------------------------------------

            /// <summary>
            /// Gets or sets the font used in the text of the cell when it is being flashed.
            /// </summary>
            public Font FlashFont
            {
                get { return this.fldFlashFont; }
                set { this.fldFlashFont = value; }
            }

            /// <summary>
            /// Gets or sets the font used for the pre-text string of the cell when it is in a normal state.
            /// </summary>
            public Font PreTextFont
            {
                get { return this.fldPreTextFont; }
                set { this.fldPreTextFont = value; }
            }

            /// <summary>
            /// Gets or sets the font used for the post-text string of the cell when it is in a normal state.
            /// </summary>
            public Font PostTextFont
            {
                get { return this.fldPostTextFont; }
                set { this.fldPostTextFont = value; }
            }

            #endregion

            #region --- Strings ---------------------------------------

            /// <summary>
            /// Main text of the cell
            /// </summary>
            public new String Text
            {
                get
                {
                    string strVal = String.Empty;
                    if (this._useDecimal)
                    {
                        strVal = String.Format(this.fldFormat, this._decValue);

                        // Do we display zero values?
                        if (this._decValue == Decimal.Zero && !this.fldDisplayZeroValues)
                            strVal = String.Empty;
                    }
                    else
                    {
                        strVal = String.Format(this.fldFormat, base.Text);
                    }

                    return strVal;
                }
                set
                {
                    this._useDecimal = false;
                    base.Text = value;
                    // Has the value changed?
                    if (!base.Text.Equals(String.IsNullOrEmpty(value) ? String.Empty : value))
                    {
                        base.Text = String.IsNullOrEmpty(value) ? String.Empty : value;

                        Flash();
                    }
                }
            }

            /// <summary>
            /// Gets or sets the format specifier for the cell text.
            /// </summary>
            public String Format
            {
                get { return this.fldFormat; }
                set { this.fldFormat = String.IsNullOrEmpty(value) ? DEFAULT_FORMAT_SPECIFIER : "{0:" + value.Trim() + "}"; }
            }


            /// <summary>
            /// Gets or sets the text that appears at the far left of the cell when it is in a normal state.
            /// </summary>
            public String PreText
            {
                get { return this.fldPreText; }
                set { this.fldPreText = (value == null) ? String.Empty : value.TrimEnd(); }
            }

            /// <summary>
            /// Gets or sets the text that appears at the far left of the cell when it is being 'flashed'.
            /// </summary>
            public String FlashPreText
            {
                get { return this.fldFlashPreText; }
                set { this.fldFlashPreText = (value == null) ? String.Empty : value.TrimEnd(); }
            }

            /// <summary>
            /// Gets or sets the text that appears at the far right of the cell when it is in a normal state.
            /// </summary>
            public String PostText
            {
                get { return this.fldPostText; }
                set { this.fldPostText = (value == null) ? String.Empty : value.TrimStart(); }
            }

            /// <summary>
            /// Gets or sets the text that appears at the far right of the cell when it is being 'flashed'.
            /// </summary>
            public String FlashPostText
            {
                get { return this.fldFlashPostText; }
                set { this.fldFlashPostText = (value == null) ? String.Empty : value.TrimStart(); }
            }

            #endregion

            #region --- Miscellaneous ---------------------------------

            /// <summary>
            /// Gets the row to which this cell belongs
            /// </summary>
            public ActiveRow Row
            {
                get { return this._row; }
            }

            /// <summary>
            /// Gets the column to which this cell belongs
            /// </summary>
            public ActiveColumnHeader Column
            {
                get { return this._column; }
            }

            /// <summary>
            /// Gets or sets the vertical alignment of all text within the cell.
            /// </summary>
            public StringAlignment VerticalAlignment
            {
                get { return this.fldVerticalAlignment; }
                set { this.fldVerticalAlignment = value; }
            }

            /// <summary>
            /// Gets or sets the horizontal alignment of all text within the cell.
            /// </summary>
            public StringAlignment HorizontalAlignment
            {
                get { return this.fldHorizontalAlignment; }
                set { this.fldHorizontalAlignment = value; }
            }

            /// <summary>
            /// Gets or sets the Decimal value to display within the cell.
            /// </summary>
            public Decimal DecimalValue
            {
                get { return this._decValue; }
                set
                {
                    this._useDecimal = true;

                    // Has the value changed?
                    if (this._decValue != value)
                    {
                        this._decValue = value;

                        this.fldUseNegativeForeColor = (this._decValue < Decimal.Zero);

                        Flash();
                    }
                }
            }

            /// <summary>
            /// Gets a flag indicating whether the cell contains a decimal value or not
            /// </summary>
            public Boolean ValueIsDecimal
            {
                get { return this._useDecimal; }
            }

            /// <summary>
            /// Gets or sets the linear gradient mode for the background of the cell when it
            /// is in a flashed state.
            /// </summary>
            public LinearGradientMode FlashLinearGradientMode
            {
                get { return this.fldFlashLinearGradientMode; }
                set { this.fldFlashLinearGradientMode = value; }
            }

            /// <summary>
            /// Gets or sets the current state of the cell
            /// </summary>
            public CellState CellState
            {
                get { lock (this.synchRoot) { return this._state;  } }
                set { lock (this.synchRoot) { this._state = value; } }
            }

            /// <summary>
            /// Gets or sets the current style of the cell
            /// </summary>
            public CellStyle CellStyle
            {
                get { return this.CellStyle; }
                set { this.CellStyle = value; }
            }

            #endregion

            #endregion

            #region --- Nested Class ActiveCellFader ------------------

            /// <summary>
            /// Class that handles the Fade-effect logic for the cell.
            /// </summary>
            private class ActiveCellFader
            {
                #region --- Class Data -----------------------------------

                /// <summary>
                /// The number of iterations to use for the fade effect
                /// </summary>
                public static int DEFAULT_TOTAL_ITERATIONS = 10;
                /// <summary>
                /// The colour at the start of the fade.
                /// </summary>
                private Color _startColor;
                /// <summary>
                /// The colour at the end of the fade.
                /// </summary>
                private Color _endColor;
                /// <summary>
                /// The total number of iterations in the fade effect
                /// </summary>
                private int _iterations;
                /// <summary>
                /// The difference to be added to the Red component during each iteration
                /// </summary>
                private int _deltaR;
                /// <summary>
                /// The difference to be added to the Green component during each iteration
                /// </summary>
                private int _deltaG;
                /// <summary>
                /// The difference to be added to the Blue component during each iteration
                /// </summary>
                private int _deltaB;

                #endregion

                #region --- Constructor ----------------------------------

                /// <summary>
                /// Default constructor (White background, no fade)
                /// </summary>
                public ActiveCellFader()
                    : this(Color.White, Color.White, 0)
                {
                }

                /// <summary>
                /// Construcor fading to a white background from a given starting colour.
                /// </summary>
                /// <param name="startColor">The starting colour of the fade</param>
                public ActiveCellFader(Color startColor)
                    : this(startColor, Color.White, DEFAULT_TOTAL_ITERATIONS)
                {
                }

                /// <summary>
                /// Constructor fading from the start and end colours provided
                /// </summary>
                /// <param name="startColor">The starting colour of the fade</param>
                /// <param name="endColor">The finishing colour of the fade</param>
                public ActiveCellFader(Color startColor, Color endColor)
                    : this(startColor, endColor, DEFAULT_TOTAL_ITERATIONS)
                {
                }

                /// <summary>
                /// Constructor fading from the start and end colours provided using the 
                /// given number of iterations
                /// </summary>
                /// <param name="startColor">The starting colour of the fade</param>
                /// <param name="endColor">The finishing colour of the fade</param>
                /// <param name="iterations">The number of iterations in the fade</param>
                public ActiveCellFader(Color startColor, Color endColor, int iterations)
                {
                    this._startColor = startColor;
                    this._endColor = endColor;
                    this._iterations = (iterations < 0) ? 0 : iterations;

                    CalculateDeltas();
                }

                #endregion

                #region --- Public Methods -------------------------------

                /// <summary>
                /// Retrieves the colour associated with the given iteration of the fade.
                /// </summary>
                /// <param name="iteration">The fade iteration</param>
                /// <returns></returns>
                public Color GetIterationColor(int iteration)
                {
                    if (iteration <= 0)
                        return this._endColor;

                    if(iteration >= this._iterations)
                        return this._startColor;

                    int R = this._startColor.R - ((this._iterations - iteration) * this._deltaR);
                    int G = this._startColor.G - ((this._iterations - iteration) * this._deltaG);
                    int B = this._startColor.B - ((this._iterations - iteration) * this._deltaB);

                    return Color.FromArgb(R, G, B);
                }


                #endregion

                #region --- Private Methods ------------------------------

                /// <summary>
                /// Calculate the difference to be added to each of the colour components
                /// during a single iteration.
                /// </summary>
                private void CalculateDeltas()
                {
                    this._deltaR = 0;
                    this._deltaG = 0;
                    this._deltaB = 0;

                    if (this._startColor == this._endColor)
                        this._iterations = 0;

                    if (this._iterations > 0)
                    {
                        this._deltaR = (this._startColor.R - this._endColor.R) / this._iterations;
                        this._deltaG = (this._startColor.G - this._endColor.G) / this._iterations;
                        this._deltaB = (this._startColor.B - this._endColor.B) / this._iterations;
                    }
                }

                #endregion

                #region --- Public Properties ----------------------------

                /// <summary>
                /// Gets or sets the starting colour of the fade effect.
                /// </summary>
                public Color StartColor
                {
                    get { return this._startColor; }
                    set
                    {
                        if (this._startColor != value)
                        {
                            this._startColor = value;
                            CalculateDeltas();
                        }
                    }
                }

                /// <summary>
                /// Gets or sets the finishing colour of the fade effect.
                /// </summary>
                public Color EndColor
                {
                    get { return this._endColor; }
                    set
                    {
                        if (this._endColor != value)
                        {
                            this._endColor = value;
                            CalculateDeltas();
                        }
                    }
                }

                /// <summary>
                /// Gets or sets the total number of iterations in the fade effect.
                /// </summary>
                public Int32 TotalIterations
                {
                    get { return this._iterations; }
                    set
                    {
                        if (this._iterations != value)
                        {
                            this._iterations = value;
                            CalculateDeltas();
                        }
                    }

                }

                #endregion
            }

            #endregion

            #region --- Nested Class ActiveCellState ------------------

            /// <summary>
            /// Class to store the Fade-state of an ActiveCell instance for use in timer callbacks.
            /// </summary>
            private class ActiveCellState
            {
                #region --- Class Data --------------------------------

                /// <summary>
                /// Flag to indicate if the fade effect should be used.
                /// </summary>
                private bool _useFadeOut;
                /// <summary>
                /// The number of iterations remaining
                /// </summary>
                private int _iterations;
                /// <summary>
                /// The interval to wait between iterations
                /// </summary>
                private int _interval;
                /// <summary>
                /// Flag to indicate that the fader has been disposed of.
                /// </summary>
                private bool _isDisposed;

                #endregion

                #region --- Constructor -------------------------------

                /// <summary>
                /// Default constructor
                /// </summary>
                /// <param name="useFadeOut">Indicates whether a fade-effect is required</param>
                /// <param name="iterations">The total number of iterations to use in the fade-effect</param>
                /// <param name="interval">The interval to wait between callbacks</param>
                public ActiveCellState(bool useFadeOut, int iterations, int interval)
                {
                    this._useFadeOut = useFadeOut;
                    this._iterations = iterations;
                    this._interval = interval;
                    this._isDisposed = false;
                }

                #endregion

                #region --- Public Methods ----------------------------

                /// <summary>
                /// Increment the number of iterations remaining
                /// </summary>
                /// <returns>The number of iterations remaining</returns>
                public int IncrementIterations()
                {
                    return Interlocked.Increment(ref this._iterations);
                }

                /// <summary>
                /// Decrements the number of iterations remaining
                /// </summary>
                /// <returns>The number of iterations remaining</returns>
                public int DecrementIterations()
                {
                    return (this._iterations > 0 ? Interlocked.Decrement(ref this._iterations) : this._iterations);
                }

                #endregion

                #region --- Properties --------------------------------

                /// <summary>
                /// Gets a flag to indicate if the fade-effect is being used.
                /// </summary>
                public Boolean UseFadeOut
                {
                    get { return this._useFadeOut; }
                }

                /// <summary>
                /// Gets or sets the number of iterations remaining.
                /// </summary>
                public Int32 IterationsRemaining
                {
                    get { return this._iterations;  }
                    set { Interlocked.Exchange( ref this._iterations, value); }
                }

                /// <summary>
                /// Gets the time interval to wait between callbacks.
                /// </summary>
                public Int32 Interval
                {
                    get { return this._interval;  }
                }

                /// <summary>
                /// Gets a flag to indicate if the current timer can be disposed of.
                /// </summary>
                public Boolean CanDispose
                {
                    get { return (this._useFadeOut ? (this._iterations <= 0) : true);  }
                }

                /// <summary>
                /// Gets or sets a flag to indicate if the current timer has been disposed of.
                /// </summary>
                public Boolean Disposed
                {
                    get { return this._isDisposed;  }
                    set { this._isDisposed = value; }
                }

                #endregion
            }

            #endregion
        }

        #endregion

        #region --- Nested Class LinkLabelCell --------------------

        /// <summary>
        /// Class to provide LinkLabel functionality to the row header.
        /// </summary>
        private class LinkLabelCell
        {
            #region --- Class Data ------------------------------------

            /// <summary>
            /// The bounding rectangle of the text
            /// </summary>
            private RectangleF _rcText;
            /// <summary>
            /// The bounding rectangle of the cell
            /// </summary>
            private Rectangle _parentRect;
            /// <summary>
            /// Flag to indicate if the cursor is currently over the text
            /// </summary>
            private bool _mouseOver = false;
            /// <summary>
            /// Flag to indicate whether the cursor status has changed
            /// i.e. whether it is over or not-over the text
            /// </summary>
            private bool _changed = false;
            /// <summary>
            /// The brush to use for drawing the text.
            /// </summary>
            private Brush _linkBrush = Brushes.Blue;

            #endregion

            #region --- Delegates -------------------------------------

            public event OnLeftMouseClickHandler OnLeftMouseClick;
            public delegate void OnLeftMouseClickHandler(object sender, EventArgs e);

            public event OnRightMouseClickHandler OnRightMouseClick;
            public delegate void OnRightMouseClickHandler(object sender, EventArgs e);

            #endregion

            #region --- Public Constructors ---------------------------

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="rc">The bounding rectangle of the parent cell</param>
            public LinkLabelCell(Rectangle rc)
            {
                this._parentRect = rc;
            }

            #endregion

            #region --- Public Methods --------------------------------

            public bool OnMouseClick(System.Windows.Forms.MouseEventArgs e)
            {
                bool handled = false;

                if (e!= null && Contains(e.Location))
                {
                    switch (e.Button)
                    {
                        case System.Windows.Forms.MouseButtons.Left:
                            OnLeftMouseClickHandler onLeftMouseClick = OnLeftMouseClick;
                            if (onLeftMouseClick != null)
                                onLeftMouseClick(this, EventArgs.Empty);
                            handled = true;
                            break;
                        case System.Windows.Forms.MouseButtons.Middle:
                            // System.Windows.Forms.MessageBox.Show("Middle Mouse Click");
                            break;
                        case System.Windows.Forms.MouseButtons.Right:
                            OnRightMouseClickHandler onRightMouseClick = OnRightMouseClick;
                            if (onRightMouseClick != null)
                                onRightMouseClick(this, EventArgs.Empty);
                            handled = true;
                            break;
                        case System.Windows.Forms.MouseButtons.XButton1:
                            // System.Windows.Forms.MessageBox.Show("XButton1 Mouse Click");
                            break;
                        case System.Windows.Forms.MouseButtons.XButton2:
                            // System.Windows.Forms.MessageBox.Show("XButton2 Mouse Click");
                            break;
                        default:
                            break;
                    }
                }

                return handled;
            }

            public bool MouseOver(PointF pt)
            {
                if (this._mouseOver != Contains(pt))
                {
                    this._mouseOver = Contains(pt);
                    this._changed = true;
                    this._linkBrush = this._mouseOver ? Brushes.Purple : Brushes.Blue;
                }

                return this._mouseOver;
            }


            public void Draw(Graphics g, string text, Font font, Rectangle bounds, StringFormat sf, bool selected)
            {
                this._parentRect = bounds;

                if (selected)
                {
                    g.DrawString(text, font, SystemBrushes.HighlightText, this._parentRect, sf);
                }
                else
                {
                    g.DrawString(text, font, this._linkBrush, this._parentRect, sf);
                }

                PointF pt = this._parentRect.Location;
                SizeF size = g.MeasureString(text, font, this._parentRect.Location, sf);
                switch(sf.LineAlignment)
                {
                    case StringAlignment.Near:
                        pt = this._parentRect.Location;
                        break;
                    case StringAlignment.Far:
                        pt = new PointF( (this._parentRect.X + this._parentRect.Width - size.Width), this._parentRect.Y );
                        break;
                    case StringAlignment.Center:
                        pt = new PointF( (this._parentRect.X + (this._parentRect.Width - size.Width)/2), this._parentRect.Y );
                        break;
                }

                // Set the bounding rectangle of the text
                this._rcText = new RectangleF(pt, size);
            }

            #endregion

            #region --- Private Methods -------------------------------

            /// <summary>
            /// Determines whether the given point lies within the bounding 
            /// rectangle of the text
            /// </summary>
            /// <param name="pt">The location point to test</param>
            /// <returns>true if the point lies over the text; otherwise, false</returns>
            private bool Contains(PointF pt)
            {
                return this._rcText.Contains(pt);
            }

            #endregion

            #region --- Public Properties -----------------------------

            /// <summary>
            /// Gets a flag to indicate if the status of the link has changed.
            /// </summary>
            public Boolean Changed
            {
                get { return this._changed; }
            }

            #endregion
        }

        #endregion
    }

    #endregion

    #region --- Class RowHeaderEventArgs -----------------------------

    public class RowHeaderEventArgs : EventArgs
    {
        #region --- Class Data ---------------------------

        private string _rowName;
        private int _rowIndex;
        private object _rowTag;

        #endregion

        #region --- Constructor --------------------------

        public RowHeaderEventArgs(int index, string name, object tag)
        {
            this._rowIndex = index;
            this._rowName = name;
            this._rowTag = tag;
        }

        #endregion

        #region --- Properties ---------------------------

        public String RowName
        {
            get { return this._rowName; }
        }

        public Int32 RowIndex
        {
            get { return this._rowIndex; }
        }

        public Object RowTag
        {
            get { return this._rowTag;  }
        }

        #endregion
    }

    #endregion

    #region --- Custom Exception Classes -----------------------------

    class DuplicateRowNameException : ApplicationException
    {
        public DuplicateRowNameException(string message)
            : base(message)
        {
        }

        public DuplicateRowNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    class DuplicateColumnNameException : ApplicationException
    {
        public DuplicateColumnNameException(string message)
            : base(message)
        {
        }

        public DuplicateColumnNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    class DuplicateCellNameException : ApplicationException
    {
        public DuplicateCellNameException(string message)
            : base(message)
        {
        }

        public DuplicateCellNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    #endregion

    #region --- ActiveGridItemComparer -------------------------------

    // Implements the manual sorting of items by columns.
    class ActiveGridItemComparer : System.Collections.IComparer
    {
        #region --- Class Data -----------------------------

        private int _col;
        private ActiveColumnHeader _columnHeader;
        private SortOrderEnum _sortOrder;

        #endregion

        #region --- Constructors ---------------------------

        public ActiveGridItemComparer()
        {
            this._col = 0;
            this._sortOrder = SortOrderEnum.Ascending;
        }

        public ActiveGridItemComparer(ActiveColumnHeader columnHeader)
        {
            this._columnHeader = columnHeader;
            this._sortOrder = columnHeader.SortOrder;
            this._col = columnHeader.Index;
        }

        #endregion

        #region --- IComparer ------------------------------

        public int Compare(object x, object y)
        {
            int compare = 0;

            ActiveRow lvi1 = x as ActiveRow;
            ActiveRow lvi2 = y as ActiveRow;

            if (lvi1 != null && lvi2 != null)
            {
                if (this._col == 0)
                {
                    if (this._sortOrder == SortOrderEnum.Ascending)
                        compare = String.Compare(lvi1.Text, lvi2.Text);
                    else
                        compare = String.Compare(lvi2.Text, lvi1.Text);
                }
                else if (lvi1.SubItems.Count > this._col && lvi2.SubItems.Count > this._col)
                {
                    // Are we comparing devima or string values?
                    if (lvi1.SubItems[this._col].ValueIsDecimal && lvi2.SubItems[this._col].ValueIsDecimal)
                    {
                        if (this._sortOrder == SortOrderEnum.Ascending)
                            compare = Decimal.Compare(lvi1.SubItems[this._col].DecimalValue, lvi2.SubItems[this._col].DecimalValue);
                        else
                            compare = Decimal.Compare(lvi2.SubItems[this._col].DecimalValue, lvi1.SubItems[this._col].DecimalValue);
                    }
                    else
                    {
                        if (this._sortOrder == SortOrderEnum.Ascending)
                            compare = String.Compare(lvi1.SubItems[this._col].Text, lvi2.SubItems[this._col].Text);
                        else
                            compare = String.Compare(lvi2.SubItems[this._col].Text, lvi1.SubItems[this._col].Text);
                    }
                }
            }

            return compare;
        }

        #endregion
    }

    #endregion

}