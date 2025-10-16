using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Nexo
{
	public class NexoParser(ICurrencyMapper currencyMapper) : RecordBaseImporter<NexoRecord>
	{
		readonly Dictionary<string, string> Translation = new()
		{
			{ "EURX", "EUR" },
			{ "USDX", "USD" }
		};

		protected override IEnumerable<PartialActivity> ParseRow(NexoRecord record, int rowNumber)
		{
			if (!record.Details.StartsWith("approved"))
			{
				yield break;
			}

			var activities = record.Type switch
			{
				"Top up Crypto" => HandleTopUpCrypto(record),
				"Exchange Cashback" or "Referral Bonus" => HandleCashbackAndBonus(record),
				"Withdraw Exchanged" => HandleWithdrawExchanged(record),
				"Deposit" or "Exchange Deposited On" => HandleDeposit(record),
				"Exchange" => HandleExchange(record),
				"Interest" or "Fixed Term Interest" or "Dual Investment Interest" => HandleInterest(record),
				"Exchange To Withdraw" or "Deposit To Exchange" or "Locking Term Deposit" or
				"Unlocking Term Deposit" or "Dual Investment Unlock" or "Dual Investment Lock" =>
					[],
				_ => throw new NotSupportedException(record.Type)
			};

			foreach (var activity in activities)
			{
				yield return activity;
			}
		}

		private static IEnumerable<PartialActivity> HandleTopUpCrypto(NexoRecord record)
		{
			yield return PartialActivity.CreateReceive(
								record.DateTime,
								[PartialSymbolIdentifier.CreateCrypto(record.OutputCurrency)],
								Math.Abs(record.OutputAmount),
								record.Transaction);
		}

		private IEnumerable<PartialActivity> HandleCashbackAndBonus(NexoRecord record)
		{
			var outputCurrency = GetCurrency(record.OutputCurrency);

			if (outputCurrency.IsFiat())
			{
				yield return PartialActivity.CreateGift(
								outputCurrency,
								record.DateTime,
								Math.Abs(record.OutputAmount),
								new Money(Currency.USD, record.USDEquivalent),
								record.Transaction);
			}
			else
			{
				yield return PartialActivity.CreateGift(
								record.DateTime,
								[PartialSymbolIdentifier.CreateCrypto(record.OutputCurrency)],
								Math.Abs(record.OutputAmount),
								record.Transaction);
			}
		}

		private IEnumerable<PartialActivity> HandleWithdrawExchanged(NexoRecord record)
		{
			var outputCurrency = GetCurrency(record.OutputCurrency);
			yield return PartialActivity.CreateCashWithdrawal(
								outputCurrency,
								record.DateTime,
								Math.Abs(record.OutputAmount),
								new Money(Currency.USD, record.USDEquivalent),
								record.Transaction);
		}

		private IEnumerable<PartialActivity> HandleDeposit(NexoRecord record)
		{
			var outputCurrency = GetCurrency(record.OutputCurrency);
			yield return PartialActivity.CreateCashDeposit(
								outputCurrency,
								record.DateTime,
								Math.Abs(record.OutputAmount),
								new Money(Currency.USD, record.USDEquivalent),
								record.Transaction);
		}

		private IEnumerable<PartialActivity> HandleExchange(NexoRecord record)
		{
			var inputCurrency = GetCurrency(record.InputCurrency);
			var outputCurrency = GetCurrency(record.OutputCurrency);

			if (inputCurrency.IsFiat() && outputCurrency.IsFiat())
			{
				throw new NotSupportedException();
			}

			if (!inputCurrency.IsFiat() && !outputCurrency.IsFiat())
			{
				var activities = PartialActivity.CreateAssetConvert(
							record.DateTime,
							[PartialSymbolIdentifier.CreateCrypto(record.InputCurrency)],
							Math.Abs(record.InputAmount),
							null,
							[PartialSymbolIdentifier.CreateCrypto(record.OutputCurrency)],
							Math.Abs(record.OutputAmount),
							null,
							record.Transaction);
				foreach (var activity in activities)
				{
					yield return activity;
				}
			}
			else if (outputCurrency.IsFiat())
			{
				yield return PartialActivity.CreateSell(
							outputCurrency,
							record.DateTime,
							[PartialSymbolIdentifier.CreateCrypto(record.InputCurrency)],
							Math.Abs(record.InputAmount),
							Math.Abs(record.OutputAmount) / Math.Abs(record.InputAmount),
							new Money(Currency.USD, record.USDEquivalent),
							record.Transaction);
			}
			else
			{
				yield return PartialActivity.CreateBuy(
							inputCurrency,
							record.DateTime,
							[PartialSymbolIdentifier.CreateCrypto(record.OutputCurrency)],
							Math.Abs(record.OutputAmount),
							Math.Abs(record.InputAmount) / Math.Abs(record.OutputAmount),
							new Money(Currency.USD, record.USDEquivalent),
							record.Transaction);
			}
		}

		private IEnumerable<PartialActivity> HandleInterest(NexoRecord record)
		{
			var outputCurrency = GetCurrency(record.OutputCurrency);

			if (outputCurrency.IsFiat())
			{
				yield return PartialActivity.CreateInterest(
								outputCurrency,
								record.DateTime,
								Math.Abs(record.OutputAmount),
								record.Type,
								new Money(Currency.USD, record.USDEquivalent),
								record.Transaction);
			}
			else
			{
				yield return PartialActivity.CreateStakingReward(
								record.DateTime,
								[PartialSymbolIdentifier.CreateCrypto(record.OutputCurrency)],
								Math.Abs(record.OutputAmount),
								record.Transaction);
			}
		}

		private Currency GetCurrency(string currency)
		{
			if (Translation.TryGetValue(currency, out var translated))
			{
				return currencyMapper.Map(translated);
			}

			return currencyMapper.Map(currency);
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
			};
		}
	}
}
