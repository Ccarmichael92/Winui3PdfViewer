using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Winui3PdfViewer.Interfaces;

namespace Winui3PdfViewer.Providers
{
    /// <summary>
    /// Provides bitmap conversion services for TIFF image files.
    /// Uses Windows Imaging Component (WIC) when available, with automatic fallback to System.Drawing.
    /// </summary>
    public sealed class TiffToBitmapListProvider : IBitmapProvider
    {
        private const int MinDpi = 1;
        private const int MaxDpi = 2400;

        private bool UseLocalFiles { get; }
        private string? LocalPath { get; }
        private static bool? _isWicTiffCodecAvailable;
        private static readonly object _codecCheckLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="TiffToBitmapListProvider"/> class.
        /// </summary>
        /// <param name="localPath">Optional path to save bitmaps as local files. If null or empty, bitmaps are kept in memory.</param>
        public TiffToBitmapListProvider(string? localPath)
        {
            LocalPath = localPath;
            UseLocalFiles = !string.IsNullOrWhiteSpace(localPath);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="dpi"/> is out of valid range.</exception>
        /// <exception cref="InvalidOperationException">Thrown when local path is required but not accessible.</exception>
        public async Task<BitmapResult> GetBitmapsAsync(StorageFile file, int dpi, CancellationToken cancellationToken = default)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (dpi < MinDpi || dpi > MaxDpi)
                throw new ArgumentOutOfRangeException(nameof(dpi), dpi, $"DPI must be between {MinDpi} and {MaxDpi}.");

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

            // Try WIC first if available, otherwise fall back to System.Drawing
            bool useWic = await IsWicTiffCodecAvailableAsync();

            if (useWic)
            {
                try
                {
                    return await GetBitmapsUsingWicAsync(file, dpi, cancellationToken);
                }
                catch (COMException ex)
                {
                    Debug.WriteLine($"[TiffProvider] WIC failed with {ex.HResult:X}, falling back to System.Drawing");
                    // WIC failed, mark it as unavailable and fall back
                    lock (_codecCheckLock)
                    {
                        _isWicTiffCodecAvailable = false;
                    }
                }
            }

            // Fallback to System.Drawing (works on all Windows versions)
            return await GetBitmapsUsingSystemDrawingAsync(file, dpi, cancellationToken);
        }

        /// <summary>
        /// Checks if WIC TIFF codec is available on this system (cached result).
        /// </summary>
        private static async Task<bool> IsWicTiffCodecAvailableAsync()
        {
            lock (_codecCheckLock)
            {
                if (_isWicTiffCodecAvailable.HasValue)
                    return _isWicTiffCodecAvailable.Value;
            }

            try
            {
                // Try to get the TIFF decoder info to verify codec availability
                var decoderInfos = BitmapDecoder.GetDecoderInformationEnumerator();
                foreach (var info in decoderInfos)
                {
                    if (info.CodecId == BitmapDecoder.TiffDecoderId)
                    {
                        lock (_codecCheckLock)
                        {
                            _isWicTiffCodecAvailable = true;
                        }
                        Debug.WriteLine("[TiffProvider] WIC TIFF codec is available");
                        return await Task.FromResult(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TiffProvider] WIC codec check failed: {ex.Message}");
            }

            lock (_codecCheckLock)
            {
                _isWicTiffCodecAvailable = false;
            }
            Debug.WriteLine("[TiffProvider] WIC TIFF codec not available, will use System.Drawing fallback");
            return false;
        }

        /// <summary>
        /// Converts TIFF using Windows Imaging Component (faster, async).
        /// </summary>
        private async Task<BitmapResult> GetBitmapsUsingWicAsync(StorageFile file, int dpi, CancellationToken cancellationToken)
        {
            var filePaths = new List<string>();
            var bitmaps = new List<Bitmap>();

            try
            {
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    BitmapDecoder decoder;
                    try
                    {
                        decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.TiffDecoderId, stream);
                    }
                    catch (COMException ex) when (ex.HResult == unchecked((int)0x88982F50))
                    {
                        throw new InvalidOperationException("The file does not appear to be a valid TIFF image.", ex);
                    }
                    catch (COMException ex) when (ex.HResult == unchecked((int)0x88982F41))
                    {
                        throw new InvalidOperationException("TIFF codec is not available via WIC.", ex);
                    }

                    for (uint i = 0; i < decoder.FrameCount; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var frame = await decoder.GetFrameAsync(i);

                        using (var softwareBitmap = await frame.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied))
                        {
                            if (UseLocalFiles)
                            {
                                string outputPath = Path.Combine(LocalPath!, $"frame_{i}.png");

                                using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                                using (IRandomAccessStream outStream = fs.AsRandomAccessStream())
                                {
                                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
                                    encoder.SetSoftwareBitmap(softwareBitmap);

                                    var properties = new BitmapPropertySet
                                    {
                                        { "System.Image.HorizontalResolution", new BitmapTypedValue((double)dpi, Windows.Foundation.PropertyType.Double) },
                                        { "System.Image.VerticalResolution", new BitmapTypedValue((double)dpi, Windows.Foundation.PropertyType.Double) }
                                    };
                                    await encoder.BitmapProperties.SetPropertiesAsync(properties);
                                    await encoder.FlushAsync();
                                }

                                filePaths.Add(outputPath);
                            }
                            else
                            {
                                var buffer = new byte[softwareBitmap.PixelWidth * softwareBitmap.PixelHeight * 4];
                                softwareBitmap.CopyToBuffer(buffer.AsBuffer());

                                var bmp = new Bitmap(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight, PixelFormat.Format32bppArgb);
                                bmp.SetResolution(dpi, dpi);

                                var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
                                try
                                {
                                    Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                                }
                                finally
                                {
                                    bmp.UnlockBits(bmpData);
                                }

                                bitmaps.Add(bmp);
                            }
                        }
                    }
                }

                return new BitmapResult
                {
                    FilePaths = UseLocalFiles ? filePaths : null,
                    Bitmaps = UseLocalFiles ? null : bitmaps
                };
            }
            catch
            {
                // Clean up bitmaps if an error occurred
                foreach (var bitmap in bitmaps)
                {
                    bitmap?.Dispose();
                }
                throw;
            }
        }

        /// <summary>
        /// Converts TIFF using System.Drawing (works on all Windows versions).
        /// </summary>
        private async Task<BitmapResult> GetBitmapsUsingSystemDrawingAsync(StorageFile file, int dpi, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var filePaths = new List<string>();
                var bitmaps = new List<Bitmap>();

                try
                {
                    // System.Drawing.Image supports TIFF natively on all Windows versions
                    using (var tiffImage = Image.FromFile(file.Path))
                    {
                        // Get number of frames (pages) in TIFF
                        var frameCount = tiffImage.GetFrameCount(FrameDimension.Page);
                        Debug.WriteLine($"[TiffProvider] Processing {frameCount} frames using System.Drawing");

                        for (int i = 0; i < frameCount; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Select the frame
                            tiffImage.SelectActiveFrame(FrameDimension.Page, i);

                            // Create a new bitmap from the frame
                            var frameBitmap = new Bitmap(tiffImage.Width, tiffImage.Height, PixelFormat.Format32bppArgb);
                            frameBitmap.SetResolution(dpi, dpi);

                            // Copy the frame to the bitmap
                            using (var g = Graphics.FromImage(frameBitmap))
                            {
                                g.DrawImage(tiffImage, 0, 0, tiffImage.Width, tiffImage.Height);
                            }

                            if (UseLocalFiles)
                            {
                                string outputPath = Path.Combine(LocalPath!, $"frame_{i}.png");
                                frameBitmap.Save(outputPath, ImageFormat.Png);
                                frameBitmap.Dispose();
                                filePaths.Add(outputPath);
                            }
                            else
                            {
                                bitmaps.Add(frameBitmap);
                            }
                        }
                    }

                    return new BitmapResult
                    {
                        FilePaths = UseLocalFiles ? filePaths : null,
                        Bitmaps = UseLocalFiles ? null : bitmaps
                    };
                }
                catch (Exception ex)
                {
                    // Clean up bitmaps if an error occurred
                    foreach (var bitmap in bitmaps)
                    {
                        bitmap?.Dispose();
                    }

                    throw new InvalidOperationException($"Failed to process TIFF file: {ex.Message}", ex);
                }
            }, cancellationToken);
        }
    }
}
