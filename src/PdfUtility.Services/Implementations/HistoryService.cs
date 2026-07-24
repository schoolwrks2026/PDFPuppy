using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PdfUtility.Core.Interfaces;
using PdfUtility.Core.Models;

namespace PdfUtility.Services.Implementations
{
    public class HistoryService : IHistoryService
    {
        private readonly string _historyFilePath;
        private readonly List<HistoryEntry> _historyEntries = new();
        private readonly object _lock = new();

        public HistoryService()
        {
            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PDFUtility"
            );
            Directory.CreateDirectory(appDataFolder);
            _historyFilePath = Path.Combine(appDataFolder, "history.json");

            LoadHistoryFromFile();
        }

        public IEnumerable<HistoryEntry> GetHistory()
        {
            lock (_lock)
            {
                // Return only active (not soft deleted) entries, ordered by CreatedAt descending
                return _historyEntries
                    .Where(e => e.DeletedAt == null)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToList();
            }
        }

        public void AddEntry(HistoryEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            lock (_lock)
            {
                entry.CreatedAt = DateTime.UtcNow;
                entry.UpdatedAt = DateTime.UtcNow;
                _historyEntries.Add(entry);
                SaveHistoryToFile();
            }
        }

        public void ClearHistory()
        {
            lock (_lock)
            {
                // Soft delete all entries as per database standard
                foreach (var entry in _historyEntries)
                {
                    entry.DeletedAt = DateTime.UtcNow;
                    entry.UpdatedAt = DateTime.UtcNow;
                }
                SaveHistoryToFile();
            }
        }

        private void LoadHistoryFromFile()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    var list = JsonSerializer.Deserialize<List<HistoryEntry>>(json);
                    if (list != null)
                    {
                        _historyEntries.AddRange(list);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load history: {ex.Message}");
            }
        }

        private void SaveHistoryToFile()
        {
            try
            {
                var json = JsonSerializer.Serialize(_historyEntries, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save history: {ex.Message}");
            }
        }
    }
}
