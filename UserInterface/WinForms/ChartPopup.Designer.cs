namespace QuantConnect.Views.WinForms
{
    partial class ChartPopup
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlCharts = new System.Windows.Forms.Panel();
            this.tsMain = new System.Windows.Forms.ToolStrip();
            this.tsCbxDurations = new System.Windows.Forms.ToolStripComboBox();
            this.tsCbxResolutions = new System.Windows.Forms.ToolStripComboBox();
            this.tsContainer = new System.Windows.Forms.ToolStripContainer();
            this.tsMain.SuspendLayout();
            this.tsContainer.BottomToolStripPanel.SuspendLayout();
            this.tsContainer.ContentPanel.SuspendLayout();
            this.tsContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlCharts
            // 
            this.pnlCharts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCharts.Location = new System.Drawing.Point(0, 0);
            this.pnlCharts.Name = "pnlCharts";
            this.pnlCharts.Size = new System.Drawing.Size(284, 111);
            this.pnlCharts.TabIndex = 0;
            // 
            // tsMain
            // 
            this.tsMain.BackColor = System.Drawing.SystemColors.Control;
            this.tsMain.Dock = System.Windows.Forms.DockStyle.None;
            this.tsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsCbxDurations,
            this.tsCbxResolutions});
            this.tsMain.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.tsMain.Location = new System.Drawing.Point(3, 0);
            this.tsMain.Name = "tsMain";
            this.tsMain.Size = new System.Drawing.Size(199, 25);
            this.tsMain.TabIndex = 1;
            this.tsMain.Text = "toolStrip1";
            // 
            // tsCbxDurations
            // 
            this.tsCbxDurations.Name = "tsCbxDurations";
            this.tsCbxDurations.Size = new System.Drawing.Size(75, 25);
            // 
            // tsCbxResolutions
            // 
            this.tsCbxResolutions.Name = "tsCbxResolutions";
            this.tsCbxResolutions.Size = new System.Drawing.Size(75, 25);
            // 
            // tsContainer
            // 
            // 
            // tsContainer.BottomToolStripPanel
            // 
            this.tsContainer.BottomToolStripPanel.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.tsContainer.BottomToolStripPanel.Controls.Add(this.tsMain);
            // 
            // tsContainer.ContentPanel
            // 
            this.tsContainer.ContentPanel.BackColor = System.Drawing.SystemColors.Control;
            this.tsContainer.ContentPanel.Controls.Add(this.pnlCharts);
            this.tsContainer.ContentPanel.Size = new System.Drawing.Size(284, 111);
            this.tsContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tsContainer.LeftToolStripPanelVisible = false;
            this.tsContainer.Location = new System.Drawing.Point(0, 0);
            this.tsContainer.Name = "tsContainer";
            this.tsContainer.RightToolStripPanelVisible = false;
            this.tsContainer.Size = new System.Drawing.Size(284, 161);
            this.tsContainer.TabIndex = 2;
            this.tsContainer.Text = "toolStripContainer1";
            // 
            // tsContainer.TopToolStripPanel
            // 
            this.tsContainer.TopToolStripPanel.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            // 
            // ChartPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 161);
            this.Controls.Add(this.tsContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "ChartPopup";
            this.Text = "ChartPopup";
            this.Load += new System.EventHandler(this.ChartPopup_Load);
            this.tsMain.ResumeLayout(false);
            this.tsMain.PerformLayout();
            this.tsContainer.BottomToolStripPanel.ResumeLayout(false);
            this.tsContainer.BottomToolStripPanel.PerformLayout();
            this.tsContainer.ContentPanel.ResumeLayout(false);
            this.tsContainer.ResumeLayout(false);
            this.tsContainer.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlCharts;
        private System.Windows.Forms.ToolStrip tsMain;
        private System.Windows.Forms.ToolStripComboBox tsCbxDurations;
        private System.Windows.Forms.ToolStripComboBox tsCbxResolutions;
        private System.Windows.Forms.ToolStripContainer tsContainer; 
    }
}