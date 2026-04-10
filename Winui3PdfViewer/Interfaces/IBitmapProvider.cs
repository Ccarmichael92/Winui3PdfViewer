using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Winui3PdfViewer.Providers;

namespace Winui3PdfViewer.Interfaces
{
    /// <summary>
    /// Defines a contract for converting document files into bitmap representations.
    /// </summary>
    public interface IBitmapProvider
    {
        /// <summary>
        /// Asynchronously converts a document file into a collection of bitmaps.
        /// </summary>
        /// <param name="file">The storage file to process.</param>
        /// <param name="dpi">The desired DPI (dots per inch) for the output bitmaps.</param>
        /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation, containing the bitmap results.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="dpi"/> is less than or equal to zero.</exception>
        Task<BitmapResult> GetBitmapsAsync(StorageFile file, int dpi, CancellationToken cancellationToken = default);
    }
}
