using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfUtility.Core.Exceptions;
using PdfUtility.Core.Interfaces;

namespace PdfUtility.Services.Implementations
{
    public class PdfSplitService : IPdfSplitService
    {
        public Task ValidateSplitInputAsync(string inputPdfPath, string outputDirectory, bool splitAllPages, string pageRange)
        {
            if (string.IsNullOrWhiteSpace(inputPdfPath))
            {
                throw new SecurityAndValidationException("Input PDF path is required.", "EMPTY_INPUT_PATH");
            }

            if (!File.Exists(inputPdfPath))
            {
                throw new SecurityAndValidationException($"File not found: {inputPdfPath}", "INPUT_FILE_NOT_FOUND");
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new SecurityAndValidationException("Output directory is required.", "EMPTY_OUTPUT_DIR");
            }

            if (!Directory.Exists(outputDirectory))
            {
                throw new SecurityAndValidationException($"Output directory does not exist: {outputDirectory}", "OUTPUT_DIR_NOT_FOUND");
            }

            if (!splitAllPages)
            {
                if (string.IsNullOrWhiteSpace(pageRange))
                {
                    throw new SecurityAndValidationException("Please specify a page range (e.g. 1-3, 5).", "EMPTY_PAGE_RANGE");
                }

                // Verify page range syntax roughly
                try
                {
                    ParsePageRange(pageRange, 100000); // Test parse with a large max pages
                }
                catch (Exception ex)
                {
                    throw new SecurityAndValidationException($"Invalid page range format: {ex.Message}", "INVALID_PAGE_RANGE");
                }
            }

            return Task.CompletedTask;
        }

        public async Task SplitPdfFileAsync(
            string inputPdfPath,
            string outputDirectory,
            bool splitAllPages,
            string pageRange,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            await ValidateSplitInputAsync(inputPdfPath, outputDirectory, splitAllPages, pageRange);

            await Task.Run(() =>
            {
                using var inputDocument = PdfReader.Open(inputPdfPath, PdfDocumentOpenMode.Import);
                int totalPages = inputDocument.PageCount;

                if (totalPages == 0)
                {
                    throw new PdfMergeException("The input PDF contains no pages.", "EMPTY_PDF");
                }

                List<int> pagesToExtract;
                if (splitAllPages)
                {
                    pagesToExtract = Enumerable.Range(1, totalPages).ToList();
                }
                else
                {
                    pagesToExtract = ParsePageRange(pageRange, totalPages);
                    if (pagesToExtract.Count == 0)
                    {
                        throw new SecurityAndValidationException("No valid pages were resolved from the specified range.", "EMPTY_RESOLVED_PAGES");
                    }
                }

                int processedCount = 0;
                string baseFileName = Path.GetFileNameWithoutExtension(inputPdfPath);

                if (splitAllPages)
                {
                    // Split mode A: Extract each page as its own single-page document
                    for (int i = 0; i < pagesToExtract.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int pageNum = pagesToExtract[i];
                        using (var outputDoc = new PdfDocument())
                        {
                            outputDoc.AddPage(inputDocument.Pages[pageNum - 1]);
                            var outPath = Path.Combine(outputDirectory, $"{baseFileName}_Page_{pageNum}.pdf");
                            outputDoc.Save(outPath);
                        }

                        processedCount++;
                        progress?.Report((double)processedCount / pagesToExtract.Count * 100.0);
                    }
                }
                else
                {
                    // Split mode B: Extract specified pages as a single sub-document
                    using (var outputDoc = new PdfDocument())
                    {
                        for (int i = 0; i < pagesToExtract.Count; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            int pageNum = pagesToExtract[i];
                            outputDoc.AddPage(inputDocument.Pages[pageNum - 1]);

                            processedCount++;
                            progress?.Report((double)processedCount / pagesToExtract.Count * 90.0);
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        var outPath = Path.Combine(outputDirectory, $"{baseFileName}_Subset.pdf");
                        outputDoc.Save(outPath);
                        progress?.Report(100.0);
                    }
                }
            }, cancellationToken);
        }

        private List<int> ParsePageRange(string pageRange, int maxPages)
        {
            var pages = new HashSet<int>();
            var parts = pageRange.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var cleanPart = part.Trim();
                if (cleanPart.Contains('-'))
                {
                    var rangeParts = cleanPart.Split('-');
                    if (rangeParts.Length != 2)
                    {
                        throw new FormatException($"Range '{cleanPart}' is malformed.");
                    }

                    if (!int.TryParse(rangeParts[0].Trim(), out int start) ||
                        !int.TryParse(rangeParts[1].Trim(), out int end))
                    {
                        throw new FormatException($"Range values in '{cleanPart}' must be integers.");
                    }

                    if (start <= 0 || end <= 0 || start > end)
                    {
                        throw new FormatException($"Range '{cleanPart}' must be positive, ordered integers.");
                    }

                    for (int i = start; i <= end; i++)
                    {
                        if (i <= maxPages)
                        {
                            pages.Add(i);
                        }
                    }
                }
                else
                {
                    if (!int.TryParse(cleanPart, out int page))
                    {
                        throw new FormatException($"Page number '{cleanPart}' is not a valid integer.");
                    }

                    if (page <= 0)
                    {
                        throw new FormatException("Page numbers must be greater than 0.");
                    }

                    if (page <= maxPages)
                    {
                        pages.Add(page);
                    }
                }
            }

            return pages.OrderBy(p => p).ToList();
        }
    }
}
