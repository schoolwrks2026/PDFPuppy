using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PdfUtility.Core.Interfaces
{
    public interface IPdfMergeService
    {
        Task MergePdfFilesAsync(
            IEnumerable<string> sourcePdfPaths,
            string outputPath,
            IProgress<double> progress,
            CancellationToken cancellationToken);

        Task ValidateMergeInputAsync(IEnumerable<string> sourcePdfPaths, string outputPath);
    }
}
