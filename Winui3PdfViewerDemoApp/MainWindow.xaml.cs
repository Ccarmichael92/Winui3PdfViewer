using Microsoft.UI.Xaml;

namespace Winui3PdfViewerDemoApp
{
    /// <summary>
    /// The main application window.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            RootFrame.Navigate(typeof(Views.MainPage));
        }
    }
}
