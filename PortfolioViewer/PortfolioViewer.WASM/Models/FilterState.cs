using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public class FilterState : INotifyPropertyChanged
	{
		private DateOnly _startDate = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, 1, 1, 0, 0, 0, DateTimeKind.Local));  // YTD start - January 1st of current year
		private DateOnly _endDate = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0, DateTimeKind.Local));
		private int _selectedAccountId;
		private string _selectedSymbol = ""; // Add symbol filter
		private List<string> _selectedTransactionType = []; // Change to List<string> for multi-selection
		private string _searchText = ""; // Add search text filter

		public FilterState() { }

		public FilterState(FilterState source)
		{
			_startDate = source._startDate;
			_endDate = source._endDate;
			_selectedAccountId = source._selectedAccountId;
			_selectedSymbol = source._selectedSymbol;
			_selectedTransactionType = [.. source._selectedTransactionType];
			_searchText = source._searchText;
		}

		public DateOnly StartDate
		{
			get => _startDate;
			set
			{
				if (_startDate != value)
				{
					_startDate = value;
					OnPropertyChanged(nameof(StartDate));
				}
			}
		}

		public DateOnly EndDate
		{
			get => _endDate;
			set
			{
				if (_endDate != value)
				{
					_endDate = value;
					OnPropertyChanged(nameof(EndDate));
				}
			}
		}

		public int SelectedAccountId
		{
			get => _selectedAccountId;
			set
			{
				if (_selectedAccountId != value)
				{
					_selectedAccountId = value;
					OnPropertyChanged(nameof(SelectedAccountId));
				}
			}
		}

		public string SelectedSymbol
		{
			get => _selectedSymbol;
			set
			{
				if (_selectedSymbol != value)
				{
					_selectedSymbol = value ?? "";
					OnPropertyChanged(nameof(SelectedSymbol));
				}
			}
		}

		public List<string> SelectedTransactionType
		{
			get => _selectedTransactionType;
			set
			{
				if (!_selectedTransactionType.SequenceEqual(value ?? []))
				{
					_selectedTransactionType = value ?? [];
					OnPropertyChanged(nameof(SelectedTransactionType));
				}
			}
		}

		public string SearchText
		{
			get => _searchText;
			set
			{
				if (_searchText != value)
				{
					_searchText = value ?? "";
					OnPropertyChanged(nameof(SearchText));
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public bool IsEqual(FilterState? other)
		{
			if (other == null)
			{
				return false;
			}

			return StartDate == other.StartDate &&
				   EndDate == other.EndDate &&
				   SelectedAccountId == other.SelectedAccountId &&
				   SelectedSymbol == other.SelectedSymbol &&
				   SelectedTransactionType.SequenceEqual(other.SelectedTransactionType) &&
				   SearchText == other.SearchText;
		}
	}
}