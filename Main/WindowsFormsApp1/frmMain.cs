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

		private void button1_Click(object sender, EventArgs e)
		{
			CStdf stdf = new CStdf(@"D:\P2020 Log\2023-08-26-10-53-30", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "testSTDF.stdf"));
			stdf.DoWork();
		}
	}
}