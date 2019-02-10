using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JP.InvestCalc
{
	internal partial class FormHistory :Form
	{
		private readonly Data db;

		private readonly DataGridViewRow[] rowsOrdered;

		public FormHistory(Data db, IEnumerable<string> stocks, DateTime dateFrom, DateTime dateTo)
		{
			Debug.Assert(db != null && stocks != null);
			this.db = db;

			var portfolio = stocks.ToArray();
			Debug.Assert(portfolio.Length > 0);
			deleteCheckCache = new HashSet<string>(portfolio.Length);

			InitializeComponent();

			/* We use these events just to control what menu options are available
			 * or grayed out upon right-click, depending on the selection.
			 * In the chronological order they are triggered: */
			table.MouseDown += Table_MouseDown;
			table.CellMouseDown += Table_CellMouseDown;

			mnuDelete.Click += DoDelete;
			mnuExport.Click += DoExport;
			mnuImport.Click += DoImport;

			colShares.DefaultCellStyle.Alignment =
			colFlow  .DefaultCellStyle.Alignment =
			colPrice .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

			colFlow.DefaultCellStyle.Format =
			colPrice.DefaultCellStyle.Format =
				"C" + FormMain.precisionMoney;

			rowsOrdered =
			db.GetFlowDetails(table, portfolio, dateFrom, dateTo);
			// Keeping this order is needed by IsDeleteAllowable(), in case the user reorders the table by clicking on the column headers.
			Debug.Assert(rowsOrdered.Length == table.Rows.Count);

			headers = (
				from c in DataColumns
				select table.Columns[c].HeaderText
				).ToArray();
		}


		private IEnumerable<int> DataColumns =>
			from DataGridViewColumn col in table.Columns
			where col != colPrice // the avg price is derived data for info
			select col.Index;


		private void Table_MouseDown(object sender, MouseEventArgs ea)
		{
			// Gray these options out by default from the context menu, because they aren't valid if the user clicked on an empty area; just afterwards, CellMouseDown chooses what to enable.
			mnuDelete.Enabled =
			mnuExport.Enabled = false;
		}

		private void Table_CellMouseDown(object sender, DataGridViewCellMouseEventArgs ea)
		{
			if(ea.RowIndex < 0) return; // happens -1 if the user clicks on the headers.

			var rowClicked = table.Rows[ea.RowIndex];
			/* Right-clicking on a DataGridView does not change selection by default.
			 * We want it to select the right-clicked row, if not already selected,
			 * unless there is already another multiple selection, which may be annoying to lose. */
			if(ea.Button == MouseButtons.Right)
			{
				if(!rowClicked.Selected && table.SelectedRows.Count <= 1)
				{
					table.CurrentCell = rowClicked.Cells[ea.ColumnIndex];
					Debug.Assert(rowClicked.Selected);
				}
				mnuExport.Enabled = rowClicked.Selected;
			}
			mnuDelete.Enabled = rowClicked.Selected && IsDeleteAllowable();
			mnuDelete.ToolTipText = mnuDelete.Enabled ? null : "Only the chronologically last operation(s) on each stock may be deleted.";
		}
		

		private bool IsDeleteAllowable()
		{
			deleteCheckCache.Clear();
			// Proceed back in time from the last flow:
			for(int i = rowsOrdered.Length - 1; i >= 0; --i)
			{
				var row = rowsOrdered[i];
				if(row.Selected)
				{
					// allow deletion only of the last flow(s) for each stock:
					if(deleteCheckCache.Contains(GetStockName(row)))
						return false;
				}
				else // note down what stocks have been operated chronologically after the records to delete:
					deleteCheckCache.Add(GetStockName(row));
			}
			return true;
		}

		private HashSet<string> deleteCheckCache;

		private string GetStockName(DataGridViewRow row)
			=> (string)row.Cells[colStock.Index].Value;

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
			Debug.Assert(IsDeleteAllowable());
			db.DeleteFlows(table.SelectedRows);

			// Delete from GUI:
			table.SuspendLayout();

			foreach(DataGridViewRow row in table.SelectedRows)
				table.Rows.Remove(row);

			table.ResumeLayout();
		}


		private StringBuilder csv = new StringBuilder();
		private string csvSeparator = Properties.Settings.Default.csvSeparator;

		private void DoExport(object sender, EventArgs ea)
		{
			csv.Clear();

			foreach(DataGridViewRow row in table.Rows) // iterate all .Rows to preserve display order; .SelectedRows may have a different order
			{
				if(!row.Selected) continue;

				foreach(var c in DataColumns)
					csv.Append(row.Cells[c].Value).Append(csvSeparator);

				BackDown(csv, csvSeparator)
					.AppendLine();
			}

			using(var dlg = new FormTextPad(true, headers, csv.ToString()))
				dlg.ShowDialog(this);
		}

		private void DoImport(object sender, EventArgs ea)
		{
			using(var dlg = new FormTextPad(false, headers, null))
			{
				int n;
				do {
					dlg.ShowDialog(this);
					if(dlg.DialogResult == DialogResult.Cancel)
						return;

					n = db.ImportFlows(dlg.Content, csvSeparator);
				}
				while(n <= 0);
				Close(); // the displayed data are no longer up to date after importing
			}
		}


		private static StringBuilder
		BackDown(StringBuilder text, string trail)
		{
			Debug.Assert( text[text.Length - trail.Length] == trail[0] );
			return text.Remove(text.Length - trail.Length, trail.Length);
		}

		private readonly string[] headers;
	}
}
