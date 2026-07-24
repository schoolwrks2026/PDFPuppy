using System;
using System.IO;
using System.Text.Json;
using PdfUtility.Core.Interfaces;
using PdfUtility.Core.Models;

namespace PdfUtility.Services.Implementations
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private AppSettings _currentSettings;
        private readonly object _lock = new();

        public SettingsService()
        {
            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PDFUtility"
            );
            Directory.CreateDirectory(appDataFolder);
            _settingsFilePath = Path.Combine(appDataFolder, "settings.json");

            _currentSettings = LoadSettingsFromFile();
        }

        public AppSettings GetSettings()
        {
            lock (_lock)
            {
                return new AppSettings
                {
                    DefaultOutputFolder = _currentSettings.DefaultOutputFolder,
                    Theme = _currentSettings.Theme,
                    OpenPdfAfterCompletion = _currentSettings.OpenPdfAfterCompletion,
                    RememberLastFolder = _currentSettings.RememberLastFolder,
                    OverwriteExistingFiles = _currentSettings.OverwriteExistingFiles,
                    LastSelectedFolder = _currentSettings.LastSelectedFolder,
                    Language = _currentSettings.Language
                };
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            lock (_lock)
            {
                _currentSettings = settings;
                try
                {
                    var json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_settingsFilePath, json);
                }
                catch (Exception ex)
                {
                    // Fail gracefully in production
                    System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
                }
            }
        }

        public void UpdateLastSelectedFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return;

            lock (_lock)
            {
                _currentSettings.LastSelectedFolder = folderPath;
                if (_currentSettings.RememberLastFolder)
                {
                    SaveSettings(_currentSettings);
                }
            }
        }

        private AppSettings LoadSettingsFromFile()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            // Return default settings
            return new AppSettings
            {
                DefaultOutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PDFUtility"),
                Theme = "System",
                OpenPdfAfterCompletion = true,
                RememberLastFolder = true,
                OverwriteExistingFiles = false,
                LastSelectedFolder = string.Empty,
                Language = "en"
            };
        }
    }
}
