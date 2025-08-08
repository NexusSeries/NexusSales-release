using System.ComponentModel;
using System.Windows.Input;
using NexusSales.Core.Interfaces;

namespace NexusSales.UI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IMessengerService _messengerService;
        private string _windowStateText = "Maximize";

        public MainWindowViewModel(IMessengerService messengerService)
        {
            _messengerService = messengerService;
        }

        public ICommand CloseCommand { get; set; }
        public ICommand MinMaxCommand { get; set; }
        public ICommand HideCommand { get; set; }
        public ICommand NavigateCommand { get; set; }
        public ICommand ShutdownCommand { get; set; }

        public string WindowStateText
        {
            get => _windowStateText;
            set
            {
                if (_windowStateText != value)
                {
                    _windowStateText = value;
                    OnPropertyChanged(nameof(WindowStateText));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}