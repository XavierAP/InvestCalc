using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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


		/// <summary>Populates a <see cref="DataGridView"/> with full details of
		/// the operations done between given dates concerning given stocks.</summary>
		public void
		GetHistory(ref DataGridView guiTable,
			IEnumerable<string> stockNames, DateTime dateFrom, DateTime dateTo)
		{
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

			sql.Append("order by utcDate");

			Debug.Assert(
				guiTable.Columns.Count >= 6 &&
				guiTable.Columns[0].Name == "colDate"    &&
				guiTable.Columns[1].Name == "colStock"   &&
				guiTable.Columns[2].Name == "colShares"  &&
				guiTable.Columns[3].Name == "colFlow"    &&
				guiTable.Columns[4].Name == "colPrice"   &&
				guiTable.Columns[5].Name == "colComment"
				);

			using(var table = connection.Select(sql.ToString()))
				foreach(DataRow record in table.Rows)
				{
					var date = new DateTime((long)record[1], DateTimeKind.Utc).ToLocalTime().ToShortDateString();
					var stockName = (string)record[2];
					var shares    = (double)record[3];
					var flow      = (double)record[4];

					double priceAvg = Math.Round(-flow / shares, FormMain.precisionMoney);

					string comment = record[5] == DBNull.Value ? null : (string)record[5];

					int i =
					guiTable.Rows.Add( date, stockName, shares, flow, priceAvg, comment );
					guiTable.Rows[i].Tag = record[0]; // store database rowid internally
				}
		}
	}
}
