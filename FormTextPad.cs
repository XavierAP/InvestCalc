using System;
using System.Windows.Forms;

namespace JP.InvestCalc
{
	internal partial class FormTextPad :Form
	{
		public FormTextPad(bool readOnly, string content)
		{
			InitializeComponent();

			txt.ReadOnly = readOnly;
			txt.Text = content;

			if(readOnly)
			{
				txt.SelectAll();
				Clipboard.SetText(content);
				if(showHelp)
				{
					Shown += PromptHelp;
					showHelp = false;
				}
			}
		}

		private static bool showHelp = true;

		private void PromptHelp(object sender, EventArgs ea)
		{
			MessageBox.Show(this, "Copied to clipboard. You can paste directly into Excel.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
			Shown -= PromptHelp;
		}
	}
}
