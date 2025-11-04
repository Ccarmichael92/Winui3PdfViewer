using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Winui3PdfViewerDemoApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            //LoadFile(@"C:\Users\ccarm\iCloudDrive\Downloads\ Cormen, Thomas H_ Leiserson, Charles E_ Rivest, Ronald L_ Stein, - Introduction to Algorithms (2011) - libgen.li.pdf");
            //LoadFile(@"C:\Users\ccarm\iCloudDrive\Downloads\2023 CWM Benefit Guide.pdf");
            LoadFile(@"C:\Users\ccarm\Downloads\multipage_tiff_example.tif");
        }

        private async void LoadFile(string pdf)
        {
            // Load the file from disk
            StorageFile file = await StorageFile.GetFileFromPathAsync(pdf);

            // Assign it to the control
            await pdfViewerControl.LoadFileAsync(file);
            //pdfViewerControl.SinglePageDisplay = true;
        }

    }
}
