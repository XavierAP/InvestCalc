using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
			public string Name;
			public double Shares;
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

			// The rest may be later looked up from web service(s) if so available:
			var fetchNames = new List<string>(stocks.Count - prices.Count);

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
					var irow = GetRow(name);
					GetCell(irow, colPrice).Value = price;
					ProcessInput(price, irow);
				}
				else
					fetchNames.Add(name);
			}

			table.ResumeLayout();

			// Fetch prices from web service(s) if available:
			if(Visible) // prevent race condition at startup
				TryFetchPrices(fetchNames);
			else
				Shown += (o,e) => TryFetchPrices(fetchNames);
			
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

			var stk = new Stock { Name = name, Shares = shares };
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
					var irow = GetRow(dlg.StockName);
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
			var value = (string)ea.FormattedValue;
			bool ok = ProcessInput(value, ea.RowIndex);
			ea.Cancel = !ok;
			if(ok) table[ea.ColumnIndex, ea.RowIndex].ToolTipText = null; // clear possible error messages from previous input
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
		/// <param name="today">Present date.</param>
		/// <param name="selected">Selected stocks (rows).</param>
		private void TryCalcReturnAvg(DateTime today, bool selected = false)
		{
			var rows = selected ? (IList)table.SelectedRows : (IList)table.Rows;
			var stocks = new string[rows.Count];
			int i = 0;

			TextBox
				txtValue  = selected ? txtValueSelected : txtValueTotal,
				txtReturn = selected ? txtReturnSelected : txtReturnAvg;

			// In order to calculate the total average return, I need values for all stocks:
			double total = 0;
			foreach(DataGridViewRow row in rows)
			{
				object content = GetCell(row, colValue).Value;
				if(content == null)
				{
					txtValue.Text = txtReturn.Text = null;
					return;
				}
				var value = (double)content;
				Debug.Assert(!(double.IsNaN(value) || double.IsInfinity(value)));

				total += value;
				stocks[i++] = (string)GetCell(row, colStock).Value;
			}

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


		private async void
		TryFetchPrices(List<string> fetchNames)
		{
			if(!retrieving.IsCompleted) return;

			retrieving = FetchPrices(fetchNames); ;
			await retrieving;
		}

		private Task retrieving = Task.CompletedTask;

		private async Task
		FetchPrices(List<string> fetchNames)
		{
			var fetchJobs = (
				from stock_code in db.GetFetchCodes(fetchNames)
				select FetchPrice(stock_code)
				).ToList();

			while(fetchJobs.Any())
			{
				var done = await Task.WhenAny(fetchJobs);
				Debug.Assert(!done.IsFaulted);
				var fetched = done.Result;

				Invoke(new Action( () =>
				{
					var irow = GetRow(fetched.StockName);
					var cell = GetCell(irow, colPrice);

					cell.Value = fetched.Price;

					if(fetched.Error == null)
					{
						cell.ToolTipText = null;
						double value = fetched.Price * stocks[fetched.StockName].Shares;
						GetCell(irow, colValue).Value = value;
						CalcReturn(fetched.StockName, irow, value);
					}
					else
						cell.ToolTipText = fetched.Error.Message;
				} ));

				fetchJobs.Remove(done);
			}
		}

		private async Task<(string StockName, double Price, Exception Error)>
		FetchPrice((string Name, string Code) stock)
		{
			var qt = Quote.Prepare(stock.Code);
			if(qt == null)
				return (stock.Name, double.NaN, new Exception(
					$"Invalid fetch code \"{stock.Code}\"."));
			
			double price = await qt.LoadPrice();
			return (stock.Name, price, qt.Error);
		}


		private DataGridViewCell
		GetCell(int irow, DataGridViewColumn col)
			=> GetCell(table.Rows[irow], col);
		
		private static DataGridViewCell
		GetCell(DataGridViewRow row, DataGridViewColumn col)
			=> row.Cells[col.Index];

		/// <summary>There must be one and only one.</summary>
		private int
		GetRow(string stockName)
		{
			var ids =
				from DataGridViewRow row in table.Rows
				where stockName == (string)GetCell(row, colStock).Value
				select row.Index;

			Debug.Assert(1 == ids.Count());
			return ids.First();
		}
	}
}
