using AwesomeAssertions;
using CsvHelper.Configuration;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

		private class TestRecordBaseImporter(IApplicationSettings settings, ILogger logger) : RecordBaseImporter<TestRecord>(settings, logger)
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

		private class UnparseableRowImporter(IApplicationSettings settings, ILogger logger) : RecordBaseImporter<TestRecord>(settings, logger)
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
			var importer = new TestRecordBaseImporter(TestApplicationSettings.Create(), NullLogger<TestRecordBaseImporter>.Instance);
			var result = await importer.CanParse("test.txt");
			result.Should().BeFalse();
		}

		[Fact]
		public async Task CanParse_ReturnsFalse_WhenCsvHeaderIsInvalid()
		{
			var importer = new TestRecordBaseImporter(TestApplicationSettings.Create(), NullLogger<TestRecordBaseImporter>.Instance);
			var result = await importer.CanParse("invalid.csv");
			result.Should().BeFalse();
		}

		[Fact]
		public async Task ParseActivities_ParsesRecordsCorrectly()
		{
			var importer = new TestRecordBaseImporter(TestApplicationSettings.Create(), NullLogger<TestRecordBaseImporter>.Instance);
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
			var mockLogger = new Mock<ILogger>();
			var importer = new UnparseableRowImporter(TestApplicationSettings.Create(warnOnUnsupportedRow: true), mockLogger.Object);
			var mockActivityManager = new Mock<IActivityManager>();

			var tempFilePath = Path.GetTempFileName();
			await File.WriteAllTextAsync(tempFilePath, "Header1\nValue1", TestContext.Current.CancellationToken);

			try
			{
				await importer.ParseActivities(tempFilePath, mockActivityManager.Object, "account1");
				mockActivityManager.Verify(m => m.AddPartialActivity(It.IsAny<string>(), It.IsAny<IEnumerable<PartialActivity>>()), Times.Never);
				mockLogger.Verify(l => l.Log(
					It.Is<LogLevel>(x => x == LogLevel.Warning),
					It.IsAny<EventId>(),
					(It.Is<object>(o => o is FormattableString f && f.ToString().Contains("Unsupported test row"))),
					It.IsAny<Exception?>(),
					It.IsAny<Func<object?, Exception?, string>>()));
			}
			finally
			{
				File.Delete(tempFilePath);
			}
		}

		[Fact]
		public async Task ParseActivities_Throws_WhenWarningModeDisabled()
		{
			var importer = new UnparseableRowImporter(TestApplicationSettings.Create(), NullLogger<UnparseableRowImporter>.Instance);
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

