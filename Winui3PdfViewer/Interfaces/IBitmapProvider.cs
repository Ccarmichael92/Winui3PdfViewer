using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Winui3PdfViewer.Providers;

namespace Winui3PdfViewer.Interfaces
{
    public interface IBitmapProvider
    {
        Task<BitmapResult> GetBitmapsAsync(StorageFile file, int Dpi, CancellationToken cancellationToken = default);
    }
}
