using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using PdfUtility.App.Helpers;
using PdfUtility.Core.Exceptions;
using PdfUtility.Core.Interfaces;
using PdfUtility.Core.Models;

namespace PdfUtility.App.ViewModels
{
    public class ConvertViewModel : ViewModelBase
    {
        private readonly IFileConverterService _converterService;
        private readonly IHistoryService _historyService;
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;

        private string _selectedFilePath = string.Empty;
        private string _outputDirectory = string.Empty;
        private double _progressValue;
        private bool _isConverting;
        private string _conversionStatus = string.Empty;
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

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public bool IsConverting
        {
            get => _isConverting;
            set => SetProperty(ref _isConverting, value);
        }

        public string ConversionStatus
        {
            get => _conversionStatus;
            set => SetProperty(ref _conversionStatus, value);
        }

        public ICommand BrowseFileCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand ConvertCommand { get; }
        public ICommand CancelCommand { get; }

        public ConvertViewModel(
            IFileConverterService converterService,
            IHistoryService historyService,
            ISettingsService settingsService,
            INotificationService notificationService)
        {
            _converterService = converterService ?? throw new ArgumentNullException(nameof(converterService));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            // Load default output folder from settings
            var settings = _settingsService.GetSettings();
            OutputDirectory = settings.DefaultOutputFolder;
            if (string.IsNullOrEmpty(OutputDirectory))
            {
                OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PDFUtility");
            }
            Directory.CreateDirectory(OutputDirectory);

            BrowseFileCommand = new RelayCommand(BrowseFile);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            ConvertCommand = new RelayCommand(async () => await StartConversionAsync(), () => HasSelectedFile && !IsConverting);
            CancelCommand = new RelayCommand(CancelConversion, () => IsConverting);
        }

        public void HandleFileDrop(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            if (_converterService.IsSupportedFileFormat(filePath))
            {
                SelectedFilePath = filePath;
            }
            else
            {
                _notificationService.ShowError("Unsupported Format", $"The file format of '{Path.GetFileName(filePath)}' is not supported.");
            }
        }

        private void BrowseFile()
        {
            // The UI layer can call open file dialog or bind directly.
            // We expose this standard method. In WPF page, code-behind can handle Windows CommonDialogs.
        }

        private void BrowseFolder()
        {
            // UI layer common folder browser dialog helper.
        }

        private async Task StartConversionAsync()
        {
            if (!HasSelectedFile) return;

            IsConverting = true;
            ProgressValue = 0;
            ConversionStatus = "Validating input...";
            _cts = new CancellationTokenSource();

            var auditEntry = new HistoryEntry
            {
                ActionType = "CONVERT",
                SourceFiles = Path.GetFileName(SelectedFilePath),
                TenantId = "default"
            };

            try
            {
                await _converterService.ValidateConversionInputAsync(SelectedFilePath, OutputDirectory);

                var outputFileName = Path.GetFileNameWithoutExtension(SelectedFilePath) + ".pdf";
                var fullOutputPath = Path.Combine(OutputDirectory, outputFileName);
                auditEntry.OutputPath = fullOutputPath;

                // Overwrite protection check
                var settings = _settingsService.GetSettings();
                if (File.Exists(fullOutputPath) && !settings.OverwriteExistingFiles)
                {
                    bool overwrite = _notificationService.ShowConfirmation(
                        "File Already Exists",
                        $"The file '{outputFileName}' already exists in the output folder. Overwrite it?"
                    );

                    if (!overwrite)
                    {
                        ConversionStatus = "Cancelled (Filename conflict)";
                        IsConverting = false;
                        return;
                    }
                }

                ConversionStatus = "Converting document...";
                var progressHandler = new Progress<double>(value => ProgressValue = value);

                await _converterService.ConvertFileToPdfAsync(SelectedFilePath, OutputDirectory, progressHandler, _cts.Token);

                // Update last selected folder setting
                _settingsService.UpdateLastSelectedFolder(OutputDirectory);

                // Open completion if configured
                if (settings.OpenPdfAfterCompletion && File.Exists(fullOutputPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullOutputPath,
                        UseShellExecute = true
                    });
                }

                ConversionStatus = "Conversion completed successfully!";
                auditEntry.Status = "Success";
                auditEntry.TotalFileSize = new FileInfo(fullOutputPath).Length;
                _historyService.AddEntry(auditEntry);

                _notificationService.ShowSuccess("Success", $"Converted '{SelectedFileName}' to PDF successfully.");
                SelectedFilePath = string.Empty; // Reset selection
            }
            catch (OperationCanceledException)
            {
                ConversionStatus = "Conversion cancelled.";
                auditEntry.Status = "Cancelled";
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowWarning("Cancelled", "The conversion process was cancelled by user.");
            }
            catch (PdfUtilityException ex)
            {
                ConversionStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Conversion Failed", ex.Message);
            }
            catch (Exception ex)
            {
                ConversionStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Critical Error", $"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                IsConverting = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void CancelConversion()
        {
            _cts?.Cancel();
            ConversionStatus = "Cancelling conversion...";
        }
    }
}
