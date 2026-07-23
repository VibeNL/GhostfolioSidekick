using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services
{
	public class CsvExportServiceTests
	{
		private readonly CsvExportService _service;

		public CsvExportServiceTests()
		{
			var jsRuntimeMock = new Mock<IJSRuntime>();
			var loggerMock = new Mock<ILogger<CsvExportService>>();
			_service = new CsvExportService(jsRuntimeMock.Object, loggerMock.Object);
		}

		[Fact]
		public void ExportToCsvString_EmptyData_ReturnsEmptyString()
		{
			var result = _service.ExportToCsvString<string>([]);
			result.Should().BeEmpty();
		}

		[Fact]
		public void ExportToCsvString_NullData_ReturnsEmptyString()
		{
			var result = _service.ExportToCsvString<string?>(null!);
			result.Should().BeEmpty();
		}

		[Fact]
		public void ExportToCsvString_SimpleStringData_UsesDefaultHeaders()
		{
			// Use a simple wrapper class since string has no public properties
			var data = new[]
			{
				new { Value = "hello" },
				new { Value = "world" }
			};
			var result = _service.ExportToCsvString(data);

			result.Should().Contain("Value");
			result.Should().Contain("hello");
			result.Should().Contain("world");
		}

		[Fact]
		public void ExportToCsvString_TransactionData_GeneratesCorrectCsv()
		{
			var data = new List<TransactionDisplayModel>
			{
				new()
				{
					Id = 1,
					Date = new DateTime(2024, 1, 15),
					Type = "Buy",
					Symbol = "AAPL",
					Name = "Apple Inc.",
					Description = "Test transaction",
					TransactionId = "TXN001",
					AccountName = "Brokerage",
					Quantity = 10,
					UnitPrice = new Money("USD", 150.50m),
					Amount = new Money("USD", 1505.00m),
					TotalValue = new Money("USD", 1505.00m),
					Currency = "USD",
					Fee = new Money("USD", 9.99m),
					Tax = null
				}
			};

			var result = _service.ExportToCsvString(data);

			result.Should().Contain("Id,Date,Type,Symbol,Name,Description,Transaction ID,AccountName,Quantity,Unit Price,Amount,TotalValue,Currency,Fee,Tax");
			result.Should().Contain("1,2024-01-15,Buy,AAPL,Apple Inc.,Test transaction,TXN001,Brokerage,10,150.50 USD,1505.00 USD,1505.00 USD,USD,9.99 USD");
		}

		[Fact]
		public void ExportToCsvString_HoldingData_GeneratesCorrectCsv()
		{
			var data = new List<HoldingDisplayModel>
			{
				new()
				{
					Symbols = ["AAPL", "AAPL.US"],
					Name = "Apple Inc.",
					CurrentValue = new Money("USD", 1500.00m),
					Quantity = 10,
					AveragePrice = new Money("USD", 140.00m),
					CurrentPrice = new Money("USD", 150.00m),
					GainLoss = new Money("USD", 100.00m),
					GainLossPercentage = 7.14m,
					Weight = 15.5m,
					Sector = "Technology",
					AssetClass = "Stock",
					Currency = "USD"
				}
			};

			var result = _service.ExportToCsvString(data);

			result.Should().Contain("Symbols");
			result.Should().Contain("Name");
			result.Should().Contain("Apple Inc.");
			result.Should().Contain("1500.00 USD");
			result.Should().Contain("Technology");
			result.Should().Contain("Stock");
		}

		[Fact]
		public void ExportToCsvString_DividendData_GeneratesCorrectCsv()
		{
			var data = new List<DividendModel>
			{
				new()
				{
					Symbol = "AAPL",
					CompanyName = "Apple Inc.",
					ExDate = new DateOnly(2024, 1, 15),
					PaymentDate = new DateOnly(2024, 2, 1),
					Amount = 0.24m,
					Currency = "USD",
					DividendPerShare = 0.24m,
					AmountPrimaryCurrency = 0.24m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 0.24m,
					Quantity = 10,
					IsPredicted = false
				}
			};

			var result = _service.ExportToCsvString(data);

			result.Should().Contain("Symbol");
			result.Should().Contain("CompanyName");
			result.Should().Contain("AAPL");
			result.Should().Contain("Apple Inc.");
			result.Should().Contain("2024-01-15");
			result.Should().Contain("2024-02-01");
		}

		[Fact]
		public void ExportToCsvString_TaxReportData_GeneratesCorrectCsv()
		{
			var data = new List<TaxReportRow>
			{
				new()
				{
					Year = 2024,
					Date = new DateOnly(2024, 1, 1),
					AccountId = 1,
					AccountName = "Brokerage",
					AssetValue = new Money("USD", 10000.00m),
					CashBalance = new Money("USD", 2500.00m),
					TotalValue = new Money("USD", 12500.00m)
				}
			};

			var result = _service.ExportToCsvString(data);

			result.Should().Contain("Year,Date,AccountId,AccountName,AssetValue,CashBalance,TotalValue");
			result.Should().Contain("2024");
			result.Should().Contain("2024-01-01");
			result.Should().Contain("10000.00 USD");
			result.Should().Contain("2500.00 USD");
			result.Should().Contain("12500.00 USD");
		}

		[Fact]
		public void ExportToCsvString_AccountValueData_GeneratesCorrectCsv()
		{
			var data = new List<AccountValueDisplayModel>
			{
				new()
				{
					Date = new DateOnly(2024, 1, 15),
					AccountName = "Brokerage",
					AccountId = 1,
					Value = new Money("USD", 10000.00m),
					Invested = new Money("USD", 8000.00m),
					Balance = new Money("USD", 2000.00m),
					GainLoss = new Money("USD", 2000.00m),
					GainLossPercentage = 25.00m,
					Currency = "USD"
				}
			};

			var result = _service.ExportToCsvString(data);

			result.Should().Contain("Date,AccountName,AccountId,Value,Invested,Balance,Gain/Loss,Gain/Loss %,Currency,AssetValue");
			result.Should().Contain("2024-01-15");
			result.Should().Contain("Brokerage");
			result.Should().Contain("10000.00 USD");
		}

		[Fact]
		public void ExportToCsvString_DataIssueData_GeneratesCorrectCsv()
		{
			var data = new List<DataIssueDisplayModel>
			{
				new()
				{
					Id = 1,
					IssueType = "MissingData",
					Description = "Test issue",
					Date = new DateTime(2024, 1, 15),
					AccountName = "Brokerage",
					ActivityType = "Buy",
					Symbol = "AAPL",
					SymbolIdentifiers = "ISIN:US0378331005",
					PartialIdentifiers = [new PartialSymbolIdentifier(IdentifierType.ISIN, "US0378331005", null, [], [])],
					Quantity = 10,
					UnitPrice = new Money("USD", 150.00m),
					Amount = new Money("USD", 1500.00m),
					TransactionId = "TXN001",
					ActivityDescription = "Test",
					Severity = "Warning"
				}
			};

			var result = _service.ExportToCsvString(data);

			result.Should().Contain("Id");
			result.Should().Contain("IssueType");
			result.Should().Contain("MissingData");
			result.Should().Contain("2024-01-15");
			result.Should().Contain("150.00 USD");
			result.Should().Contain("1500.00 USD");
		}

		[Fact]
		public void ExportToCsvString_SpecialCharacters_IncludesProperEscaping()
		{
			var data = new List<TransactionDisplayModel>
			{
				new()
				{
					Id = 1,
					Type = "Buy",
					Symbol = "AAPL",
					Description = "Contains, comma",
					TransactionId = "TXN\"quoted\"",
					AccountName = "Test",
					Quantity = 10,
					Currency = "USD",
					UnitPrice = new Money("USD", 100m),
					Amount = new Money("USD", 1000m),
					TotalValue = new Money("USD", 1000m)
				}
			};

			var result = _service.ExportToCsvString(data);

			// Commas should be quoted
			result.Should().Contain("\"Contains, comma\"");
			// Quotes should be doubled
			result.Should().Contain("\"TXN\"\"quoted\"\"\"");
		}

		[Fact]
		public void ExportToCsvString_CustomHeaders_UsesProvidedHeaders()
		{
			var data = new List<TransactionDisplayModel>
			{
				new()
				{
					Id = 1,
					Type = "Buy",
					Symbol = "AAPL",
					Description = "Test",
					TransactionId = "TXN001",
					AccountName = "Brokerage",
					Quantity = 10,
					Currency = "USD",
					UnitPrice = new Money("USD", 100m),
					Amount = new Money("USD", 1000m),
					TotalValue = new Money("USD", 1000m)
				}
			};

			var customHeaders = new[] { "ID", "Type", "Symbol", "Description", "Txn ID", "Account", "Qty", "Currency", "Price", "Total" };
			var result = _service.ExportToCsvString(data, customHeaders);

			// Verify custom headers are present in the first line
			var firstLine = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)[0];
			firstLine.Should().Contain("ID");
			firstLine.Should().Contain("Type");
			firstLine.Should().Contain("Symbol");
			firstLine.Should().Contain("Txn ID");
			firstLine.Should().Contain("Account");
			firstLine.Should().Contain("Qty");
			firstLine.Should().Contain("Price");
			firstLine.Should().Contain("Total");
		}

		[Fact]
		public void ExportToCsvString_NullPropertyValues_HandlesCorrectly()
		{
			var data = new List<TransactionDisplayModel>
			{
				new()
				{
					Id = 1,
					Type = "Buy",
					Symbol = null,
					Name = null,
					Description = "Test",
					TransactionId = "TXN001",
					AccountName = "Brokerage",
					Quantity = null,
					UnitPrice = null,
					Amount = null,
					TotalValue = null,
					Currency = "USD",
					Fee = null,
					Tax = null
				}
			};

			var result = _service.ExportToCsvString(data);

			result.Should().Contain("\"\"");
			result.Should().NotContain("null");
		}

		[Fact]
		public void ExportToCsvString_MultipleRows_GeneratesCorrectCsv()
		{
			var data = new List<TransactionDisplayModel>
			{
				new()
				{
					Id = 1,
					Type = "Buy",
					Symbol = "AAPL",
					Description = "First",
					TransactionId = "TXN001",
					AccountName = "Brokerage",
					Quantity = 10,
					Currency = "USD",
					UnitPrice = new Money("USD", 100m),
					Amount = new Money("USD", 1000m),
					TotalValue = new Money("USD", 1000m)
				},
				new()
				{
					Id = 2,
					Type = "Sell",
					Symbol = "GOOGL",
					Description = "Second",
					TransactionId = "TXN002",
					AccountName = "Brokerage",
					Quantity = 5,
					Currency = "USD",
					UnitPrice = new Money("USD", 200m),
					Amount = new Money("USD", 1000m),
					TotalValue = new Money("USD", 1000m)
				}
			};

			var result = _service.ExportToCsvString(data);

			var lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
			lines.Length.Should().Be(3); // header + 2 data rows
			lines[0].Should().Contain("Id,Date,Type,Symbol,Name,Description,Transaction ID,AccountName,Quantity,Unit Price,Amount,TotalValue,Currency,Fee,Tax");
			lines[1].Should().Contain("1");
			lines[1].Should().Contain("AAPL");
			lines[2].Should().Contain("2");
			lines[2].Should().Contain("GOOGL");
		}
	}
}
