using System;

namespace Winui3PdfViewer.Helpers
{
    /// <summary>
    /// Represents the supported document types for viewing.
    /// </summary>
    public enum DocumentType
    {
        /// <summary>
        /// Portable Document Format (.pdf)
        /// </summary>
        Pdf,

        /// <summary>
        /// Tagged Image File Format (.tif, .tiff)
        /// </summary>
        Tiff
    }

    /// <summary>
    /// Provides helper methods for working with document types.
    /// </summary>
    public static class DocumentTypeHelper
    {
        /// <summary>
        /// Attempts to parse a file extension into a <see cref="DocumentType"/>.
        /// </summary>
        /// <param name="extension">The file extension to parse (e.g., ".pdf", ".tiff").</param>
        /// <param name="type">When this method returns, contains the parsed document type if successful; otherwise, the default value.</param>
        /// <returns><c>true</c> if the extension was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool TryParse(string extension, out DocumentType type)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                type = default;
                return false;
            }

            switch (extension.ToLowerInvariant())
            {
                case ".pdf":
                    type = DocumentType.Pdf;
                    return true;
                case ".tif":
                case ".tiff":
                    type = DocumentType.Tiff;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }
    }
}
