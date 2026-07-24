using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PdfUtility.Core.Exceptions;
using PdfUtility.Services.Implementations;

namespace PdfUtility.Tests
{
    [TestFixture]
    public class FileConverterServiceTests
    {
        private FileConverterService _converterService = null!;
        private string _tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            _converterService = new FileConverterService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PdfConverterTests_" + Guid.NewGuid().ToString("N"));
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
        public void IsSupportedFileFormat_ValidFormats_ReturnsTrue()
        {
            Assert.That(_converterService.IsSupportedFileFormat("test.txt"), Is.True);
            Assert.That(_converterService.IsSupportedFileFormat("photo.jpg"), Is.True);
            Assert.That(_converterService.IsSupportedFileFormat("sheet.xlsx"), Is.True);
            Assert.That(_converterService.IsSupportedFileFormat("doc.docx"), Is.True);
        }

        [Test]
        public void IsSupportedFileFormat_InvalidFormats_ReturnsFalse()
        {
            Assert.That(_converterService.IsSupportedFileFormat("test.mp3"), Is.False);
            Assert.That(_converterService.IsSupportedFileFormat("run.exe"), Is.False);
            Assert.That(_converterService.IsSupportedFileFormat(string.Empty), Is.False);
        }

        [Test]
        public async Task ConvertTextToPdf_ValidTextFile_SuccessfullyConverts()
        {
            var textPath = Path.Combine(_tempDirectory, "sample.txt");
            var outputDir = _tempDirectory;
            var expectedPdfPath = Path.Combine(_tempDirectory, "sample.pdf");

            // Write test lines
            var lines = new[] { "Line 1 of text", "Line 2 of text with formatting.", "Done!" };
            File.WriteAllLines(textPath, lines);

            // Execute conversion
            await _converterService.ConvertFileToPdfAsync(textPath, outputDir, null, CancellationToken.None);

            // Verify output PDF
            Assert.That(File.Exists(expectedPdfPath), Is.True);
            Assert.That(new FileInfo(expectedPdfPath).Length, Is.GreaterThan(0));

            using (var doc = PdfSharp.Pdf.IO.PdfReader.Open(expectedPdfPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
            {
                Assert.That(doc.PageCount, Is.EqualTo(1));
            }
        }
    }
}
