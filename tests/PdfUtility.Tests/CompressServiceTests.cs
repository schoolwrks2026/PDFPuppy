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
    public class CompressServiceTests
    {
        private PdfCompressService _compressService = null!;
        private string _tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            _compressService = new PdfCompressService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PdfCompressTests_" + Guid.NewGuid().ToString("N"));
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
        public void ValidateCompressInput_EmptyInput_ThrowsException()
        {
            var outputPath = Path.Combine(_tempDirectory, "out.pdf");
            Assert.ThrowsAsync<SecurityAndValidationException>(async () =>
            {
                await _compressService.ValidateCompressInputAsync(string.Empty, outputPath);
            });
        }

        [Test]
        public async Task CompressPdf_ValidParameters_SuccessfullyOptimizes()
        {
            var pdfPath = Path.Combine(_tempDirectory, "heavy.pdf");
            using (var doc = new PdfDocument())
            {
                doc.AddPage();
                doc.Save(pdfPath);
            }

            var outputPath = Path.Combine(_tempDirectory, "compact.pdf");

            await _compressService.CompressPdfAsync(pdfPath, outputPath, "High", null, CancellationToken.None);

            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));
        }
    }
}
