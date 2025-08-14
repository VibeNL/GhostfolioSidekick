using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Common.SQL;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class Tables : ComponentBase
    {
        [Inject] private DatabaseContext DbContext { get; set; } = default!;

        private List<string?> TableNames = new();
        private string? SelectedTable;
        private TableDataRecord TableData = new();
        private int page = 1;
        private int TotalRecords = 0;
        private int TotalPages = 0;
        private const int PageSize = 250;

        protected override async Task OnInitializedAsync()
        {
            await LoadTableNamesAsync();
        }

        private Task LoadTableNamesAsync()
        {
            TableNames = DbContext.Model.GetEntityTypes()
                .Select(t => t.GetTableName())
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .OrderBy(name => name)
                .ToList();
            return Task.CompletedTask;
        }

        private async Task LoadSelectedTableData()
        {
            if (!string.IsNullOrEmpty(SelectedTable))
            {
                await LoadTableDataAsync(SelectedTable);
            }
        }

        private async Task LoadTableDataAsync(string tableName)
        {
            try
            {
                // Get total record count
                TotalRecords = await RawQuery.GetTableCount(DbContext, tableName);
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                var result = await RawQuery.ReadTable(DbContext, tableName, page, PageSize);

                if (result == null || result.Count == 0)
                {
                    TableData.Columns = Array.Empty<string>();
                    TableData.Rows = new();
                    TotalRecords = 0;
                    TotalPages = 0;
                    return;
                }

                TableData.Columns = result.First().Keys.ToArray();
                TableData.Rows = result.Select(x => x.Values.ToArray()).ToList();
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                TableData.Columns = new[] { "Error" };
                TableData.Rows = new List<object[]> { new object[] { ex.Message } };
                TotalRecords = 0;
                TotalPages = 0;
            }
        }

        private class TableDataRecord
        {
            public string[] Columns { get; set; } = Array.Empty<string>();
            public List<object[]> Rows { get; set; } = new();
        }
    }
}