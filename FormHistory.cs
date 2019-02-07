using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace JP.InvestCalc
{
	internal partial class FormHistory :Form
	{
		public FormHistory(Data db, IEnumerable<string> stocks, DateTime dateFrom, DateTime dateTo)
		{
			InitializeComponent();

			/* We use these events just to control what menu options are available
			 * or grayed out upon right-click, depending on the selection.
			 * In the chronological order they are triggered: */
			table.MouseDown += Table_MouseDown;
			table.CellMouseDown += Table_CellMouseDown;

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
	}
}
