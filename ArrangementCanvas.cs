using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AtlasToolEditorAvalonia
{
    public class ArrangementCanvas : Control
    {
        // Custom Background property for hit-testing.
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            AvaloniaProperty.Register<ArrangementCanvas, IBrush?>(nameof(Background));

        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public List<TextureItem> Items { get; } = new List<TextureItem>();

        public bool ShowGrid { get; set; } = true;
        public double Zoom { get; set; } = 1.0;
        public Point Pan { get; set; } = new Point(0, 0);

        private bool _isPanning;
        private Point _lastPanPoint;

        private bool _isDragging;
        private List<TextureItem> _selectedItems = new List<TextureItem>();
        private Point _dragStart;
        private Dictionary<TextureItem, Rect> _originalRects = new Dictionary<TextureItem, Rect>();

        private readonly Rect _arrangementArea = new Rect(0, 0, 1280, 720);

        public ArrangementCanvas()
        {
            Background = Brushes.Transparent;
            Focusable = true;
            ClipToBounds = true;
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerWheelChanged += OnPointerWheelChanged;
            DoubleTapped += OnDoubleTapped; // Instead of Tapped
        }

        // Public method to allow external selection via the list.
        public void SelectItem(TextureItem item)
        {
            // Clear previous selection and add the specified item.
            _selectedItems.Clear();
            _selectedItems.Add(item);
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            // Fill the background of the control.
            context.FillRectangle(Background, new Rect(Bounds.Size));

            var matrix = Matrix.CreateScale(Zoom, Zoom) * Matrix.CreateTranslation(Pan.X, Pan.Y);
            using (context.PushTransform(matrix))
            {
                if (ShowGrid)
                {
                    var gridPen = new Pen(Brushes.LightGray, 1 / Zoom);
                    int spacing = 50;
                    for (double x = 0; x <= _arrangementArea.Width; x += spacing)
                        context.DrawLine(gridPen, new Point(x, 0), new Point(x, _arrangementArea.Height));
                    for (double y = 0; y <= _arrangementArea.Height; y += spacing)
                        context.DrawLine(gridPen, new Point(0, y), new Point(_arrangementArea.Width, y));
                }
                // Draw arrangement area boundary.
                var boundaryPen = new Pen(Brushes.Red, 2 / Zoom);
                context.DrawRectangle(null, boundaryPen, _arrangementArea);

                // Draw texture items.
                foreach (var item in Items.OrderBy(i => i.Z))
                {
                    if (item.Image != null)
                        context.DrawImage(item.Image,
                            new Rect(0, 0, item.Image.PixelSize.Width, item.Image.PixelSize.Height),
                            item.Bounds);
                    var formattedText = new FormattedText(
                        $"{item.Name} (Z: {item.Z})",
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        12,
                        Brushes.WhiteSmoke);

                    var textGeometry = formattedText.BuildGeometry(new Point(item.Bounds.X + 2, item.Bounds.Y + 2));

                    context.DrawGeometry(
                        Brushes.WhiteSmoke,
                        new Pen(Brushes.Black, 0.5) { LineJoin = PenLineJoin.Round },
                        textGeometry);
                    if (_selectedItems.Contains(item))
                    {
                        var selPen = new Pen(Brushes.Blue, 1 / Zoom, DashStyle.Dash);
                        context.DrawRectangle(null, selPen, item.Bounds);
                    }
                }
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pt = e.GetPosition(this);
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                _isPanning = true;
                _lastPanPoint = pt;
                Focus();
                e.Handled = true;
                return;
            }
            else if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var worldPt = ScreenToWorld(pt);
                var hitItem = Items.OrderByDescending(i => i.Z).FirstOrDefault(i => i.Bounds.Contains(worldPt));
                if (hitItem != null)
                {
                    _selectedItems.Clear();
                    _selectedItems.Add(hitItem);
                    _isDragging = true;
                    _dragStart = worldPt;
                    _originalRects.Clear();
                    foreach (var sel in _selectedItems)
                        _originalRects[sel] = sel.Bounds;
                }
                else
                {
                    _selectedItems.Clear();
                }
                InvalidateVisual();
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var pt = e.GetPosition(this);
            if (_isPanning && e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                var dx = pt.X - _lastPanPoint.X;
                var dy = pt.Y - _lastPanPoint.Y;
                Pan = new Point(Pan.X + dx, Pan.Y + dy);
                _lastPanPoint = pt;
                InvalidateVisual();
            }
            else if (_isDragging && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var worldPt = ScreenToWorld(pt);
                var delta = new Point(worldPt.X - _dragStart.X, worldPt.Y - _dragStart.Y);
                foreach (var sel in _selectedItems)
                {
                    var orig = _originalRects[sel];
                    var candidate = new Rect(orig.X + delta.X, orig.Y + delta.Y, orig.Width, orig.Height);
                    candidate = ClampToArrangement(candidate);
                    sel.Bounds = candidate;
                }
                InvalidateVisual();
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isPanning && e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                _isPanning = false;
            if (_isDragging && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                _isDragging = false;
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            Focus();
            var pt = e.GetPosition(this);
            double oldZoom = Zoom;
            double factor = e.Delta.Y > 0 ? 1.1 : 0.9;
            Zoom *= factor;
            Pan = pt - (pt - Pan) * (Zoom / oldZoom);
            InvalidateVisual();
            e.Handled = true;
        }

        private Point ScreenToWorld(Point screenPt)
        {
            double wx = (screenPt.X - Pan.X) / Zoom;
            double wy = (screenPt.Y - Pan.Y) / Zoom;
            return new Point(wx, wy);
        }

        private Rect ClampToArrangement(Rect r)
        {
            double newX = r.X;
            double newY = r.Y;
            if (r.X < _arrangementArea.X)
                newX = _arrangementArea.X;
            if (r.Y < _arrangementArea.Y)
                newY = _arrangementArea.Y;
            if (r.X + r.Width > _arrangementArea.Right)
                newX = _arrangementArea.Right - r.Width;
            if (r.Y + r.Height > _arrangementArea.Bottom)
                newY = _arrangementArea.Bottom - r.Height;
            return new Rect(newX, newY, r.Width, r.Height);
        }

        private async void OnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (_selectedItems.Any())
            {
                var item = _selectedItems.First();
                _isDragging = false;

                var owner = TopLevel.GetTopLevel(this) as Window;
                if (owner == null)
                    return;

                var dlg = new InputDialog("Change Z", "Enter new Z:");
                dlg.FindControl<TextBox>("InputBox").Text = item.Z.ToString();

                var result = await dlg.ShowDialog<string>(owner);
                if (!string.IsNullOrEmpty(result) && int.TryParse(result, out int newZ))
                {
                    item.Z = newZ;
                }

                InvalidateVisual();
            }
        }
    }
}