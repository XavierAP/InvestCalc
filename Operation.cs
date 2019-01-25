using System;
using System.Diagnostics;

namespace JP.InvestCalc
{
	/// <summary>Each instance represents a type of operation; therefore no instances 
	/// need/can be created besides the ones in static fields. The members serve to translate
	/// logically between the unsigned UI that distinguishes operations by type, and a
	/// back-end that uses mathematical signs to treat all operations equally.</summary>
	internal class Operation
	{
		public readonly string Text;
		public readonly bool
			SharesChange,
			SharesMinus,
			MoneyMinus;
		
		private Operation(string text, bool sharesChange, bool sharesMinus, bool moneyMinus)
		{
			Debug.Assert(!sharesMinus || sharesChange);
			this.Text         = text;
			this.SharesChange = sharesChange;
			this.SharesMinus  = sharesMinus;
			this.MoneyMinus   = moneyMinus;
		}
		
		public readonly static Operation
			Buy = new Operation("Buy",
				sharesChange: true , sharesMinus: false, moneyMinus: true ),
			Sell = new Operation("Sell",
				sharesChange: true , sharesMinus: true , moneyMinus: false),
			Div = new Operation("Dividend",
				sharesChange: false, sharesMinus: false, moneyMinus: false),
			Cost = new Operation("Cost",
				sharesChange: false, sharesMinus: false, moneyMinus: true );
	}
}
