using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace JP.InvestCalc
{
	internal partial class FormHistory :Form
	{
		private readonly Data db;

		public FormHistory(Data db, IEnumerable<string> stocks, DateTime dateFrom, DateTime dateTo)
		{
			Debug.Assert(db != null);
			this.db = db;

			InitializeComponent();

			/* We use these events just to control what menu options are available
			 * or grayed out upon right-click, depending on the selection.
			 * In the chronological order they are triggered: */
			table.MouseDown += Table_MouseDown;
			table.CellMouseDown += Table_CellMouseDown;

			mnuDelete.Click += DoDelete;

			colShares.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			colFlow  .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			colPrice .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

			colFlow.DefaultCellStyle.Format =
			colPrice.DefaultCellStyle.Format =
				"C" + FormMain.precisionMoney;

			db.GetHistory(ref table, stocks, dateFrom, dateTo);
		}

		private void Table_MouseDown(object sender, MouseEventArgs ea)
		{
			mnuCommands.Enabled = false; // gray menu out by default, in case clicked on an empty area; just afterwards, CellMouseDown chooses what to enable.
		}

		private void Table_CellMouseDown(object sender, DataGridViewCellMouseEventArgs ea)
		{
			/* Right-clicking on a DataGridView does not change selection by default.
			 * We want it to select the right-clicked row, if not already selected,
			 * unless there is already another multiple selection, which may be annoying to lose. */
			if(ea.Button == MouseButtons.Right)
			{
				var rowClicked = table.Rows[ea.RowIndex];
				if(rowClicked.Selected)
				{
					mnuCommands.Enabled = true;
				}
				else if(table.SelectedRows.Count <= 1)
				{
					table.CurrentCell = rowClicked.Cells[ea.ColumnIndex];
					Debug.Assert(rowClicked.Selected);
					mnuCommands.Enabled = true;
				}
			}
		}


		private void DoDelete(object sender, EventArgs ea)
		{
			Debug.Assert(table.SelectedRows.Count > 0);
			
			// Compose confirmation message:
			var msg = table.SelectedRows.Count > 1 ?
				"Are you sure you want to delete ALL the selected records?" :
				"Are you sure you want to delete the selected record?" ;

			var ans = MessageBox.Show(this, msg, "Please confirm",
				MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

			if(ans != DialogResult.Yes) return;

			// Delete from database:
			db.DeleteFlows(table.SelectedRows);

			// Delete from GUI:
			table.SuspendLayout();

			foreach(DataGridViewRow row in table.SelectedRows)
				table.Rows.Remove(row);

			table.ResumeLayout();
		}
	}
}
