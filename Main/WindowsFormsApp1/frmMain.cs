using STDF;
using System;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
	public partial class frmMain : Form
	{
		public frmMain()
		{
			InitializeComponent();
		}

		private void GenerateStdfButton_Click(object sender, EventArgs e)
		{
			// 以目前指定的 P2020 測試資料夾產生 STDF 檔案。
			string outputPath    = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "testSTDF.stdf");
			CStdf  stdfConverter = new CStdf(@"D:\P2020 Log\2023-08-26-10-53-30", outputPath);
			stdfConverter.DoWork();
		}
	}
}