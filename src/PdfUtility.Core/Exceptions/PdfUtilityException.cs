using System;

namespace PdfUtility.Core.Exceptions
{
    public class PdfUtilityException : Exception
    {
        public string ErrorCode { get; }

        public PdfUtilityException(string message, string errorCode = "GENERAL_ERROR") : base(message)
        {
            ErrorCode = errorCode;
        }

        public PdfUtilityException(string message, Exception innerException, string errorCode = "GENERAL_ERROR")
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    public class FileConversionException : PdfUtilityException
    {
        public string FilePath { get; }

        public FileConversionException(string message, string filePath, string errorCode = "CONVERSION_FAILED")
            : base(message, errorCode)
        {
            FilePath = filePath;
        }

        public FileConversionException(string message, string filePath, Exception innerException, string errorCode = "CONVERSION_FAILED")
            : base(message, innerException, errorCode)
        {
            FilePath = filePath;
        }
    }

    public class PdfMergeException : PdfUtilityException
    {
        public PdfMergeException(string message, string errorCode = "MERGE_FAILED") : base(message, errorCode) { }

        public PdfMergeException(string message, Exception innerException, string errorCode = "MERGE_FAILED")
            : base(message, innerException, errorCode) { }
    }

    public class SecurityAndValidationException : PdfUtilityException
    {
        public SecurityAndValidationException(string message, string errorCode = "VALIDATION_ERROR") : base(message, errorCode) { }
    }
}
