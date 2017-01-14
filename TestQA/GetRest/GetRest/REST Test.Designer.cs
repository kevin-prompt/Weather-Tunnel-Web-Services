namespace GetRest
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
            this.btnGet = new System.Windows.Forms.Button();
            this.txtURI = new System.Windows.Forms.TextBox();
            this.wwwResponse = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // btnGet
            // 
            this.btnGet.Location = new System.Drawing.Point(12, 12);
            this.btnGet.Name = "btnGet";
            this.btnGet.Size = new System.Drawing.Size(75, 23);
            this.btnGet.TabIndex = 0;
            this.btnGet.Text = "GET";
            this.btnGet.UseVisualStyleBackColor = true;
            this.btnGet.Click += new System.EventHandler(this.btnGet_Click);
            // 
            // txtURI
            // 
            this.txtURI.Location = new System.Drawing.Point(93, 15);
            this.txtURI.Name = "txtURI";
            this.txtURI.Size = new System.Drawing.Size(254, 20);
            this.txtURI.TabIndex = 1;
            // 
            // wwwResponse
            // 
            this.wwwResponse.Location = new System.Drawing.Point(12, 69);
            this.wwwResponse.MinimumSize = new System.Drawing.Size(20, 20);
            this.wwwResponse.Name = "wwwResponse";
            this.wwwResponse.Size = new System.Drawing.Size(250, 217);
            this.wwwResponse.TabIndex = 2;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(573, 519);
            this.Controls.Add(this.wwwResponse);
            this.Controls.Add(this.txtURI);
            this.Controls.Add(this.btnGet);
            this.Name = "Main";
            this.Text = "Test REST Calls";
            this.Load += new System.EventHandler(this.Main_Load);
            this.Resize += new System.EventHandler(this.Main_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGet;
        private System.Windows.Forms.TextBox txtURI;
        private System.Windows.Forms.WebBrowser wwwResponse;
    }
}

