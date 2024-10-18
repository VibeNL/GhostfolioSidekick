using System;
using System.Collections.Generic;

namespace YahooFinanceApi;

public class SecurityProfile
{
    public IReadOnlyDictionary<string, dynamic> Fields { get; private set; }

    // ctor
    internal SecurityProfile(IReadOnlyDictionary<string, dynamic> fields) => Fields = fields;

    public dynamic this[string fieldName] => Fields[fieldName];
    public dynamic this[ProfileFields field] => Fields[field.ToString()];

    public string Address1 => this[ProfileFields.Address1];
    public string City => this[ProfileFields.City];
    public string State => this[ProfileFields.State];
    public string Zip => this[ProfileFields.Zip];
    public string Country => this[ProfileFields.Country];
    public string Phone => this[ProfileFields.Phone];
    public string Website => this[ProfileFields.Website];
    public string Industry => this[ProfileFields.Industry];
    public string IndustryKey => this[ProfileFields.IndustryKey];
    public string IndustryDisp => this[ProfileFields.IndustryDisp];
    public string Sector => this[ProfileFields.Sector];
    public string SectorKey => this[ProfileFields.SectorKey];
    public string SectorDisp => this[ProfileFields.SectorDisp];
    public string LongBusinessSummary => this[ProfileFields.LongBusinessSummary];
    public long FullTimeEmployees => this[ProfileFields.FullTimeEmployees];
    public List<dynamic> CompanyOfficers => this[ProfileFields.CompanyOfficers];
    public long AuditRisk => this[ProfileFields.AuditRisk];
    public long BoardRisk => this[ProfileFields.BoardRisk];
    public long CompensationRisk => this[ProfileFields.CompensationRisk];
    public long ShareHolderRightsRisk => this[ProfileFields.ShareHolderRightsRisk];
    public long OverallRisk => this[ProfileFields.OverallRisk];
    public DateTime GovernanceEpochDate => DateTimeOffset.FromUnixTimeSeconds((long)this[ProfileFields.GovernanceEpochDate]).LocalDateTime;
    public DateTime CompensationAsOfEpochDate => DateTimeOffset.FromUnixTimeSeconds((long)this[ProfileFields.CompensationAsOfEpochDate]).LocalDateTime;
    public long MaxAge => this[ProfileFields.MaxAge];
}