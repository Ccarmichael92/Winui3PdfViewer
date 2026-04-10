using System.Collections.Generic;
using System.Drawing;

namespace Winui3PdfViewer.Providers
{
    /// <summary>
    /// Represents the result of a bitmap conversion operation, containing either in-memory bitmaps or file paths.
    /// </summary>
    public sealed class BitmapResult
    {
        /// <summary>
        /// Gets the collection of in-memory bitmaps, if applicable.
        /// </summary>
        public IReadOnlyList<Bitmap>? Bitmaps { get; init; }

        /// <summary>
        /// Gets the collection of file paths to saved bitmap files, if applicable.
        /// </summary>
        public IReadOnlyList<string>? FilePaths { get; init; }

        /// <summary>
        /// Gets a value indicating whether the result uses local files instead of in-memory bitmaps.
        /// </summary>
        public bool IsLocalFiles => FilePaths != null;
    }
}
