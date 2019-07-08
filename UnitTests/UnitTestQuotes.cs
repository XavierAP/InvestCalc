using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace JP.InvestCalc
{
	[TestClass]
	public class UnitTestQuotes
	{
		[TestMethod]
		public async Task TestBloomberg()
		{
			const string
				provider = "Bloomberg",
				code = "ASML:NA";

			var qt = Quote.Prepare($"{provider} {code} blah gargabe");
			Assert.AreEqual(code, qt.Code);

			double price = await qt.LoadPrice();

			qt = new QuoteBloomberg(code);
			Assert.AreEqual(price, await qt.LoadPrice());
		}
	}
}
