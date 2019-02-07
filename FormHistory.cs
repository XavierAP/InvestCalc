using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JP.InvestCalc
{
	internal partial class FormHistory :Form
	{
		public FormHistory(Data db, IEnumerable<string> stocks, DateTime dateFrom, DateTime dateTo)
		{
			InitializeComponent();

			colShares.DefaultCellStyle.Alignment =
			colFlow  .DefaultCellStyle.Alignment =
			colPrice .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

			colFlow.DefaultCellStyle.Format =
			colPrice.DefaultCellStyle.Format =
				"C" + FormMain.precisionMoney;

			db.GetHistory(ref table, stocks, dateFrom, dateTo);
		}
	}
}
