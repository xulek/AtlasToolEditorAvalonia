using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtlasToolEditorAvalonia
{
    public partial class ArrangementWindow : Window
    {
        private Button? _loadArrangementButton, _saveArrangementButton, _undoButton;
        private ArrangementCanvas? _arrCanvas;
        private Bitmap _fullImage;
        private List<RegionDefinition> _regionDefinitions;
        private Stack<List<UndoItem>> _undoStack = new Stack<List<UndoItem>>();

        public ArrangementWindow(Bitmap fullImage, List<RegionDefinition> regionDefinitions)
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _fullImage = fullImage;
            _regionDefinitions = regionDefinitions;

            _loadArrangementButton = this.FindControl<Button>("LoadArrangementButton");
            _saveArrangementButton = this.FindControl<Button>("SaveArrangementButton");
            _undoButton = this.FindControl<Button>("UndoButton");
            _arrCanvas = this.FindControl<ArrangementCanvas>("ArrangementCanvasControl");

            if (_arrCanvas != null)
            {
                _arrCanvas.Focusable = true;
                _arrCanvas.Focus();

                // Save the layout state when the left mouse button is pressed.
                _arrCanvas.PointerPressed += (_, e) =>
                {
                    if (e.GetCurrentPoint(_arrCanvas).Properties.IsLeftButtonPressed)
                    {
                        SaveState();
                    }
                };
            }

            _loadArrangementButton.Click += async (_, __) => await LoadArrangementAsync();
            _saveArrangementButton.Click += async (_, __) => await SaveArrangementAsync();
            _undoButton.Click += (_, __) => Undo();

            InitializeItems();
        }

        private void InitializeItems()
        {
            if (_arrCanvas == null)
                return;
            _arrCanvas.Items.Clear();
            foreach (var def in _regionDefinitions)
            {
                int right = def.X + def.Width;
                int bottom = def.Y + def.Height;
                if (right > _fullImage.PixelSize.Width || bottom > _fullImage.PixelSize.Height)
                    continue;
                // Crop the region from the full image.
                var cropped = new RenderTargetBitmap(new PixelSize(def.Width, def.Height), new Vector(96, 96));
                using (var ctx = cropped.CreateDrawingContext())
                {
                    ctx.DrawImage(
                        _fullImage,
                        new Rect(def.X, def.Y, def.Width, def.Height),
                        new Rect(0, 0, def.Width, def.Height));
                }
                var item = new TextureItem
                {
                    Name = def.Name,
                    Image = cropped,
                    Bounds = new Rect(def.X, def.Y, def.Width, def.Height),
                    Z = 0
                };
                _arrCanvas.Items.Add(item);
            }
            _arrCanvas.InvalidateVisual();
        }

        private async Task LoadArrangementAsync()
        {
#pragma warning disable CS0618
            var jsonDialog = new OpenFileDialog();
            jsonDialog.Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "JSON", Extensions = new List<string> { "json" } }
            };
#pragma warning restore CS0618
            var jsonResult = await jsonDialog.ShowAsync(this);
            if (jsonResult == null || jsonResult.Length == 0)
                return;
            string jsonPath = jsonResult[0];
            List<ArrangedRegion>? arranged = null;
            try
            {
                string json = File.ReadAllText(jsonPath);
                arranged = JsonSerializer.Deserialize<List<ArrangedRegion>>(json);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, "Error loading JSON: " + ex.Message, "Error");
                return;
            }
            if (arranged == null)
                return;
            // Update items positions.
            if (_arrCanvas != null)
            {
                foreach (var arr in arranged)
                {
                    var item = _arrCanvas.Items.FirstOrDefault(x => x.Name == arr.Name);
                    if (item != null)
                    {
                        item.Bounds = new Rect(arr.ScreenX, arr.ScreenY, arr.Width, arr.Height);
                        item.Z = arr.Z;
                    }
                }
                _arrCanvas.InvalidateVisual();
            }
        }

        private async Task SaveArrangementAsync()
        {
            if (_arrCanvas == null || _arrCanvas.Items.Count == 0)
            {
                await MessageBox.Show(this, "No items to save.", "Info");
                return;
            }
            var arranged = new List<ArrangedRegion>();
            foreach (var item in _arrCanvas.Items)
            {
                arranged.Add(new ArrangedRegion
                {
                    Name = item.Name,
                    ScreenX = (int)item.Bounds.X,
                    ScreenY = (int)item.Bounds.Y,
                    Width = (int)item.Bounds.Width,
                    Height = (int)item.Bounds.Height,
                    Z = item.Z
                });
            }
#pragma warning disable CS0618
            var saveDialog = new SaveFileDialog();
            saveDialog.Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "JSON", Extensions = new List<string> { "json" } }
            };
            saveDialog.InitialFileName = "arranged_layout.json";
#pragma warning restore CS0618
            var path = await saveDialog.ShowAsync(this);
            if (string.IsNullOrEmpty(path))
                return;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(arranged, options);
                File.WriteAllText(path, json);
                await MessageBox.Show(this, "Arrangement saved to " + path, "Save");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(this, "Error saving arrangement: " + ex.Message, "Error");
            }
        }

        private class UndoItem
        {
            public string Name { get; set; } = "";
            public Rect Bounds { get; set; }
            public int Z { get; set; }
        }

        private void SaveState()
        {
            if (_arrCanvas == null)
                return;
            var snapshot = new List<UndoItem>();
            foreach (var item in _arrCanvas.Items)
            {
                snapshot.Add(new UndoItem
                {
                    Name = item.Name,
                    Bounds = item.Bounds,
                    Z = item.Z
                });
            }
            _undoStack.Push(snapshot);
        }

        private void Undo()
        {
            if (_undoStack.Count == 0 || _arrCanvas == null)
            {
                _ = MessageBox.Show(this, "Nothing to undo.", "Undo");
                return;
            }
            var snapshot = _undoStack.Pop();
            foreach (var snap in snapshot)
            {
                var item = _arrCanvas.Items.FirstOrDefault(i => i.Name == snap.Name);
                if (item != null)
                {
                    item.Bounds = snap.Bounds;
                    item.Z = snap.Z;
                }
            }
            _arrCanvas.InvalidateVisual();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }

    public class ArrangedRegion
    {
        public string Name { get; set; } = "";
        public int ScreenX { get; set; }
        public int ScreenY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Z { get; set; }
    }
}