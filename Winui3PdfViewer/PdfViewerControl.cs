using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Winui3PdfViewer
{
    public interface IBitmapProvider
    {
        Task<IReadOnlyList<Bitmap>> GetBitmapsAsync(StorageFile file, CancellationToken cancellationToken = default);
    }

    public sealed class PdfViewerControl : Control
    {
        //constants representing controls
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

        //controls
        private ScrollViewer? _scrollViewer;
        private Grid? _viewportRoot;
        private StackPanel? _pagePanel;
        private TextBlock? _zoomOverlay;
        private Button? _btnPrevPage;
        private Button? _btnNextPage;
        private TextBlock? _pageOverlay;
        private ProgressRing? _progressRing;
        private Grid? _loadingOverlay;
        private bool _isTemplateApplied;
        private bool _hasFitOnInitialLoad = false;
        private StorageFile? _pendingFileToLoad;




        private double _imagePixelWidth;
        private double _imagePixelHeight;
        private double scrollRatioX = 0;
        private double scrollRatioY = 0;

        private int _currentIndex;
        private List<BitmapImage> _images = new();
        private List<Bitmap>? _rawBitmaps;
        private List<BitmapImage>? _pendingImages;

        private bool IsFitPending { get; set; }
        

        public PdfViewerControl()
        {
            DefaultStyleKey = typeof(PdfViewerControl);
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        public IBitmapProvider? BitmapProvider { get; set; }

        #region Dependency Properties

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(PdfViewerControl),
                new PropertyMetadata(1.0, OnZoomChanged));

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public static readonly DependencyProperty MinZoomProperty =
            DependencyProperty.Register(nameof(MinZoom), typeof(double), typeof(PdfViewerControl),
                new PropertyMetadata(0.1, OnZoomBoundsChanged));

        public double MinZoom
        {
            get => (double)GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        public static readonly DependencyProperty MaxZoomProperty =
            DependencyProperty.Register(nameof(MaxZoom), typeof(double), typeof(PdfViewerControl),
                new PropertyMetadata(8.0, OnZoomBoundsChanged));

        public double MaxZoom
        {
            get => (double)GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        public static readonly DependencyProperty SinglePageDisplayProperty =
            DependencyProperty.Register(nameof(SinglePageDisplay), typeof(bool), typeof(PdfViewerControl),
                new PropertyMetadata(false, OnSinglePageDisplayChanged));

        public bool SinglePageDisplay
        {
            get => (bool)GetValue(SinglePageDisplayProperty);
            set => SetValue(SinglePageDisplayProperty, value);
        }

        private static void OnSinglePageDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PdfViewerControl)d;
            control.UpdateImageSource();
        }

        #endregion

        #region Template hookup

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
            _viewportRoot = GetTemplateChild(PartViewportRoot) as Grid;
            _pagePanel = GetTemplateChild(PartPagePanel) as StackPanel;
            _zoomOverlay = GetTemplateChild(PartZoomOverlay) as TextBlock;
            _btnPrevPage = GetTemplateChild(PartBtnPrevPage) as Button;
            _btnNextPage = GetTemplateChild(PartBtnNextPage) as Button;
            _pageOverlay = GetTemplateChild(PartPageOverlay) as TextBlock;
            _progressRing = GetTemplateChild(PartProgressRing) as ProgressRing;
            _loadingOverlay = GetTemplateChild(PartLoadingOverlay) as Grid;

            _isTemplateApplied = true;

            // If a load was queued before template was ready, run it now
            if (_pendingFileToLoad != null)
            {
                _ = LoadFromStorageFileAsync(_pendingFileToLoad);
                _pendingFileToLoad = null;
            }


            if (GetTemplateChild(PartBtnZoomIn) is Button btnIn) btnIn.Click += (_, __) => ZoomIn();
            if (GetTemplateChild(PartBtnZoomOut) is Button btnOut) btnOut.Click += (_, __) => ZoomOut();
            if (GetTemplateChild(PartBtnFit) is Button btnFit) btnFit.Click += (_, __) => FitToControl();
            if (GetTemplateChild(PartBtn100) is Button btn100) btn100.Click += (_, __) => ZoomTo100();

            if (_btnPrevPage != null) _btnPrevPage.Click += (_, __) => ShowPage(_currentIndex - 1);
            if (_btnNextPage != null) _btnNextPage.Click += (_, __) => ShowPage(_currentIndex + 1);

            if (_pendingImages != null)
            {
                _images = _pendingImages;
                _pendingImages = null;
                _currentIndex = 0;
                UpdateImageSource();
            }

            UpdateScaledLayout();
            CenterInView();
            UpdateZoomOverlay();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            UpdateScaledLayout();
            CenterInView();
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (IsFitPending)
                FitToControl();
            else
            {
                UpdateScaledLayout();
                CenterInView();
            }
        }

        #endregion

        #region PDF ingestion

        public async Task LoadFromStorageFileAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            _hasFitOnInitialLoad = false;

            if (file is null) throw new ArgumentNullException(nameof(file));
            if (BitmapProvider == null)
                throw new InvalidOperationException("BitmapProvider is not set.");

            if (!_isTemplateApplied)
            {
                _pendingFileToLoad = file;
                return;
            }

            SetLoadingState(true);

            try
            {
                var bitmaps = await BitmapProvider.GetBitmapsAsync(file, cancellationToken).ConfigureAwait(false);

                await RunOnUIThreadAsync(() =>
                {
                    SetBitmapsInternal(bitmaps);
                });
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void SetBitmapsInternal(IReadOnlyList<Bitmap> bitmaps)
        {
            DisposeAndClearRawBitmaps();

            _rawBitmaps = new List<Bitmap>(bitmaps.Count);
            _rawBitmaps.AddRange(bitmaps);

            var converted = bitmaps.Select(ConvertBitmapToBitmapImage).ToList();

            if (_pagePanel == null)
            {
                _pendingImages = converted;
                return;
            }

            _images = converted;
            _currentIndex = 0;
            UpdateImageSource();
        }

        private void DisposeAndClearRawBitmaps()
        {
            if (_rawBitmaps != null)
            {
                foreach (var b in _rawBitmaps)
                {
                    try { b.Dispose(); } catch { }
                }
                _rawBitmaps = null;
            }
            _images.Clear();
            _currentIndex = 0;
            _imagePixelWidth = 0;
            _imagePixelHeight = 0;
        }

        private async Task RunOnUIThreadAsync(Action action)
        {
            var dq = DispatcherQueue;
            if (dq == null || dq.HasThreadAccess)
            {
                action();
                return;
            }

            var tcs = new TaskCompletionSource();
            dq.TryEnqueue(() =>
            {
                try { action(); tcs.SetResult(); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            await tcs.Task.ConfigureAwait(false);
        }

        #endregion

        #region Image handling

        private void UpdateImageSource()
        {
            if (_images.Count == 0 || _pagePanel == null) return;

            _pagePanel.Children.Clear();

            if (SinglePageDisplay)
            {
                _pagePanel.Children.Add(CreateImageElement(_images[_currentIndex]));
                _btnPrevPage!.Visibility = _currentIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
                _btnNextPage!.Visibility = _currentIndex < _images.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                foreach (var bmp in _images)
                    _pagePanel.Children.Add(CreateImageElement(bmp));

                _btnPrevPage!.Visibility = Visibility.Collapsed;
                _btnNextPage!.Visibility = Visibility.Collapsed;
            }

            _imagePixelWidth = _images[_currentIndex].PixelWidth;
            _imagePixelHeight = _images[_currentIndex].PixelHeight;
            UpdateScaledLayout();
            CenterInView();
            UpdatePageOverlay();

            if (!_hasFitOnInitialLoad)
            {
                FitToControl();
                _hasFitOnInitialLoad = true;
            }

        }

        private Microsoft.UI.Xaml.Controls.Image CreateImageElement(BitmapImage bmp)
        {
            return new Microsoft.UI.Xaml.Controls.Image
            {
                Source = bmp,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void ShowPage(int index)
        {
            if (index < 0 || index >= _images.Count) return;
            _currentIndex = index;
            UpdateImageSource();
            UpdatePageOverlay();

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

        #endregion

        #region Zoom logic and animation

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PdfViewerControl)d;

            var newValue = (double)e.NewValue;
            if (newValue < control.MinZoom) newValue = control.MinZoom;
            if (newValue > control.MaxZoom) newValue = control.MaxZoom;

            if (Math.Abs(newValue - (double)e.NewValue) > double.Epsilon)
                control.Zoom = newValue;

            control.IsFitPending = false;
            control.UpdateScaledLayout();
            control.CenterInView();
            control.UpdateZoomOverlay();
        }

        private static void OnZoomBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PdfViewerControl)d;
            if (control.Zoom < control.MinZoom) control.Zoom = control.MinZoom;
            if (control.Zoom > control.MaxZoom) control.Zoom = control.MaxZoom;
        }

        private void UpdateScaledLayout()
        {
            if (_pagePanel == null || _scrollViewer == null) return;

            foreach (var child in _pagePanel.Children.OfType<Microsoft.UI.Xaml.Controls.Image>())
            {
                if (child.Source is BitmapImage bmp)
                {
                    child.Width = bmp.PixelWidth * Zoom;
                    child.Height = bmp.PixelHeight * Zoom;
                }
            }

            if (_viewportRoot != null)
            {
                _viewportRoot.MinWidth = _scrollViewer.ActualWidth;
                _viewportRoot.MinHeight = _scrollViewer.ActualHeight;
            }
        }

        private void CenterInView()
        {
            if (_scrollViewer == null) return;

            DispatcherQueue.TryEnqueue(() =>
            {
                double extentW = _scrollViewer.ExtentWidth;
                double extentH = _scrollViewer.ExtentHeight;
                double viewportW = _scrollViewer.ViewportWidth;
                double viewportH = _scrollViewer.ViewportHeight;

                double targetX = extentW > viewportW ? (extentW - viewportW) / 2 : 0;
                double targetY = extentH > viewportH ? (extentH - viewportH) / 2 : 0;

                _scrollViewer.ChangeView(targetX, targetY, null, true);
            });
        }

        private void UpdateZoomOverlay()
        {
            if (_zoomOverlay != null)
                _zoomOverlay.Text = $"{Zoom * 100:0}%";
        }

        private bool _isAnimatingZoom;
        private double _zoomStart;
        private double _zoomTarget;
        private double _zoomDurationMs;
        private DateTime _zoomStartTime;
        private double _centerX, _centerY, _viewportW, _viewportH;

        private void AnimateZoom(double targetZoom, double durationMs = 200)
        {
            if (_scrollViewer == null)
            {
                Zoom = targetZoom;
                return;
            }

            if (_scrollViewer != null)
            {
                scrollRatioX = _scrollViewer.HorizontalOffset / Math.Max(1, _scrollViewer.ExtentWidth - _scrollViewer.ViewportWidth);
                scrollRatioY = _scrollViewer.VerticalOffset / Math.Max(1, _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight);
            }


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

            if (!_isAnimatingZoom)
            {
                Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnZoomAnimationFrame;
                _isAnimatingZoom = true;
            }
        }

        private void OnZoomAnimationFrame(object? sender, object e)
        {
            var elapsed = (DateTime.Now - _zoomStartTime).TotalMilliseconds;
            var progress = Math.Min(1.0, elapsed / _zoomDurationMs);
            var eased = 1 - Math.Pow(1 - progress, 3);

            var currentZoom = _zoomStart + (_zoomTarget - _zoomStart) * eased;
            Zoom = currentZoom;

            var newCenterX = _centerX * (currentZoom / _zoomStart);
            var newCenterY = _centerY * (currentZoom / _zoomStart);

            _scrollViewer?.ChangeView(
                newCenterX - _viewportW / 2,
                newCenterY - _viewportH / 2,
                null,
                true);

            if (progress >= 1.0)
            {
                Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnZoomAnimationFrame;
                _isAnimatingZoom = false;

                DispatcherQueue?.TryEnqueue(() =>
                {
                    if (_scrollViewer != null)
                    {
                        double newOffsetX = scrollRatioX * Math.Max(1, _scrollViewer.ExtentWidth - _scrollViewer.ViewportWidth);
                        double newOffsetY = scrollRatioY * Math.Max(1, _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight);
                        _scrollViewer.ChangeView(newOffsetX, newOffsetY, null, true);
                    }
                });

            }
        }

        public void ZoomIn(double step = 0.1) => AnimateZoom(Zoom + step);
        public void ZoomOut(double step = 0.1) => AnimateZoom(Zoom - step);
        public void ZoomTo100() { IsFitPending = false; AnimateZoom(1.0); }
        public void FitToControl()
        {
            IsFitPending = true;
            if (_scrollViewer == null || _imagePixelWidth <= 0 || _imagePixelHeight <= 0) return;

            double scaleX = _scrollViewer.ActualWidth / _imagePixelWidth;
            double scaleY = _scrollViewer.ActualHeight / _imagePixelHeight;
            AnimateZoom(Math.Min(scaleX, scaleY));
        }

        private void UpdatePageOverlay()
        {
            if (_pageOverlay == null || !SinglePageDisplay || _images.Count == 0)
            {
                if (_pageOverlay != null) _pageOverlay.Text = "";
                return;
            }

            _pageOverlay.Text = $"Page {_currentIndex + 1} of {_images.Count}";
        }

        private void SetLoadingState(bool isLoading)
        {
            if (!_isTemplateApplied || DispatcherQueue == null) return;

            DispatcherQueue.TryEnqueue(() =>
            {
                if (_progressRing != null)
                    _progressRing.IsActive = isLoading;

                if (_loadingOverlay != null)
                    _loadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            });
        }








        #endregion
    }
}