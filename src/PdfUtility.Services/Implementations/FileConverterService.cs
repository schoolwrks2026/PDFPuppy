using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using PdfUtility.Core.Exceptions;
using PdfUtility.Core.Interfaces;

namespace PdfUtility.Services.Implementations
{
    public class FileConverterService : IFileConverterService
    {
        private readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp",
            // Documents
            ".txt", ".csv", ".rtf",
            // Office (Needs LibreOffice or MS Interop)
            ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".odp"
        };

        public bool IsSupportedFileFormat(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            var ext = Path.GetExtension(filePath);
            return _supportedExtensions.Contains(ext);
        }

        public Task ValidateConversionInputAsync(string inputFilePath, string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
            {
                throw new SecurityAndValidationException("Input file path cannot be empty.", "EMPTY_INPUT_PATH");
            }

            if (!File.Exists(inputFilePath))
            {
                throw new SecurityAndValidationException($"Input file does not exist: {inputFilePath}", "INPUT_FILE_NOT_FOUND");
            }

            var fileInfo = new FileInfo(inputFilePath);
            if (fileInfo.Length == 0)
            {
                throw new SecurityAndValidationException($"Selected file is empty (0 bytes): {Path.GetFileName(inputFilePath)}", "EMPTY_FILE");
            }

            var ext = Path.GetExtension(inputFilePath);
            if (!IsSupportedFileFormat(inputFilePath))
            {
                throw new SecurityAndValidationException($"File format '{ext}' is not supported.", "UNSUPPORTED_FORMAT");
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new SecurityAndValidationException("Output directory is not specified.", "NULL_OUTPUT_DIR");
            }

            if (!Directory.Exists(outputDirectory))
            {
                throw new SecurityAndValidationException($"Output directory does not exist: {outputDirectory}", "OUTPUT_DIR_NOT_FOUND");
            }

            // Disk space check
            var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(outputDirectory)) ?? "/");
            if (driveInfo.AvailableFreeSpace < fileInfo.Length * 2) // Estimate PDF will be at most twice the source size
            {
                throw new SecurityAndValidationException("Insufficient disk space in output directory.", "DISK_FULL");
            }

            return Task.CompletedTask;
        }

        public async Task ConvertFileToPdfAsync(
            string inputFilePath,
            string outputDirectory,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            await ValidateConversionInputAsync(inputFilePath, outputDirectory);

            var ext = Path.GetExtension(inputFilePath).ToLower();
            var outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".pdf";
            var outputPath = Path.Combine(outputDirectory, outputFileName);

            progress?.Report(10);
            cancellationToken.ThrowIfCancellationRequested();

            if (IsImageExtension(ext))
            {
                await ConvertImageToPdfAsync(inputFilePath, outputPath, progress, cancellationToken);
            }
            else if (ext == ".txt" || ext == ".csv")
            {
                await ConvertTextOrCsvToPdfAsync(inputFilePath, outputPath, progress, cancellationToken);
            }
            else
            {
                // Is an Office document
                await ConvertOfficeDocumentToPdfAsync(inputFilePath, outputPath, progress, cancellationToken);
            }

            progress?.Report(100);
        }

        private bool IsImageExtension(string ext)
        {
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif" || ext == ".tiff" || ext == ".webp";
        }

        private async Task ConvertImageToPdfAsync(
            string inputFilePath,
            string outputPath,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using var document = new PdfDocument();
                var page = document.AddPage();

                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(30);

                // Load image using ImageSharp to check validity and auto-orient
                using var image = Image.Load<Rgba32>(inputFilePath);

                // Auto orientation: set page size and orientation based on image dimensions
                if (image.Width > image.Height)
                {
                    page.Orientation = PageOrientation.Landscape;
                }
                else
                {
                    page.Orientation = PageOrientation.Portrait;
                }

                // Convert pixels to points (72 points per inch)
                // standard A4/Letter sizing scale
                double imageWidthPoints = image.Width * 72.0 / 96.0; // Assume 96 DPI
                double imageHeightPoints = image.Height * 72.0 / 96.0;

                page.Width = XUnit.FromPoint(imageWidthPoints);
                page.Height = XUnit.FromPoint(imageHeightPoints);

                progress?.Report(60);
                cancellationToken.ThrowIfCancellationRequested();

                // Draw the image onto the page
                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    using var ximg = XImage.FromFile(inputFilePath);
                    gfx.DrawImage(ximg, 0, 0, imageWidthPoints, imageHeightPoints);
                }

                document.Save(outputPath);
            }, cancellationToken);
        }

        private async Task ConvertTextOrCsvToPdfAsync(
            string inputFilePath,
            string outputPath,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using var document = new PdfDocument();
                var lines = File.ReadAllLines(inputFilePath);

                var font = new XFont("Courier New", 10);
                double margin = 40;
                double currentY = margin;
                double lineHeight = font.Height + 2;

                PdfPage? currentPage = null;
                XGraphics? gfx = null;

                progress?.Report(30);

                for (int i = 0; i < lines.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (currentPage == null || gfx == null || currentY + lineHeight > currentPage.Height.Point - margin)
                    {
                        if (gfx != null) gfx.Dispose();
                        currentPage = document.AddPage();
                        currentPage.Size = PageSize.A4;
                        gfx = XGraphics.FromPdfPage(currentPage);
                        currentY = margin;
                    }

                    var line = lines[i];
                    line = line.Replace("\t", "    "); // replace tabs

                    gfx.DrawString(line, font, XBrushes.Black, margin, currentY);
                    currentY += lineHeight;

                    double percent = 30 + ((double)i / lines.Length * 50);
                    progress?.Report(percent);
                }

                if (gfx != null) gfx.Dispose();

                if (document.PageCount == 0)
                {
                    var emptyPage = document.AddPage();
                    emptyPage.Size = PageSize.A4;
                }

                cancellationToken.ThrowIfCancellationRequested();
                document.Save(outputPath);
            }, cancellationToken);
        }

        private async Task ConvertOfficeDocumentToPdfAsync(
            string inputFilePath,
            string outputPath,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            bool success = await TryConvertWithLibreOfficeAsync(inputFilePath, outputPath, progress, cancellationToken);
            if (success) return;

            cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable CA1416
            success = await TryConvertWithMSOfficeInteropAsync(inputFilePath, outputPath, progress, cancellationToken);
#pragma warning restore CA1416
            if (success) return;

            throw new FileConversionException(
                "Offline Office conversion requires either headless LibreOffice Portable configured, or MS Office locally installed.",
                inputFilePath,
                "CONVERSION_ENGINE_UNAVAILABLE"
            );
        }

        private async Task<bool> TryConvertWithLibreOfficeAsync(
            string inputFilePath,
            string outputPath,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            string sofficePath = FindLibreOfficeSofficePath();
            if (string.IsNullOrEmpty(sofficePath)) return false;

            progress?.Report(40);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = sofficePath,
                    Arguments = $"--headless --convert-to pdf --outdir \"{Path.GetDirectoryName(outputPath)}\" \"{inputFilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var tcs = new TaskCompletionSource<bool>();
                using var registration = cancellationToken.Register(() =>
                {
                    try { process.Kill(); } catch { }
                    tcs.TrySetCanceled();
                });

                await Task.Run(() =>
                {
                    process.WaitForExit(15000); // 15-second timeout
                    tcs.TrySetResult(process.ExitCode == 0);
                });

                progress?.Report(80);

                if (process.ExitCode == 0)
                {
                    var expectedOutput = Path.Combine(
                        Path.GetDirectoryName(outputPath) ?? string.Empty,
                        Path.GetFileNameWithoutExtension(inputFilePath) + ".pdf"
                    );

                    if (File.Exists(expectedOutput))
                    {
                        if (expectedOutput != outputPath)
                        {
                            if (File.Exists(outputPath)) File.Delete(outputPath);
                            File.Move(expectedOutput, outputPath);
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed LibreOffice conversion execution.");
            }

            return false;
        }

        [SupportedOSPlatform("windows")]
        private async Task<bool> TryConvertWithMSOfficeInteropAsync(
            string inputFilePath,
            string outputPath,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return false;
            }

            progress?.Report(40);

            return await Task.Run(() =>
            {
                var ext = Path.GetExtension(inputFilePath).ToLower();
                if (ext == ".doc" || ext == ".docx")
                {
                    return ConvertWordWithInterop(inputFilePath, outputPath);
                }
                else if (ext == ".xls" || ext == ".xlsx")
                {
                    return ConvertExcelWithInterop(inputFilePath, outputPath);
                }
                else if (ext == ".ppt" || ext == ".pptx")
                {
                    return ConvertPowerPointWithInterop(inputFilePath, outputPath);
                }

                return false;
            }, cancellationToken);
        }

        [SupportedOSPlatform("windows")]
        private bool ConvertWordWithInterop(string inputFilePath, string outputPath)
        {
#pragma warning disable CA1416
            Type? wordType = Type.GetTypeFromProgID("Word.Application");
            if (wordType == null) return false;

            dynamic? wordApp = null;
            dynamic? document = null;

            try
            {
                wordApp = Activator.CreateInstance(wordType);
                if (wordApp == null) return false;
                wordApp.Visible = false;
                wordApp.ScreenUpdating = false;

                dynamic documents = wordApp.Documents;
                document = documents.Open(
                    FileName: inputFilePath,
                    ConfirmConversions: false,
                    ReadOnly: true,
                    AddToRecentFiles: false,
                    PasswordDocument: Type.Missing,
                    PasswordTemplate: Type.Missing,
                    Revert: false
                );

                if (document == null) return false;

                document.ExportAsFixedFormat(
                    OutputFileName: outputPath,
                    ExportFormat: 17,
                    OpenAfterExport: false,
                    OptimizeFor: 0
                );

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed MS Word Interop conversion.");
                return false;
            }
            finally
            {
                try
                {
                    if (document != null) document.Close(0);
                    if (wordApp != null) wordApp.Quit();
                }
                catch { }
            }
#pragma warning restore CA1416
        }

        [SupportedOSPlatform("windows")]
        private bool ConvertExcelWithInterop(string inputFilePath, string outputPath)
        {
#pragma warning disable CA1416
            Type? excelType = Type.GetTypeFromProgID("Excel.Application");
            if (excelType == null) return false;

            dynamic? excelApp = null;
            dynamic? workbook = null;

            try
            {
                excelApp = Activator.CreateInstance(excelType);
                if (excelApp == null) return false;
                excelApp.Visible = false;
                excelApp.ScreenUpdating = false;

                dynamic workbooks = excelApp.Workbooks;
                workbook = workbooks.Open(
                    Filename: inputFilePath,
                    UpdateLinks: 0,
                    ReadOnly: true
                );

                if (workbook == null) return false;

                workbook.ExportAsFixedFormat(
                    Type: 0,
                    Filename: outputPath,
                    Quality: 0
                );

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed MS Excel Interop conversion.");
                return false;
            }
            finally
            {
                try
                {
                    if (workbook != null) workbook.Close(false);
                    if (excelApp != null) excelApp.Quit();
                }
                catch { }
            }
#pragma warning restore CA1416
        }

        [SupportedOSPlatform("windows")]
        private bool ConvertPowerPointWithInterop(string inputFilePath, string outputPath)
        {
#pragma warning disable CA1416
            Type? pptType = Type.GetTypeFromProgID("PowerPoint.Application");
            if (pptType == null) return false;

            dynamic? pptApp = null;
            dynamic? presentation = null;

            try
            {
                pptApp = Activator.CreateInstance(pptType);
                if (pptApp == null) return false;

                dynamic presentations = pptApp.Presentations;
                presentation = presentations.Open(
                    FileName: inputFilePath,
                    ReadOnly: -1,
                    Untitled: 0,
                    WithWindow: 0
                );

                if (presentation == null) return false;

                presentation.SaveAs(outputPath, 32);

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed MS PowerPoint Interop conversion.");
                return false;
            }
            finally
            {
                try
                {
                    if (presentation != null) presentation.Close();
                    if (pptApp != null) pptApp.Quit();
                }
                catch { }
            }
#pragma warning restore CA1416
        }

        private string FindLibreOfficeSofficePath()
        {
            string[] searchPaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LibreOfficePortable", "App", "libreoffice", "program", "soffice.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LibreOffice", "program", "soffice.exe"),
                @"C:\Program Files\LibreOffice\program\soffice.exe",
                @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
                "/usr/bin/soffice",
                "/usr/bin/libreoffice"
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }
    }
}
