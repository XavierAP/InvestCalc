using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
	}
}
