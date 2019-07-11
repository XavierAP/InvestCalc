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
			const string
				provider = "AlphaVantage",
				code = "ASML.AMS";

			var qt = Quote.Prepare($"{provider} {code}");
			Assert.AreEqual(code, qt.Code);

			double price = await qt.LoadPrice();
			Assert.IsFalse(double.IsNaN(price) || double.IsInfinity(price) || price <= 0);

			double priceBis = await new QuoteAlphaVantage(code).LoadPrice();
			Assert.AreEqual(price, priceBis);
		}
	}
}
