using System;
using System.IO;
using NUnit.Framework;
using PdfSharp.Pdf;
using PdfUtility.App.ViewModels;
using PdfUtility.Core.Interfaces;
using PdfUtility.Services.Implementations;

namespace PdfUtility.Tests
{
    [TestFixture]
    public class ViewModelTests
    {
        private ISettingsService _settingsService = null!;
        private IHistoryService _historyService = null!;
        private IPdfMergeService _mergeService = null!;
        private IFileConverterService _converterService = null!;
        private INotificationService _notificationService = null!;
        private string _tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            _settingsService = new SettingsService();
            _historyService = new HistoryService();
            _mergeService = new PdfMergeService();
            _converterService = new FileConverterService();
            _notificationService = new NotificationService();

            _tempDirectory = Path.Combine(Path.GetTempPath(), "PdfViewModelTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch { }
        }

        [Test]
        public void ConvertViewModel_InitialState_Correct()
        {
            var vm = new ConvertViewModel(_converterService, _historyService, _settingsService, _notificationService);
            Assert.That(vm.HasSelectedFile, Is.False);
            Assert.That(vm.IsConverting, Is.False);
            Assert.That(vm.SelectedFileName, Is.EqualTo("No file selected"));
        }

        [Test]
        public void ConvertViewModel_HandleFileDrop_SupportedFormat_UpdatesSelectedFile()
        {
            var vm = new ConvertViewModel(_converterService, _historyService, _settingsService, _notificationService);
            var filePath = Path.Combine(_tempDirectory, "input.txt");
            File.WriteAllText(filePath, "test content");

            vm.HandleFileDrop(filePath);

            Assert.That(vm.HasSelectedFile, Is.True);
            Assert.That(vm.SelectedFilePath, Is.EqualTo(filePath));
            Assert.That(vm.SelectedFileName, Is.EqualTo("input.txt"));
        }

        [Test]
        public void MergeViewModel_AddPdfFile_ValidPdf_UpdatesList()
        {
            var vm = new MergeViewModel(_mergeService, _historyService, _settingsService, _notificationService);
            var pdfPath = Path.Combine(_tempDirectory, "test.pdf");

            // Generate valid header pdf
            using (var doc = new PdfDocument())
            {
                doc.AddPage();
                doc.Save(pdfPath);
            }

            vm.AddPdfFile(pdfPath);

            Assert.That(vm.HasFiles, Is.True);
            Assert.That(vm.SelectedPdfFiles.Count, Is.EqualTo(1));
            Assert.That(vm.SelectedPdfFiles[0].FileName, Is.EqualTo("test.pdf"));
            Assert.That(vm.TotalPageCount, Is.EqualTo(1));
        }

        [Test]
        public void MergeViewModel_MoveUpAndDown_ReordersCorrectly()
        {
            var vm = new MergeViewModel(_mergeService, _historyService, _settingsService, _notificationService);

            var item1 = new PdfItemViewModel("file1.pdf", 1, 1024);
            var item2 = new PdfItemViewModel("file2.pdf", 2, 2048);
            var item3 = new PdfItemViewModel("file3.pdf", 3, 4096);

            vm.SelectedPdfFiles.Add(item1);
            vm.SelectedPdfFiles.Add(item2);
            vm.SelectedPdfFiles.Add(item3);

            // Move item2 up
            vm.MoveUpCommand.Execute(item2);
            Assert.That(vm.SelectedPdfFiles[0], Is.EqualTo(item2));
            Assert.That(vm.SelectedPdfFiles[1], Is.EqualTo(item1));

            // Move item2 down
            vm.MoveDownCommand.Execute(item2);
            Assert.That(vm.SelectedPdfFiles[0], Is.EqualTo(item1));
            Assert.That(vm.SelectedPdfFiles[1], Is.EqualTo(item2));
        }
    }
}
