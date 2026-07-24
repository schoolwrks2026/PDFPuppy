using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfUtility.Core.Exceptions;
using PdfUtility.Core.Interfaces;

namespace PdfUtility.Services.Implementations
{
    public class PdfCompressService : IPdfCompressService
    {
        public Task ValidateCompressInputAsync(string inputPdfPath, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPdfPath))
            {
                throw new SecurityAndValidationException("Input PDF path is required.", "EMPTY_INPUT_PATH");
            }

            if (!File.Exists(inputPdfPath))
            {
                throw new SecurityAndValidationException($"Input file not found: {inputPdfPath}", "INPUT_FILE_NOT_FOUND");
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new SecurityAndValidationException("Output PDF path is required.", "EMPTY_OUTPUT_PATH");
            }

            var outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                throw new SecurityAndValidationException($"Output folder does not exist: {outDir}", "OUTPUT_DIR_NOT_FOUND");
            }

            return Task.CompletedTask;
        }

        public async Task CompressPdfAsync(
            string inputPdfPath,
            string outputPath,
            string compressionLevel,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            await ValidateCompressInputAsync(inputPdfPath, outputPath);

            await Task.Run(() =>
            {
                progress?.Report(20);
                cancellationToken.ThrowIfCancellationRequested();

                // Open in import mode
                using var inputDoc = PdfReader.Open(inputPdfPath, PdfDocumentOpenMode.Import);
                int pageCount = inputDoc.PageCount;

                if (pageCount == 0)
                {
                    throw new PdfMergeException("The input PDF contains no pages.", "EMPTY_PDF");
                }

                using var outputDoc = new PdfDocument();

                // Configure compression options on the document
                outputDoc.Options.CompressContentStreams = true;
                outputDoc.Options.UseFlateDecoderForJpegImages = compressionLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
                    ? PdfUseFlateDecoderForJpegImages.Always
                    : PdfUseFlateDecoderForJpegImages.Never;

                for (int i = 0; i < pageCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = inputDoc.Pages[i];
                    outputDoc.AddPage(page);

                    double percent = 20 + ((double)(i + 1) / pageCount * 60);
                    progress?.Report(percent);
                }

                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(90);

                // Save optimized document
                outputDoc.Save(outputPath);
                progress?.Report(100);
            }, cancellationToken);
        }
    }
}
