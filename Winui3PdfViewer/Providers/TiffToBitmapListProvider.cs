using PdfToBitmapList;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Winui3PdfViewer.Interfaces;

namespace Winui3PdfViewer.Providers
{
    public sealed class TiffToBitmapListProvider : IBitmapProvider
    {
        private bool UseLocalFiles { get; set; }
        private string LocalPath { get; set; }

        public TiffToBitmapListProvider(string localPath)
        {
            LocalPath = localPath;
            UseLocalFiles = !string.IsNullOrWhiteSpace(localPath);
        }

        public async Task<BitmapResult> GetBitmapsAsync(StorageFile file, int Dpi, CancellationToken cancellationToken = default)
        {
            // Use WinRT imaging pipeline for decoding and encoding (faster and async-friendly).
            var filePaths = new List<string>();
            var bitmaps = new List<Bitmap>();

            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);

                for (uint i = 0; i < decoder.FrameCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var frame = await decoder.GetFrameAsync(i);

                    // Get a BGRA8 SoftwareBitmap suitable for encoder or conversion
                    using (var softwareBitmap = await frame.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied))
                    {
                        // If caller requested local files, use BitmapEncoder to save PNG directly from SoftwareBitmap
                        if (UseLocalFiles)
                        {
                            string outputPath = Path.Combine(LocalPath, $"frame_{i}.png");

                            // Create or overwrite the destination file via FileStream, then obtain an IRandomAccessStream wrapper
                            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                            {
                                using (IRandomAccessStream outStream = fs.AsRandomAccessStream())
                                {
                                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
                                    encoder.SetSoftwareBitmap(softwareBitmap);
                                    // Optionally set DPI via encoder properties
                                    var properties = new BitmapPropertySet
                                    {
                                        { "System.Image.HorizontalResolution", new BitmapTypedValue((double)Dpi, Windows.Foundation.PropertyType.Double) },
                                        { "System.Image.VerticalResolution", new BitmapTypedValue((double)Dpi, Windows.Foundation.PropertyType.Double) }
                                    };
                                    await encoder.BitmapProperties.SetPropertiesAsync(properties);
                                    await encoder.FlushAsync();
                                }
                            }

                            filePaths.Add(outputPath);
                        }
                        else
                        {
                            // Convert SoftwareBitmap -> byte[] -> System.Drawing.Bitmap
                            // Copy pixels to managed buffer
                            var buffer = new byte[softwareBitmap.PixelWidth * softwareBitmap.PixelHeight * 4];
                            softwareBitmap.CopyToBuffer(buffer.AsBuffer());

                            var bmp = new Bitmap(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight, PixelFormat.Format32bppArgb);
                            bmp.SetResolution(Dpi, Dpi); // set requested DPI metadata

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
    }
}