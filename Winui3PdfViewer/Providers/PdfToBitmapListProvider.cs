using PdfToBitmapList;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Winui3PdfViewer.Interfaces;

namespace Winui3PdfViewer.Providers
{
    /// <summary>
    /// Provides bitmap conversion services for PDF files.
    /// </summary>
    public sealed class PdfToBitmapListProvider : IBitmapProvider
    {
        private const int MinDpi = 1;
        private const int MaxDpi = 2400;

        private bool UseLocalFiles { get; }
        private string? LocalPath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfToBitmapListProvider"/> class.
        /// </summary>
        /// <param name="localPath">Optional path to save bitmaps as local files. If null or empty, bitmaps are kept in memory.</param>
        public PdfToBitmapListProvider(string? localPath)
        {
            LocalPath = localPath;
            UseLocalFiles = !string.IsNullOrWhiteSpace(localPath);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="dpi"/> is out of valid range.</exception>
        /// <exception cref="InvalidOperationException">Thrown when local path is required but not accessible.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the PDF file cannot be accessed.</exception>
        public async Task<BitmapResult> GetBitmapsAsync(StorageFile file, int dpi, CancellationToken cancellationToken = default)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (dpi < MinDpi || dpi > MaxDpi)
                throw new ArgumentOutOfRangeException(nameof(dpi), dpi, $"DPI must be between {MinDpi} and {MaxDpi}.");

            if (string.IsNullOrWhiteSpace(file.Path))
                throw new ArgumentException("File path is required but is null or empty.", nameof(file));

            if (!File.Exists(file.Path))
                throw new FileNotFoundException($"PDF file not found at path: {file.Path}", file.Path);

            if (UseLocalFiles && string.IsNullOrWhiteSpace(LocalPath))
                throw new InvalidOperationException("Local path is required but not configured.");

            if (UseLocalFiles && !Directory.Exists(LocalPath))
            {
                try
                {
                    Directory.CreateDirectory(LocalPath!);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create local path directory: {LocalPath}", ex);
                }
            }

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (UseLocalFiles)
                    {
                        var bitmapPaths = Pdf2Bmp.Split(file.Path, LocalPath, dpi);
                        return new BitmapResult { FilePaths = bitmapPaths };
                    }
                    else
                    {
                        var streams = Pdf2Bmp.Split(file.Path, dpi);
                        var bitmaps = streams.Select(stream =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            return new Bitmap(stream);
                        }).ToList();

                        return new BitmapResult { Bitmaps = bitmaps };
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    throw new InvalidOperationException($"Failed to convert PDF to bitmaps: {file.Name}", ex);
                }
            }, cancellationToken);
        }
    }
}