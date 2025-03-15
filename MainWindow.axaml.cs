using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtlasToolEditorAvalonia
{
    public partial class MainWindow : Window
    {
        private Button? _loadImageButton, _saveJsonButton, _loadJsonButton, _clearButton, _zoomInButton, _zoomOutButton, _arrangeButton;
        private AtlasCanvas? _atlasCanvas;

        private Bitmap? _loadedImage;
        private List<RegionDefinition> _regions = new List<RegionDefinition>();

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _loadImageButton = this.FindControl<Button>("LoadImageButton");
            _saveJsonButton = this.FindControl<Button>("SaveJsonButton");
            _loadJsonButton = this.FindControl<Button>("LoadJsonButton");
            _clearButton = this.FindControl<Button>("ClearButton");
            _zoomInButton = this.FindControl<Button>("ZoomInButton");
            _zoomOutButton = this.FindControl<Button>("ZoomOutButton");
            _arrangeButton = this.FindControl<Button>("ArrangeButton");
            _atlasCanvas = this.FindControl<AtlasCanvas>("AtlasCanvasControl");

            _loadImageButton.Click += async (_, __) => await LoadImageAsync();
            _saveJsonButton.Click += async (_, __) => await SaveJsonAsync();
            _loadJsonButton.Click += async (_, __) => await LoadJsonAsync();
            _clearButton.Click += (_, __) => { _atlasCanvas!.Regions.Clear(); _atlasCanvas!.InvalidateVisual(); };
            _zoomInButton.Click += (_, __) => { _atlasCanvas!.Zoom *= 1.1; _atlasCanvas!.InvalidateVisual(); };
            _zoomOutButton.Click += (_, __) => { _atlasCanvas!.Zoom *= 0.9; _atlasCanvas!.InvalidateVisual(); };
            _arrangeButton.Click += (_, __) =>
            {
                var image = _atlasCanvas?.LoadedImage;
                var regions = _atlasCanvas?.Regions;
                if (image != null && regions != null && regions.Any())
                {
                    var arrangementWin = new ArrangementWindow(image, regions);
                    arrangementWin.Show();
                }
                else
                {
                    MessageBox.Show(this, "Image or regions not loaded.", "Warning");
                }
            };
        }

        private async Task LoadImageAsync()
        {
            var dialog = new OpenFileDialog();
            dialog.Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "Images", Extensions = new List<string> { "png", "jpg", "jpeg", "bmp", "ozj", "ozt", "ozb", "ozd", "ozp" } }
            };
            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                string filePath = result[0];
                WriteableBitmap? bitmap = null;
                var ext = Path.GetExtension(filePath).ToLowerInvariant();

                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        // Standardowe obrazy ładowane przy użyciu Avalonia Bitmap.
                        bitmap = new Bitmap(stream) as WriteableBitmap;
                    }
                }
                else
                {
                    // Niestandardowe rozszerzenia obsługujemy za pomocą CustomImageLoader.
                    var loader = new CustomImageLoader();
                    bitmap = await loader.InitAsync(filePath);
                }

                if (bitmap != null)
                {
                    _atlasCanvas!.LoadedImage = bitmap;
                    _atlasCanvas!.InvalidateVisual();
                }
            }
        }

        private async Task SaveJsonAsync()
        {
#pragma warning disable CS0618
            var dialog = new SaveFileDialog();
            dialog.Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "JSON", Extensions = new List<string> { "json" } }
            };
            dialog.InitialFileName = "Rect.json";
#pragma warning restore CS0618
            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_atlasCanvas!.Regions, options);
                File.WriteAllText(result, json);
                await MessageBox.Show(this, "Saved to JSON file.", "Save");
            }
        }

        private async Task LoadJsonAsync()
        {
#pragma warning disable CS0618
            var dialog = new OpenFileDialog();
            dialog.Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "JSON", Extensions = new List<string> { "json" } }
            };
#pragma warning restore CS0618
            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                try
                {
                    string json = File.ReadAllText(result[0]);
                    var regions = JsonSerializer.Deserialize<List<RegionDefinition>>(json);
                    _atlasCanvas!.Regions = regions ?? new List<RegionDefinition>();
                    _atlasCanvas!.InvalidateVisual();
                    await MessageBox.Show(this, "Loaded regions from JSON.", "Load");
                }
                catch (System.Exception ex)
                {
                    await MessageBox.Show(this, "JSON read error: " + ex.Message, "Error");
                }
            }
        }

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                _atlasCanvas!.DeleteSelectedRegion();
            }
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}