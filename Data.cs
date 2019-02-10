using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using JP.SQLite;

namespace JP.InvestCalc
{
	internal class Data
	{
		private readonly
		SQLiteConnector connection;

		public Data(SQLiteConnector connection)
		{
			Debug.Assert(connection != null);
			this.connection = connection;

			Dirty = true;
		}


		/// <summary>Returns all the stocks in the database and how many shares are owned at the moment.</summary>
		public IEnumerable<(string StockName, double TotalShares)>
		GetPortfolio()
		{
			using(var table = connection.Select(@"
SELECT Stocks.name, total(Flows.shares) as shares
from Stocks left join Flows
on Flows.stock == Stocks.id
group by Stocks.id
order by name"))
			{
				Debug.Assert(
					"name"   == table.Columns[0].ColumnName.ToLower() &&
					"shares" == table.Columns[1].ColumnName.ToLower() );

				foreach(DataRow record in table.Rows)
					yield return ( (string)record[0], (double)record[1] );
			}

			Dirty = false;
		}


		/// <summary>Records a new operation into the database.</summary>
		/// <param name="newStock">Whether this stock was traded before.</param>
		/// <param name="stockName">What.</param>
		/// <param name="day">When.</param>
		/// <param name="shares">How many.</param>
		/// <param name="money">How much.</param>
		/// <param name="comment">Description of the operation for the user's own recollection.</param>
		public void
		OpRecord(bool newStock, string stockName, DateTime day, double shares, double money, string comment)
		{
			var sql = new List<string>(2);

			if(newStock) sql.Add($"INSERT into Stocks(name) values('{stockName}')");

			sql.Add($@"
INSERT into Flows values (
{day.ToUniversalTime().Ticks},
(select id from Stocks where name = '{stockName}'),
{shares}, {money},
{(string.IsNullOrWhiteSpace(comment) ? "NULL" : $"'{comment}'")}
)");
			connection.Write(sql);
		}


		/// <summary>Gets cash flows from the database.</summary>
		/// <param name="stockNames">What stocks to restrict the retrieval to.
		/// If empty or null, flows from ALL stocks are returned.</param>
		public IEnumerable<(double Cash, DateTime Day)>
		GetFlows(params string[] stockNames)
		{
			var sql = new StringBuilder(@"
SELECT flow, utcDate
from Flows, Stocks ON Flows.stock == Stocks.id
");
			if(stockNames != null && stockNames.Length > 0)
				sql.Append("where ").Append(string.Join(" OR ",
					from name in stockNames
					select $"name == '{name}'"));

			using(var table = connection.Select(sql.ToString()))
				return from DataRow record in table.Rows select (
					(double)record[0],
					new DateTime((long)record[1], DateTimeKind.Utc) );
		}


		/// <summary>Retrieves the full details of the flows
		/// between given dates concerning given stocks,
		/// and populates a <see cref="DataGridView"/> with them.</summary>
		/// <returns>A populated array of <see cref="DataGridViewRow"/>s,
		/// ordered chronologically according to the database.
		/// The same rows are already added into the <see cref="DataGridView"/>,
		/// with this same initial order.</returns>
		internal DataGridViewRow[]
		GetFlowDetails(DataGridView guiTable, string[] stockNames, DateTime dateFrom, DateTime dateTo)
		{
			dateFrom = dateFrom.Date;
			dateTo   = dateTo  .Date;
			Debug.Assert(dateFrom <= dateTo);

			var sql = new StringBuilder(@"
SELECT Flows.rowid, utcDate, name, shares, flow, comment
from Flows, Stocks ON Flows.stock == Stocks.id
");
			sql.AppendLine(string.Format("where utcDate >= {0} AND utcDate <= {1}",
				dateFrom.ToUniversalTime().Ticks, dateTo.ToUniversalTime().Ticks));

			if(stockNames != null && stockNames.Any())
				sql.Append("AND ( ").Append(string.Join(" OR ",
					from name in stockNames
					select $"name == '{name}'")).AppendLine(" )");

			sql.Append("order by utcDate, shares DESC"); // TL;DR why order by shares DESC: corner case of several operations, with the same stock, in the same day. Chronological order may be lots because dates are rounded down to days; and it would create an absurd history, if the user deleted manually (see DeleteFlows, FormHistory.DoDelete and FormHistory.Table_CellMouseDown) a flow buying shares, so that later flows selling put the total owned into negative.

			Debug.Assert(
				guiTable.Columns.Count >= 6 &&
				guiTable.Columns[0].Name == "colDate"    &&
				guiTable.Columns[1].Name == "colStock"   &&
				guiTable.Columns[2].Name == "colShares"  &&
				guiTable.Columns[3].Name == "colFlow"    &&
				guiTable.Columns[4].Name == "colPrice"   &&
				guiTable.Columns[5].Name == "colComment"
				);

			DataGridViewRow[] guiRowsOrdered;
			int k = 0;
			using(var table = connection.Select(sql.ToString()))
			{
				guiRowsOrdered = new DataGridViewRow[table.Rows.Count];
				foreach(DataRow record in table.Rows)
				{
					var date = new DateTime((long)record[1], DateTimeKind.Utc).ToLocalTime().ToShortDateString();
					var stockName = (string)record[2];
					var shares = (double)record[3];
					var flow = (double)record[4];

					double priceAvg = Math.Round(-flow / shares, FormMain.precisionMoney);

					string comment = record[5] == DBNull.Value ? null : (string)record[5];

					int i =
					guiTable.Rows.Add(date, stockName, shares, flow, priceAvg, comment);
					var guiRow = guiTable.Rows[i];
					guiRow.Tag = record[0]; // store database rowid internally
					guiRowsOrdered[k] = guiRow;
					++k;
				}
				Debug.Assert(k == guiRowsOrdered.Length);
			}
			return guiRowsOrdered;
		}


		/// <summary>Whether the database has been modified and
		/// it is not yet updated on <see cref="FormMain"/>.</summary>
		public bool Dirty { get; private set; }


		/// <summary>Deletes the rows currently selected in the DataGridView.</summary>
		public void
		DeleteFlows(DataGridViewSelectedRowCollection guiRows)
		{
			Debug.Assert(guiRows != null && guiRows.Count > 0);

			var sql = string.Format("DELETE from Flows where rowid IN ( {0} )", string.Join(", ",
				from DataGridViewRow row in guiRows select (long)row.Tag
				));
			connection.Write(sql);
			Dirty = true;
		}


		/// <summary>Parses CSV text ("comma-separated values," although the separator may be other than the comma)
		/// and saves the resulting rows into the Flows database table.</summary>
		/// <returns>Number of rows read; or negative on failure due to bad CSV data or format.</returns>
		internal int
		ImportFlows(string csv, string separator)
		{
			int n = 0;
			var records = new Dictionary<string, List<(DateTime Date, double Shares, double Flow, string Comment)>>();

			var seps = new[] { separator };
			string line;
			using(var cursor = new StringReader(csv))
				while(null != (line = cursor.ReadLine()))
				{
					if(string.IsNullOrWhiteSpace(line)) continue; // alow and skip empty lines

					var values = line.Split(seps,
						StringSplitOptions.RemoveEmptyEntries); // allow the user to have entered e.g. several tabs in a row for visual reasons; only the last column (comment) may optionally be empty

					if(values.Length < ImportFlowsMinCols)
					{
						MessageBox.Show("Cannot parse columns from line: " + line,
							Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return -1;
					}

					int i = 0;

					var dateText = values[i].Trim();
					if(!DateTime.TryParse(dateText, out DateTime date))
					{
						ImportFlowsError(dateText, "date");
						return -1;
					}
					Debug.Assert(date.Kind == DateTimeKind.Unspecified); // from TryParse(); considered local
					date = date.Date.ToUniversalTime();

					string stock = values[++i].Trim();

					var sharesText = values[++i].Trim();
					if(!double.TryParse(sharesText, out double shares))
					{
						ImportFlowsError(sharesText, "number");
						return -1;
					}

					var flowText = values[++i].Trim();
					if(!double.TryParse(flowText, out double flow))
					{
						ImportFlowsError(flowText, "number");
						return -1;
					}
					
					var comment = values.Length > ++i ? // comments are optional
						values[i].Trim() : null;

					if(!records.TryGetValue(stock, out var stockRecords))
						records.Add(stock,
						stockRecords = new List<(DateTime Date, double Shares, double Flow, string Comment)>(csv.Length / 20 - n)
						);
					stockRecords.Add((date, shares, flow, comment));

					++n;
				}

			// Order operations chronologically:
			foreach(var rec in records.Values)
				rec.Sort(ImportFlowsSort); // ORDER by utcDate, shares DESC

			// Container for SQL statements:
			var sql = new List<string>(n + records.Count); // Capacity: number of Flows to import plus 1 for each Stock INSERT or IGNORE

			/* We must now check that the records are not absurd
			i.e. that at no time more shares are sold than owned.
			Step 1.- Learn how many were owned before the first operation to be imported:
			*/
			foreach(var stockRecords in records)
			{
				double sharesOwned;
				string stockName = stockRecords.Key;
				using(var query = connection.Select($@"
SELECT total(Flows.shares) as shares
from Stocks left join Flows
on Flows.stock == Stocks.id
where name == '{stockName}'
and utcDate <= {stockRecords.Value[0].Date.Ticks}
group by Stocks.id"))
				{
					if(query.Rows.Count > 0)
					{
						Debug.Assert(1 == query.Rows.Count);
						sharesOwned = (double)query.Rows[0][0];
					}
					else
						sharesOwned = 0;
				}
				sql.Add($"INSERT or IGNORE into Stocks(name) values('{stockName}')");

				// 2.- Check every record:
				Debug.Assert(sharesOwned >= 0);
				foreach(var rec in stockRecords.Value)
				{
					if(-rec.Shares > sharesOwned)
					{
						MessageBox.Show(
$@"Cannot sell more shares than you own! Imported record
of {stockName} on {rec.Date.ToLocalTime().ToShortDateString()}
selling {-rec.Shares} while owning only {sharesOwned} at the time .", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

						return -2;
					}
					sharesOwned += rec.Shares;
					Debug.Assert(sharesOwned >= 0);

					// 3.- Add it to the commit:
					sql.Add($@"
INSERT into Flows values (
{rec.Date.Ticks},
(select id from Stocks where name = '{stockName}'),
{rec.Shares}, {rec.Flow},
{(string.IsNullOrWhiteSpace(rec.Comment) ? "NULL" : $"'{rec.Comment}'")}
)");
				}
			}

			// All Korrect, finally! write to the database:
			connection.Write(sql);
			return n;
		}

		public const int ImportFlowsMinCols = 4;

		private static int
		ImportFlowsSort((DateTime Date, double Shares, double Flow, string Comment) a,
			(DateTime Date, double Shares, double Flow, string Comment) b)
		{
			if(a.Date < b.Date)
				return -1;
			else if(a.Date > b.Date)
				return 1;
			else if(a.Shares > b.Shares)
				return -1;
			else if(a.Shares < b.Shares)
				return 1;
			else
				return 0;
		}

		private static void
		ImportFlowsError(string text, string parseType) => MessageBox.Show(
			$"Cannot parse as {parseType}:\n{text}",
			Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	}
}
