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
    public class WatermarkViewModel : ViewModelBase
    {
        private readonly IPdfWatermarkService _watermarkService;
        private readonly IHistoryService _historyService;
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;

        private string _selectedFilePath = string.Empty;
        private string _outputDirectory = string.Empty;
        private string _watermarkText = "CONFIDENTIAL";
        private string _fontName = "Helvetica";
        private double _fontSize = 48;
        private double _opacity = 0.3;
        private double _rotationAngle = -45;
        private string _colorHex = "#FF0000"; // Red
        private double _progressValue;
        private bool _isApplying;
        private string _watermarkStatus = string.Empty;
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

        public string WatermarkText
        {
            get => _watermarkText;
            set => SetProperty(ref _watermarkText, value);
        }

        public string FontName
        {
            get => _fontName;
            set => SetProperty(ref _fontName, value);
        }

        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public double Opacity
        {
            get => _opacity;
            set => SetProperty(ref _opacity, value);
        }

        public double RotationAngle
        {
            get => _rotationAngle;
            set => SetProperty(ref _rotationAngle, value);
        }

        public string ColorHex
        {
            get => _colorHex;
            set => SetProperty(ref _colorHex, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public bool IsApplying
        {
            get => _isApplying;
            set => SetProperty(ref _isApplying, value);
        }

        public string WatermarkStatus
        {
            get => _watermarkStatus;
            set => SetProperty(ref _watermarkStatus, value);
        }

        public ICommand BrowseFileCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand CancelCommand { get; }

        public WatermarkViewModel(
            IPdfWatermarkService watermarkService,
            IHistoryService historyService,
            ISettingsService settingsService,
            INotificationService notificationService)
        {
            _watermarkService = watermarkService ?? throw new ArgumentNullException(nameof(watermarkService));
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
            ApplyCommand = new RelayCommand(async () => await StartWatermarkAsync(), () => HasSelectedFile && !IsApplying);
            CancelCommand = new RelayCommand(CancelWatermark, () => IsApplying);
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
                _notificationService.ShowError("Unsupported Format", "Only PDF documents can be watermarked.");
            }
        }

        private void BrowseFile() { }
        private void BrowseFolder() { }

        private async Task StartWatermarkAsync()
        {
            if (!HasSelectedFile) return;

            IsApplying = true;
            ProgressValue = 0;
            WatermarkStatus = "Preparing watermark stamp...";
            _cts = new CancellationTokenSource();

            var outFileName = Path.GetFileNameWithoutExtension(SelectedFilePath) + "_Watermarked.pdf";
            var fullOutputPath = Path.Combine(OutputDirectory, outFileName);

            var auditEntry = new HistoryEntry
            {
                ActionType = "WATERMARK",
                SourceFiles = Path.GetFileName(SelectedFilePath),
                TenantId = "default",
                OutputPath = fullOutputPath
            };

            try
            {
                await _watermarkService.ValidateWatermarkInputAsync(SelectedFilePath, fullOutputPath, WatermarkText);

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
                        WatermarkStatus = "Cancelled (Filename conflict)";
                        IsApplying = false;
                        return;
                    }
                }

                WatermarkStatus = "Stamping pages...";
                var progressHandler = new Progress<double>(value => ProgressValue = value);

                await _watermarkService.ApplyTextWatermarkAsync(
                    SelectedFilePath,
                    fullOutputPath,
                    WatermarkText,
                    FontName,
                    FontSize,
                    Opacity,
                    RotationAngle,
                    ColorHex,
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

                WatermarkStatus = "Watermark completed successfully!";
                auditEntry.Status = "Success";
                auditEntry.TotalFileSize = new FileInfo(fullOutputPath).Length;
                _historyService.AddEntry(auditEntry);

                _notificationService.ShowSuccess("Success", $"Applied watermark to '{SelectedFileName}' successfully.");
                SelectedFilePath = string.Empty;
            }
            catch (OperationCanceledException)
            {
                WatermarkStatus = "Watermark cancelled.";
                auditEntry.Status = "Cancelled";
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowWarning("Cancelled", "The watermark process was cancelled.");
            }
            catch (PdfUtilityException ex)
            {
                WatermarkStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Watermark Failed", ex.Message);
            }
            catch (Exception ex)
            {
                WatermarkStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Critical Error", $"Unexpected error: {ex.Message}");
            }
            finally
            {
                IsApplying = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void CancelWatermark()
        {
            _cts?.Cancel();
            WatermarkStatus = "Cancelling watermark process...";
        }
    }
}
