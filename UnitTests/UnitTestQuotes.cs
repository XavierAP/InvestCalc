using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace JP.InvestCalc
{
	[TestClass]
	public class UnitTestQuotes
	{
		[TestMethod]
		public async Task TestAlphaVantage()
		{
			const Quote.Provider api = Quote.Provider.AlphaVantage;
			const string code = "ASML.AMS";

			var qt = Quote.Prepare(api, code);
			Assert.IsInstanceOfType(qt, typeof(QuoteAlphaVantage));
			Assert.AreEqual(code, qt.Code);

			double price = await qt.LoadPrice();
			Assert.IsFalse(double.IsNaN(price) || double.IsInfinity(price) || price <= 0);

			qt = Quote.Prepare("AV " + code);
			double priceBis = await qt.LoadPrice();
			Assert.AreEqual(price, priceBis);
		}
	}
}
