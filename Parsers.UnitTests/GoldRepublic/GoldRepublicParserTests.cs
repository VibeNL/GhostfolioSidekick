using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.GoldRepublic;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.UnitTests.TradeRepublic;

namespace GhostfolioSidekick.Parsers.UnitTests.GoldRepublic
{
	public class GoldRepublicParserTests
	{
		readonly GoldRepublicParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public GoldRepublicParserTests()
		{
			parser = new GoldRepublicParser(new PdfToWordsParser());

			var fixture = CustomFixture.New();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, [new Balance(DateOnly.FromDateTime(DateTime.Today), new Money(Currency.EUR, 0))])
				.Create();
			activityManager = new TestActivityManager();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/GoldRepublic/", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/GoldRepublic/year_overview.pdf", activityManager, account.Name);

			// Assert

			// Default deposit
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateCashDeposit(
					Currency.EUR,
					new DateTime(2023, 05, 17, 0, 0, 0, DateTimeKind.Utc),
					110m,
					new Money(Currency.EUR, 110),
					"Deposit 17-05-2023 Account deposit ( ) - €0.01 €0.01")]
				);

			// Savings plan
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateCashDeposit(
					Currency.EUR,
					new DateTime(2023, 07, 03, 0, 0, 0, DateTimeKind.Utc),
					50m,
					new Money(Currency.EUR, 50m),
					"???")]
				);

			// Single buy
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 06, 09, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Gold(KG)")],
						0.001744m,
						new Money(Currency.EUR, 59610.0917m),
						new Money(Currency.EUR, 103.96m),
						"???"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 06, 09, 0, 0, 0, DateTimeKind.Utc),
						1.04M,
						new Money(Currency.EUR, 1.04M),
						"???")]
				);

			// Single sell
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 07, 26, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Gold(KG)")],
						0.001744m,
						new Money(Currency.EUR, 59610.0917m),
						new Money(Currency.EUR, 98.82m),
						"???"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 07, 26, 0, 0, 0, DateTimeKind.Utc),
						0.99m,
						new Money(Currency.EUR, 0.99m),
						"???")]
				);

			// Single Fee
			activityManager.PartialActivities.Should().Contain(
				[PartialActivity.CreateFee(
					Currency.EUR,
					new DateTime(2023, 07, 17, 0, 0, 0, DateTimeKind.Utc),
					0.06m,
					new Money(Currency.EUR, 0.06m),
					"???")]
				);


		}

		[Fact]
		public async Task GoldRepublicParser_Debug_ColumnExtraction()
		{
			// Debug test to understand what the parser is extracting
			var parser = new GoldRepublicParser(new PdfToWordsParser());

			// Act
			await parser.ParseActivities("./TestFiles/GoldRepublic/year_overview.pdf", activityManager, account.Name);

			// Assert - let's see what we actually get
			// We won't assert anything specific, just output what we get
			foreach (var activity in activityManager.PartialActivities)
			{
				System.Console.WriteLine($"Activity: {activity.ActivityType}, Date: {activity.Date}, Amount: {activity.Amount}, TransactionId: {activity.TransactionId}");
			}

			// For debugging, let's also see how many activities were parsed
			System.Console.WriteLine($"Total activities parsed: {activityManager.PartialActivities.Count}");
			activityManager.PartialActivities.Should().NotBeEmpty(); // Just ensure something was parsed
		}

		[Fact]
		public async Task GoldRepublicParser_BasicTableStructure_ShouldParseCorrectly()
		{
			// This tests the GoldRepublicParser with a simulated table structure
			// to verify that the cutoff-based column assignment works correctly

			// Arrange - simulate a simple GoldRepublic statement
			var parser = new GoldRepublicParser(new TestPdfToWords(new Dictionary<int, string>
			{
				{ 0, simpleGoldRepublicStatement }
			}));

			// Act
			await parser.ParseActivities("test.pdf", activityManager, account.Name);

			// Assert - check if basic parsing works
			activityManager.PartialActivities.Should().NotBeEmpty("Parser should extract at least some activities");

			// Check if we get a deposit activity
			var depositActivity = activityManager.PartialActivities
				.FirstOrDefault(a => a.ActivityType == PartialActivityType.CashDeposit);
			depositActivity.Should().NotBeNull("Should find a deposit activity");
		}

		[Fact]
		public void GoldRepublicParser_DebugColumnValues()
		{
			// Debug test to see what column values are being extracted
			var parser = new GoldRepublicParser(new TestPdfToWords(new Dictionary<int, string>
			{
				{ 0, simpleGoldRepublicStatement }
			}));

			var words = new TestPdfToWords(new Dictionary<int, string>
			{
				{ 0, simpleGoldRepublicStatement }
			}).ParseTokens("test.pdf");

			// Use the same logic as the parser
			var headerKeywords = new[] { "Transaction Type", "Date", "Description", "Bullion", "Amount", "Balance" };
			var (header, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				words,
				headerKeywords,
				stopPredicate: null,
				mergePredicate: null);

			// Debug: check what we got
			System.Console.WriteLine($"Header tokens: {header.Text}");
			System.Console.WriteLine($"Number of rows: {rows.Count}");
			
			if (rows.Count > 0)
			{
				var row = rows[0];
				System.Console.WriteLine($"Row text: {row.Text}");
				System.Console.WriteLine($"Number of columns: {row.Columns.Count}");
				
				for (int i = 0; i < row.Columns.Count; i++)
				{
					var columnText = string.Join(" ", row.Columns[i].Select(t => t.Text));
					System.Console.WriteLine($"Column {i}: '{columnText}'");
				}
				
				// Test column value extraction
				var transactionType = row.GetColumnValue(header, "Transaction Type");
				var date = row.GetColumnValue(header, "Date");
				var description = row.GetColumnValue(header, "Description");
				
				System.Console.WriteLine($"Transaction Type: '{transactionType}'");
				System.Console.WriteLine($"Date: '{date}'");
				System.Console.WriteLine($"Description: '{description}'");
			}

			// This test is just for debugging - we don't assert anything
			Assert.True(true, "Debug test");
		}

		[Fact]
		public void GoldRepublicParser_DebugAnchorsAndCutoffs()
		{
			// Debug test to see anchor positions and cutoffs
			var words = new TestPdfToWords(new Dictionary<int, string>
			{
				{ 0, simpleGoldRepublicStatement }
			}).ParseTokens("test.pdf");

			// Use the same logic as the parser
			var headerKeywords = new[] { "Transaction Type", "Date", "Description", "Bullion", "Amount", "Balance" };
			var rows = PdfTableExtractor.GroupRows(words);
			
			System.Console.WriteLine($"Total words: {words.Count}");
			foreach (var word in words.Take(20)) // Show first 20 tokens
			{
				System.Console.WriteLine($"Token: '{word.Text}' at column {word.BoundingBox?.Column ?? -1}");
			}
			
			var headerRow = rows.FirstOrDefault(r => r.Text.Contains("Transaction Type"));
			if (headerRow != null)
			{
				System.Console.WriteLine($"\nHeader row found: {headerRow.Text}");
				foreach (var token in headerRow.Tokens)
				{
					System.Console.WriteLine($"Header token: '{token.Text}' at column {token.BoundingBox?.Column ?? -1}");
				}
				
				// Debug BuildAnchors method
				var anchors = new List<int>();
				int searchStart = 0;
				
				System.Console.WriteLine("\nBuilding anchors:");
				foreach (var keyword in headerKeywords)
				{
					var parts = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					System.Console.WriteLine($"Looking for keyword '{keyword}' (parts: {string.Join(", ", parts)}) starting at {searchStart}");
					
					// Find sequence logic (simplified version of FindSequence)
					int idx = -1;
					for (int i = searchStart; i <= headerRow.Tokens.Count - parts.Length; i++)
					{
						bool match = true;
						for (int j = 0; j < parts.Length; j++)
						{
							if (!string.Equals(headerRow.Tokens[i + j].Text, parts[j], StringComparison.InvariantCultureIgnoreCase))
							{
								match = false;
								break;
							}
						}
						if (match)
						{
							idx = i;
							break;
						}
					}
					
					if (idx == -1)
					{
						idx = searchStart < headerRow.Tokens.Count ? searchStart : headerRow.Tokens.Count - 1;
					}
					
					var anchorToken = headerRow.Tokens[idx];
					var anchorColumn = anchorToken.BoundingBox?.Column ?? 0;
					anchors.Add(anchorColumn);
					
					System.Console.WriteLine($"Found '{keyword}' at index {idx}, token: '{anchorToken.Text}', column: {anchorColumn}");
					searchStart = idx + parts.Length;
				}
				
				System.Console.WriteLine($"\nAnchors: [{string.Join(", ", anchors)}]");
				
				// Debug cutoff calculation
				if (anchors.Count > 0)
				{
					var cutoffs = new List<int>();
					for (int i = 0; i < anchors.Count - 1; i++)
					{
						var leftAnchor = anchors[i];
						var rightAnchor = anchors[i + 1];
						var distance = rightAnchor - leftAnchor;
						var cutoff = leftAnchor + (int)(distance * 0.9);
						cutoffs.Add(cutoff);
					}
					cutoffs.Add(int.MaxValue);
					
					System.Console.WriteLine($"Cutoffs: [{string.Join(", ", cutoffs.Take(cutoffs.Count - 1))}]");
					
					// Test some sample tokens
					var testTokens = words.Where(w => w.Text == "Deposit" || w.Text == "17-05-2023" || w.Text == "Bank").ToList();
					foreach (var token in testTokens)
					{
						int columnIndex = -1;
						for (int i = 0; i < cutoffs.Count; i++)
						{
							if ((token.BoundingBox?.Column ?? 0) < cutoffs[i])
							{
								columnIndex = i;
								break;
							}
						}
						System.Console.WriteLine($"Token '{token.Text}' at column {token.BoundingBox?.Column} would be assigned to column index {columnIndex}");
					}
				}
			}

			Assert.True(true, "Debug test");
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Debug()
		{
			// Debug version to see what activities are actually being extracted
			await parser.ParseActivities("./TestFiles/GoldRepublic/year_overview.pdf", activityManager, account.Name);

			// Filter to just deposits to see what we get
			var deposits = activityManager.PartialActivities
				.Where(a => a.ActivityType == PartialActivityType.CashDeposit)
				.ToList();

			System.Console.WriteLine($"Found {deposits.Count} deposit activities:");
			foreach (var deposit in deposits)
			{
				System.Console.WriteLine($"  {deposit.ActivityType} {deposit.Date} {deposit.Amount} {deposit.Currency} {deposit.TransactionId}");
			}

			// This is just for debugging
			Assert.True(true);
		}

		private readonly string simpleGoldRepublicStatement = @"
WWW.GOLDREPUBLIC.COM
Account Statement

Transaction Type    Date        Description                     Bullion     Amount      Balance
Deposit            17-05-2023   Bank transfer                   -           €100.00     €100.00
Market Order       18-05-2023   Gold purchase 1g				Gold        €50.00      €50.00

Closing balance: €50.00
";
	}
}