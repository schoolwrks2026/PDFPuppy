using System;

namespace PdfUtility.Core.Models
{
    public class AppSettings
    {
        public string DefaultOutputFolder { get; set; } = string.Empty;
        public string Theme { get; set; } = "System"; // "Light", "Dark", "System"
        public bool OpenPdfAfterCompletion { get; set; } = true;
        public bool RememberLastFolder { get; set; } = true;
        public bool OverwriteExistingFiles { get; set; } = false;
        public string LastSelectedFolder { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
    }
}
