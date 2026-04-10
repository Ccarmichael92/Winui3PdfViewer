using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Winui3PdfViewer.Helpers;
using Winui3PdfViewer.Interfaces;
using Winui3PdfViewer.Providers;

namespace Winui3PdfViewer
{
    public sealed class PdfViewerControl : Control, IDisposable
    {
        // Template part names
        private const string PartScrollViewer = "PART_ScrollViewer";
        private const string PartViewportRoot = "PART_ViewportRoot";
        private const string PartPagePanel = "PART_PagePanel";
        private const string PartZoomOverlay = "PART_ZoomOverlay";
        private const string PartBtnZoomIn = "PART_BtnZoomIn";
        private const string PartBtnZoomOut = "PART_BtnZoomOut";
        private const string PartBtnFit = "PART_BtnFit";
        private const string PartBtn100 = "PART_Btn100";
        private const string PartBtnPrevPage = "PART_BtnPrevPage";
        private const string PartBtnNextPage = "PART_BtnNextPage";
        private const string PartPageOverlay = "PART_PageOverlay";
        private const string PartProgressRing = "PART_ProgressRing";
        private const string PartLoadingOverlay = "PART_LoadingOverlay";

        // Template parts
        private ScrollViewer? _scrollViewer;
        private Grid? _viewportRoot;
        private StackPanel? _pagePanel;
        private TextBlock? _zoomOverlay;
        private TextBlock? _pageOverlay;
        private Button? _btnPrevPage;
        private Button? _btnNextPage;
        private ProgressRing? _progressRing;
        private Grid? _loadingOverlay;

        // State
        private readonly List<BitmapImage> _images = new();
        private List<Bitmap>? _rawBitmaps;
        private readonly List<(int Width, int Height)> _pagePixelSizes = new();
        private int _currentIndex;
        private double _imagePixelWidth;
        private double _imagePixelHeight;
        private bool _isTemplateApplied;
        private bool _hasFitOnInitialLoad = true;
        private bool _suppressAutoFitDuringPageSwap;
        private bool _autoFitEnabled = true;

        // Hydration timer
        private DispatcherQueueTimer? _hydrateTimer;

        // Zoom / animation state
        private bool _isAnimatingZoom;
        private bool _fitRequested;
        private bool IsFitPending { get; set; }
        private double _zoomStart;
        private double _zoomTarget;
        private double _zoomDurationMs;
        private DateTime _zoomStartTime;
        private double _centerX, _centerY, _viewportW, _viewportH;
        private double scrollRatioX, scrollRatioY;

        // Focal-point fields
        private Windows.Foundation.Point _zoomFocalContentPoint;
        private bool _hasZoomFocalPoint;

        // Configurable properties
        /// <summary>
        /// Gets or sets the bitmap provider used to convert document files to bitmaps.
        /// </summary>
        public IBitmapProvider? BitmapProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to fit the document to the viewport width.
        /// </summary>
        public bool UseWidthFit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display only one page at a time.
        /// </summary>
        public bool SinglePageDisplay { get; set; }

        /// <summary>
        /// Gets or sets the number of pages to preload before and after the current page.
        /// </summary>
        public int PreloadedPages { get; set; } = 20;

        /// <summary>
        /// Gets or sets the maximum zoom level for the Fit operation. Set to 0 for no limit.
        /// Values greater than 10 are treated as percentages.
        /// </summary>
        public double FitMax { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets a value indicating whether to use temporary files for bitmap storage instead of in-memory storage.
        /// </summary>
        public bool UseTempFiles { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to fit only the first loaded file.
        /// </summary>
        public bool OnlyFitFirstFile { get; set; } = false;

        /// <summary>
        /// Gets or sets the DPI (dots per inch) for rendering documents.
        /// </summary>
        public int Dpi { get; set; } = 100;

        // Cache folder
        /// <summary>
        /// Gets the path to the temporary cache folder used for storing bitmap files.
        /// </summary>
        public string CacheFolder { get; private set; } = string.Empty;

        // Zoom DependencyProperty
        /// <summary>
        /// Identifies the <see cref="Zoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(PdfViewerControl),
                new PropertyMetadata(1.0, OnZoomChanged));

        /// <summary>
        /// Gets or sets the current zoom level. Default is 1.0 (100%).
        /// </summary>
        public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }

        /// <summary>
        /// Identifies the <see cref="MinZoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinZoomProperty =
            DependencyProperty.Register(nameof(MinZoom), typeof(double), typeof(PdfViewerControl),
                new PropertyMetadata(0.1));

        /// <summary>
        /// Gets or sets the minimum allowed zoom level. Default is 0.1 (10%).
        /// </summary>
        public double MinZoom { get => (double)GetValue(MinZoomProperty); set => SetValue(MinZoomProperty, value); }

        /// <summary>
        /// Identifies the <see cref="MaxZoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxZoomProperty =
            DependencyProperty.Register(nameof(MaxZoom), typeof(double), typeof(PdfViewerControl),
                new PropertyMetadata(8.0));

        /// <summary>
        /// Gets or sets the maximum allowed zoom level. Default is 8.0 (800%).
        /// </summary>
        public double MaxZoom { get => (double)GetValue(MaxZoomProperty); set => SetValue(MaxZoomProperty, value); }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfViewerControl"/> class.
        /// </summary>
        public PdfViewerControl()
        {
            DefaultStyleKey = typeof(PdfViewerControl);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;

            // Start a background sweep to delete any stale PdfViewer_... temp folders left from previous runs.
            // Best-effort; do not block construction or UI thread.
            Task.Run(() => DeleteStaleCacheFoldersOnStartup());
        }

        #region Template + lifecycle + loading

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
            _viewportRoot = GetTemplateChild(PartViewportRoot) as Grid;
            _pagePanel = GetTemplateChild(PartPagePanel) as StackPanel;
            _zoomOverlay = GetTemplateChild(PartZoomOverlay) as TextBlock;
            _pageOverlay = GetTemplateChild(PartPageOverlay) as TextBlock;
            _btnPrevPage = GetTemplateChild(PartBtnPrevPage) as Button;
            _btnNextPage = GetTemplateChild(PartBtnNextPage) as Button;
            _progressRing = GetTemplateChild(PartProgressRing) as ProgressRing;
            _loadingOverlay = GetTemplateChild(PartLoadingOverlay) as Grid;

            _isTemplateApplied = true;

            if (_scrollViewer != null)
                _scrollViewer.ViewChanged += OnScrollViewerViewChanged;

            if (GetTemplateChild(PartBtnZoomIn) is Button zin) zin.Click += (_, __) => ZoomIn();
            if (GetTemplateChild(PartBtnZoomOut) is Button zout) zout.Click += (_, __) => ZoomOut();
            if (GetTemplateChild(PartBtnFit) is Button fit) fit.Click += (_, __) => FitToControl();
            if (GetTemplateChild(PartBtn100) is Button z100) z100.Click += (_, __) => ZoomTo100();

            if (_btnPrevPage != null) _btnPrevPage.Click += (_, __) => ShowPage(_currentIndex - 1);
            if (_btnNextPage != null) _btnNextPage.Click += (_, __) => ShowPage(_currentIndex + 1);

            UpdateScaledLayout();
            UpdateZoomOverlay();
            UpdatePageOverlay();
            UpdateNavigationButtons();
        }

        private void OnLoaded(object? s, RoutedEventArgs e) => UpdateScaledLayout();
        private void OnUnloaded(object? s, RoutedEventArgs e) => Dispose();

        private void OnSizeChanged(object? s, SizeChangedEventArgs e)
        {
            if (IsFitPending)
                FitToControl();
            else
                UpdateScaledLayout();
        }

        public async Task LoadFileAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            // clear existing state
            _images.Clear();
            _pagePixelSizes.Clear();
            DisposeAndClearRawBitmaps();
            _currentIndex = 0;
            _imagePixelWidth = 0;
            _imagePixelHeight = 0;

            EnsureCacheFolderIfNeeded(file.Name);

            // Always (re-)select provider for the incoming file type
            SetBitmapProviderForFile(file.Name);
            if (BitmapProvider == null) throw new InvalidOperationException("BitmapProvider missing");

            SetLoadingState(true);
            try
            {
                var bitmaps = await BitmapProvider.GetBitmapsAsync(file, Dpi, cancellationToken).ConfigureAwait(false);
                await RunOnUIThreadAsync(() => ApplyBitmapResult(bitmaps)).ConfigureAwait(false);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void SetBitmapProviderForFile(string name)
        {
            if (!DocumentTypeHelper.TryParse(Path.GetExtension(name), out var docType)) return;
            if (docType == DocumentType.Pdf) BitmapProvider = new PdfToBitmapListProvider(UseTempFiles ? CacheFolder : null);
            else if (docType == DocumentType.Tiff) BitmapProvider = new TiffToBitmapListProvider(UseTempFiles ? CacheFolder : null);
        }

        private void ApplyBitmapResult(BitmapResult bitmaps)
        {
            _images.Clear();
            _pagePixelSizes.Clear();

            if (!bitmaps.IsLocalFiles)
            {
                _rawBitmaps = new List<Bitmap>(bitmaps.Bitmaps);
                foreach (var b in bitmaps.Bitmaps)
                {
                    var bi = ConvertBitmapToBitmapImage(b);
                    _images.Add(bi);
                    _pagePixelSizes.Add((bi.PixelWidth, bi.PixelHeight));
                }
            }
            else
            {
                foreach (var path in bitmaps.FilePaths)
                {
                    var bi = ConvertBitmapToBitmapImage(path);
                    _images.Add(bi);
                    _pagePixelSizes.Add((bi.PixelWidth, bi.PixelHeight));
                }
            }

            if (_pagePanel == null) return;

            _pagePanel.Children.Clear();

            if (_images.Count == 0)
            {
                UpdatePageOverlay();
                UpdateNavigationButtons();
                return;
            }

            if (SinglePageDisplay)
            {
                _pagePanel.Children.Add(CreatePlaceholderImage(_currentIndex));
            }
            else
            {
                for (int i = 0; i < _images.Count; i++)
                    _pagePanel.Children.Add(CreatePlaceholderImage(i));
            }

            RequestHydration();
            UpdatePageOverlay();

            // Ensure initial Fit runs once after load. Allow ImageOpened to finish hydration if needed.
            _hasFitOnInitialLoad = false;
            _fitRequested = true;
            FitToControl();

            UpdateNavigationButtons();
        }

        #endregion

        #region Placeholders + hydration (robust + deferred-fit)

        private Microsoft.UI.Xaml.Controls.Image CreatePlaceholderImage(int pageIndex)
        {
            var img = new Microsoft.UI.Xaml.Controls.Image
            {
                Source = null,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = pageIndex
            };

            // Use cached pixel sizes (keeps initial measure close to final)
            if (pageIndex >= 0 && pageIndex < _pagePixelSizes.Count)
            {
                var (pw, ph) = _pagePixelSizes[pageIndex];
                pw = pw > 0 ? pw : 1024;
                ph = ph > 0 ? ph : 1330;
                img.Width = pw * Zoom;
                img.Height = ph * Zoom;
            }

            img.ImageOpened += (s, e) =>
            {
                if (!(img.Source is BitmapImage bmp)) return;

                _imagePixelWidth = bmp.PixelWidth;
                _imagePixelHeight = bmp.PixelHeight;
                img.Width = bmp.PixelWidth * Zoom;
                img.Height = bmp.PixelHeight * Zoom;

                // initial auto-fit: if requested for initial load
                if (_autoFitEnabled && !_suppressAutoFitDuringPageSwap && !_hasFitOnInitialLoad)
                {
                    _hasFitOnInitialLoad = true;
                    FitToControl();
                    return;
                }

                // If a fit was requested earlier but deferred because anchor wasn't ready, run it now
                if (_fitRequested || IsFitPending)
                {
                    _fitRequested = false;
                    FitToControl();
                }
            };

            return img;
        }

        private void EnsureHydrationTimer()
        {
            if (DispatcherQueue == null) return;
            if (_hydrateTimer != null) return;

            _hydrateTimer = DispatcherQueue.CreateTimer();
            _hydrateTimer.Interval = TimeSpan.FromMilliseconds(50);
            _hydrateTimer.IsRepeating = false;
            _hydrateTimer.Tick += (s, e) =>
            {
                _hydrateTimer?.Stop();
                HydrateVisibleImages();
            };
        }

        private void RequestHydration()
        {
            EnsureHydrationTimer();
            _hydrateTimer?.Stop();
            _hydrateTimer?.Start();
        }

        private void HydrateVisibleImages()
        {
            if (_scrollViewer == null || _pagePanel == null || _images.Count == 0) return;

            int anchor = _currentIndex;
            int half = Math.Max(0, PreloadedPages);
            int start = Math.Max(0, anchor - half);
            int end = Math.Min(_images.Count - 1, anchor + half);

            bool hydratedAnchor = false;
            double viewportW = Math.Max(1.0, _scrollViewer.ViewportWidth);

            for (int i = 0; i < _pagePanel.Children.Count; i++)
            {
                if (!(_pagePanel.Children[i] is Microsoft.UI.Xaml.Controls.Image child)) continue;
                int pageIndex = child.Tag is int idx ? idx : i;
                bool shouldLoad = pageIndex >= start && pageIndex <= end;

                if (shouldLoad && child.Source == null)
                {
                    var src = _images[pageIndex];

                    // Create fresh BitmapImage so ImageOpened reliably fires; request decode width to populate PixelWidth early
                    var bi = new BitmapImage();
                    try
                    {
                        //bi.DecodePixelWidth = Math.Max(1, (int)Math.Ceiling(viewportW));
                        if (src.UriSource != null) bi.UriSource = src.UriSource;
                        // If provider yields streams instead of UriSource, set them on the UI thread here.
                    }
                    catch { /* ignore decode hint failures */ }

                    child.Source = bi;

                    // Use cached pixel sizes if we know them
                    if (src.PixelWidth > 0 && src.PixelHeight > 0)
                    {
                        child.Width = src.PixelWidth * Zoom;
                        child.Height = src.PixelHeight * Zoom;
                    }

                    if (pageIndex == _currentIndex) hydratedAnchor = true;
                }
                else if (!shouldLoad && child.Source != null)
                {
                    // unload image source to free memory, keep placeholder dims
                    child.Source = null;
                }
            }

            // quick layout pass to reduce race windows (final snap when anchor opens)
            _pagePanel.UpdateLayout();
            _viewportRoot?.UpdateLayout();

            if (hydratedAnchor)
            {
                // defensive finalization (ImageOpened will normally trigger finalization)
                EnsureFinalLayoutAndSnapToCurrentPage();
            }
        }

        #endregion

        #region Zoom / Fit (deferred-fit + focal-point zoom with 20% width reduction stop-gap)

        /// <summary>
        /// Increases the zoom level by the specified step.
        /// </summary>
        /// <param name="step">The amount to increase the zoom level. Default is 0.1 (10%).</param>
        public void ZoomIn(double step = 0.1) => AnimateZoom(Zoom + step);

        /// <summary>
        /// Decreases the zoom level by the specified step.
        /// </summary>
        /// <param name="step">The amount to decrease the zoom level. Default is 0.1 (10%).</param>
        public void ZoomOut(double step = 0.1) => AnimateZoom(Zoom - step);

        /// <summary>
        /// Sets the zoom level to 100% (1.0).
        /// </summary>
        public void ZoomTo100() { IsFitPending = false; AnimateZoom(1.0); }

        /// <summary>
        /// Fits the current document page to the viewport dimensions.
        /// </summary>
        public void FitToControl()
        {
            // mark intent
            IsFitPending = true;

            if (_scrollViewer == null || _pagePanel == null)
                return;

            // Find anchor image
            var anchorImg = _pagePanel.Children
                .OfType<Microsoft.UI.Xaml.Controls.Image>()
                .FirstOrDefault(i => i.Tag is int t && t == _currentIndex);

            // If anchor missing or not yet hydrated, request hydration and defer the fit
            if (anchorImg == null || anchorImg.Source == null)
            {
                RequestHydration();
                _fitRequested = true;
                return;
            }

            // If decoded pixel size unknown, defer until ImageOpened
            if (anchorImg.Source is BitmapImage bi && (bi.PixelWidth <= 0 || bi.PixelHeight <= 0))
            {
                RequestHydration();
                _fitRequested = true;
                return;
            }

            // Use decoded metrics if available
            if (anchorImg.Source is BitmapImage bmp && bmp.PixelWidth > 0 && bmp.PixelHeight > 0)
            {
                _imagePixelWidth = bmp.PixelWidth;
                _imagePixelHeight = bmp.PixelHeight;
            }

            if (_imagePixelWidth <= 0 || _imagePixelHeight <= 0)
                return;

            double scaleX = _scrollViewer.ActualWidth / _imagePixelWidth;
            double scaleY = _scrollViewer.ActualHeight / _imagePixelHeight;
            double target = UseWidthFit ? scaleX : Math.Min(scaleX, scaleY);

            if (FitMax > 0)
            {
                double cap = FitMax;
                if (FitMax > 10) cap = FitMax / 100.0;
                target = Math.Min(target, cap);
            }

            // STOP-GAP: when UseWidthFit is true, apply 20% reduction to the computed target
            if (UseWidthFit)
            {
                target = target * 0.80;
            }

            // start animation with focal-point capture
            _fitRequested = true;
            AnimateZoom(target);
        }

        private void AnimateZoom(double targetZoom, double durationMs = 200)
        {
            if (_scrollViewer == null)
            {
                Zoom = targetZoom;
                _fitRequested = false;
                return;
            }

            if (_isAnimatingZoom) return;

            // preserve ratios (fallback)
            scrollRatioX = _scrollViewer.HorizontalOffset / Math.Max(1, _scrollViewer.ExtentWidth - _scrollViewer.ViewportWidth);
            scrollRatioY = _scrollViewer.VerticalOffset / Math.Max(1, _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight);

            if (targetZoom < MinZoom) targetZoom = MinZoom;
            if (targetZoom > MaxZoom) targetZoom = MaxZoom;

            _viewportW = _scrollViewer.ViewportWidth;
            _viewportH = _scrollViewer.ViewportHeight;
            _centerX = _scrollViewer.HorizontalOffset + _viewportW / 2;
            _centerY = _scrollViewer.VerticalOffset + _viewportH / 2;

            _zoomStart = Zoom;
            _zoomTarget = targetZoom;
            _zoomDurationMs = durationMs;
            _zoomStartTime = DateTime.Now;

            // Capture focal point: viewport center → content (pagePanel) coordinates
            _hasZoomFocalPoint = false;
            try
            {
                if (_scrollViewer != null && _pagePanel != null)
                {
                    var viewportCenter = new Windows.Foundation.Point(_scrollViewer.ViewportWidth / 2.0, _scrollViewer.ViewportHeight / 2.0);
                    // Map viewport center into pagePanel coords
                    _zoomFocalContentPoint = _scrollViewer.TransformToVisual(_pagePanel).TransformPoint(viewportCenter);
                    _hasZoomFocalPoint = true;
                }
            }
            catch
            {
                _hasZoomFocalPoint = false;
            }

            CompositionTarget.Rendering += OnZoomAnimationFrame;
            _isAnimatingZoom = true;
        }

        private void OnZoomAnimationFrame(object? sender, object e)
        {
            var elapsed = (DateTime.Now - _zoomStartTime).TotalMilliseconds;
            var progress = Math.Min(1.0, elapsed / _zoomDurationMs);
            var eased = 1 - Math.Pow(1 - progress, 3);

            var currentZoom = _zoomStart + (_zoomTarget - _zoomStart) * eased;
            Zoom = currentZoom;

            if (_scrollViewer == null || _pagePanel == null)
                return;

            try
            {
                var viewportAnchor = new Windows.Foundation.Point(_scrollViewer.ViewportWidth / 2.0, _scrollViewer.ViewportHeight / 2.0);

                if (_hasZoomFocalPoint)
                {
                    var focalInViewport = _pagePanel.TransformToVisual(_scrollViewer).TransformPoint(_zoomFocalContentPoint);

                    double dx = focalInViewport.X - viewportAnchor.X;
                    double dy = focalInViewport.Y - viewportAnchor.Y;

                    double newOffsetX = _scrollViewer.HorizontalOffset + dx;
                    double newOffsetY = _scrollViewer.VerticalOffset + dy;

                    newOffsetX = Math.Max(0, Math.Min(newOffsetX, Math.Max(0, _scrollViewer.ExtentWidth - _scrollViewer.ViewportWidth)));
                    newOffsetY = Math.Max(0, Math.Min(newOffsetY, Math.Max(0, _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight)));

                    _scrollViewer.ChangeView(newOffsetX, newOffsetY, null, true);
                }
                else
                {
                    double newOffsetX = scrollRatioX * Math.Max(1, _scrollViewer.ExtentWidth - _scrollViewer.ViewportWidth);
                    double newOffsetY = scrollRatioY * Math.Max(1, _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight);
                    _scrollViewer.ChangeView(newOffsetX, newOffsetY, null, true);
                }
            }
            catch
            {
                // ignore frame errors
            }

            if (progress >= 1.0)
            {
                CompositionTarget.Rendering -= OnZoomAnimationFrame;
                DispatcherQueue?.TryEnqueue(() =>
                {
                    _hasZoomFocalPoint = false;
                    _pagePanel?.UpdateLayout();
                    _viewportRoot?.UpdateLayout();
                    EnsureFinalLayoutAndSnapToCurrentPage();
                    _fitRequested = false;
                    IsFitPending = false;
                    _isAnimatingZoom = false;
                    UpdateNavigationButtons();
                });
            }
        }

        /// <summary>
        /// Finalization: force measure/arrange, compute horizontal center and vertical snap,
        /// then restore local values cleanly.
        /// </summary>
        private void EnsureFinalLayoutAndSnapToCurrentPage()
        {
            if (_scrollViewer == null || _pagePanel == null || _viewportRoot == null) return;

            var viewportW = Math.Ceiling(_scrollViewer.ViewportWidth);
            var prevWidth = _pagePanel.Width;
            _pagePanel.Width = viewportW;
            _pagePanel.MinWidth = viewportW;
            _viewportRoot.MinWidth = viewportW;

            var size = new Windows.Foundation.Size(_scrollViewer.ViewportWidth, _scrollViewer.ViewportHeight);
            try
            {
                _pagePanel.Measure(size);
                _pagePanel.Arrange(new Windows.Foundation.Rect(0, 0, size.Width, Math.Max(size.Height, _pagePanel.DesiredSize.Height)));
                _viewportRoot.Measure(size);
                _viewportRoot.Arrange(new Windows.Foundation.Rect(0, 0, size.Width, size.Height));
            }
            catch { }

            try { _pagePanel.UpdateLayout(); } catch { }
            try { _viewportRoot.UpdateLayout(); } catch { }
            try { _scrollViewer.UpdateLayout(); } catch { }

            var timer = DispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(32);
            timer.IsRepeating = false;
            timer.Tick += (s, ev) =>
            {
                try { timer.Stop(); } catch { }

                double targetY = 0;
                try
                {
                    var image = _pagePanel.Children
                        .OfType<Microsoft.UI.Xaml.Controls.Image>()
                        .FirstOrDefault(img => img.Tag is int t && t == _currentIndex);

                    if (image != null)
                    {
                        var pt = image.TransformToVisual(_pagePanel).TransformPoint(new Windows.Foundation.Point(0, 0));
                        targetY = pt.Y;
                    }
                }
                catch { targetY = 0; }

                double maxY = Math.Max(0, _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight);
                if (double.IsNaN(targetY) || targetY < 0) targetY = 0;
                if (targetY > maxY) targetY = maxY;

                double targetX = 0;
                try
                {
                    var extentW = _scrollViewer.ExtentWidth;
                    var viewportWInner = _scrollViewer.ViewportWidth;
                    targetX = extentW > viewportWInner ? (extentW - viewportWInner) / 2.0 : 0;
                    if (double.IsNaN(targetX) || targetX < 0) targetX = 0;
                }
                catch { targetX = 0; }

                try { _scrollViewer.ChangeView(targetX, Math.Min(targetY, maxY), null, true); } catch { }
                try { _scrollViewer.ChangeView(targetX, targetY, null, true); } catch { }

                try { _pagePanel.Width = prevWidth; } catch { }
                try { _pagePanel.ClearValue(FrameworkElement.MinWidthProperty); } catch { }
                try { _viewportRoot.ClearValue(FrameworkElement.MinWidthProperty); } catch { }

                UpdateNavigationButtons();
            };
            timer.Start();
        }

        #endregion

        #region Resize scroll content helper (optional)

        private void ResizeScrollContentToCurrentImage(bool centerHorizontally = true, bool snapToTop = true)
        {
            if (_scrollViewer == null || _pagePanel == null || _viewportRoot == null) return;

            var image = _pagePanel.Children
                .OfType<Microsoft.UI.Xaml.Controls.Image>()
                .FirstOrDefault(img => img.Tag is int t && t == _currentIndex);

            if (image == null) return;

            double imgPixelsW = 0, imgPixelsH = 0;
            if (image.Source is BitmapImage bi && bi.PixelWidth > 0 && bi.PixelHeight > 0)
            {
                imgPixelsW = bi.PixelWidth;
                imgPixelsH = bi.PixelHeight;
            }
            else if (_pagePixelSizes != null && _currentIndex >= 0 && _currentIndex < _pagePixelSizes.Count)
            {
                var (pw, ph) = _pagePixelSizes[_currentIndex];
                imgPixelsW = pw;
                imgPixelsH = ph;
            }
            else
            {
                return;
            }

            double desiredW = Math.Max(1.0, imgPixelsW * Zoom);
            double desiredH = Math.Max(1.0, imgPixelsH * Zoom);

            var prevViewportWidth = _viewportRoot.Width;
            var prevViewportHeight = _viewportRoot.Height;
            var prevPanelWidth = _pagePanel.Width;

            try
            {
                _viewportRoot.Width = desiredW;
                _viewportRoot.Height = desiredH;
                _pagePanel.Width = desiredW;
                _pagePanel.HorizontalAlignment = HorizontalAlignment.Left;

                _pagePanel.UpdateLayout();
                _viewportRoot.UpdateLayout();
                _scrollViewer.UpdateLayout();

                double targetX = 0;
                if (centerHorizontally)
                {
                    var extentW = _scrollViewer.ExtentWidth;
                    var viewportW = _scrollViewer.ViewportWidth;
                    targetX = extentW > viewportW ? (extentW - viewportW) / 2.0 : 0;
                }

                double targetY = 0;
                if (snapToTop) targetY = 0;
                else
                {
                    var pt = image.TransformToVisual(_pagePanel).TransformPoint(new Windows.Foundation.Point(0, 0));
                    targetY = pt.Y;
                }

                try { _scrollViewer.ChangeView(targetX, targetY, null, true); } catch { }
            }
            finally
            {
                // Caller decides when/if to restore values
            }
        }

        #endregion

        #region Scroll detection + page overlay

        private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_scrollViewer == null || _pagePanel == null || _images.Count == 0) return;

            RequestHydration();

            if (SinglePageDisplay) return;

            double viewportTop = _scrollViewer.VerticalOffset;
            double viewportBottom = viewportTop + _scrollViewer.ViewportHeight;

            int bestPage = _currentIndex;
            double bestVisible = -1;

            for (int i = 0; i < _pagePanel.Children.Count; i++)
            {
                if (!(_pagePanel.Children[i] is FrameworkElement child)) continue;

                double childTop = child.TransformToVisual(_pagePanel).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
                double childBottom = childTop + child.ActualHeight;
                double visible = Math.Min(childBottom, viewportBottom) - Math.Max(childTop, viewportTop);
                if (visible > bestVisible)
                {
                    bestVisible = visible;
                    if (child is Microsoft.UI.Xaml.Controls.Image img && img.Tag is int tagIndex)
                        bestPage = tagIndex;
                    else
                        bestPage = i;
                }
            }

            if (_currentIndex != bestPage)
            {
                _currentIndex = bestPage;
                UpdatePageOverlay();
                UpdateNavigationButtons();
            }
        }

        private void ShowPage(int index)
        {
            if (index < 0 || index >= _images.Count) return;

            _suppressAutoFitDuringPageSwap = true;
            _currentIndex = index;

            _pagePanel?.Children.Clear();
            if (SinglePageDisplay) _pagePanel?.Children.Add(CreatePlaceholderImage(_currentIndex));
            else
            {
                for (int i = 0; i < _images.Count; i++)
                    _pagePanel?.Children.Add(CreatePlaceholderImage(i));
            }

            UpdateScaledLayout();
            UpdatePageOverlay();
            UpdateNavigationButtons();
            _suppressAutoFitDuringPageSwap = false;
            RequestHydration();

            if (SinglePageDisplay) _scrollViewer?.ChangeView(0, 0, null, true);
        }

        private void UpdatePageOverlay()
        {
            if (_pageOverlay == null || _images.Count == 0) { if (_pageOverlay != null) _pageOverlay.Text = ""; return; }
            _pageOverlay.Text = $"Page {_currentIndex + 1} of {_images.Count}";
        }

        #endregion

        #region Helpers: layout, loading, conversion

        private void UpdateScaledLayout()
        {
            if (_pagePanel == null || _viewportRoot == null) return;

            for (int i = 0; i < _pagePanel.Children.Count; i++)
            {
                if (!(_pagePanel.Children[i] is Microsoft.UI.Xaml.Controls.Image child)) continue;
                int pageIndex = child.Tag is int idx ? idx : i;

                if (pageIndex >= 0 && pageIndex < _pagePixelSizes.Count)
                {
                    var (pw, ph) = _pagePixelSizes[pageIndex];
                    pw = pw > 0 ? pw : 1024;
                    ph = ph > 0 ? ph : 1330;
                    child.Width = pw * Zoom;
                    child.Height = ph * Zoom;
                }

                if (child.Source is BitmapImage bmp && bmp.PixelWidth > 0 && bmp.PixelHeight > 0)
                {
                    child.Width = bmp.PixelWidth * Zoom;
                    child.Height = bmp.PixelHeight * Zoom;
                }
            }

            if (_viewportRoot != null && _scrollViewer != null)
            {
                _viewportRoot.MinWidth = _scrollViewer.ActualWidth;
                _viewportRoot.MinHeight = _scrollViewer.ActualHeight;
            }

            UpdateZoomOverlay();
        }

        private void UpdateZoomOverlay()
        {
            if (_zoomOverlay != null) _zoomOverlay.Text = $"{Zoom * 100:0}%";
        }

        private void SetLoadingState(bool isLoading)
        {
            if (!_isTemplateApplied || DispatcherQueue == null) return;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_progressRing != null) _progressRing.IsActive = isLoading;
                if (_loadingOverlay != null) _loadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        // Navigation buttons: visible only in single-page mode; enabled depending on index
        private void UpdateNavigationButtons()
        {
            if (_btnPrevPage == null || _btnNextPage == null) return;

            var visibility = SinglePageDisplay ? Visibility.Visible : Visibility.Collapsed;
            _btnPrevPage.Visibility = visibility;
            _btnNextPage.Visibility = visibility;

            if (!SinglePageDisplay) return;

            bool hasPages = _images != null && _images.Count > 0;
            _btnPrevPage.IsEnabled = hasPages && _currentIndex > 0;
            _btnNextPage.IsEnabled = hasPages && _currentIndex < _images.Count - 1;
        }

        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            var bmpImage = new BitmapImage();
            bmpImage.SetSource(ms.AsRandomAccessStream());
            return bmpImage;
        }

        public static BitmapImage ConvertBitmapToBitmapImage(string bitmapPath)
        {
            var bmpImage = new BitmapImage();
            bmpImage.UriSource = new Uri(bitmapPath + "?t=" + Guid.NewGuid());
            return bmpImage;
        }

        private void EnsureCacheFolderIfNeeded(string name)
        {
            // If temp files are not in use we do nothing
            if (!UseTempFiles) return;

            // If there's already a CacheFolder for this control instance, keep it.
            // If we are about to create a new one for a new load we will cleanup the previous one first.
            if (!string.IsNullOrEmpty(CacheFolder) && Directory.Exists(CacheFolder))
            {
                // We keep existing folder for reuse during same control lifetime.
                return;
            }

            // If previous CacheFolder existed but is missing on disk, clear the variable and create a fresh one.
            if (!string.IsNullOrEmpty(CacheFolder) && !Directory.Exists(CacheFolder))
            {
                CacheFolder = string.Empty;
            }

            try
            {
                // Before we create a new CacheFolder for this control instance make sure any previous
                // temp folder belonging to this control (if any) is removed.
                // Also attempt to cleanup any stale folder created earlier by THIS control (best-effort).
                if (!string.IsNullOrEmpty(CacheFolder) && Directory.Exists(CacheFolder))
                {
                    TryDeleteDirectoryRecursive(CacheFolder);
                    CacheFolder = string.Empty;
                }

                CacheFolder = Path.Combine(Path.GetTempPath(), "PdfViewer_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(CacheFolder);
                Debug.WriteLine($"Created cache folder: {CacheFolder}");
            }
            catch
            {
                CacheFolder = string.Empty;
            }
        }

        // Try to delete a directory and ignore any exceptions (best-effort).
        private static void TryDeleteDirectoryRecursive(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return;
                if (!Directory.Exists(path)) return;
                Directory.Delete(path, true);
            }
            catch
            {
                // ignore failures (files may be locked by other processes)
            }
        }

        // Delete stale PdfViewer_* temp folders found in the system temp path on startup (best-effort).
        private static void DeleteStaleCacheFoldersOnStartup()
        {
            try
            {
                var temp = Path.GetTempPath();
                if (string.IsNullOrEmpty(temp) || !Directory.Exists(temp)) return;

                var dirs = Directory.GetDirectories(temp, "PdfViewer_*", SearchOption.TopDirectoryOnly);
                foreach (var dir in dirs)
                {
                    // Attempt to delete; ignore if in-use
                    TryDeleteDirectoryRecursive(dir);
                }
            }
            catch
            {
                // best-effort; swallow any exceptions
            }
        }

        private async Task RunOnUIThreadAsync(Action action)
        {
            var dq = DispatcherQueue;
            if (dq == null || dq.HasThreadAccess) { action(); return; }

            var tcs = new TaskCompletionSource();
            dq.TryEnqueue(() => { try { action(); tcs.SetResult(); } catch (Exception ex) { tcs.SetException(ex); } });
            await tcs.Task.ConfigureAwait(false);
        }

        #endregion

        #region Dispose

        private void DisposeAndClearRawBitmaps()
        {
            if (_rawBitmaps == null) return;
            foreach (var b in _rawBitmaps) try { b.Dispose(); } catch { }
            _rawBitmaps = null;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="PdfViewerControl"/>.
        /// </summary>
        public void Dispose()
        {
            DisposeAndClearRawBitmaps();

            if (_scrollViewer != null) _scrollViewer.ViewChanged -= OnScrollViewerViewChanged;
            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
            SizeChanged -= OnSizeChanged;

            if (_hydrateTimer != null) { _hydrateTimer.Stop(); _hydrateTimer = null; }

            // Attempt to delete our cache folder if we created one
            try
            {
                if (!string.IsNullOrEmpty(CacheFolder))
                {
                    TryDeleteDirectoryRecursive(CacheFolder);
                    CacheFolder = string.Empty;
                }
            }
            catch
            {
                // swallow
            }
        }

        #endregion

        #region Static DP callbacks

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PdfViewerControl)d;
            var newValue = (double)e.NewValue;

            if (newValue < control.MinZoom) newValue = control.MinZoom;
            if (newValue > control.MaxZoom) newValue = control.MaxZoom;
            if (Math.Abs(newValue - (double)e.NewValue) > double.Epsilon)
                control.Zoom = newValue;

            control.UpdateScaledLayout();
            control.UpdateZoomOverlay();

            if (!control._isAnimatingZoom && !control._fitRequested && !control.IsFitPending)
            {
                control.CenterInView();
            }
        }

        private void CenterInView()
        {
            if (_scrollViewer == null) return;

            DispatcherQueue?.TryEnqueue(() =>
            {
                double extentW = _scrollViewer.ExtentWidth;
                double extentH = _scrollViewer.ExtentHeight;
                double viewportW = _scrollViewer.ViewportWidth;
                double viewportH = _scrollViewer.ViewportHeight;

                double targetX = extentW > viewportW ? (extentW - viewportW) / 2 : 0;
                double targetY = extentH > viewportH ? (extentH - viewportH) / 2 : 0;

                try { _scrollViewer.ChangeView(targetX, targetY, null, true); } catch { }
            });
        }

        #endregion
    }
}