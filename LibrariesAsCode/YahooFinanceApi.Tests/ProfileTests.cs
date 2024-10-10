using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace YahooFinanceApi.Tests;

public class ProfileTests
{
    [Fact]
    public async Task TestProfileAsync()
    {
        const string AAPL = "AAPL";

        var aaplProfile = await Yahoo.QueryProfileAsync(AAPL);

        Assert.NotNull(aaplProfile.Address1);
        Assert.NotNull(aaplProfile.AuditRisk);
        Assert.NotNull(aaplProfile.BoardRisk);
        Assert.NotNull(aaplProfile.City);
        Assert.NotNull(aaplProfile.CompanyOfficers);
        Assert.NotNull(aaplProfile.CompensationAsOfEpochDate);
        Assert.NotNull(aaplProfile.CompensationRisk);
        Assert.NotNull(aaplProfile.Country);
        Assert.NotNull(aaplProfile.FullTimeEmployees);
        Assert.NotNull(aaplProfile.GovernanceEpochDate);
        Assert.NotNull(aaplProfile.Industry);
        Assert.NotNull(aaplProfile.IndustryDisp);
        Assert.NotNull(aaplProfile.IndustryKey);
        Assert.NotNull(aaplProfile.LongBusinessSummary);
        Assert.NotNull(aaplProfile.MaxAge);
        Assert.NotNull(aaplProfile.State);
        Assert.NotNull(aaplProfile.Zip);
        Assert.NotNull(aaplProfile.Phone);
        Assert.NotNull(aaplProfile.Website);
        Assert.NotNull(aaplProfile.Sector);
        Assert.NotNull(aaplProfile.SectorKey);
        Assert.NotNull(aaplProfile.SectorDisp);
        Assert.NotNull(aaplProfile.ShareHolderRightsRisk);
        Assert.NotNull(aaplProfile.OverallRisk);
    }
}