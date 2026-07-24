using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PdfUtility.App.ViewModels;

namespace PdfUtility.App.Views
{
    public partial class ConvertView : UserControl
    {
        public ConvertView()
        {
            InitializeComponent();
        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    if (DataContext is ConvertViewModel viewModel)
                    {
                        viewModel.HandleFileDrop(files[0]);
                    }
                }
            }
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select File to Convert",
                Filter = "All Supported Files|*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx;*.txt;*.csv;*.rtf;*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.webp|Office Documents|*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx|Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.webp|Plain Text & CSV|*.txt;*.csv|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                if (DataContext is ConvertViewModel viewModel)
                {
                    viewModel.SelectedFilePath = dialog.FileName;
                }
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            // The modern FolderBrowserDialog requires Windows-specific API or third-party WPF assemblies,
            // or we can fall back to a SaveFileDialog to prompt directory paths, or standard WinForms FolderBrowserDialog if referenced.
            // A common WPF alternative is using OpenFileDialog in Folder Selection mode in .NET 9:
            var dialog = new OpenFolderDialog
            {
                Title = "Select Output Directory",
                InitialDirectory = (DataContext is ConvertViewModel vm) ? vm.OutputDirectory : string.Empty
            };

            if (dialog.ShowDialog() == true)
            {
                if (DataContext is ConvertViewModel viewModel)
                {
                    viewModel.OutputDirectory = dialog.FolderName;
                }
            }
        }
    }
}
