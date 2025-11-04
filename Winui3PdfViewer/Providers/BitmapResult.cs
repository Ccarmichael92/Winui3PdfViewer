using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winui3PdfViewer.Providers
{
    public sealed class BitmapResult
    {
        public IReadOnlyList<Bitmap>? Bitmaps { get; init; }
        public IReadOnlyList<string>? FilePaths { get; init; }

        public bool IsLocalFiles => FilePaths != null;

    }
}
