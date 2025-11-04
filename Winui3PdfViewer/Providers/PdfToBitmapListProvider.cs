using PdfToBitmapList;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Winui3PdfViewer.Interfaces;

namespace Winui3PdfViewer.Providers
{
    public sealed class PdfToBitmapListProvider : IBitmapProvider
    {
        private bool UseLocalFiles { get; set; }
        private string LocalPath { get; set; }

        public PdfToBitmapListProvider(string localPath)
        {
            LocalPath = localPath;
            UseLocalFiles = !string.IsNullOrWhiteSpace(localPath);
        }
        public async Task<BitmapResult> GetBitmapsAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                if (UseLocalFiles)
                {
                    var bitmapPaths = Pdf2Bmp.Split(file.Path, LocalPath, 100);
                    return new BitmapResult { FilePaths = bitmapPaths };
                }
                else
                {
                    var bitmaps = Pdf2Bmp.Split(file.Path, 100);
                    return new BitmapResult { Bitmaps = bitmaps.Select(s => new Bitmap(s)).ToList() };
                }                
            });
                
        }
    }
}