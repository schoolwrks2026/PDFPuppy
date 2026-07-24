using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using PdfUtility.App.Helpers;
using PdfUtility.Core.Interfaces;
using PdfUtility.Core.Models;

namespace PdfUtility.App.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IHistoryService _historyService;
        private readonly INotificationService _notificationService;
        private readonly INavigationService _navigationService;
        private string _lastOutputFolder = string.Empty;

        public ObservableCollection<HistoryEntry> RecentHistory { get; } = new();
        public string LastOutputFolder
        {
            get => _lastOutputFolder;
            set => SetProperty(ref _lastOutputFolder, value);
        }

        public ICommand QuickConvertCommand { get; }
        public ICommand QuickMergeCommand { get; }
        public ICommand OpenOutputFolderCommand { get; }

        public DashboardViewModel(
            IHistoryService historyService,
            INotificationService notificationService,
            INavigationService navigationService)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            QuickConvertCommand = new RelayCommand(() => _navigationService.NavigateTo("Convert"));
            QuickMergeCommand = new RelayCommand(() => _navigationService.NavigateTo("Merge"));
            OpenOutputFolderCommand = new RelayCommand(OpenFolder);

            RefreshDashboard();
        }

        public void RefreshDashboard()
        {
            RecentHistory.Clear();
            var history = _historyService.GetHistory().Take(5);
            foreach (var entry in history)
            {
                RecentHistory.Add(entry);
            }

            var lastEntry = history.FirstOrDefault(e => e.Status == "Success");
            if (lastEntry != null && !string.IsNullOrEmpty(lastEntry.OutputPath))
            {
                LastOutputFolder = Path.GetDirectoryName(lastEntry.OutputPath) ?? string.Empty;
            }
            else
            {
                LastOutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PDFUtility");
            }
        }

        private void OpenFolder()
        {
            try
            {
                if (Directory.Exists(LastOutputFolder))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = LastOutputFolder,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else
                {
                    _notificationService.ShowError("Folder Not Found", $"The folder '{LastOutputFolder}' does not exist on your computer.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Error Opening Folder", ex.Message);
            }
        }
    }
}
