using AwesomeAssertions;
using CsvHelper.Configuration;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Moq;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	public class RecordBaseImporterTests
	{
		private class TestRecord
		{
			public string? Header1 { get; set; } = string.Empty;
		}

		private class TestRecordBaseImporter(IApplicationSettings settings) : RecordBaseImporter<TestRecord>(settings)
		{
			protected override IEnumerable<PartialActivity> ParseRow(TestRecord record, int rowNumber)
			{
				_ = record.Header1;
				return [new PartialActivity(PartialActivityType.Buy, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "txn1")];
			}

			protected override CsvConfiguration GetConfig()
			{
				return new CsvConfiguration(CultureInfo.InvariantCulture);
			}
		}

		private class UnparseableRowImporter(IApplicationSettings settings) : RecordBaseImporter<TestRecord>(settings)
		{
			protected override IEnumerable<PartialActivity> ParseRow(TestRecord record, int rowNumber)
			{
				_ = record.Header1;
				throw new NotSupportedException("Unsupported test row");
			}

			protected override CsvConfiguration GetConfig()
			{
				return new CsvConfiguration(CultureInfo.InvariantCulture);
			}
		}

		[Fact]
		public async Task CanParse_ReturnsFalse_WhenFileExtensionIsNotCsv()
		{
			var importer = new TestRecordBaseImporter(TestApplicationSettings.Create());
			var result = await importer.CanParse("test.txt");
			result.Should().BeFalse();
		}

		[Fact]
		public async Task CanParse_ReturnsFalse_WhenCsvHeaderIsInvalid()
		{
			var importer = new TestRecordBaseImporter(TestApplicationSettings.Create());
			var result = await importer.CanParse("invalid.csv");
			result.Should().BeFalse();
		}

		[Fact]
		public async Task ParseActivities_ParsesRecordsCorrectly()
		{
			var importer = new TestRecordBaseImporter(TestApplicationSettings.Create());
			var mockActivityManager = new Mock<IActivityManager>();

			var tempFilePath = Path.GetTempFileName();
			await File.WriteAllTextAsync(tempFilePath, "Header1,Header2\nValue1,Value2", TestContext.Current.CancellationToken);

			try
			{
				await importer.ParseActivities(tempFilePath, mockActivityManager.Object, "account1");
				mockActivityManager.Verify(m => m.AddPartialActivity(It.IsAny<string>(), It.IsAny<IEnumerable<PartialActivity>>()), Times.AtLeastOnce);
			}
			finally
			{
				File.Delete(tempFilePath);
			}
		}

		[Fact]
		public async Task ParseActivities_SkipsUnsupportedRows_WhenWarningModeEnabled()
		{
			var importer = new UnparseableRowImporter(TestApplicationSettings.Create(warnOnUnsupportedRow: true));
			var mockActivityManager = new Mock<IActivityManager>();

			var tempFilePath = Path.GetTempFileName();
			await File.WriteAllTextAsync(tempFilePath, "Header1\nValue1", TestContext.Current.CancellationToken);

			try
			{
				await importer.ParseActivities(tempFilePath, mockActivityManager.Object, "account1");
				mockActivityManager.Verify(m => m.AddPartialActivity(It.IsAny<string>(), It.IsAny<IEnumerable<PartialActivity>>()), Times.Never);
			}
			finally
			{
				File.Delete(tempFilePath);
			}
		}

		[Fact]
		public async Task ParseActivities_Throws_WhenWarningModeDisabled()
		{
			var importer = new UnparseableRowImporter(TestApplicationSettings.Create());
			var mockActivityManager = new Mock<IActivityManager>();

			var tempFilePath = Path.GetTempFileName();
			await File.WriteAllTextAsync(tempFilePath, "Header1\nValue1", TestContext.Current.CancellationToken);

			try
			{
				var act = async () => await importer.ParseActivities(tempFilePath, mockActivityManager.Object, "account1");
				await act.Should().ThrowAsync<NotSupportedException>();
			}
			finally
			{
				File.Delete(tempFilePath);
			}
		}
	}
}

