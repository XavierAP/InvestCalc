using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

using JP.Maths;

namespace JP.InvestCalc
{
	internal partial class FormMain :Form
	{
		// GUI decimal places:
		const byte
			precisionMoney = 2, // for money amounts
			precisionPer100 = precisionMoney, // for percentage amounts
			precisionPer1 = precisionPer100 + 2; // for amounts per 1 (i.e. % / 100)

		private class Stock // I want a tuple type by reference with named members; these aren't built into the language (System.Tuple's members are unnamed) like the ones by value are since C# 7.0.
		{
			public double Shares;
			public int IndexGUI;
		}
		private readonly SortedDictionary<string, Stock>
			stocks  = new SortedDictionary<string, Stock>(StringComparer.CurrentCultureIgnoreCase);

		private readonly Data db;

		public FormMain(Data db)
		{
			Debug.Assert(db != null);
			this.db = db;

			InitializeComponent();
			table.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			colStock.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

			UpdateDate();
			FillTable();

			mnuBuy.Click  += (s,e)=> OpRecord(Operation.Buy );
			mnuSell.Click += (s,e)=> OpRecord(Operation.Sell);
			mnuDiv.Click  += (s,e)=> OpRecord(Operation.Div );
			mnuCost.Click += (s,e)=> OpRecord(Operation.Cost);

			table.CellValidating += ValidatePrice;
		}


		/// <summary>Updates today's date and returns it. Call every time returns are recalculated.</summary>
		private DateTime UpdateDate()
		{
			var today = DateTime.Now.Date;
			lblDate.Text = today.ToLongDateString();
			return today;
		}


		/// <summary>Populates the table, assuming it's empty.</summary>
		private void FillTable()
		{
			Debug.Assert(stocks.Count == 0);

			foreach(var stk in db.GetPortfolio())
			{
				var (name, shares) = stk;
				Debug.Assert(shares >= 0); // database sanity check
				AddStock(name, shares);
			}
		}


		/// <summary>Adds to the portfolio a <see cref="Stock"/> that wasn't already owned.</summary>
		/// <returns>The new <see cref="Stock"/>.</returns>
		private Stock AddStock(string name, double shares)
		{
			var i =
			table.Rows.Add(name, shares);

			var stk = new Stock { IndexGUI = i, Shares = shares };
			stocks.Add(name, stk);
			return stk;
		}


		/// <summary>Lets the user record an operation.</summary>
		private void OpRecord(Operation op)
		{
			using(var dlg = new FormOp(op,
				stocks.ToDictionary(stk => stk.Key, stk => stk.Value.Shares)) )
			{
				var ans = dlg.ShowDialog(this);
				if(ans != DialogResult.OK) return;

				bool already = stocks.ContainsKey(dlg.StockName);
				if(already)
				{
					var stk = stocks[dlg.StockName];
					stk.Shares += dlg.Shares;
					// Update GUI:
					var irow = stk.IndexGUI;
					GetCell(irow, colShares).Value = stk.Shares;
					GetCell(irow, colReturn).Value = txtReturnAvg.Text = null; // just in case this row's return is calculated, clear it
				}
				else // new stock in portfolio
				{
					AddStock(dlg.StockName, dlg.Shares);
					txtReturnAvg.Text = null; // new stock with unknown price prevents average calculation
				}
				db.OpRecord(!already, dlg.StockName, dlg.Date, dlg.Shares, dlg.Total, dlg.Comment);
			}
		}


		/// <summary>Handles user input of prices, and if valid triggers return calculation.</summary>
		private void ValidatePrice(object sender, DataGridViewCellValidatingEventArgs ea)
		{
			if(ea.ColumnIndex != colPrice.Index)
			{
				Debug.Assert(table.Columns[ea.ColumnIndex].ReadOnly);
				return; // nothing to do.
			}

			string input = ea.FormattedValue.ToString();
			if(string.IsNullOrEmpty(input)) return; // blank entry or tabbing out mean to cancel input

			bool ok =
			double.TryParse(input, out double price);
			if(!ok)
			{
				ea.Cancel = true;
				return;
			}
			if(price < 0 || double.IsNaN(price) || double.IsInfinity(price))
			{
				MessageBox.Show(this, "Prices must be positive, real numbers.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				ea.Cancel = true;
				return;
			}

			// Update value:
			var stockName = (string)GetCell(ea.RowIndex, colStock).Value;
			double shares = stocks[stockName].Shares;
			Debug.Assert(shares >= 0);
			Debug.Assert(shares == (double)GetCell(ea.RowIndex, colShares).Value);
			GetCell(ea.RowIndex, colValue).Value = Math.Round(price * shares, precisionMoney);

			// Finally:
			CalcReturn(stockName, ea.RowIndex, price, shares);
		}
		
		
		private void CalcReturn(string stockName, int irow, double price, double shares)
		{
			var today = UpdateDate();

			GetCell(irow, colReturn).Value = Money.SolveRateInvest(
				db.GetFlows(stockName), (price * shares, today), precisionPer1 // Data.GetFlows() will never return emtpy: from such a database there would have appeared no row in the DataGridView, to trigger the event this call is coming from.
				).ToString("P"+precisionPer100);

			TryCalcReturnAvg(today); // try to calculate the global average return
		}
		
		private void TryCalcReturnAvg(DateTime today)
		{
			// In order to calculate the total average return, I need values for all stocks:
			double total = 0;
			foreach(DataGridViewRow row in table.Rows)
			{
				object content = GetCell(row, colValue).Value;
				if(content == null) return;
				var value = (double)content;
				Debug.Assert(!(double.IsNaN(value) || double.IsInfinity(value)));

				total += value;
			}

			txtTotal.Text = total.ToString("C");

			txtReturnAvg.Text = Money.SolveRateInvest(
				db.GetFlows(), (total, today), precisionPer1
				).ToString("P"+precisionPer100);
		}


		private DataGridViewCell
		GetCell(int irow, DataGridViewColumn col)
			=> GetCell(table.Rows[irow], col);
		
		private static DataGridViewCell
		GetCell(DataGridViewRow row, DataGridViewColumn col)
			=> row.Cells[col.Index];
	}
}
