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
    public class WatermarkServiceTests
    {
        private PdfWatermarkService _watermarkService = null!;
        private string _tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            _watermarkService = new PdfWatermarkService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PdfWatermarkTests_" + Guid.NewGuid().ToString("N"));
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
        public void ValidateWatermarkInput_EmptyText_ThrowsException()
        {
            var pdfPath = Path.Combine(_tempDirectory, "test.pdf");
            using (var doc = new PdfDocument())
            {
                doc.AddPage();
                doc.Save(pdfPath);
            }

            var outputPath = Path.Combine(_tempDirectory, "watermarked.pdf");

            Assert.ThrowsAsync<SecurityAndValidationException>(async () =>
            {
                await _watermarkService.ValidateWatermarkInputAsync(pdfPath, outputPath, string.Empty);
            });
        }

        [Test]
        public async Task ApplyWatermark_ValidParameters_SuccessfullyWatermarks()
        {
            var pdfPath = Path.Combine(_tempDirectory, "source.pdf");
            using (var doc = new PdfDocument())
            {
                doc.AddPage();
                doc.Save(pdfPath);
            }

            var outputPath = Path.Combine(_tempDirectory, "watermarked.pdf");

            await _watermarkService.ApplyTextWatermarkAsync(
                pdfPath,
                outputPath,
                "SAMPLE",
                "Arial",
                36,
                0.2,
                -30,
                "#00FF00",
                null,
                CancellationToken.None
            );

            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));
        }
    }
}
