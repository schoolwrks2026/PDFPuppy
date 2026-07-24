using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using PdfSharp.Pdf.IO;
using PdfUtility.App.Helpers;
using PdfUtility.Core.Exceptions;
using PdfUtility.Core.Interfaces;
using PdfUtility.Core.Models;

namespace PdfUtility.App.ViewModels
{
    public class MergeViewModel : ViewModelBase
    {
        private readonly IPdfMergeService _mergeService;
        private readonly IHistoryService _historyService;
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;

        private string _outputFileName = "MergedDocument.pdf";
        private string _outputDirectory = string.Empty;
        private double _progressValue;
        private bool _isMerging;
        private string _mergeStatus = string.Empty;
        private CancellationTokenSource? _cts;

        public ObservableCollection<PdfItemViewModel> SelectedPdfFiles { get; } = new();

        public string OutputFileName
        {
            get => _outputFileName;
            set
            {
                if (SetProperty(ref _outputFileName, value))
                {
                    if (!_outputFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        _outputFileName += ".pdf";
                    }
                }
            }
        }

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

        public bool IsMerging
        {
            get => _isMerging;
            set => SetProperty(ref _isMerging, value);
        }

        public string MergeStatus
        {
            get => _mergeStatus;
            set => SetProperty(ref _mergeStatus, value);
        }

        public bool HasFiles => SelectedPdfFiles.Count > 0;
        public int TotalPageCount => SelectedPdfFiles.Sum(f => f.PageCount);
        public string TotalFilesSizeFormatted
        {
            get
            {
                long totalBytes = SelectedPdfFiles.Sum(f => f.SizeBytes);
                return FormatFileSize(totalBytes);
            }
        }

        // Commands
        public ICommand BrowsePdfsCommand { get; }
        public ICommand BrowseOutputFolderCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand MergeCommand { get; }
        public ICommand CancelCommand { get; }

        public MergeViewModel(
            IPdfMergeService mergeService,
            IHistoryService historyService,
            ISettingsService settingsService,
            INotificationService notificationService)
        {
            _mergeService = mergeService ?? throw new ArgumentNullException(nameof(mergeService));
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

            BrowsePdfsCommand = new RelayCommand(BrowsePdfs);
            BrowseOutputFolderCommand = new RelayCommand(BrowseOutputFolder);
            MoveUpCommand = new RelayCommand<PdfItemViewModel>(MoveItemUp, item => item != null && SelectedPdfFiles.IndexOf(item) > 0);
            MoveDownCommand = new RelayCommand<PdfItemViewModel>(MoveItemDown, item => item != null && SelectedPdfFiles.IndexOf(item) < SelectedPdfFiles.Count - 1);
            RemoveFileCommand = new RelayCommand<PdfItemViewModel>(RemoveItem, item => item != null);
            ClearAllCommand = new RelayCommand(ClearAll, () => SelectedPdfFiles.Count > 0);
            MergeCommand = new RelayCommand(async () => await StartMergeAsync(), () => SelectedPdfFiles.Count >= 2 && !IsMerging);
            CancelCommand = new RelayCommand(CancelMerge, () => IsMerging);
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(TotalPageCount));
            OnPropertyChanged(nameof(TotalFilesSizeFormatted));
        }

        public void AddPdfFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            if (!Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                _notificationService.ShowError("Invalid Extension", $"'{Path.GetFileName(filePath)}' is not a PDF file.");
                return;
            }

            // Check duplicate selection
            if (SelectedPdfFiles.Any(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
            {
                _notificationService.ShowWarning("Duplicate File", $"'{Path.GetFileName(filePath)}' is already in the merge list.");
                return;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    _notificationService.ShowError("Empty File", $"PDF file '{Path.GetFileName(filePath)}' is empty (0 bytes).");
                    return;
                }

                // Quick pre-load to check validity & get page counts
                int pageCount = 0;
                using (var doc = PdfReader.Open(filePath, PdfDocumentOpenMode.Import))
                {
                    pageCount = doc.PageCount;
                    if (doc.SecuritySettings.IsEncrypted)
                    {
                        _notificationService.ShowError("Encrypted File", $"'{Path.GetFileName(filePath)}' is password protected and cannot be merged.");
                        return;
                    }
                }

                SelectedPdfFiles.Add(new PdfItemViewModel(filePath, pageCount, fileInfo.Length));
                Refresh();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Corrupted PDF", $"Failed to read '{Path.GetFileName(filePath)}'. The document may be corrupted: {ex.Message}");
            }
        }

        private void BrowsePdfs() { }
        private void BrowseOutputFolder() { }

        private void MoveItemUp(PdfItemViewModel? item)
        {
            if (item == null) return;
            int oldIdx = SelectedPdfFiles.IndexOf(item);
            if (oldIdx > 0)
            {
                SelectedPdfFiles.Move(oldIdx, oldIdx - 1);
            }
        }

        private void MoveItemDown(PdfItemViewModel? item)
        {
            if (item == null) return;
            int oldIdx = SelectedPdfFiles.IndexOf(item);
            if (oldIdx < SelectedPdfFiles.Count - 1)
            {
                SelectedPdfFiles.Move(oldIdx, oldIdx + 1);
            }
        }

        private void RemoveItem(PdfItemViewModel? item)
        {
            if (item == null) return;
            SelectedPdfFiles.Remove(item);
            Refresh();
        }

        private void ClearAll()
        {
            SelectedPdfFiles.Clear();
            Refresh();
        }

        private async Task StartMergeAsync()
        {
            if (SelectedPdfFiles.Count < 2) return;

            IsMerging = true;
            ProgressValue = 0;
            MergeStatus = "Validating layout parameters...";
            _cts = new CancellationTokenSource();

            var fullOutputPath = Path.Combine(OutputDirectory, OutputFileName);
            var fileList = SelectedPdfFiles.Select(f => f.FilePath).ToList();

            var auditEntry = new HistoryEntry
            {
                ActionType = "MERGE",
                SourceFiles = string.Join(", ", SelectedPdfFiles.Select(f => Path.GetFileName(f.FilePath))),
                TenantId = "default"
            };

            try
            {
                await _mergeService.ValidateMergeInputAsync(fileList, fullOutputPath);

                // Overwrite protection check
                var settings = _settingsService.GetSettings();
                if (File.Exists(fullOutputPath) && !settings.OverwriteExistingFiles)
                {
                    bool overwrite = _notificationService.ShowConfirmation(
                        "File Already Exists",
                        $"The file '{OutputFileName}' already exists in the output folder. Overwrite it?"
                    );

                    if (!overwrite)
                    {
                        MergeStatus = "Cancelled (Filename conflict)";
                        IsMerging = false;
                        return;
                    }
                }

                MergeStatus = "Merging PDF pages...";
                var progressHandler = new Progress<double>(value => ProgressValue = value);

                await _mergeService.MergePdfFilesAsync(fileList, fullOutputPath, progressHandler, _cts.Token);

                // Update settings
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

                MergeStatus = "Merge completed successfully!";
                auditEntry.OutputPath = fullOutputPath;
                auditEntry.Status = "Success";
                auditEntry.TotalFileSize = new FileInfo(fullOutputPath).Length;
                _historyService.AddEntry(auditEntry);

                _notificationService.ShowSuccess("Success", "All PDF documents merged successfully!");
                ClearAll();
            }
            catch (OperationCanceledException)
            {
                MergeStatus = "Merge process cancelled.";
                auditEntry.Status = "Cancelled";
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowWarning("Cancelled", "The PDF merge process was cancelled.");
            }
            catch (PdfUtilityException ex)
            {
                MergeStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Merge Failed", ex.Message);
            }
            catch (Exception ex)
            {
                MergeStatus = $"Failed: {ex.Message}";
                auditEntry.Status = "Failed";
                auditEntry.ErrorMessage = ex.Message;
                _historyService.AddEntry(auditEntry);
                _notificationService.ShowError("Critical Error", $"An unexpected error occurred during merge: {ex.Message}");
            }
            finally
            {
                IsMerging = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void CancelMerge()
        {
            _cts?.Cancel();
            MergeStatus = "Cancelling merge process...";
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB" };
            double dblSized = bytes;
            int i;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSized = bytes / 1024.0;
            }
            return $"{dblSized:0.##} {suffix[i]}";
        }
    }

    public class PdfItemViewModel
    {
        public string FilePath { get; }
        public string FileName => Path.GetFileName(FilePath);
        public int PageCount { get; }
        public long SizeBytes { get; }
        public string FileSizeFormatted { get; }

        public PdfItemViewModel(string filePath, int pageCount, long sizeBytes)
        {
            FilePath = filePath;
            PageCount = pageCount;
            SizeBytes = sizeBytes;

            // Format size
            string[] suffix = { "B", "KB", "MB", "GB" };
            double dblSized = sizeBytes;
            int i;
            for (i = 0; i < suffix.Length && sizeBytes >= 1024; i++, sizeBytes /= 1024)
            {
                dblSized = sizeBytes / 1024.0;
            }
            FileSizeFormatted = $"{dblSized:0.##} {suffix[i]}";
        }
    }
}
