using System;
using System.Collections.Generic;
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
    public class MergeServiceTests
    {
        private PdfMergeService _mergeService = null!;
        private string _tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            _mergeService = new PdfMergeService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PdfUtilityTests_" + Guid.NewGuid().ToString("N"));
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
        public void ValidateMergeInput_NullSourceList_ThrowsException()
        {
            var outputPath = Path.Combine(_tempDirectory, "out.pdf");
            Assert.ThrowsAsync<SecurityAndValidationException>(async () =>
            {
                await _mergeService.ValidateMergeInputAsync(null!, outputPath);
            });
        }

        [Test]
        public void ValidateMergeInput_LessThenTwoFiles_ThrowsException()
        {
            var outputPath = Path.Combine(_tempDirectory, "out.pdf");
            var files = new[] { Path.Combine(_tempDirectory, "one.pdf") };
            Assert.ThrowsAsync<SecurityAndValidationException>(async () =>
            {
                await _mergeService.ValidateMergeInputAsync(files, outputPath);
            });
        }

        [Test]
        public void ValidateMergeInput_FileNotFound_ThrowsException()
        {
            var outputPath = Path.Combine(_tempDirectory, "out.pdf");
            var files = new[] {
                Path.Combine(_tempDirectory, "nonexistent1.pdf"),
                Path.Combine(_tempDirectory, "nonexistent2.pdf")
            };
            Assert.ThrowsAsync<SecurityAndValidationException>(async () =>
            {
                await _mergeService.ValidateMergeInputAsync(files, outputPath);
            });
        }

        [Test]
        public async Task MergePdfFiles_ValidPdfs_SuccessfullyMerges()
        {
            // Create two valid PDF documents dynamically using PDFsharp
            var pdfPath1 = Path.Combine(_tempDirectory, "doc1.pdf");
            var pdfPath2 = Path.Combine(_tempDirectory, "doc2.pdf");
            var outputPath = Path.Combine(_tempDirectory, "merged.pdf");

            using (var doc1 = new PdfDocument())
            {
                doc1.AddPage();
                doc1.Save(pdfPath1);
            }

            using (var doc2 = new PdfDocument())
            {
                doc2.AddPage();
                doc2.AddPage();
                doc2.Save(pdfPath2);
            }

            // Verify input validation passes
            Assert.DoesNotThrowAsync(async () =>
            {
                await _mergeService.ValidateMergeInputAsync(new[] { pdfPath1, pdfPath2 }, outputPath);
            });

            // Execute merge
            var progress = new Progress<double>();
            await _mergeService.MergePdfFilesAsync(new[] { pdfPath1, pdfPath2 }, outputPath, progress, CancellationToken.None);

            // Verify merged file exists and contains 3 pages (1 + 2)
            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));

            using (var mergedDoc = PdfSharp.Pdf.IO.PdfReader.Open(outputPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
            {
                Assert.That(mergedDoc.PageCount, Is.EqualTo(3));
            }
        }
    }
}
