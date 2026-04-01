namespace STDF
{
	partial class Form1
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
			if(disposing && (components != null))
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
			this.btnGenerateStdf  = new System.Windows.Forms.Button();
			this.btnAnalyzeSource = new System.Windows.Forms.Button();
			this.txtInputPath     = new System.Windows.Forms.TextBox();
			this.SuspendLayout();

			// 
			// btnGenerateStdf
			// 
			this.btnGenerateStdf.Location                =  new System.Drawing.Point(22, 58);
			this.btnGenerateStdf.Name                    =  "btnGenerateStdf";
			this.btnGenerateStdf.Size                    =  new System.Drawing.Size(96, 23);
			this.btnGenerateStdf.TabIndex                =  0;
			this.btnGenerateStdf.Text                    =  "產生 STDF";
			this.btnGenerateStdf.UseVisualStyleBackColor =  true;
			this.btnGenerateStdf.Click                   += new System.EventHandler(this.btnGenerateStdf_Click);

			// 
			// btnAnalyzeSource
			// 
			this.btnAnalyzeSource.Location                =  new System.Drawing.Point(22, 29);
			this.btnAnalyzeSource.Name                    =  "btnAnalyzeSource";
			this.btnAnalyzeSource.Size                    =  new System.Drawing.Size(96, 23);
			this.btnAnalyzeSource.TabIndex                =  1;
			this.btnAnalyzeSource.Text                    =  "分析資料";
			this.btnAnalyzeSource.UseVisualStyleBackColor =  true;
			this.btnAnalyzeSource.Click                   += new System.EventHandler(this.btnAnalyzeSource_Click);

			// 
			// txtInputPath
			// 
			this.txtInputPath.Location =  new System.Drawing.Point(124, 31);
			this.txtInputPath.Name     =  "txtInputPath";
			this.txtInputPath.Size     =  new System.Drawing.Size(100, 22);
			this.txtInputPath.TabIndex =  2;
			this.txtInputPath.KeyDown  += new System.Windows.Forms.KeyEventHandler(this.txtInputPath_KeyDown);

			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize          = new System.Drawing.Size(556, 222);
			this.Controls.Add(this.txtInputPath);
			this.Controls.Add(this.btnAnalyzeSource);
			this.Controls.Add(this.btnGenerateStdf);
			this.Name = "Form1";
			this.Text = "frmMain";
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		private System.Windows.Forms.TextBox txtInputPath;

		private System.Windows.Forms.Button btnAnalyzeSource;

		private System.Windows.Forms.Button btnGenerateStdf;

#endregion
	}
}