using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class FilterState : INotifyPropertyChanged
    {
        private DateTime _startDate = new DateTime(DateTime.Today.Year, 1, 1);  // YTD start - January 1st of current year
        private DateTime _endDate = DateTime.Today;
        private string _selectedCurrency = "EUR";
        private int _selectedAccountId = 0;

        public DateTime StartDate 
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

        public DateTime EndDate 
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}