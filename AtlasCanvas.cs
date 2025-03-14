using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace AtlasToolEditorAvalonia
{
    public enum ResizeMode { None, Move, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }

    // Control for drawing, creating, and editing regions.
    public class AtlasCanvas : Control
    {
        public Bitmap? LoadedImage { get; set; }
        public List<RegionDefinition> Regions { get; set; } = new List<RegionDefinition>();
        public RegionDefinition? SelectedRegion { get; private set; }

        public double Zoom { get; set; } = 1.0;
        private Vector _pan = new Vector(0, 0);
        private bool _isPanning;
        private Point _lastPanPoint;

        // For new region drawing.
        private bool _isDrawing;
        private Point _drawStart;
        private Rect _currentRect;

        // For dragging/resizing.
        private bool _isDraggingRegion;
        private ResizeMode _resizeMode = ResizeMode.None;
        private Point _dragStart;
        private Rect _originalRegionRect;
        private const double EdgeThreshold = 8;

        public AtlasCanvas()
        {
            ClipToBounds = true;
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerWheelChanged += OnPointerWheelChanged;
            DoubleTapped += OnDoubleTapped;
        }

        public override void Render(DrawingContext context)
        {
            // Wypełniamy całe tło kontrolki na biało.
            context.FillRectangle(Brushes.White, new Rect(Bounds.Size));

            base.Render(context);

            var matrix = Matrix.CreateScale(new Vector(Zoom, Zoom)) * Matrix.CreateTranslation(_pan.X, _pan.Y);
            using (context.PushTransform(matrix))
            {
                if (LoadedImage != null)
                {
                    context.DrawImage(
                        LoadedImage,
                        new Rect(0, 0, LoadedImage.PixelSize.Width, LoadedImage.PixelSize.Height),
                        new Rect(0, 0, LoadedImage.PixelSize.Width, LoadedImage.PixelSize.Height));
                }
                foreach (var region in Regions)
                {
                    var rect = new Rect(region.X, region.Y, region.Width, region.Height);
                    var pen = new Pen(Brushes.Red, region == SelectedRegion ? 1 : 0.5);
                    context.DrawRectangle(null, pen, rect);
                    var ft = new FormattedText(
                        region.Name,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        12,
                        Brushes.Blue);
                    context.DrawText(ft, new Point(region.X, region.Y));
                }
                if (_isDrawing)
                {
                    context.DrawRectangle(null, new Pen(Brushes.Green, 0.5, DashStyle.Dash), _currentRect);
                }
            }
        }

        private async void OnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (SelectedRegion != null)
            {
                // Clear dragging/resizing state so the region is released.
                _isDraggingRegion = false;
                _resizeMode = ResizeMode.None;

                // Get the owner window reliably.
                var owner = TopLevel.GetTopLevel(this) as Window;
                if (owner == null)
                    return;
                
                var dlg = new InputDialog("Change region name", "Enter new name:");
                var inputBox = dlg.FindControl<TextBox>("InputBox");
                if (inputBox != null)
                {
                    inputBox.Text = SelectedRegion.Name;
                }
                var result = await dlg.ShowDialog<string>(owner);
                if (!string.IsNullOrEmpty(result))
                {
                    SelectedRegion.Name = result;
                }
                InvalidateVisual();
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pt = e.GetPosition(this);
            var logicalPt = (pt - _pan) / Zoom;

            // Only check for region hit if the left mouse button is pressed.
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                for (int i = Regions.Count - 1; i >= 0; i--)
                {
                    if (Regions[i].Contains(logicalPt))
                    {
                        SelectedRegion = Regions[i];
                        _resizeMode = GetResizeMode(SelectedRegion, logicalPt);
                        _isDraggingRegion = true;
                        _dragStart = logicalPt;
                        _originalRegionRect = new Rect(SelectedRegion.X, SelectedRegion.Y, SelectedRegion.Width, SelectedRegion.Height);
                        InvalidateVisual();
                        e.Handled = true;
                        return;
                    }
                }
                // No region hit: start drawing a new region.
                SelectedRegion = null;
                _isDrawing = true;
                _drawStart = logicalPt;
                _currentRect = new Rect(_drawStart, new Size(0, 0));
            }
            else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                // Right-click is only used for panning.
                _isPanning = true;
                _lastPanPoint = pt;
            }
            InvalidateVisual();
            e.Handled = true;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var pt = e.GetPosition(this);
            var logicalPt = (pt - _pan) / Zoom;
            // Update cursor if not dragging/drawing.
            if (!_isPanning && !_isDraggingRegion && !_isDrawing)
            {
                RegionDefinition? hovered = null;
                foreach (var region in Regions)
                {
                    if (region.Contains(logicalPt))
                    {
                        hovered = region;
                        break;
                    }
                }
                if (hovered != null)
                {
                    var mode = GetResizeMode(hovered, logicalPt);
                    switch (mode)
                    {
                        case ResizeMode.Left:
                        case ResizeMode.Right:
                            Cursor = new Cursor(StandardCursorType.SizeWestEast);
                            break;
                        case ResizeMode.Top:
                        case ResizeMode.Bottom:
                            Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                            break;
                        case ResizeMode.TopLeft:
                            Cursor = new Cursor(StandardCursorType.TopLeftCorner);
                            break;
                        case ResizeMode.TopRight:
                            Cursor = new Cursor(StandardCursorType.TopRightCorner);
                            break;
                        case ResizeMode.BottomLeft:
                            Cursor = new Cursor(StandardCursorType.BottomLeftCorner);
                            break;
                        case ResizeMode.BottomRight:
                            Cursor = new Cursor(StandardCursorType.BottomRightCorner);
                            break;
                        case ResizeMode.Move:
                            Cursor = new Cursor(StandardCursorType.SizeAll);
                            break;
                        default:
                            Cursor = new Cursor(StandardCursorType.Arrow);
                            break;
                    }
                }
                else
                {
                    Cursor = new Cursor(StandardCursorType.Arrow);
                }
            }
            if (_isPanning)
            {
                _pan += pt - _lastPanPoint;
                _lastPanPoint = pt;
                InvalidateVisual();
                e.Handled = true;
                return;
            }
            if (_isDraggingRegion && SelectedRegion != null)
            {
                var delta = logicalPt - _dragStart;
                Rect candidate;
                switch (_resizeMode)
                {
                    case ResizeMode.Move:
                        candidate = new Rect(_originalRegionRect.X + delta.X, _originalRegionRect.Y + delta.Y,
                                               _originalRegionRect.Width, _originalRegionRect.Height);
                        break;
                    case ResizeMode.Left:
                        int newX = (int)(_originalRegionRect.X + delta.X);
                        candidate = new Rect(newX, _originalRegionRect.Y, _originalRegionRect.Right - newX, _originalRegionRect.Height);
                        break;
                    case ResizeMode.Right:
                        candidate = new Rect(_originalRegionRect.X, _originalRegionRect.Y, (int)(_originalRegionRect.Width + delta.X), _originalRegionRect.Height);
                        break;
                    case ResizeMode.Top:
                        int newY = (int)(_originalRegionRect.Y + delta.Y);
                        candidate = new Rect(_originalRegionRect.X, newY, _originalRegionRect.Width, _originalRegionRect.Bottom - newY);
                        break;
                    case ResizeMode.Bottom:
                        candidate = new Rect(_originalRegionRect.X, _originalRegionRect.Y, _originalRegionRect.Width, (int)(_originalRegionRect.Height + delta.Y));
                        break;
                    case ResizeMode.TopLeft:
                        newX = (int)(_originalRegionRect.X + delta.X);
                        newY = (int)(_originalRegionRect.Y + delta.Y);
                        candidate = new Rect(newX, newY, _originalRegionRect.Right - newX, _originalRegionRect.Bottom - newY);
                        break;
                    case ResizeMode.TopRight:
                        newY = (int)(_originalRegionRect.Y + delta.Y);
                        candidate = new Rect(_originalRegionRect.X, newY, (int)(_originalRegionRect.Width + delta.X), _originalRegionRect.Bottom - newY);
                        break;
                    case ResizeMode.BottomLeft:
                        newX = (int)(_originalRegionRect.X + delta.X);
                        candidate = new Rect(newX, _originalRegionRect.Y, _originalRegionRect.Right - newX, (int)(_originalRegionRect.Height + delta.Y));
                        break;
                    case ResizeMode.BottomRight:
                        candidate = new Rect(_originalRegionRect.X, _originalRegionRect.Y, (int)(_originalRegionRect.Width + delta.X), (int)(_originalRegionRect.Height + delta.Y));
                        break;
                    default:
                        candidate = _originalRegionRect;
                        break;
                }
                // Clamp candidate to image boundaries.
                if (LoadedImage != null)
                {
                    double maxW = LoadedImage.PixelSize.Width;
                    double maxH = LoadedImage.PixelSize.Height;
                    double x = Math.Max(0, candidate.X);
                    double y = Math.Max(0, candidate.Y);
                    double right = Math.Min(maxW, candidate.X + candidate.Width);
                    double bottom = Math.Min(maxH, candidate.Y + candidate.Height);
                    candidate = new Rect(x, y, right - x, bottom - y);
                }
                // Check for collision with other regions.
                bool collision = false;
                foreach (var reg in Regions)
                {
                    if (reg == SelectedRegion) continue;
                    var r = new Rect(reg.X, reg.Y, reg.Width, reg.Height);
                    if (candidate.Intersects(r))
                    {
                        collision = true;
                        break;
                    }
                }
                if (!collision)
                {
                    SelectedRegion.X = (int)candidate.X;
                    SelectedRegion.Y = (int)candidate.Y;
                    SelectedRegion.Width = (int)candidate.Width;
                    SelectedRegion.Height = (int)candidate.Height;
                }
                InvalidateVisual();
                e.Handled = true;
                return;
            }
            if (_isDrawing)
            {
                var current = logicalPt;
                double x = Math.Min(_drawStart.X, current.X);
                double y = Math.Min(_drawStart.Y, current.Y);
                double w = Math.Abs(_drawStart.X - current.X);
                double h = Math.Abs(_drawStart.Y - current.Y);
                // Clamp drawing region to image boundaries.
                if (LoadedImage != null)
                {
                    double maxW = LoadedImage.PixelSize.Width;
                    double maxH = LoadedImage.PixelSize.Height;
                    x = Math.Max(0, x);
                    y = Math.Max(0, y);
                    if (x + w > maxW) w = maxW - x;
                    if (y + h > maxH) h = maxH - y;
                }
                _currentRect = new Rect(x, y, w, h);
                InvalidateVisual();
                e.Handled = true;
            }
        }

        private async void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
            }
            if (_isDraggingRegion)
            {
                _isDraggingRegion = false;
                _resizeMode = ResizeMode.None;
            }
            if (_isDrawing)
            {
                _isDrawing = false;
                if (_currentRect.Width > 5 && _currentRect.Height > 5 && LoadedImage != null)
                {
                    bool collides = false;
                    foreach (var reg in Regions)
                    {
                        var r = new Rect(reg.X, reg.Y, reg.Width, reg.Height);
                        if (_currentRect.Intersects(r))
                        {
                            collides = true;
                            break;
                        }
                    }
                    var owner = this.VisualRoot as Window;
                    if (owner == null)
                        return;
                    if (collides)
                    {
                        await MessageBox.Show(owner, "New region overlaps an existing region.", "Warning");
                    }
                    else
                    {
                        var newRegion = new RegionDefinition
                        {
                            X = (int)_currentRect.X,
                            Y = (int)_currentRect.Y,
                            Width = (int)_currentRect.Width,
                            Height = (int)_currentRect.Height,
                            Name = "Region"
                        };
                        Regions.Add(newRegion);
                        SelectedRegion = newRegion;
                        var dlg = new InputDialog("Change region name", "Enter new name:");
                        var inputBox = dlg.FindControl<TextBox>("InputBox");
                        inputBox.Text = newRegion.Name;
                        var result = await dlg.ShowDialog<string>(owner);
                        if (!string.IsNullOrEmpty(result))
                        {
                            newRegion.Name = result;
                        }
                    }
                }
            }
            InvalidateVisual();
            e.Handled = true;
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            // Get mouse pointer position in control coordinates.
            var pt = e.GetPosition(this);
            double oldZoom = Zoom;
            double factor = e.Delta.Y > 0 ? 1.1 : 0.9;
            Zoom *= factor;
            // Adjust the pan offset so that the point under the cursor remains fixed.
            _pan = pt - (pt - _pan) * (Zoom / oldZoom);
            InvalidateVisual();
            e.Handled = true;
        }

        private ResizeMode GetResizeMode(RegionDefinition region, Point pt)
        {
            bool nearLeft = Math.Abs(pt.X - region.X) <= EdgeThreshold;
            bool nearRight = Math.Abs(pt.X - (region.X + region.Width)) <= EdgeThreshold;
            bool nearTop = Math.Abs(pt.Y - region.Y) <= EdgeThreshold;
            bool nearBottom = Math.Abs(pt.Y - (region.Y + region.Height)) <= EdgeThreshold;

            if (nearLeft && nearTop) return ResizeMode.TopLeft;
            if (nearRight && nearTop) return ResizeMode.TopRight;
            if (nearLeft && nearBottom) return ResizeMode.BottomLeft;
            if (nearRight && nearBottom) return ResizeMode.BottomRight;
            if (nearLeft) return ResizeMode.Left;
            if (nearRight) return ResizeMode.Right;
            if (nearTop) return ResizeMode.Top;
            if (nearBottom) return ResizeMode.Bottom;
            if (region.Contains(pt)) return ResizeMode.Move;
            return ResizeMode.None;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Delete && SelectedRegion != null)
            {
                Regions.Remove(SelectedRegion);
                SelectedRegion = null;
                InvalidateVisual();
            }
        }

        public void DeleteSelectedRegion()
        {
            if (SelectedRegion != null)
            {
                Regions.Remove(SelectedRegion);
                SelectedRegion = null;
                InvalidateVisual();
            }
        }
    }
}
