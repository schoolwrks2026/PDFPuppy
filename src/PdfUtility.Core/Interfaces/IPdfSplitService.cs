using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PdfUtility.Core.Interfaces
{
    public interface IPdfSplitService
    {
        Task SplitPdfFileAsync(
            string inputPdfPath,
            string outputDirectory,
            bool splitAllPages,
            string pageRange,
            IProgress<double>? progress,
            CancellationToken cancellationToken);

        Task ValidateSplitInputAsync(string inputPdfPath, string outputDirectory, bool splitAllPages, string pageRange);
    }
}
