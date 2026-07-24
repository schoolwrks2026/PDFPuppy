using System;
using System.Threading;
using System.Threading.Tasks;

namespace PdfUtility.Core.Interfaces
{
    public interface IPdfWatermarkService
    {
        Task ApplyTextWatermarkAsync(
            string inputPdfPath,
            string outputPath,
            string watermarkText,
            string fontName,
            double fontSize,
            double opacity,
            double rotationAngle,
            string colorHex,
            IProgress<double>? progress,
            CancellationToken cancellationToken);

        Task ValidateWatermarkInputAsync(string inputPdfPath, string outputPath, string watermarkText);
    }
}
