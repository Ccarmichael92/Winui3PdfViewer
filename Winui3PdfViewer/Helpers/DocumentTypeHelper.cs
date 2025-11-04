using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winui3PdfViewer.Helpers
{
    public enum DocumentType
    {
        Pdf,
        Tiff
    }

    public static class DocumentTypeHelper
    {
        public static bool TryParse(string extension, out DocumentType type)
        {
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
