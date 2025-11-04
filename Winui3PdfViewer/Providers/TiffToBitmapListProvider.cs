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
        public async Task<BitmapResult> GetBitmapsAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            return await Task.Run(async () =>
            {
                var filePaths = new List<string>();
                var bitmaps = new List<Bitmap>();
                try
                {
                    string path = file.Path;
                    using var image = Image.FromFile(path);
                    int frameCount = image.GetFrameCount(FrameDimension.Page);

                    for (int i = 0; i < frameCount; i++)
                    {
                        image.SelectActiveFrame(FrameDimension.Page, i);
                        var bmp = new Bitmap(image);

                        if (UseLocalFiles)
                        {
                            string outputPath = Path.Combine(LocalPath, $"frame_{i}.png");
                            bmp.Save(outputPath, ImageFormat.Png);
                            filePaths.Add(outputPath);
                            bmp.Dispose();
                        }
                        else
                        {
                            bitmaps.Add(bmp);
                        }
                    }
                }
                catch
                {
                    using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
                    var decoder = await BitmapDecoder.CreateAsync(stream);

                    for (uint i = 0; i < decoder.FrameCount; i++)
                    {
                        var frame = await decoder.GetFrameAsync(i);
                        var softwareBitmap = await frame.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                        var buffer = new byte[softwareBitmap.PixelWidth * softwareBitmap.PixelHeight * 4];
                        softwareBitmap.CopyToBuffer(buffer.AsBuffer());

                        var bmp = new Bitmap(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight, PixelFormat.Format32bppArgb);
                        var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
                        Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                        bmp.UnlockBits(bmpData);

                        if (UseLocalFiles)
                        {
                            string outputPath = Path.Combine(LocalPath, $"frame_{i}.png");
                            bmp.Save(outputPath, ImageFormat.Png);
                            filePaths.Add(outputPath);
                            bmp.Dispose();
                        }
                        else
                        {
                            bitmaps.Add(bmp);
                        }
                    }
                }

                return new BitmapResult
                {
                    FilePaths = UseLocalFiles ? filePaths : null,
                    Bitmaps = UseLocalFiles ? null : bitmaps
                };

            });
        }
    }
}
