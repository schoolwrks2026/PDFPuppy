using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PdfUtility.App.ViewModels;

namespace PdfUtility.App.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Default Saving Directory",
                InitialDirectory = (DataContext is SettingsViewModel vm) ? vm.DefaultOutputFolder : string.Empty
            };

            if (dialog.ShowDialog() == true)
            {
                if (DataContext is SettingsViewModel viewModel)
                {
                    viewModel.DefaultOutputFolder = dialog.FolderName;
                }
            }
        }
    }
}
