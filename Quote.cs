using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace JP.InvestCalc
{
	/// <summary>Quote of a stock's price
	/// consulted asynchronously.</summary>
	public abstract class Quote
	{
		/// <summary>Stock identifier.</summary>
		public string Code { get; private set; }

		/// <summary>Gets the price from the Internet.</summary>
		/// <returns>Nonsense value in case of network error (see <see cref="Error"/>).</returns>
		public async Task<double> LoadPrice()
		{
			try
			{
				var response = await new HttpClient().GetAsync(Url);
				response.EnsureSuccessStatusCode();
				var html = await response.Content.ReadAsStringAsync();

				return ParsePrice(html); // may throw
			}
			catch(Exception err)
			{
				Error = err;
				return ErrorPrice;
			}
		}

		/// <summary>Null if <see cref="LoadPrice"/> was successful
		/// -- or not called yet.</summary>
		public Exception Error { get; private set; }

		protected const double ErrorPrice = double.NaN;

		protected abstract string Url { get; }
		protected abstract double ParsePrice(string html);

		/// <summary>Factory method.</summary>
		/// <param name="provider_code">Two "words" separated by a space.
		/// The first determines the website where the quote will be got from, e.g. "bloomberg".
		/// The second word is the code of the stock on this website, e.g. "ASML:NA".
		/// Must not be null or will throw.</param>
		/// <returns>Null if code is invalid.
		/// Otherwise object that provides the quote from the appropriate website.</returns>
		public static Quote Prepare(string provider_code)
		{
			if(provider_code == null) return null;

			const int n = 2; // words needed; any more are ignored
			string[] words = provider_code.Split(separator, StringSplitOptions.RemoveEmptyEntries);

			if(words.Length < n || words.Take(n).Any(c => string.IsNullOrWhiteSpace(c)))
				return null;
			
			switch(words[0].ToLower())
			{
				case "bloomberg": return new QuoteBloomberg(words[1]);
				default: return null;
			}
		}
		private readonly static char[] separator =
			" \t\r\n".ToCharArray();

		/// <summary>Constructor</summary>
		/// <param name="code">Stock identifier.</param>
		protected Quote(string code)
		{
			Debug.Assert(!string.IsNullOrWhiteSpace(code));
			Code = code;
		}
	}

	/// <summary><see cref="Quote"/> acquired from Bloomberg.com</summary>
	public class QuoteBloomberg : Quote
	{
		/// <summary>Constructor</summary>
		/// <param name="code">Stock identifier.</param>
		public QuoteBloomberg(string code) : base(code) { }

		protected override string Url => "https://www.bloomberg.com/quote/" + Code;

		protected override double ParsePrice(string html)
		{
			// Easier than regex for this simple case... :)
			//     Whenever html is not correct (IndexOf returns -1)
			// an exception will be automatically thrown from a subsequent call, to IndexOf or Substring.
			var pos0 = html.IndexOf("<span class=\"priceText"); // outer HTML element containing the price
			pos0 = html.IndexOf('>', pos0); // start of inner content (price)
			++pos0; // step over the '>' into the content
			var pos1 = html.IndexOf('<', pos0); // end of inner content (price)
			Debug.Assert(pos1 == html.IndexOf("</span>", pos0)); // longer version of the same, if html is well formed

			// This may also throw if html did not contain a numeric price:
			return double.Parse(html.Substring(pos0, pos1 - pos0));
		}
	}
}
