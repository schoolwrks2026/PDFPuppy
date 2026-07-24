using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using PdfUtility.App.Helpers;
using PdfUtility.Core.Interfaces;
using PdfUtility.Core.Models;

namespace PdfUtility.App.ViewModels
{
    public class HistoryViewModel : ViewModelBase
    {
        private readonly IHistoryService _historyService;
        private readonly INotificationService _notificationService;

        public ObservableCollection<HistoryEntry> HistoryEntries { get; } = new();

        public ICommand ClearHistoryCommand { get; }
        public ICommand OpenOutputCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public HistoryViewModel(
            IHistoryService historyService,
            INotificationService notificationService)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            ClearHistoryCommand = new RelayCommand(ClearHistory, () => HistoryEntries.Count > 0);
            OpenOutputCommand = new RelayCommand<HistoryEntry>(OpenOutput, entry => entry != null && File.Exists(entry.OutputPath));
            OpenFolderCommand = new RelayCommand<HistoryEntry>(OpenFolder, entry => entry != null && !string.IsNullOrEmpty(entry.OutputPath));

            LoadHistory();
        }

        public void LoadHistory()
        {
            HistoryEntries.Clear();
            var history = _historyService.GetHistory();
            foreach (var entry in history)
            {
                HistoryEntries.Add(entry);
            }
        }

        private void ClearHistory()
        {
            bool confirm = _notificationService.ShowConfirmation("Clear History", "Are you sure you want to clear your local processing history? This will soft delete all logs.");
            if (confirm)
            {
                _historyService.ClearHistory();
                LoadHistory();
            }
        }

        private void OpenOutput(HistoryEntry? entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.OutputPath)) return;

            try
            {
                if (File.Exists(entry.OutputPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = entry.OutputPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    _notificationService.ShowError("File Not Found", "The converted PDF file is no longer available at the recorded path.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Open Failed", ex.Message);
            }
        }

        private void OpenFolder(HistoryEntry? entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.OutputPath)) return;

            var folder = Path.GetDirectoryName(entry.OutputPath);
            if (string.IsNullOrEmpty(folder)) return;

            try
            {
                if (Directory.Exists(folder))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = folder,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else
                {
                    _notificationService.ShowError("Folder Not Found", "The output folder is no longer available.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Open Folder Failed", ex.Message);
            }
        }
    }
}
