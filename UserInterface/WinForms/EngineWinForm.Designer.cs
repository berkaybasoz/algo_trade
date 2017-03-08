namespace QuantConnect.Views.WinForms
{
    partial class EngineWinForm
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
        public void InitializeComponent()
        {
            this.btnStart = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpAlgo = new System.Windows.Forms.TabPage();
            this.rtbAlgo = new System.Windows.Forms.RichTextBox();
            this.tpLogs = new System.Windows.Forms.TabPage();
            this.lbxLog = new System.Windows.Forms.ListBox();
            this.tpErrors = new System.Windows.Forms.TabPage();
            this.lbxError = new System.Windows.Forms.ListBox();
            this.tpResult = new System.Windows.Forms.TabPage();
            this.pnlCharts = new System.Windows.Forms.FlowLayoutPanel();
            this.button1 = new System.Windows.Forms.Button();
            this.cbAlgoName = new System.Windows.Forms.ComboBox();
            this.btnTest = new System.Windows.Forms.Button();
            this.rbExistingAlgo = new System.Windows.Forms.RadioButton();
            this.rbCustomAlgo = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tabControl1.SuspendLayout();
            this.tpAlgo.SuspendLayout();
            this.tpLogs.SuspendLayout();
            this.tpErrors.SuspendLayout();
            this.tpResult.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(18, 56);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tpAlgo);
            this.tabControl1.Controls.Add(this.tpLogs);
            this.tabControl1.Controls.Add(this.tpErrors);
            this.tabControl1.Controls.Add(this.tpResult);
            this.tabControl1.Location = new System.Drawing.Point(12, 111);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(460, 256);
            this.tabControl1.TabIndex = 4;
            // 
            // tpAlgo
            // 
            this.tpAlgo.Controls.Add(this.rtbAlgo);
            this.tpAlgo.Location = new System.Drawing.Point(4, 22);
            this.tpAlgo.Name = "tpAlgo";
            this.tpAlgo.Padding = new System.Windows.Forms.Padding(3);
            this.tpAlgo.Size = new System.Drawing.Size(452, 230);
            this.tpAlgo.TabIndex = 2;
            this.tpAlgo.Text = "Algo";
            this.tpAlgo.UseVisualStyleBackColor = true;
            // 
            // rtbAlgo
            // 
            this.rtbAlgo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbAlgo.Location = new System.Drawing.Point(3, 3);
            this.rtbAlgo.Name = "rtbAlgo";
            this.rtbAlgo.Size = new System.Drawing.Size(446, 224);
            this.rtbAlgo.TabIndex = 0;
            this.rtbAlgo.Text = "";
            // 
            // tpLogs
            // 
            this.tpLogs.Controls.Add(this.lbxLog);
            this.tpLogs.Location = new System.Drawing.Point(4, 22);
            this.tpLogs.Name = "tpLogs";
            this.tpLogs.Padding = new System.Windows.Forms.Padding(3);
            this.tpLogs.Size = new System.Drawing.Size(1252, 630);
            this.tpLogs.TabIndex = 0;
            this.tpLogs.Text = "Logs";
            this.tpLogs.UseVisualStyleBackColor = true;
            // 
            // lbxLog
            // 
            this.lbxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbxLog.FormattingEnabled = true;
            this.lbxLog.Location = new System.Drawing.Point(3, 3);
            this.lbxLog.Name = "lbxLog";
            this.lbxLog.Size = new System.Drawing.Size(1246, 624);
            this.lbxLog.TabIndex = 3;
            // 
            // tpErrors
            // 
            this.tpErrors.Controls.Add(this.lbxError);
            this.tpErrors.Location = new System.Drawing.Point(4, 22);
            this.tpErrors.Name = "tpErrors";
            this.tpErrors.Padding = new System.Windows.Forms.Padding(3);
            this.tpErrors.Size = new System.Drawing.Size(1252, 630);
            this.tpErrors.TabIndex = 1;
            this.tpErrors.Text = "Errors";
            this.tpErrors.UseVisualStyleBackColor = true;
            // 
            // lbxError
            // 
            this.lbxError.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbxError.FormattingEnabled = true;
            this.lbxError.Location = new System.Drawing.Point(3, 3);
            this.lbxError.Name = "lbxError";
            this.lbxError.Size = new System.Drawing.Size(1246, 624);
            this.lbxError.TabIndex = 4;
            // 
            // tpResult
            // 
            this.tpResult.Controls.Add(this.pnlCharts);
            this.tpResult.Location = new System.Drawing.Point(4, 22);
            this.tpResult.Name = "tpResult";
            this.tpResult.Size = new System.Drawing.Size(1252, 630);
            this.tpResult.TabIndex = 3;
            this.tpResult.Text = "Result";
            this.tpResult.UseVisualStyleBackColor = true;
            // 
            // pnlCharts
            // 
            this.pnlCharts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCharts.Location = new System.Drawing.Point(0, 0);
            this.pnlCharts.Name = "pnlCharts";
            this.pnlCharts.Size = new System.Drawing.Size(1252, 630);
            this.pnlCharts.TabIndex = 6;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1299, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(120, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Data Download";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // cbAlgoName
            // 
            this.cbAlgoName.FormattingEnabled = true;
            this.cbAlgoName.Location = new System.Drawing.Point(85, 10);
            this.cbAlgoName.Name = "cbAlgoName";
            this.cbAlgoName.Size = new System.Drawing.Size(302, 21);
            this.cbAlgoName.TabIndex = 6;
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(1218, 12);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(75, 23);
            this.btnTest.TabIndex = 7;
            this.btnTest.Text = "Test";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // rbExistingAlgo
            // 
            this.rbExistingAlgo.AutoSize = true;
            this.rbExistingAlgo.Checked = true;
            this.rbExistingAlgo.Location = new System.Drawing.Point(18, 10);
            this.rbExistingAlgo.Name = "rbExistingAlgo";
            this.rbExistingAlgo.Size = new System.Drawing.Size(61, 17);
            this.rbExistingAlgo.TabIndex = 8;
            this.rbExistingAlgo.TabStop = true;
            this.rbExistingAlgo.Text = "Existing";
            this.rbExistingAlgo.UseVisualStyleBackColor = true;
            this.rbExistingAlgo.CheckedChanged += new System.EventHandler(this.rbExistingAlgo_CheckedChanged);
            // 
            // rbCustomAlgo
            // 
            this.rbCustomAlgo.AutoSize = true;
            this.rbCustomAlgo.Location = new System.Drawing.Point(18, 33);
            this.rbCustomAlgo.Name = "rbCustomAlgo";
            this.rbCustomAlgo.Size = new System.Drawing.Size(60, 17);
            this.rbCustomAlgo.TabIndex = 8;
            this.rbCustomAlgo.Text = "Custom";
            this.rbCustomAlgo.UseVisualStyleBackColor = true;
            this.rbCustomAlgo.CheckedChanged += new System.EventHandler(this.rbCustomAlgo_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.Controls.Add(this.cbAlgoName);
            this.panel1.Controls.Add(this.rbCustomAlgo);
            this.panel1.Controls.Add(this.btnStart);
            this.panel1.Controls.Add(this.rbExistingAlgo);
            this.panel1.Location = new System.Drawing.Point(12, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(402, 93);
            this.panel1.TabIndex = 9;
            // 
            // EngineWinForm
            // 
            this.ClientSize = new System.Drawing.Size(484, 379);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.button1);
            this.Name = "EngineWinForm";
            this.Load += new System.EventHandler(this.EngineWinForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tpAlgo.ResumeLayout(false);
            this.tpLogs.ResumeLayout(false);
            this.tpErrors.ResumeLayout(false);
            this.tpResult.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabPage tpAlgo;
        private System.Windows.Forms.RichTextBox rtbAlgo;
        private System.Windows.Forms.TabPage tpResult;
        private System.Windows.Forms.FlowLayoutPanel pnlCharts;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.RadioButton rbExistingAlgo;
        private System.Windows.Forms.RadioButton rbCustomAlgo;
        private System.Windows.Forms.Panel panel1;
    }
}