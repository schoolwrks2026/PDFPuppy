using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PdfUtility.App.ViewModels;

namespace PdfUtility.App.Views
{
    public partial class WatermarkView : UserControl
    {
        public WatermarkView()
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
                    if (DataContext is WatermarkViewModel viewModel)
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
                Title = "Select PDF Document to Watermark",
                Filter = "PDF Documents (*.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                if (DataContext is WatermarkViewModel viewModel)
                {
                    viewModel.SelectedFilePath = dialog.FileName;
                }
            }
        }
    }
}
