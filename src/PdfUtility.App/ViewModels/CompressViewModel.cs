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
    public class CompressViewModel : ViewModelBase
    {
        private readonly IPdfCompressService _compressService;
        private readonly IHistoryService _historyService;
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;

        private string _selectedFilePath = string.Empty;
        private string _outputDirectory = string.Empty;
        private string _compressionLevel = "Medium"; // "Low", "Medium", "High"
        private double _progressValue;
        private bool _isCompressing;
        private string _compressStatus = string.Empty;
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

        public string CompressionLevel
        {
            get => _compressionLevel;
            set => SetProperty(ref _compressionLevel, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public bool IsCompressing
        {
            get => _isCompressing;
            set => SetProperty(ref _isCompressing, value);
        }

        public string CompressStatus
        {
            get => _compressStatus;
            set => SetProperty(ref _compressStatus, value);
        }

        public ICommand BrowseFileCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand CompressCommand { get; }
        public ICommand CancelCommand { get; }

        public CompressViewModel(
            IPdfCompressService compressService,
            IHistoryService historyService,
            ISettingsService settingsService,
            INotificationService notificationService)
        {
            _compressService = compressService ?? throw new ArgumentNullException(nameof(compressService));
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
            CompressCommand = new RelayCommand(async () => await StartCompressAsync(), () => HasSelectedFile && !IsCompressing);
            CancelCommand = new RelayCommand(CancelCompress, () => IsCompressing);
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
                _notificationService.ShowError("Unsupported Format", "Only PDF documents can be compressed.");
            }
        }

        private void BrowseFile() { }
        private void BrowseFolder() { }

        private async Task StartCompressAsync()
        {
            if (!HasSelectedFile) return;

            IsCompressing = true;
            ProgressValue = 0;
            CompressStatus = "Parsing document structures...";
            _cts = new CancellationTokenSource();

            var outFileName = Path.GetFileNameWithoutExtension(SelectedFilePath) + "_Compressed.pdf";
            var fullOutputPath = Path.Combine(OutputDirectory, outFileName);

            var auditEntry = new HistoryEntry
            {
                ActionType = "COMPRESS",
                SourceFiles = Path.GetFileName(SelectedFilePath),
                TenantId = "default",
                OutputPath = fullOutputPath
            };

            try
            {
                await _compressService.ValidateCompressInputAsync(SelectedFilePath, fullOutputPath);

                // Overwrite protection check
                var settings = _settingsService.GetSettings();
                if (File.Exists(fullOutputPath) && !settings.OverwriteExistingFiles)
                {
                    bool overwrite = _notificationService.ShowConfirmation(
                        "File Already Exists",
                        $"The file '{outFileName}' already exists in the output folder. Overwrite it?"
                    );

                    if (!overwrite)
                    {
                        CompressStatus = "Cancelled (Filename conflict)";
                        IsCompressing = false;
                        return;
                    }
                }

                CompressStatus = "Optimizing data streams...";
                var progressHandler = new Progress<double>(value => ProgressValue = value);

                await _compressService.CompressPdfAsync(
                    SelectedFilePath,
                    fullOutputPath,
                    CompressionLevel,
                    progressHandler,
                    _cts.Token
                );

                _settingsService.UpdateLastSelectedFolder(OutputDirectory);

                if (settings.OpenPdfAfterCompletion && File.Exists(fullOutputPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullOutputPath,
                        UseShellExecute = true
                    });
                }

                CompressStatus = "Compression completed successfully!";
                auditEntry.Status = "Success";
                auditEntry.TotalFileSize = new FileInfo(fullOutputPath).Length;
                _historyService.AddEntry(auditEntry);

                _notificationService.ShowSuccess("Success", $"Compressed '{SelectedFileName}' successfully.");
                SelectedFilePath = string.Empty;
            }
            catch (OperationCanceledException)
            {
                CompressStatus = "Compression cancelled.";
                auditEntry.Status = "Cancelled";
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowWarning("Cancelled", "The compression process was cancelled.");
            }
            catch (PdfUtilityException ex)
            {
                CompressStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Compression Failed", ex.Message);
            }
            catch (Exception ex)
            {
                CompressStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Critical Error", $"Unexpected error: {ex.Message}");
            }
            finally
            {
                IsCompressing = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void CancelCompress()
        {
            _cts?.Cancel();
            CompressStatus = "Cancelling compression process...";
        }
    }
}
