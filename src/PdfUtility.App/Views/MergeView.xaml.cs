using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PdfUtility.App.ViewModels;

namespace PdfUtility.App.Views
{
    public partial class MergeView : UserControl
    {
        public MergeView()
        {
            InitializeComponent();
        }

        private void FileList_DragOver(object sender, DragEventArgs e)
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

        private void FileList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    if (DataContext is MergeViewModel viewModel)
                    {
                        foreach (var file in files)
                        {
                            viewModel.AddPdfFile(file);
                        }
                    }
                }
            }
        }

        private void AddPdfFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Add PDF Documents to Merge List",
                Filter = "PDF Documents (*.pdf)|*.pdf",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                if (DataContext is MergeViewModel viewModel)
                {
                    foreach (var file in dialog.FileNames)
                    {
                        viewModel.AddPdfFile(file);
                    }
                }
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Output Directory",
                InitialDirectory = (DataContext is MergeViewModel vm) ? vm.OutputDirectory : string.Empty
            };

            if (dialog.ShowDialog() == true)
            {
                if (DataContext is MergeViewModel viewModel)
                {
                    viewModel.OutputDirectory = dialog.FolderName;
                }
            }
        }
    }
}
