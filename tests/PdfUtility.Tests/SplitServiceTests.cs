using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PdfSharp.Pdf;
using PdfUtility.Core.Exceptions;
using PdfUtility.Services.Implementations;

namespace PdfUtility.Tests
{
    [TestFixture]
    public class SplitServiceTests
    {
        private PdfSplitService _splitService = null!;
        private string _tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            _splitService = new PdfSplitService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PdfSplitTests_" + Guid.NewGuid().ToString("N"));
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
        public void ValidateSplitInput_EmptyPath_ThrowsException()
        {
            Assert.ThrowsAsync<SecurityAndValidationException>(async () =>
            {
                await _splitService.ValidateSplitInputAsync(string.Empty, _tempDirectory, true, string.Empty);
            });
        }

        [Test]
        public void ValidateSplitInput_NonExistentPath_ThrowsException()
        {
            var nonExistentPath = Path.Combine(_tempDirectory, "ghost.pdf");
            Assert.ThrowsAsync<SecurityAndValidationException>(async () =>
            {
                await _splitService.ValidateSplitInputAsync(nonExistentPath, _tempDirectory, true, string.Empty);
            });
        }

        [Test]
        public void ValidateSplitInput_CustomRange_EmptyRange_ThrowsException()
        {
            var pdfPath = Path.Combine(_tempDirectory, "test.pdf");
            using (var doc = new PdfDocument())
            {
                doc.AddPage();
                doc.Save(pdfPath);
            }

            Assert.ThrowsAsync<SecurityAndValidationException>(async () =>
            {
                await _splitService.ValidateSplitInputAsync(pdfPath, _tempDirectory, false, string.Empty);
            });
        }

        [Test]
        public void ValidateSplitInput_CustomRange_MalformedRange_ThrowsException()
        {
            var pdfPath = Path.Combine(_tempDirectory, "test.pdf");
            using (var doc = new PdfDocument())
            {
                doc.AddPage();
                doc.Save(pdfPath);
            }

            Assert.ThrowsAsync<SecurityAndValidationException>(async () =>
            {
                await _splitService.ValidateSplitInputAsync(pdfPath, _tempDirectory, false, "abc-def");
            });
        }

        [Test]
        public async Task SplitPdf_ExtractAllPages_SuccessfullyCreatesSinglePageFiles()
        {
            var pdfPath = Path.Combine(_tempDirectory, "multi.pdf");
            using (var doc = new PdfDocument())
            {
                doc.AddPage(); // Page 1
                doc.AddPage(); // Page 2
                doc.Save(pdfPath);
            }

            // Split all
            await _splitService.SplitPdfFileAsync(pdfPath, _tempDirectory, true, string.Empty, null, CancellationToken.None);

            var page1Path = Path.Combine(_tempDirectory, "multi_Page_1.pdf");
            var page2Path = Path.Combine(_tempDirectory, "multi_Page_2.pdf");

            Assert.That(File.Exists(page1Path), Is.True);
            Assert.That(File.Exists(page2Path), Is.True);
        }

        [Test]
        public async Task SplitPdf_ExtractRange_SuccessfullyCreatesSubset()
        {
            var pdfPath = Path.Combine(_tempDirectory, "multi_range.pdf");
            using (var doc = new PdfDocument())
            {
                doc.AddPage(); // Page 1
                doc.AddPage(); // Page 2
                doc.AddPage(); // Page 3
                doc.Save(pdfPath);
            }

            // Extract range 1-2
            await _splitService.SplitPdfFileAsync(pdfPath, _tempDirectory, false, "1-2", null, CancellationToken.None);

            var subsetPath = Path.Combine(_tempDirectory, "multi_range_Subset.pdf");

            Assert.That(File.Exists(subsetPath), Is.True);
            using (var subsetDoc = PdfSharp.Pdf.IO.PdfReader.Open(subsetPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
            {
                Assert.That(subsetDoc.PageCount, Is.EqualTo(2));
            }
        }
    }
}
