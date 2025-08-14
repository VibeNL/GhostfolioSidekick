using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class FilterState : INotifyPropertyChanged
    {
        private DateTime _startDate = DateTime.Today.AddMonths(-6);
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
            Console.WriteLine($"FilterState.OnPropertyChanged - Property: {propertyName}, Current value: {propertyName switch { 
                nameof(SelectedCurrency) => _selectedCurrency,
                nameof(StartDate) => _startDate.ToString(),
                nameof(EndDate) => _endDate.ToString(),
                nameof(SelectedAccountId) => _selectedAccountId.ToString(),
                _ => "Unknown"
            }}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Method to update all properties at once to reduce multiple notifications
        public void UpdateAll(DateTime startDate, DateTime endDate, string selectedCurrency, int selectedAccountId)
        {
            Console.WriteLine($"FilterState.UpdateAll called - Current: {_selectedCurrency}, New: {selectedCurrency}");
            
            var hasChanges = false;

            if (_startDate != startDate)
            {
                _startDate = startDate;
                hasChanges = true;
            }

            if (_endDate != endDate)
            {
                _endDate = endDate;
                hasChanges = true;
            }

            if (_selectedCurrency != selectedCurrency)
            {
                Console.WriteLine($"Currency will change from {_selectedCurrency} to {selectedCurrency}");
                _selectedCurrency = selectedCurrency ?? "EUR";
                hasChanges = true;
            }

            if (_selectedAccountId != selectedAccountId)
            {
                _selectedAccountId = selectedAccountId;
                hasChanges = true;
            }

            // Only notify if there were actual changes
            if (hasChanges)
            {
                Console.WriteLine($"FilterState changes detected - firing PropertyChanged events");
                OnPropertyChanged(nameof(StartDate));
                OnPropertyChanged(nameof(EndDate));
                OnPropertyChanged(nameof(SelectedCurrency));
                OnPropertyChanged(nameof(SelectedAccountId));
                Console.WriteLine($"PropertyChanged events fired");
            }
            else
            {
                Console.WriteLine($"No changes detected in FilterState.UpdateAll");
            }
        }
    }
}