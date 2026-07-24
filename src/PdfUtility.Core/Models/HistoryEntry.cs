using System;

namespace PdfUtility.Core.Models
{
    public class HistoryEntry
    {
        public Guid UUID { get; set; } = Guid.NewGuid();
        public string TenantId { get; set; } = "default"; // Configured for future-proofing multi-tenant context
        public string ActionType { get; set; } = string.Empty; // e.g., "CONVERT", "MERGE"
        public string SourceFiles { get; set; } = string.Empty; // Comma separated list of files
        public string OutputPath { get; set; } = string.Empty;
        public long TotalFileSize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = "SystemUser";
        public string UpdatedBy { get; set; } = "SystemUser";
        public DateTime? DeletedAt { get; set; }
        public int Version { get; set; } = 1;
        public string Status { get; set; } = "Success"; // Success, Failed
        public string? ErrorMessage { get; set; }
    }
}
