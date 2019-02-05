using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace JP.InvestCalc
{
	internal partial class FormHistoryFilter :Form
	{
		private readonly Data db;

		public FormHistoryFilter(Data db, IEnumerable<string> portfolio)
		{
			Debug.Assert(db != null);
			this.db = db;

			InitializeComponent();
			
			listStocks.Items.AddRange(portfolio.ToArray());
			pickDateFrom.Value = pickDateFrom.MinDate;

			SelectAll();

			KeyPreview = true;
			KeyDown += Form_KeyDown;

			RestrictDate(pickDateFrom);
			RestrictDate(pickDateTo  );
			pickDateFrom.ValueChanged += (s,e)=> RestrictDate(pickDateFrom);
			pickDateTo  .ValueChanged += (s,e)=> RestrictDate(pickDateTo  );

			btnOK.Click += Launch;
		}

		private void RestrictDate(DateTimePicker pickControl)
		{
			if(pickControl == pickDateFrom)
				pickDateTo.MinDate = pickDateFrom.Value;
			else if(pickControl == pickDateTo)
				pickDateFrom.MaxDate = pickDateTo.Value;
			else Debug.Assert(false);
		}

		private void SelectAll()
		{
			for(int i = 0; i < listStocks.Items.Count; ++i)
				listStocks.SetSelected(i, true);
		}

		private void Form_KeyDown(object sender, KeyEventArgs ea)
		{
			const Keys keySelectAll = Keys.Control | Keys.A;

			if(ea.KeyData == keySelectAll)
				SelectAll();
		}

		private void Launch(object sender, EventArgs ea)
		{
			if(listStocks.SelectedItems.Count <= 0)
			{
				MessageBox.Show(this, "No stocks selected.\nPress Ctrl+A to select all.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
				SelectAll();
				return;
			}

			var stocks = (
				from object item in listStocks.SelectedItems
				select (string)item
				);

			using(var dlg = new FormHistory(db, stocks, pickDateFrom.Value, pickDateTo.Value))
				dlg.ShowDialog(this);
		}
	}
}
