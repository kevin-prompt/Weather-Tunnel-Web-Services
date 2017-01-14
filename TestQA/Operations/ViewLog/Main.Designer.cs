namespace ViewLog
{
    partial class Main
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
            this.label1 = new System.Windows.Forms.Label();
            this.rdoWebService = new System.Windows.Forms.RadioButton();
            this.rdoLocalFile = new System.Windows.Forms.RadioButton();
            this.txtSource = new System.Windows.Forms.TextBox();
            this.btnSource = new System.Windows.Forms.Button();
            this.btnLoad = new System.Windows.Forms.Button();
            this.tsStart = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.fileSource = new System.Windows.Forms.OpenFileDialog();
            this.btnClear = new System.Windows.Forms.Button();
            this.lblRawCnt = new System.Windows.Forms.Label();
            this.tsEnd = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.btnDateAgain = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.fileDestination = new System.Windows.Forms.SaveFileDialog();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 74);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Date Range";
            // 
            // rdoWebService
            // 
            this.rdoWebService.AutoSize = true;
            this.rdoWebService.Location = new System.Drawing.Point(10, 12);
            this.rdoWebService.Name = "rdoWebService";
            this.rdoWebService.Size = new System.Drawing.Size(87, 17);
            this.rdoWebService.TabIndex = 1;
            this.rdoWebService.TabStop = true;
            this.rdoWebService.Text = "Web Service";
            this.rdoWebService.UseVisualStyleBackColor = true;
            // 
            // rdoLocalFile
            // 
            this.rdoLocalFile.AutoSize = true;
            this.rdoLocalFile.Location = new System.Drawing.Point(102, 12);
            this.rdoLocalFile.Name = "rdoLocalFile";
            this.rdoLocalFile.Size = new System.Drawing.Size(70, 17);
            this.rdoLocalFile.TabIndex = 2;
            this.rdoLocalFile.TabStop = true;
            this.rdoLocalFile.Text = "Local File";
            this.rdoLocalFile.UseVisualStyleBackColor = true;
            // 
            // txtSource
            // 
            this.txtSource.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.txtSource.ForeColor = System.Drawing.Color.Silver;
            this.txtSource.Location = new System.Drawing.Point(179, 12);
            this.txtSource.Name = "txtSource";
            this.txtSource.Size = new System.Drawing.Size(437, 20);
            this.txtSource.TabIndex = 3;
            this.txtSource.Text = "Source File";
            this.txtSource.Enter += new System.EventHandler(this.txtSource_Enter);
            this.txtSource.Leave += new System.EventHandler(this.txtSource_Leave);
            // 
            // btnSource
            // 
            this.btnSource.Location = new System.Drawing.Point(622, 9);
            this.btnSource.Name = "btnSource";
            this.btnSource.Size = new System.Drawing.Size(75, 23);
            this.btnSource.TabIndex = 4;
            this.btnSource.Text = "Browse...";
            this.btnSource.UseVisualStyleBackColor = true;
            this.btnSource.Click += new System.EventHandler(this.btnSource_Click);
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(10, 35);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(75, 23);
            this.btnLoad.TabIndex = 5;
            this.btnLoad.Text = "Load Log";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // tsStart
            // 
            this.tsStart.CustomFormat = "MMM dd yyyy - HH:mm";
            this.tsStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.tsStart.Location = new System.Drawing.Point(115, 73);
            this.tsStart.Name = "tsStart";
            this.tsStart.Size = new System.Drawing.Size(146, 20);
            this.tsStart.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(48, 108);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Search";
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(115, 110);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(330, 20);
            this.txtSearch.TabIndex = 8;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(10, 209);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.Size = new System.Drawing.Size(748, 391);
            this.txtLog.TabIndex = 9;
            this.txtLog.WordWrap = false;
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(10, 180);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 10;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // lblRawCnt
            // 
            this.lblRawCnt.AutoSize = true;
            this.lblRawCnt.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRawCnt.Location = new System.Drawing.Point(91, 180);
            this.lblRawCnt.Name = "lblRawCnt";
            this.lblRawCnt.Size = new System.Drawing.Size(78, 20);
            this.lblRawCnt.TabIndex = 11;
            this.lblRawCnt.Text = "Count = 0";
            // 
            // tsEnd
            // 
            this.tsEnd.CustomFormat = "MMM dd yyyy - HH:mm";
            this.tsEnd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.tsEnd.Location = new System.Drawing.Point(299, 73);
            this.tsEnd.Name = "tsEnd";
            this.tsEnd.Size = new System.Drawing.Size(146, 20);
            this.tsEnd.TabIndex = 12;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(274, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(16, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "to";
            // 
            // btnDateAgain
            // 
            this.btnDateAgain.Location = new System.Drawing.Point(460, 72);
            this.btnDateAgain.Name = "btnDateAgain";
            this.btnDateAgain.Size = new System.Drawing.Size(75, 23);
            this.btnDateAgain.TabIndex = 14;
            this.btnDateAgain.Text = "Refresh";
            this.btnDateAgain.UseVisualStyleBackColor = true;
            this.btnDateAgain.Click += new System.EventHandler(this.btnDateAgain_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.Location = new System.Drawing.Point(683, 180);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 15;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(602, 180);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 16;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(770, 612);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnDateAgain);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tsEnd);
            this.Controls.Add(this.lblRawCnt);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tsStart);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.btnSource);
            this.Controls.Add(this.txtSource);
            this.Controls.Add(this.rdoLocalFile);
            this.Controls.Add(this.rdoWebService);
            this.Controls.Add(this.label1);
            this.Name = "Main";
            this.Text = "ViewLog";
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton rdoWebService;
        private System.Windows.Forms.RadioButton rdoLocalFile;
        private System.Windows.Forms.TextBox txtSource;
        private System.Windows.Forms.Button btnSource;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.DateTimePicker tsStart;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.OpenFileDialog fileSource;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label lblRawCnt;
        private System.Windows.Forms.DateTimePicker tsEnd;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnDateAgain;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.SaveFileDialog fileDestination;
    }
}

