using System.Windows;
using PdfUtility.App.ViewModels;

namespace PdfUtility.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
