using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using PdfUtility.App.Helpers;
using PdfUtility.Core.Exceptions;
using PdfUtility.Core.Interfaces;
using PdfUtility.Core.Models;

namespace PdfUtility.App.ViewModels
{
    public class SplitViewModel : ViewModelBase
    {
        private readonly IPdfSplitService _splitService;
        private readonly IHistoryService _historyService;
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;

        private string _selectedFilePath = string.Empty;
        private string _outputDirectory = string.Empty;
        private bool _splitAllPages = true;
        private string _pageRange = string.Empty;
        private double _progressValue;
        private bool _isSplitting;
        private string _splitStatus = string.Empty;
        private CancellationTokenSource? _cts;

        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                if (SetProperty(ref _selectedFilePath, value))
                {
                    OnPropertyChanged(nameof(SelectedFileName));
                    OnPropertyChanged(nameof(HasSelectedFile));
                }
            }
        }

        public string SelectedFileName => string.IsNullOrEmpty(_selectedFilePath) ? "No file selected" : Path.GetFileName(_selectedFilePath);
        public bool HasSelectedFile => !string.IsNullOrEmpty(_selectedFilePath);

        public string OutputDirectory
        {
            get => _outputDirectory;
            set => SetProperty(ref _outputDirectory, value);
        }

        public bool SplitAllPages
        {
            get => _splitAllPages;
            set
            {
                if (SetProperty(ref _splitAllPages, value))
                {
                    OnPropertyChanged(nameof(IsRangeSelectionEnabled));
                }
            }
        }

        public bool IsRangeSelectionEnabled => !SplitAllPages;

        public string PageRange
        {
            get => _pageRange;
            set => SetProperty(ref _pageRange, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public bool IsSplitting
        {
            get => _isSplitting;
            set => SetProperty(ref _isSplitting, value);
        }

        public string SplitStatus
        {
            get => _splitStatus;
            set => SetProperty(ref _splitStatus, value);
        }

        public ICommand BrowseFileCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand SplitCommand { get; }
        public ICommand CancelCommand { get; }

        public SplitViewModel(
            IPdfSplitService splitService,
            IHistoryService historyService,
            ISettingsService settingsService,
            INotificationService notificationService)
        {
            _splitService = splitService ?? throw new ArgumentNullException(nameof(splitService));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            var settings = _settingsService.GetSettings();
            OutputDirectory = settings.DefaultOutputFolder;
            if (string.IsNullOrEmpty(OutputDirectory))
            {
                OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PDFUtility");
            }
            Directory.CreateDirectory(OutputDirectory);

            BrowseFileCommand = new RelayCommand(BrowseFile);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            SplitCommand = new RelayCommand(async () => await StartSplitAsync(), () => HasSelectedFile && !IsSplitting);
            CancelCommand = new RelayCommand(CancelSplit, () => IsSplitting);
        }

        public void HandleFileDrop(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            if (Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                SelectedFilePath = filePath;
            }
            else
            {
                _notificationService.ShowError("Unsupported Format", "Only PDF documents can be split.");
            }
        }

        private void BrowseFile() { }
        private void BrowseFolder() { }

        private async Task StartSplitAsync()
        {
            if (!HasSelectedFile) return;

            IsSplitting = true;
            ProgressValue = 0;
            SplitStatus = "Parsing split parameters...";
            _cts = new CancellationTokenSource();

            var auditEntry = new HistoryEntry
            {
                ActionType = "SPLIT",
                SourceFiles = Path.GetFileName(SelectedFilePath),
                TenantId = "default"
            };

            try
            {
                await _splitService.ValidateSplitInputAsync(SelectedFilePath, OutputDirectory, SplitAllPages, PageRange);

                SplitStatus = "Extracting pages...";
                var progressHandler = new Progress<double>(value => ProgressValue = value);

                await _splitService.SplitPdfFileAsync(SelectedFilePath, OutputDirectory, SplitAllPages, PageRange, progressHandler, _cts.Token);

                _settingsService.UpdateLastSelectedFolder(OutputDirectory);

                SplitStatus = "Split completed successfully!";
                auditEntry.OutputPath = OutputDirectory;
                auditEntry.Status = "Success";
                _historyService.AddEntry(auditEntry);

                _notificationService.ShowSuccess("Success", $"PDF split completed. Output pages saved in: {OutputDirectory}");
                SelectedFilePath = string.Empty;
            }
            catch (OperationCanceledException)
            {
                SplitStatus = "Split cancelled.";
                auditEntry.Status = "Cancelled";
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowWarning("Cancelled", "The PDF split process was cancelled.");
            }
            catch (PdfUtilityException ex)
            {
                SplitStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Split Failed", ex.Message);
            }
            catch (Exception ex)
            {
                SplitStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Critical Error", $"Unexpected error during splitting: {ex.Message}");
            }
            finally
            {
                IsSplitting = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void CancelSplit()
        {
            _cts?.Cancel();
            SplitStatus = "Cancelling split process...";
        }
    }
}
