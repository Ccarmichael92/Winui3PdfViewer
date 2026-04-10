# API Documentation

## Namespace: Winui3PdfViewer

### PdfViewerControl Class

The main control for displaying PDF and TIFF documents in WinUI 3 applications.

#### Constructors

##### PdfViewerControl()
Initializes a new instance of the PdfViewerControl class.

```csharp
var viewer = new PdfViewerControl
{
    Dpi = 150,
    UseWidthFit = true
};
```

#### Properties

##### BitmapProvider
**Type:** `IBitmapProvider?`  
**Default:** `null`

Gets or sets the bitmap provider used to convert document files to bitmaps. This is automatically set based on file type when loading a document.

##### Dpi
**Type:** `int`  
**Default:** `100`  
**Range:** `1-2400`

Gets or sets the DPI (dots per inch) for rendering documents. Higher values produce better quality but use more memory.

```csharp
pdfViewerControl.Dpi = 150; // Good balance of quality and performance
```

##### Zoom
**Type:** `double`  
**Default:** `1.0`  
**Range:** `MinZoom` to `MaxZoom`

Gets or sets the current zoom level. 1.0 = 100%, 2.0 = 200%, etc.

```csharp
pdfViewerControl.Zoom = 1.5; // 150%
```

##### MinZoom
**Type:** `double`  
**Default:** `0.1`

Gets or sets the minimum allowed zoom level.

##### MaxZoom
**Type:** `double`  
**Default:** `8.0`

Gets or sets the maximum allowed zoom level.

##### UseWidthFit
**Type:** `bool`  
**Default:** `false`

Gets or sets whether to fit the document to the viewport width.

```csharp
pdfViewerControl.UseWidthFit = true; // Fit to width
```

##### SinglePageDisplay
**Type:** `bool`  
**Default:** `false`

Gets or sets whether to display only one page at a time with navigation buttons.

```csharp
pdfViewerControl.SinglePageDisplay = true;
```

##### PreloadedPages
**Type:** `int`  
**Default:** `20`

Gets or sets the number of pages to preload before and after the current page. Reduce for memory-constrained devices.

```csharp
pdfViewerControl.PreloadedPages = 5;
```

##### UseTempFiles
**Type:** `bool`  
**Default:** `false`

Gets or sets whether to use temporary files for bitmap storage instead of in-memory storage. Useful for very large documents.

```csharp
pdfViewerControl.UseTempFiles = true;
```

##### FitMax
**Type:** `double`  
**Default:** `0.0`

Gets or sets the maximum zoom level for the Fit operation. Set to 0 for no limit. Values greater than 10 are treated as percentages.

```csharp
pdfViewerControl.FitMax = 2.0;  // Max 200%
pdfViewerControl.FitMax = 150;  // Max 150%
```

##### CacheFolder
**Type:** `string` (read-only)  
**Default:** `string.Empty`

Gets the path to the temporary cache folder used for storing bitmap files when `UseTempFiles` is true.

#### Methods

##### LoadFileAsync
Asynchronously loads a document file into the viewer.

```csharp
public async Task LoadFileAsync(
    StorageFile file, 
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `file`: The storage file to load (PDF or TIFF)
- `cancellationToken`: Token to observe for cancellation requests

**Returns:** `Task` representing the asynchronous operation

**Exceptions:**
- `ArgumentNullException`: When file is null
- `InvalidOperationException`: When no bitmap provider is available for the file type

**Example:**
```csharp
try
{
    var file = await StorageFile.GetFileFromPathAsync(@"C:\Documents\example.pdf");
    await pdfViewerControl.LoadFileAsync(file);
}
catch (Exception ex)
{
    Debug.WriteLine($"Failed to load: {ex.Message}");
}
```

**With Cancellation:**
```csharp
var cts = new CancellationTokenSource();
try
{
    await pdfViewerControl.LoadFileAsync(file, cts.Token);
}
catch (OperationCanceledException)
{
    Debug.WriteLine("Load operation cancelled");
}
```

##### ZoomIn
Increases the zoom level by the specified step.

```csharp
public void ZoomIn(double step = 0.1)
```

**Parameters:**
- `step`: Amount to increase zoom (default: 0.1 = 10%)

**Example:**
```csharp
pdfViewerControl.ZoomIn();      // Zoom in by 10%
pdfViewerControl.ZoomIn(0.25);  // Zoom in by 25%
```

##### ZoomOut
Decreases the zoom level by the specified step.

```csharp
public void ZoomOut(double step = 0.1)
```

**Parameters:**
- `step`: Amount to decrease zoom (default: 0.1 = 10%)

**Example:**
```csharp
pdfViewerControl.ZoomOut();     // Zoom out by 10%
pdfViewerControl.ZoomOut(0.25); // Zoom out by 25%
```

##### ZoomTo100
Sets the zoom level to 100% (1.0).

```csharp
public void ZoomTo100()
```

**Example:**
```csharp
pdfViewerControl.ZoomTo100();
```

##### FitToControl
Fits the current document page to the viewport dimensions.

```csharp
public void FitToControl()
```

**Example:**
```csharp
pdfViewerControl.FitToControl();
```

##### ConvertBitmapToBitmapImage (static)
Converts a System.Drawing.Bitmap to a WinUI BitmapImage.

```csharp
public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
```

**Parameters:**
- `bitmap`: The bitmap to convert

**Returns:** `BitmapImage` for display in WinUI controls

**Exceptions:**
- `ArgumentNullException`: When bitmap is null

**Overload:**
```csharp
public static BitmapImage ConvertBitmapToBitmapImage(string bitmapPath)
```

**Parameters:**
- `bitmapPath`: File system path to the bitmap

**Returns:** `BitmapImage` loaded from the specified path

**Exceptions:**
- `ArgumentException`: When path is null or empty

##### Dispose
Releases all resources used by the control.

```csharp
public void Dispose()
```

**Example:**
```csharp
pdfViewerControl.Dispose();
```

---

## Namespace: Winui3PdfViewer.Interfaces

### IBitmapProvider Interface

Defines a contract for converting document files into bitmap representations.

#### Methods

##### GetBitmapsAsync
Asynchronously converts a document file into a collection of bitmaps.

```csharp
Task<BitmapResult> GetBitmapsAsync(
    StorageFile file, 
    int dpi, 
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `file`: The storage file to process
- `dpi`: Desired DPI for output bitmaps
- `cancellationToken`: Token to observe for cancellation

**Returns:** `Task<BitmapResult>` containing the conversion results

**Exceptions:**
- `ArgumentNullException`: When file is null
- `ArgumentOutOfRangeException`: When dpi is ≤ 0

---

## Namespace: Winui3PdfViewer.Providers

### PdfToBitmapListProvider Class

Provides bitmap conversion services for PDF files.

#### Constructors

##### PdfToBitmapListProvider(string? localPath)
Initializes a new instance.

**Parameters:**
- `localPath`: Optional path to save bitmaps as local files. If null or empty, bitmaps are kept in memory.

**Example:**
```csharp
var provider = new PdfToBitmapListProvider(null); // In-memory
var provider2 = new PdfToBitmapListProvider(@"C:\Temp"); // File-based
```

#### Methods

##### GetBitmapsAsync
Converts a PDF file to bitmaps.

**Exceptions:**
- `ArgumentNullException`: When file is null
- `ArgumentOutOfRangeException`: When dpi is out of valid range (1-2400)
- `FileNotFoundException`: When the PDF file cannot be accessed
- `InvalidOperationException`: When conversion fails

### TiffToBitmapListProvider Class

Provides bitmap conversion services for TIFF files using Windows Imaging Component.

#### Constructors

##### TiffToBitmapListProvider(string? localPath)
Initializes a new instance.

**Parameters:**
- `localPath`: Optional path to save bitmaps as local files

#### Methods

##### GetBitmapsAsync
Converts a TIFF file to bitmaps.

**Exceptions:**
- `ArgumentNullException`: When file is null
- `ArgumentOutOfRangeException`: When dpi is out of valid range (1-2400)
- `InvalidOperationException`: When TIFF codec is not available (error 0x88982F41)
- `InvalidOperationException`: When file is not a valid TIFF (error 0x88982F50)
- `COMException`: For other Windows Imaging Component errors

### BitmapResult Class

Represents the result of a bitmap conversion operation.

#### Properties

##### Bitmaps
**Type:** `IReadOnlyList<Bitmap>?`

Gets the collection of in-memory bitmaps, if applicable.

##### FilePaths
**Type:** `IReadOnlyList<string>?`

Gets the collection of file paths to saved bitmap files, if applicable.

##### IsLocalFiles
**Type:** `bool`

Gets whether the result uses local files instead of in-memory bitmaps.

---

## Namespace: Winui3PdfViewer.Helpers

### DocumentType Enum

Represents supported document types.

#### Values

- `Pdf` - Portable Document Format (.pdf)
- `Tiff` - Tagged Image File Format (.tif, .tiff)

### DocumentTypeHelper Class

Provides helper methods for working with document types.

#### Methods

##### TryParse
Attempts to parse a file extension into a DocumentType.

```csharp
public static bool TryParse(string extension, out DocumentType type)
```

**Parameters:**
- `extension`: File extension to parse (e.g., ".pdf", ".tiff")
- `type`: When this method returns, contains the parsed document type if successful

**Returns:** `true` if successfully parsed; otherwise, `false`

**Example:**
```csharp
if (DocumentTypeHelper.TryParse(".pdf", out var docType))
{
    Debug.WriteLine($"Document type: {docType}");
}
```

---

## Common Usage Patterns

### Basic File Loading with File Picker

```csharp
private async void OpenFile_Click(object sender, RoutedEventArgs e)
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
```

### Custom Zoom Buttons

```csharp
private void ZoomInButton_Click(object sender, RoutedEventArgs e)
{
    pdfViewerControl.ZoomIn(0.25); // 25% increment
}

private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
{
    pdfViewerControl.ZoomOut(0.25);
}

private void FitButton_Click(object sender, RoutedEventArgs e)
{
    pdfViewerControl.FitToControl();
}
```

### High-Quality Rendering for Printing

```csharp
pdfViewerControl.Dpi = 300;
pdfViewerControl.UseTempFiles = true;
await pdfViewerControl.LoadFileAsync(file);
```

### Memory-Efficient Configuration

```csharp
pdfViewerControl.Dpi = 96;
pdfViewerControl.PreloadedPages = 3;
pdfViewerControl.SinglePageDisplay = true;
await pdfViewerControl.LoadFileAsync(file);
```

### Error Handling Best Practices

```csharp
try
{
    await pdfViewerControl.LoadFileAsync(file);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("0x88982F41"))
{
    ShowMessage("TIFF codec not available. Please install Windows Imaging Component.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("0x88982F50"))
{
    ShowMessage("Invalid TIFF file format.");
}
catch (FileNotFoundException ex)
{
    ShowMessage($"File not found: {ex.FileName}");
}
catch (Exception ex)
{
    ShowMessage($"Error loading file: {ex.Message}");
}
```
