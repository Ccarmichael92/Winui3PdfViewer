using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;

namespace Winui3PdfViewerDemoApp.Views
{
    /// <summary>
    /// The main page that hosts the PDF viewer control.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Example: Load a default file for demo purposes
            // Replace with your own file path or remove for production
            string defaultFilePath = @"C:\DMS\Test.pdf";
            if (File.Exists(defaultFilePath))
            {
                _ = LoadFileAsync(defaultFilePath);
            }
        }

        /// <summary>
        /// Asynchronously loads a PDF or TIFF file into the viewer.
        /// </summary>
        /// <param name="filePath">The full path to the file to load.</param>
        private async System.Threading.Tasks.Task LoadFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.WriteLine("[MainPage] File path is null or empty.");
                return;
            }

            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[MainPage] File not found: {filePath}");
                return;
            }

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                await pdfViewerControl.LoadFileAsync(file);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[MainPage] Access denied to file: {filePath}. Error: {ex.Message}");
                ShowErrorDialog("Access Denied", $"Cannot access the file: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Failed to load file: {filePath}. Error: {ex}");
                ShowErrorDialog("Load Error", $"Failed to load file: {ex.Message}");
            }
        }

        private async void ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
