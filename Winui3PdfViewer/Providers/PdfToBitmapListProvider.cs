using PdfToBitmapList;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Winui3PdfViewer.Providers
{
    public sealed class PdfToBitmapListProvider : IBitmapProvider
    {
        public async Task<IReadOnlyList<Bitmap>> GetBitmapsAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var bitmaps = Pdf2Bmp.Split(file.Path, 100);
                return (IReadOnlyList<Bitmap>)bitmaps;
            });
                
        }
    }
}