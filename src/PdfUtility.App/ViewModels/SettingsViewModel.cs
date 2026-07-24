using System;
using System.IO;
using System.Windows.Input;
using PdfUtility.App.Helpers;
using PdfUtility.Core.Interfaces;
using PdfUtility.Core.Models;

namespace PdfUtility.App.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;

        private string _defaultOutputFolder = string.Empty;
        private string _theme = "System";
        private bool _openPdfAfterCompletion;
        private bool _rememberLastFolder;
        private bool _overwriteExistingFiles;

        public string DefaultOutputFolder
        {
            get => _defaultOutputFolder;
            set
            {
                if (SetProperty(ref _defaultOutputFolder, value))
                {
                    SaveSettings();
                }
            }
        }

        public string Theme
        {
            get => _theme;
            set
            {
                if (SetProperty(ref _theme, value))
                {
                    SaveSettings();
                }
            }
        }

        public bool OpenPdfAfterCompletion
        {
            get => _openPdfAfterCompletion;
            set
            {
                if (SetProperty(ref _openPdfAfterCompletion, value))
                {
                    SaveSettings();
                }
            }
        }

        public bool RememberLastFolder
        {
            get => _rememberLastFolder;
            set
            {
                if (SetProperty(ref _rememberLastFolder, value))
                {
                    SaveSettings();
                }
            }
        }

        public bool OverwriteExistingFiles
        {
            get => _overwriteExistingFiles;
            set
            {
                if (SetProperty(ref _overwriteExistingFiles, value))
                {
                    SaveSettings();
                }
            }
        }

        public ICommand BrowseFolderCommand { get; }

        public SettingsViewModel(
            ISettingsService settingsService,
            INotificationService notificationService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            BrowseFolderCommand = new RelayCommand(BrowseFolder);

            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = _settingsService.GetSettings();
            _defaultOutputFolder = settings.DefaultOutputFolder;
            _theme = settings.Theme;
            _openPdfAfterCompletion = settings.OpenPdfAfterCompletion;
            _rememberLastFolder = settings.RememberLastFolder;
            _overwriteExistingFiles = settings.OverwriteExistingFiles;
        }

        public void UpdateThemeBinding(string theme)
        {
            SetProperty(ref _theme, theme, nameof(Theme));
        }

        private void SaveSettings()
        {
            var settings = new AppSettings
            {
                DefaultOutputFolder = DefaultOutputFolder,
                Theme = Theme,
                OpenPdfAfterCompletion = OpenPdfAfterCompletion,
                RememberLastFolder = RememberLastFolder,
                OverwriteExistingFiles = OverwriteExistingFiles
            };

            _settingsService.SaveSettings(settings);
        }

        private void BrowseFolder()
        {
            // Windows native folder browsing can be handled in UI.
        }
    }
}
