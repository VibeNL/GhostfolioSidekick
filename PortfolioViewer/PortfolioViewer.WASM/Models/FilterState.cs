using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public class FilterState : INotifyPropertyChanged
	{
		private DateOnly _startDate = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, 1, 1));  // YTD start - January 1st of current year
		private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Today);
		private string _selectedCurrency = "EUR";
		private int _selectedAccountId = 0;
		private string _selectedSymbol = ""; // Add symbol filter

		public FilterState()
		{
			
		}

		public FilterState(FilterState source)
		{
			_startDate = source._startDate;
			_endDate = source._endDate;
			_selectedCurrency = source._selectedCurrency;
			_selectedAccountId = source._selectedAccountId;
			_selectedSymbol = source._selectedSymbol;
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

		public string SelectedCurrency
		{
			get => _selectedCurrency;
			set
			{
				if (_selectedCurrency != value)
				{
					_selectedCurrency = value ?? "EUR"; // Ensure non-null value
					OnPropertyChanged(nameof(SelectedCurrency));
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
				   SelectedCurrency == other.SelectedCurrency &&
				   SelectedAccountId == other.SelectedAccountId &&
				   SelectedSymbol == other.SelectedSymbol;
		}
	}
}