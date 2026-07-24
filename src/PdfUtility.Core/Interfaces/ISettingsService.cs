using PdfUtility.Core.Models;

namespace PdfUtility.Core.Interfaces
{
    public interface ISettingsService
    {
        AppSettings GetSettings();
        void SaveSettings(AppSettings settings);
        void UpdateLastSelectedFolder(string folderPath);
    }
}
