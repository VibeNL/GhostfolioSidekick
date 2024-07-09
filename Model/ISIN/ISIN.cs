using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Model.ISIN
{
	// https://medium.com/@michael.harges/implementing-the-isin-check-digit-algorithm-in-c-93b199ba0777
	public static class Isin
	{
		private const Int32 _expectedLength = 12;
		private static readonly Int32[] _doubledValues = new Int32[] { 0, 2, 4, 6, 8, 1, 3, 5, 7, 9 };
		private static readonly Int32[,] _lettersTable = BuildLetterLookupTable();

		public static bool ValidateCheckDigit(string str)
		{
			if (String.IsNullOrEmpty(str) || str.Length != _expectedLength)
			{
				return false;
			}

			var sum = 0;
			var oddPosition = true;
			for (var index = str.Length - 2; index >= 0; index--)
			{
				var ch = str[index];
				if (ch >= '0' && ch <= '9')
				{
					var digit = ch - '0';
					sum += oddPosition ? _doubledValues[digit] : digit;
					oddPosition = !oddPosition;
				}
				else if (ch >= 'A' && ch <= 'Z')
				{
					sum += _lettersTable[oddPosition ? 1 : 0, ch - 65];
				}
				else
				{
					return false;
				}
			}
			var checkDigit = (10 - (sum % 10)) % 10;

			return str[^1] - '0' == checkDigit;
		}

		private static Int32[,] BuildLetterLookupTable()
		{
			var table = new Int32[2, 26];

			for (var n = 0; n < 26; n++)
			{
				var number = n + 10;
				var firstDigit = number / 10;
				var secondDigit = number % 10;

				table[0, n] = _doubledValues[firstDigit] + secondDigit;
				table[1, n] = firstDigit + _doubledValues[secondDigit];
			}

			return table;
		}
	}
}
