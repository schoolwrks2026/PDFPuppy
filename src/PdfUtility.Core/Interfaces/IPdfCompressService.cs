using System;
using System.Threading;
using System.Threading.Tasks;

namespace PdfUtility.Core.Interfaces
{
    public interface IPdfCompressService
    {
        Task CompressPdfAsync(
            string inputPdfPath,
            string outputPath,
            string compressionLevel, // "Low", "Medium", "High"
            IProgress<double>? progress,
            CancellationToken cancellationToken);

        Task ValidateCompressInputAsync(string inputPdfPath, string outputPath);
    }
}
