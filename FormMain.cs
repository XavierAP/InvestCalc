﻿using System;
using System.Collections;
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
		public const byte
			precisionMoney = 2, // for money amounts
			precisionPer100 = precisionMoney, // for percentage amounts
			precisionPer1 = precisionPer100 + 2; // for amounts per 1 (i.e. % / 100)

		private readonly double seedRate = Properties.Settings.Default.seedRate; // needed for the solver

		private sealed class Stock // I want a tuple type by reference with named members; these aren't built into the language (System.Tuple's members are unnamed) like the ones by value are since C# 7.0.
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
			colReturn.DefaultCellStyle.Format = "P" + precisionPer100;

			UpdateDate();
			FillTable();

			mnuBuy.Click  += (s,e)=> OpRecord(Operation.Buy );
			mnuSell.Click += (s,e)=> OpRecord(Operation.Sell);
			mnuDiv.Click  += (s,e)=> OpRecord(Operation.Div );
			mnuCost.Click += (s,e)=> OpRecord(Operation.Cost);

			mnuHistory.Click += OpsHistory;

			table.CellValidating += ValidatingInput;
			table.SelectionChanged += SelectionChanged;
			SelectionChanged(null, null);
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
			table.SuspendLayout();

			// Remember prices if the user has entered any:
			var prices = (
				from DataGridViewRow row in table.Rows
				let value = GetCell(row, colPrice).Value
				where value != null
				select ( Name: (string)GetCell(row, colStock).Value, Price: value )
				).ToDictionary( stk=>stk.Name, stk=>stk.Price );

			table.Rows.Clear();
			stocks.Clear();

			foreach(var entry in db.GetPortfolio())
			{
				var (name, shares) = entry;
				Debug.Assert(shares >= 0); // database sanity check
				var stk =
				AddStock(name, shares);

				if(prices.ContainsKey(name))
				{
					var price = (string)prices[name];
					GetCell(stk.IndexGUI, colPrice).Value = price;
					ProcessInput(price, stk.IndexGUI);
				}
			}

			table.ResumeLayout();
			
			if(!Visible && stocks.Count == 0) // program startup with empty portfolio
				Shown += PromptHelp;
		}

		private void PromptHelp(object sender, EventArgs ea)
		{
			MessageBox.Show(this,
				"Right-click on the table for options and commands.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);

			Shown -= PromptHelp;
		}


		/// <summary>Adds to the portfolio a <see cref="Stock"/> that wasn't already owned.</summary>
		/// <returns>The new <see cref="Stock"/>.</returns>
		private Stock AddStock(string name, double shares)
		{
			var i =
			table.Rows.Add(name, shares);

			var stk = new Stock { IndexGUI = i, Shares = shares };
			stocks.Add(name, stk);

			if(shares == 0) // can calculate, value is known regardless of price
				ProcessInput(null, i);
			else // average calculation no longer valid
				txtReturnAvg.Text = null;

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

					db.OpRecord(false, dlg.StockName, dlg.Date, dlg.Shares, dlg.Total, dlg.Comment);
					ProcessInput((string)GetCell(irow, colPrice).Value, irow); // only after the database is updated!
				}
				else // new stock in portfolio
				{
					AddStock(dlg.StockName, dlg.Shares);
					db.OpRecord(true, dlg.StockName, dlg.Date, dlg.Shares, dlg.Total, dlg.Comment);
				}
			}
		}


		/// <summary>Lets the user browse past operations.</summary>
		private void OpsHistory(object sender, EventArgs ea)
		{
			int n = table.Rows.Count;
			var selected = new bool[n];
			var stockNames = new string[n];
			int multi = 0;
			int i = 0;
			foreach(DataGridViewRow row in table.Rows)
			{
				stockNames[i] = (string)GetCell(i, colStock).Value;

				if(selected[i] = row.Selected)
					++multi;

				++i;
			}

			Debug.Assert(i == n && stockNames.Length == stocks.Count);
			using(var dlg = new FormHistoryFilter(db, stockNames, multi>1?selected:null))
				dlg.ShowDialog(this);

			if(db.Dirty) FillTable();
		}


		/// <summary>Handles user input of prices, and if valid triggers return calculation.</summary>
		/// <returns>False to force the user to correct or cancel input;
		/// true in case of valid input or no action required.</returns>
		private bool ProcessInput(string priceInput, int irow)
		{
			var stockName = (string)GetCell(irow, colStock).Value;
			double shares = stocks[stockName].Shares;

			bool needPrice = shares != 0; // if shares == 0, the present value is known (0) regardless of price.
			double value;

			if(needPrice)
			{
				if(string.IsNullOrEmpty(priceInput))
					return true; // blank entry or tabbing out mean to cancel input

				bool ok =
				double.TryParse(priceInput, out double price);
				if(!ok)
					return false;

				if(price < 0 || double.IsNaN(price) || double.IsInfinity(price))
				{
					MessageBox.Show(this, "Prices must be positive, real numbers.", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}

				value = price * shares;
			}
			else value = 0;

			// Update value:
			Debug.Assert(shares >= 0);
			Debug.Assert(shares == (double)GetCell(irow, colShares).Value);
			GetCell(irow, colValue).Value = Math.Round(value, precisionMoney);

			// Finally:
			CalcReturn(stockName, irow, value);
			return true;
		}

		/// <summary>Handles user input.</summary>
		private void ValidatingInput(object sender, DataGridViewCellValidatingEventArgs ea)
		{
			if(ea.ColumnIndex != colPrice.Index)
			{
				Debug.Assert(table.Columns[ea.ColumnIndex].ReadOnly);
				return; // nothing to do.
			}

			ea.Cancel = !ProcessInput((string)ea.FormattedValue, ea.RowIndex);
		}


		private void CalcReturn(string stockName, int irow, double value)
		{
			var today = UpdateDate();

			GetCell(irow, colReturn).Value = Money.SolveRateInvest(
				db.GetFlows(stockName), (value, today), precisionPer1, seedRate
				);

			TryCalcReturnAvg(today); // try to calculate the global average return
		}

		/// <summary>Calculates the average return of several stocks
		/// if their values are known.</summary>
		/// <param name="today">Presemt date.</param>
		/// <param name="selected">Selected stocks (rows).</param>
		private void TryCalcReturnAvg(DateTime today, bool selected = false)
		{
			var rows = selected ? (IList)table.SelectedRows : (IList)table.Rows;
			var stocks = new string[rows.Count];
			int i = 0;

			// In order to calculate the total average return, I need values for all stocks:
			double total = 0;
			foreach(DataGridViewRow row in rows)
			{
				object content = GetCell(row, colValue).Value;
				if(content == null) return;
				var value = (double)content;
				Debug.Assert(!(double.IsNaN(value) || double.IsInfinity(value)));

				total += value;
				stocks[i++] = (string)GetCell(row, colStock).Value;
			}
			Debug.Assert(i == rows.Count);

			TextBox
				txtValue  = selected ? txtValueSelected : txtValueTotal,
				txtReturn = selected ? txtReturnSelected : txtReturnAvg;

			txtValue.Text = total.ToString("C" + precisionMoney);

			txtReturn.Text = Money.SolveRateInvest(
				db.GetFlows(stocks), (total, today), precisionPer1, seedRate
				).ToString(colReturn.DefaultCellStyle.Format);
		}


		private void SelectionChanged(object sender, EventArgs ea)
		{
			bool multi = table.SelectedRows.Count > 1;

			txtValueSelected.Visible =
			lblValueSelected.Visible =
			txtReturnSelected.Visible =
			lblReturnSelected.Visible = multi;

			if(multi) TryCalcReturnAvg(UpdateDate(), true);
		}


		private DataGridViewCell
		GetCell(int irow, DataGridViewColumn col)
			=> GetCell(table.Rows[irow], col);
		
		private static DataGridViewCell
		GetCell(DataGridViewRow row, DataGridViewColumn col)
			=> row.Cells[col.Index];
	}
}
