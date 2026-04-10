# WinUI 3 PDF Viewer

A professional, high-performance PDF and TIFF viewer control for WinUI 3 applications, built with .NET 8.

## Features

- ✅ **PDF Support** - View PDF documents with high fidelity
- ✅ **TIFF Support** - Multi-page TIFF image viewing
- ✅ **Zoom Controls** - Smooth zoom in/out with animated transitions
- ✅ **Fit-to-View** - Automatic sizing to fit viewport
- ✅ **Page Navigation** - Navigate between pages with buttons or scrolling
- ✅ **Single/Multi-Page Display** - Toggle between viewing modes
- ✅ **Memory Optimization** - Lazy loading and virtual scrolling
- ✅ **Temp File Support** - Option to use temporary files for large documents
- ✅ **Cancellation Support** - Async operations with cancellation tokens
- ✅ **Professional Error Handling** - Comprehensive exception handling with detailed messages

## Prerequisites

- Windows 10 version 19041 or higher
- .NET 8.0 SDK
- Visual Studio 2022 (version 17.0 or later) with WinUI 3 workload
- Windows App SDK

## Installation

### Option 1: Build from Source

1. Clone the repository:
```bash
git clone https://github.com/Ccarmichael92/Winui3PdfViewer.git
cd Winui3PdfViewer
```

2. Open `Winui3PdfViewer.sln` in Visual Studio 2022

3. Restore NuGet packages

4. Build the solution

### Option 2: Reference the Library

Add a project reference to `Winui3PdfViewer.csproj` in your WinUI 3 application.

## Quick Start

### XAML Setup

```xml
<Page
    x:Class="YourApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:viewer="using:Winui3PdfViewer">

    <Grid>
        <viewer:PdfViewerControl 
            x:Name="pdfViewerControl"
            Dpi="150"
            UseWidthFit="True"
            PreloadedPages="5" />
    </Grid>
</Page>
```

### Code-Behind

```csharp
using Windows.Storage;
using Windows.Storage.Pickers;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".pdf");
        picker.FileTypeFilter.Add(".tif");
        picker.FileTypeFilter.Add(".tiff");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            await pdfViewerControl.LoadFileAsync(file);
        }
    }
}
```

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Dpi` | `int` | 100 | DPI for rendering (1-2400) |
| `Zoom` | `double` | 1.0 | Current zoom level (0.1-8.0) |
| `MinZoom` | `double` | 0.1 | Minimum zoom level |
| `MaxZoom` | `double` | 8.0 | Maximum zoom level |
| `UseWidthFit` | `bool` | false | Fit to viewport width |
| `SinglePageDisplay` | `bool` | false | Show one page at a time |
| `PreloadedPages` | `int` | 20 | Pages to preload before/after current |
| `UseTempFiles` | `bool` | false | Use temp files instead of memory |
| `FitMax` | `double` | 0.0 | Maximum zoom for fit operation |

## Public Methods

### `LoadFileAsync`
Loads a PDF or TIFF file into the viewer.

```csharp
await pdfViewerControl.LoadFileAsync(file, cancellationToken);
```

### `ZoomIn` / `ZoomOut`
Adjusts the zoom level.

```csharp
pdfViewerControl.ZoomIn(0.1);  // Zoom in by 10%
pdfViewerControl.ZoomOut(0.1); // Zoom out by 10%
```

### `ZoomTo100`
Resets zoom to 100%.

```csharp
pdfViewerControl.ZoomTo100();
```

### `FitToControl`
Fits the current page to the viewport.

```csharp
pdfViewerControl.FitToControl();
```

## Architecture

### Components

- **PdfViewerControl** - Main control with UI and state management
- **IBitmapProvider** - Interface for document-to-bitmap conversion
- **PdfToBitmapListProvider** - PDF conversion implementation
- **TiffToBitmapListProvider** - TIFF conversion implementation using Windows Imaging Component
- **BitmapResult** - Conversion result container
- **DocumentTypeHelper** - File type detection utilities

### Design Principles

- **SOLID Principles** - Clean separation of concerns
- **Dependency Injection** - Pluggable bitmap providers
- **Async/Await** - Non-blocking operations
- **IDisposable** - Proper resource management
- **Cancellation Support** - Graceful operation cancellation

## Error Handling

The library provides detailed error messages for common scenarios:

- **Missing Codecs** - Detects when TIFF codec is unavailable
- **File Access Issues** - Handles permission and file not found errors
- **Invalid Files** - Validates file format before processing
- **Out of Range** - Validates DPI and zoom parameters

### Common Error Codes

| Error Code | Description | Solution |
|------------|-------------|----------|
| 0x88982F41 | TIFF codec not found | Install Windows Imaging Component codecs |
| 0x88982F50 | Invalid TIFF file | Verify file is a valid TIFF image |

## Performance Tips

1. **Use Temp Files for Large Documents**
   ```csharp
   pdfViewerControl.UseTempFiles = true;
   ```

2. **Adjust Preload Range**
   ```csharp
   pdfViewerControl.PreloadedPages = 5; // Reduce for memory-constrained devices
   ```

3. **Lower DPI for Preview**
   ```csharp
   pdfViewerControl.Dpi = 72; // Use higher values for printing
   ```

4. **Enable Single Page Mode**
   ```csharp
   pdfViewerControl.SinglePageDisplay = true;
   ```

## Troubleshooting

### TIFF Files Won't Load

**Problem**: COM Exception 0x88982F41

**Solution**: The TIFF codec is missing. Ensure Windows Imaging Component is installed and up to date.

### High Memory Usage

**Problem**: Application uses too much memory

**Solution**: Enable temp file mode or reduce preloaded pages count.

### Zoom Animation Stuttering

**Problem**: Zoom animations are not smooth

**Solution**: Reduce the number of preloaded pages or lower the DPI setting.

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch
3. Follow the existing code style
4. Add XML documentation for public APIs
5. Include error handling
6. Test thoroughly
7. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Built with WinUI 3 and .NET 8
- Uses Windows Imaging Component for TIFF support
- PDF rendering via PdfToBitmapList library

## Support

For issues, questions, or feature requests, please open an issue on GitHub:
https://github.com/Ccarmichael92/Winui3PdfViewer/issues

---

**Made with ❤️ for the WinUI 3 community**
