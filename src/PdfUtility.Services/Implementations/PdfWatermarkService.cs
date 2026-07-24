using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfUtility.Core.Exceptions;
using PdfUtility.Core.Interfaces;

namespace PdfUtility.Services.Implementations
{
    public class PdfWatermarkService : IPdfWatermarkService
    {
        public Task ValidateWatermarkInputAsync(string inputPdfPath, string outputPath, string watermarkText)
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

            if (string.IsNullOrWhiteSpace(watermarkText))
            {
                throw new SecurityAndValidationException("Watermark text is required.", "EMPTY_TEXT");
            }

            var outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                throw new SecurityAndValidationException($"Output folder does not exist: {outDir}", "OUTPUT_DIR_NOT_FOUND");
            }

            return Task.CompletedTask;
        }

        public async Task ApplyTextWatermarkAsync(
            string inputPdfPath,
            string outputPath,
            string watermarkText,
            string fontName,
            double fontSize,
            double opacity,
            double rotationAngle,
            string colorHex,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            await ValidateWatermarkInputAsync(inputPdfPath, outputPath, watermarkText);

            await Task.Run(() =>
            {
                // Open the document in Modify mode
                using var document = PdfReader.Open(inputPdfPath, PdfDocumentOpenMode.Modify);
                int pageCount = document.PageCount;

                if (pageCount == 0)
                {
                    throw new PdfMergeException("The input PDF contains no pages.", "EMPTY_PDF");
                }

                // Parse Font & Color parameters
                if (string.IsNullOrWhiteSpace(fontName)) fontName = "Helvetica";
                if (fontSize <= 0) fontSize = 48;
                if (opacity < 0 || opacity > 1) opacity = 0.3; // Default 30% transparency

                var font = new XFont(fontName, fontSize, XFontStyleEx.Bold);
                XColor color;
                try
                {
                    // Default to grey if hex parse fails
                    if (string.IsNullOrWhiteSpace(colorHex))
                    {
                        color = XColors.Gray;
                    }
                    else
                    {
                        var sysColor = System.Drawing.ColorTranslator.FromHtml(colorHex);
                        color = XColor.FromArgb(sysColor.A, sysColor.R, sysColor.G, sysColor.B);
                    }
                }
                catch
                {
                    color = XColors.Gray;
                }

                // Apply alpha opacity channel
                color.A = opacity;

                var brush = new XSolidBrush(color);

                for (int i = 0; i < pageCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = document.Pages[i];

                    // XGraphics.FromPdfPage on PDFsharp v6 in modify mode can overlay or prepend drawings
                    using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
                    {
                        var size = gfx.PageSize;

                        // Save state
                        var state = gfx.Save();

                        // Center translation
                        gfx.TranslateTransform(size.Width / 2, size.Height / 2);
                        gfx.RotateTransform(rotationAngle);

                        // Draw centered text
                        var format = new XStringFormat
                        {
                            Alignment = XStringAlignment.Center,
                            LineAlignment = XLineAlignment.Center
                        };

                        gfx.DrawString(watermarkText, font, brush, 0, 0, format);

                        // Restore graphics state
                        gfx.Restore(state);
                    }

                    progress?.Report((double)(i + 1) / pageCount * 100.0);
                }

                cancellationToken.ThrowIfCancellationRequested();
                document.Save(outputPath);
            }, cancellationToken);
        }
    }
}
