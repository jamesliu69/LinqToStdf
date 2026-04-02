
namespace WindowsFormsApp1
{
    partial class frmMain
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
			this.btnGenerateStdf = new System.Windows.Forms.Button();
			this.txtInputFolder = new System.Windows.Forms.TextBox();
			this.txtOutputPath = new System.Windows.Forms.TextBox();
			this.lblInputFolder = new System.Windows.Forms.Label();
			this.lblOutputPath = new System.Windows.Forms.Label();
			this.btnBrowseOutputPath = new System.Windows.Forms.Button();
			this.btnBrowseInputFolder = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// btnGenerateStdf
			// 
			this.btnGenerateStdf.Location = new System.Drawing.Point(340, 177);
			this.btnGenerateStdf.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnGenerateStdf.Name = "btnGenerateStdf";
			this.btnGenerateStdf.Size = new System.Drawing.Size(98, 36);
			this.btnGenerateStdf.TabIndex = 0;
			this.btnGenerateStdf.Text = "產生 STDF";
			this.btnGenerateStdf.UseVisualStyleBackColor = true;
			this.btnGenerateStdf.Click += new System.EventHandler(this.btnGenerateStdf_Click);
			// 
			// txtInputFolder
			// 
			this.txtInputFolder.Location = new System.Drawing.Point(12, 41);
			this.txtInputFolder.Name = "txtInputFolder";
			this.txtInputFolder.Size = new System.Drawing.Size(426, 22);
			this.txtInputFolder.TabIndex = 1;
			// 
			// txtOutputPath
			// 
			this.txtOutputPath.Location = new System.Drawing.Point(12, 98);
			this.txtOutputPath.Name = "txtOutputPath";
			this.txtOutputPath.Size = new System.Drawing.Size(426, 22);
			this.txtOutputPath.TabIndex = 2;
			// 
			// lblInputFolder
			// 
			this.lblInputFolder.AutoSize = true;
			this.lblInputFolder.Location = new System.Drawing.Point(12, 23);
			this.lblInputFolder.Name = "lblInputFolder";
			this.lblInputFolder.Size = new System.Drawing.Size(65, 12);
			this.lblInputFolder.TabIndex = 3;
			this.lblInputFolder.Text = "選取資料夾";
			// 
			// lblOutputPath
			// 
			this.lblOutputPath.AutoSize = true;
			this.lblOutputPath.Location = new System.Drawing.Point(12, 83);
			this.lblOutputPath.Name = "lblOutputPath";
			this.lblOutputPath.Size = new System.Drawing.Size(53, 12);
			this.lblOutputPath.TabIndex = 4;
			this.lblOutputPath.Text = "輸出檔案";
			// 
			// btnBrowseOutputPath
			// 
			this.btnBrowseOutputPath.Location = new System.Drawing.Point(443, 98);
			this.btnBrowseOutputPath.Margin = new System.Windows.Forms.Padding(2);
			this.btnBrowseOutputPath.Name = "btnBrowseOutputPath";
			this.btnBrowseOutputPath.Size = new System.Drawing.Size(41, 24);
			this.btnBrowseOutputPath.TabIndex = 5;
			this.btnBrowseOutputPath.Text = "...";
			this.btnBrowseOutputPath.UseVisualStyleBackColor = true;
			this.btnBrowseOutputPath.Click += new System.EventHandler(this.btnBrowseOutputPath_Click);
			// 
			// btnBrowseInputFolder
			// 
			this.btnBrowseInputFolder.Location = new System.Drawing.Point(443, 41);
			this.btnBrowseInputFolder.Margin = new System.Windows.Forms.Padding(2);
			this.btnBrowseInputFolder.Name = "btnBrowseInputFolder";
			this.btnBrowseInputFolder.Size = new System.Drawing.Size(41, 24);
			this.btnBrowseInputFolder.TabIndex = 6;
			this.btnBrowseInputFolder.Text = "...";
			this.btnBrowseInputFolder.UseVisualStyleBackColor = true;
			this.btnBrowseInputFolder.Click += new System.EventHandler(this.btnBrowseInputFolder_Click);
			// 
			// frmMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(493, 248);
			this.Controls.Add(this.btnBrowseInputFolder);
			this.Controls.Add(this.btnBrowseOutputPath);
			this.Controls.Add(this.lblOutputPath);
			this.Controls.Add(this.lblInputFolder);
			this.Controls.Add(this.txtOutputPath);
			this.Controls.Add(this.txtInputFolder);
			this.Controls.Add(this.btnGenerateStdf);
			this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.Name = "frmMain";
			this.Text = "STDF 產生器";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.Button btnGenerateStdf;
		private System.Windows.Forms.TextBox txtInputFolder;
		private System.Windows.Forms.TextBox txtOutputPath;
		private System.Windows.Forms.Label lblInputFolder;
		private System.Windows.Forms.Label lblOutputPath;
		private System.Windows.Forms.Button btnBrowseOutputPath;
		private System.Windows.Forms.Button btnBrowseInputFolder;
	}
}

