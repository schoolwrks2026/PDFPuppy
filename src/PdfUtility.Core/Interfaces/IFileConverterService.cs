using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PdfUtility.Core.Interfaces
{
    public interface IFileConverterService
    {
        Task ConvertFileToPdfAsync(
            string inputFilePath,
            string outputDirectory,
            IProgress<double> progress,
            CancellationToken cancellationToken);

        Task ValidateConversionInputAsync(string inputFilePath, string outputDirectory);
        bool IsSupportedFileFormat(string filePath);
    }
}
