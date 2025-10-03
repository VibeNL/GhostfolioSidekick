using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Common.SQL;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class Tables : ComponentBase
    {
        [Inject] private DatabaseContext DbContext { get; set; } = default!;

        private List<string?> TableNames = [];
        private string? SelectedTable;
        private TableDataRecord TableData = new();
        private int page = 1;
        private int TotalRecords = 0;
        private int TotalPages = 0;
        private const int PageSize = 250;
        
        // Add column filters
        private Dictionary<string, string> ColumnFilters = [];
        private bool _filtersApplied = false;
        private bool _isLoading = false;
        
        // Add sorting state
        private string? _sortColumn = null;
        private string _sortDirection = "asc";

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

        private async Task OnTableSelectionChanged()
        {
            if (!string.IsNullOrEmpty(SelectedTable))
            {
                // Reset page to 1 when loading a new table
                page = 1;
                ColumnFilters.Clear();
                _filtersApplied = false;
                _sortColumn = null;
                _sortDirection = "asc";
                await LoadTableDataAsync(SelectedTable);
            }
            else
            {
                // Clear data when no table is selected
                TableData = new TableDataRecord();
                TotalRecords = 0;
                TotalPages = 0;
                ColumnFilters.Clear();
                _filtersApplied = false;
                _sortColumn = null;
                _sortDirection = "asc";
            }
        }

        private async Task LoadSelectedTableData()
        {
            if (!string.IsNullOrEmpty(SelectedTable))
            {
                // Reset page to 1 when loading a new table
                page = 1;
                ColumnFilters.Clear();
                _filtersApplied = false;
                _sortColumn = null;
                _sortDirection = "asc";
                await LoadTableDataAsync(SelectedTable);
            }
        }

        private async Task OnColumnHeaderClick(string columnName)
        {
            if (_sortColumn == columnName)
            {
                // Toggle sort direction if clicking the same column
                _sortDirection = _sortDirection == "asc" ? "desc" : "asc";
            }
            else
            {
                // Set new sort column and default to ascending
                _sortColumn = columnName;
                _sortDirection = "asc";
            }

            // Reset to page 1 when sorting changes
            page = 1;

            if (!string.IsNullOrEmpty(SelectedTable))
            {
                await LoadTableDataAsync(SelectedTable);
                StateHasChanged();
            }
        }

        private string GetSortIcon(string columnName)
        {
            if (_sortColumn != columnName)
                return "bi-arrow-down-up"; // Unsorted icon

            return _sortDirection == "asc" ? "bi-sort-up" : "bi-sort-down";
        }

        private string GetSortClass(string columnName)
        {
            if (_sortColumn == columnName)
                return "sorted-column";
            
            return "";
        }

        private async Task LoadTableDataAsync(string tableName)
        {
            try
            {
                _isLoading = true;
                StateHasChanged();

                // Get active filters (only non-empty values)
                var activeFilters = ColumnFilters.Where(f => !string.IsNullOrWhiteSpace(f.Value))
                                                .ToDictionary(f => f.Key, f => f.Value);

                // Get total record count with filters applied
                TotalRecords = await RawQuery.GetTableCount(DbContext, tableName, activeFilters.Count != 0 ? activeFilters : null);
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                var result = await RawQuery.ReadTable(DbContext, tableName, page, PageSize, activeFilters.Count != 0 ? activeFilters : null, _sortColumn, _sortDirection);

                if (result == null || result.Count == 0)
                {
                    // If we have an existing table structure but no data due to filters, preserve column structure
                    if (activeFilters.Count != 0 && TableData.Columns.Any())
                    {
                        TableData.Rows = [];
                    }
                    else
                    {
                        // First load or no filters - get column structure even with no data
                        var columnResult = await RawQuery.ReadTable(DbContext, tableName, 1, 1, null, _sortColumn, _sortDirection);
                        if (columnResult != null && columnResult.Count > 0)
                        {
                            TableData.Columns = columnResult.First().Keys.ToArray();
                            TableData.Rows = [];
                            
                            // Initialize column filters for new columns
                            foreach (var column in TableData.Columns)
                            {
                                if (!ColumnFilters.ContainsKey(column))
                                {
                                    ColumnFilters[column] = string.Empty;
                                }
                            }
                        }
                        else
                        {
                            TableData.Columns = Array.Empty<string>();
                            TableData.Rows = [];
                        }
                        
                        if (activeFilters.Count == 0)
                        {
                            TotalRecords = 0;
                            TotalPages = 0;
                        }
                    }
                }
                else
                {
                    TableData.Columns = result.First().Keys.ToArray();
                    TableData.Rows = result.Select(x => x.Values.ToArray()).ToList();
                    
                    // Initialize column filters for new columns
                    foreach (var column in TableData.Columns)
                    {
                        if (!ColumnFilters.ContainsKey(column))
                        {
                            ColumnFilters[column] = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                TableData.Columns = new[] { "Error" };
                TableData.Rows = [new object?[] { ex.Message }];
                TotalRecords = 0;
                TotalPages = 0;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task OnFilterChanged(string columnName, string filterValue)
        {
            ColumnFilters[columnName] = filterValue;
            
            // Reset to page 1 when filters change
            page = 1;
            _filtersApplied = ColumnFilters.Any(f => !string.IsNullOrWhiteSpace(f.Value));
            
            if (!string.IsNullOrEmpty(SelectedTable))
            {
                await LoadTableDataAsync(SelectedTable);
                StateHasChanged();
            }
        }

        private async Task ClearAllFilters()
        {
            ColumnFilters.Clear();
            _filtersApplied = false;
            page = 1;
            
            if (!string.IsNullOrEmpty(SelectedTable))
            {
                await LoadTableDataAsync(SelectedTable);
                StateHasChanged();
            }
        }

        private async Task OnPageChanged()
        {
            if (!string.IsNullOrEmpty(SelectedTable))
            {
                await LoadTableDataAsync(SelectedTable);
                StateHasChanged();
            }
        }

        private async Task OnFilterChangedWrapper(GhostfolioSidekick.PortfolioViewer.WASM.Components.FilterableSortableTableHeader.FilterChangeArgs args)
        {
            await OnFilterChanged(args.Column, args.Value);
        }

        private class TableDataRecord
        {
            public string[] Columns { get; set; } = Array.Empty<string>();
            public List<object?[]> Rows { get; set; } = [];
        }
    }
}