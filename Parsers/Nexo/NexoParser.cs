using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Nexo
{
	public class NexoParser : RecordBaseImporter<NexoRecord>
	{
		readonly Dictionary<string, string> Translation = new Dictionary<string, string>{
			{ "EURX", "EUR" },
			{ "USDX", "USD" }
		};
		private readonly ICurrencyMapper currencyMapper;

		public NexoParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(NexoRecord record, int rowNumber)
		{
			if (!record.Details.StartsWith("approved"))
			{
				yield break;
			}

			var inputCurrency = GetCurrency(record.InputCurrency);
			var outputCurrency = GetCurrency(record.OutputCurrency);
			switch (record.Type)
			{
				case "Top up Crypto":
					yield return PartialActivity.CreateReceive(
										record.DateTime,
										[PartialSymbolIdentifier.CreateCrypto(record.OutputCurrency)],
										Math.Abs(record.OutputAmount),
										record.Transaction);
					break;
				case "Exchange Cashback":
				case "Referral Bonus":
					if (outputCurrency.IsFiat())
					{
						yield return PartialActivity.CreateGift(
										outputCurrency,
										record.DateTime,
										Math.Abs(record.OutputAmount),
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
					break;
				case "Deposit":
				case "Exchange Deposited On":
					yield return PartialActivity.CreateCashDeposit(
										outputCurrency,
										record.DateTime,
										Math.Abs(record.OutputAmount),
										record.Transaction);
					break;
				case "Exchange":
					if (inputCurrency.IsFiat() && outputCurrency.IsFiat())
					{
						throw new NotSupportedException();
					}
					else if (!inputCurrency.IsFiat() && !outputCurrency.IsFiat())
					{
						var lst = PartialActivity.CreateAssetConvert(
											record.DateTime,
											[PartialSymbolIdentifier.CreateCrypto(record.InputCurrency)],
											Math.Abs(record.InputAmount),
											null,
											[PartialSymbolIdentifier.CreateCrypto(record.OutputCurrency)],
											Math.Abs(record.OutputAmount),
											null,
											record.Transaction);
						foreach (var item in lst)
						{
							yield return item;
						}
					}
					else if (outputCurrency.IsFiat())
					{
						yield return PartialActivity.CreateSell(
											outputCurrency,
											record.DateTime,
											[PartialSymbolIdentifier.CreateCrypto(record.InputCurrency)],
											Math.Abs(record.OutputAmount),
											Math.Abs(record.InputAmount) / Math.Abs(record.OutputAmount),
											record.Transaction);
					}
					else if (inputCurrency.IsFiat())
					{
						yield return PartialActivity.CreateBuy(
											inputCurrency,
											record.DateTime,
											[PartialSymbolIdentifier.CreateCrypto(record.OutputCurrency)],
											Math.Abs(record.OutputAmount),
											Math.Abs(record.InputAmount) / Math.Abs(record.OutputAmount),
											record.Transaction);
					}
					break;
				case "Interest":
				case "Fixed Term Interest":
					if (outputCurrency.IsFiat())
					{
						yield return PartialActivity.CreateInterest(
												outputCurrency,
												record.DateTime,
												Math.Abs(record.OutputAmount),
												record.Type,
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

					break;
				case "Deposit To Exchange":
				case "Locking Term Deposit":
				case "Unlocking Term Deposit":
					yield break;
				default: throw new NotSupportedException(record.Type);
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
