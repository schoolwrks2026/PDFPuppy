using System.Windows.Input;
using PdfUtility.App.Helpers;
using PdfUtility.Core.Interfaces;

namespace PdfUtility.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;
        private readonly INavigationService _navigationService;

        private ViewModelBase _currentViewModel;
        private string _statusMessage = "Ready";
        private bool _isDarkTheme;

        // ViewModels
        public DashboardViewModel Dashboard { get; }
        public ConvertViewModel Convert { get; }
        public MergeViewModel Merge { get; }
        public SplitViewModel Split { get; }
        public WatermarkViewModel Watermark { get; }
        public CompressViewModel Compress { get; }
        public HistoryViewModel History { get; }
        public SettingsViewModel Settings { get; }

        // Navigation Commands
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToConvertCommand { get; }
        public ICommand NavigateToMergeCommand { get; }
        public ICommand NavigateToSplitCommand { get; }
        public ICommand NavigateToWatermarkCommand { get; }
        public ICommand NavigateToCompressCommand { get; }
        public ICommand NavigateToHistoryCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public MainWindowViewModel(
            ISettingsService settingsService,
            INotificationService notificationService,
            INavigationService navigationService,
            DashboardViewModel dashboard,
            ConvertViewModel convert,
            MergeViewModel merge,
            SplitViewModel split,
            WatermarkViewModel watermark,
            CompressViewModel compress,
            HistoryViewModel history,
            SettingsViewModel settings)
        {
            _settingsService = settingsService;
            _notificationService = notificationService;
            _navigationService = navigationService;

            Dashboard = dashboard;
            Convert = convert;
            Merge = merge;
            Split = split;
            Watermark = watermark;
            Compress = compress;
            History = history;
            Settings = settings;

            // Default to Dashboard
            _currentViewModel = Dashboard;

            // Wire up commands
            NavigateToDashboardCommand = new RelayCommand(() => CurrentViewModel = Dashboard);
            NavigateToConvertCommand = new RelayCommand(() => CurrentViewModel = Convert);
            NavigateToMergeCommand = new RelayCommand(() => {
                Merge.Refresh();
                CurrentViewModel = Merge;
            });
            NavigateToSplitCommand = new RelayCommand(() => CurrentViewModel = Split);
            NavigateToWatermarkCommand = new RelayCommand(() => CurrentViewModel = Watermark);
            NavigateToCompressCommand = new RelayCommand(() => CurrentViewModel = Compress);
            NavigateToHistoryCommand = new RelayCommand(() => {
                History.LoadHistory();
                CurrentViewModel = History;
            });
            NavigateToSettingsCommand = new RelayCommand(() => CurrentViewModel = Settings);

            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            // Listen to unified navigation service events
            _navigationService.NavigationRequested += OnNavigationRequested;

            // Initialize theme from settings
            var appSettings = _settingsService.GetSettings();
            IsDarkTheme = appSettings.Theme == "Dark";
        }

        private void OnNavigationRequested(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName)) return;

            switch (pageName)
            {
                case "Dashboard":
                    CurrentViewModel = Dashboard;
                    break;
                case "Convert":
                    CurrentViewModel = Convert;
                    break;
                case "Merge":
                    Merge.Refresh();
                    CurrentViewModel = Merge;
                    break;
                case "Split":
                    CurrentViewModel = Split;
                    break;
                case "Watermark":
                    CurrentViewModel = Watermark;
                    break;
                case "Compress":
                    CurrentViewModel = Compress;
                    break;
                case "History":
                    History.LoadHistory();
                    CurrentViewModel = History;
                    break;
                case "Settings":
                    CurrentViewModel = Settings;
                    break;
            }
        }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value))
                {
                    var settings = _settingsService.GetSettings();
                    settings.Theme = value ? "Dark" : "Light";
                    _settingsService.SaveSettings(settings);
                    Settings.UpdateThemeBinding(settings.Theme);
                }
            }
        }

        private void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
        }
    }
}
