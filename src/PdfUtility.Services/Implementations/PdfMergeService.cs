using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfUtility.Core.Exceptions;
using PdfUtility.Core.Interfaces;

namespace PdfUtility.Services.Implementations
{
    public class PdfMergeService : IPdfMergeService
    {
        public Task ValidateMergeInputAsync(IEnumerable<string> sourcePdfPaths, string outputPath)
        {
            if (sourcePdfPaths == null)
            {
                throw new SecurityAndValidationException("Source file list cannot be null.", "NULL_SOURCE_LIST");
            }

            var pathList = new List<string>(sourcePdfPaths);
            if (pathList.Count < 2)
            {
                throw new SecurityAndValidationException("Please select at least 2 PDF files to merge.", "INSUFFICIENT_FILES");
            }

            foreach (var path in pathList)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new SecurityAndValidationException("A selected file path is empty.", "EMPTY_FILE_PATH");
                }

                if (!File.Exists(path))
                {
                    throw new SecurityAndValidationException($"File not found: {path}", "FILE_NOT_FOUND");
                }

                var fileInfo = new FileInfo(path);
                if (fileInfo.Length == 0)
                {
                    throw new SecurityAndValidationException($"File is empty (0 bytes): {Path.GetFileName(path)}", "EMPTY_PDF");
                }

                if (!Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    throw new SecurityAndValidationException($"File is not a PDF: {Path.GetFileName(path)}", "INVALID_EXTENSION");
                }
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new SecurityAndValidationException("Output file path is not specified.", "NULL_OUTPUT_PATH");
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                throw new SecurityAndValidationException($"Output folder does not exist: {outputDirectory}", "OUTPUT_DIR_NOT_FOUND");
            }

            return Task.CompletedTask;
        }

        public async Task MergePdfFilesAsync(
            IEnumerable<string> sourcePdfPaths,
            string outputPath,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            await ValidateMergeInputAsync(sourcePdfPaths, outputPath);

            await Task.Run(() =>
            {
                var files = new List<string>(sourcePdfPaths);
                int totalFiles = files.Count;
                int currentFileIndex = 0;

                using var outputDocument = new PdfDocument();

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        // Open the source document
                        // In PdfSharp 6, PdfReader.Open returns PdfDocument
                        using var inputDocument = PdfReader.Open(file, PdfDocumentOpenMode.Import);

                        if (inputDocument.SecuritySettings.IsEncrypted)
                        {
                            throw new PdfMergeException(
                                $"File '{Path.GetFileName(file)}' is encrypted/password-protected. Cannot merge protected files.",
                                "ENCRYPTED_PDF"
                            );
                        }

                        int pageCount = inputDocument.PageCount;
                        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Import page
                            var page = inputDocument.Pages[pageIndex];
                            outputDocument.AddPage(page);
                        }
                    }
                    catch (PdfReaderException ex)
                    {
                        throw new PdfMergeException(
                            $"Failed to read or parse PDF '{Path.GetFileName(file)}'. It may be corrupted or password-protected.",
                            ex,
                            "CORRUPTED_PDF"
                        );
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && ex is not PdfMergeException)
                    {
                        throw new PdfMergeException(
                            $"Error processing '{Path.GetFileName(file)}': {ex.Message}",
                            ex,
                            "READ_ERROR"
                        );
                    }

                    currentFileIndex++;
                    double percentage = (double)currentFileIndex / totalFiles * 100.0;
                    progress?.Report(percentage);
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Check disk space before saving
                    var outputDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outputDir))
                    {
                        var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(outputPath)) ?? "/");
                        // Guess required size: we approximate it by summing input sizes (safe estimation)
                        long estimatedSize = 0;
                        foreach (var f in files)
                        {
                            estimatedSize += new FileInfo(f).Length;
                        }

                        if (driveInfo.AvailableFreeSpace < estimatedSize)
                        {
                            throw new PdfMergeException("Insufficient disk space to save the merged PDF.", "DISK_FULL");
                        }
                    }

                    outputDocument.Save(outputPath);
                }
                catch (Exception ex) when (ex is not PdfMergeException)
                {
                    throw new PdfMergeException(
                        $"Failed to save the merged PDF to '{outputPath}': {ex.Message}",
                        ex,
                        "SAVE_FAILED"
                    );
                }
            }, cancellationToken);
        }
    }
}
