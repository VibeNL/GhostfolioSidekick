using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers;
using Moq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Tests.Parsers
{
	public class RecordBaseImporterTests
	{
		private class TestRecord
		{
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "<Pending>")]
			public string? Header1 { get; set; }
		}

		private class TestRecordBaseImporter : RecordBaseImporter<TestRecord>
		{
			protected override IEnumerable<PartialActivity> ParseRow(TestRecord record, int rowNumber)
			{
				return new List<PartialActivity>
				{
					new PartialActivity(PartialActivityType.Buy, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "txn1")
				};
			}

			protected override CsvConfiguration GetConfig()
			{
				return new CsvConfiguration(CultureInfo.InvariantCulture);
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
		public async Task ParseActivities_ParsesRecordsCorrectly()
		{
			var importer = new TestRecordBaseImporter();
			var mockActivityManager = new Mock<IActivityManager>();

			// Create a temporary CSV file for testing
			var tempFilePath = Path.GetTempFileName();
			await File.WriteAllTextAsync(tempFilePath, "Header1,Header2\nValue1,Value2");

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
