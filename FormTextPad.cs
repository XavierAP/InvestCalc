using System;
using System.Windows.Forms;

namespace JP.InvestCalc
{
	internal partial class FormTextPad :Form
	{
		public FormTextPad(bool readOnly, string headers, string content)
		{
			InitializeComponent();

			lblHeaders.Text = headers;
			txt.ReadOnly = readOnly;
			txt.Text = content;

			if(readOnly)
			{
				txt.SelectAll();
				Clipboard.SetText(content);
				if(showHelpOutput)
				{
					Shown += PromptHelpOutput;
					showHelpOutput = false;
				}
			}
			else if(showHelpInput)
			{
				Shown += PromptHelpInput;
				showHelpInput = false;
			}
		}

		private static bool
			showHelpOutput = true,
			showHelpInput  = true;

		private void PromptHelpOutput(object sender, EventArgs ea)
		{
			MessageBox.Show(this, "Copied to clipboard. You can paste directly into Excel.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
			Shown -= PromptHelpOutput;
		}

		private void PromptHelpInput(object sender, EventArgs ea)
		{
			MessageBox.Show(this, "Enter the CSV into this Window, then close it to continue importing.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
			Shown -= PromptHelpInput;
		}
	}
}
