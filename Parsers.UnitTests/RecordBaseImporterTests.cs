using AwesomeAssertions;
using CsvHelper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Moq;
using System.Text;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	public partial class RecordBaseImporterTests
	{
		[CsvRecord]
		internal partial class TestRecord
		{
			public string? Header1 { get; set; }
		}

		private class TestRecordBaseImporter : RecordBaseImporter<TestRecord>
		{
			protected override IEnumerable<PartialActivity> ParseRow(TestRecord record, int rowNumber)
			{
				return new List<PartialActivity>
				{
					new(PartialActivityType.Buy, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "txn1")
				};
			}

			protected override CsvReaderOptions GetConfig()
			{
				return CsvReaderOptions.Default;
			}
		}

		private sealed class HeaderOnlyTestImporter : TestRecordBaseImporter
		{
			public TrackingStreamReader? Reader { get; private set; }

			protected override StreamReader GetStreamReader(string file)
			{
				Reader = new TrackingStreamReader("Header1\nvalue1\nvalue2");
				return Reader;
			}
		}

		private sealed class TrackingStreamReader(string text) : StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text)))
		{
			public int LinesRead { get; private set; }

			public override string? ReadLine()
			{
				LinesRead++;
				return base.ReadLine();
			}
		}

		[Fact]
		public async Task CanParse_ReturnsFalse_WhenFileExtensionIsNotCsv()
		{
			var importer = new TestRecordBaseImporter();
			var result = await importer.CanParse("test.txt");
			result.Should().BeFalse();
		}

		[Fact]
		public async Task CanParse_ReturnsFalse_WhenCsvHeaderIsInvalid()
		{
			var importer = new TestRecordBaseImporter();
			var result = await importer.CanParse("invalid.csv");
			result.Should().BeFalse();
		}

		[Fact]
		public async Task CanParse_ReadsOnlyTheHeader()
		{
			var importer = new HeaderOnlyTestImporter();

			var result = await importer.CanParse("test.csv");

			result.Should().BeTrue();
			importer.Reader!.LinesRead.Should().Be(1);
		}

		[Fact]
		public async Task ParseActivities_ParsesRecordsCorrectly()
		{
			var importer = new TestRecordBaseImporter();
			var mockActivityManager = new Mock<IActivityManager>();

			// Create a temporary CSV file for testing
			var tempFilePath = Path.GetTempFileName();
			await File.WriteAllTextAsync(tempFilePath, "Header1,Header2\nValue1,Value2", TestContext.Current.CancellationToken);

			try
			{
				await importer.ParseActivities(tempFilePath, mockActivityManager.Object, "account1");

				mockActivityManager.Verify(m => m.AddPartialActivity(It.IsAny<string>(), It.IsAny<IEnumerable<PartialActivity>>()), Times.AtLeastOnce);
			}
			finally
			{
				// Clean up the temporary file
				File.Delete(tempFilePath);
			}
		}
	}
}
