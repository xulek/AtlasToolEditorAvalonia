using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    public class DisplayedFile
    {
        public string FullPath { get; set; } = "";
        public string FileName { get; set; } = "";
        public override string ToString() => FileName;
    }

    public partial class MainWindow : Window
    {
        private Button? _loadImageButton, _saveJsonButton, _loadJsonButton,
                       _clearButton, _zoomInButton, _zoomOutButton, _arrangeButton,
                       _openFolderButton;    // New button

        private ListBox? _texturesListBox;   // List of files from the folder

        private AtlasCanvas? _atlasCanvas;
        private Bitmap? _loadedImage;
        private List<RegionDefinition> _regions = new List<RegionDefinition>();

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            // Initialization of existing buttons and the Canvas control:
            _loadImageButton = this.FindControl<Button>("LoadImageButton");
            _saveJsonButton = this.FindControl<Button>("SaveJsonButton");
            _loadJsonButton = this.FindControl<Button>("LoadJsonButton");
            _clearButton = this.FindControl<Button>("ClearButton");
            _zoomInButton = this.FindControl<Button>("ZoomInButton");
            _zoomOutButton = this.FindControl<Button>("ZoomOutButton");
            _arrangeButton = this.FindControl<Button>("ArrangeButton");
            _atlasCanvas = this.FindControl<AtlasCanvas>("AtlasCanvasControl");

            // New controls:
            _openFolderButton = this.FindControl<Button>("OpenFolderButton");
            _texturesListBox = this.FindControl<ListBox>("TexturesListBox");

            // Event binding:
            _loadImageButton.Click += async (_, __) => await LoadImageAsync();
            _saveJsonButton.Click += async (_, __) => await SaveJsonAsync();
            _loadJsonButton.Click += async (_, __) => await LoadJsonAsync();

            _clearButton.Click += (_, __) =>
            {
                _atlasCanvas!.Regions.Clear();
                _atlasCanvas!.InvalidateVisual();
            };
            _zoomInButton.Click += (_, __) =>
            {
                _atlasCanvas!.Zoom *= 1.1;
                _atlasCanvas!.InvalidateVisual();
            };
            _zoomOutButton.Click += (_, __) =>
            {
                _atlasCanvas!.Zoom *= 0.9;
                _atlasCanvas!.InvalidateVisual();
            };
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

            // NEW: Handling the click of the open folder button
            _openFolderButton.Click += async (_, __) => await OpenFolderAsync();

            // NEW: Handling double-click in the file list
            _texturesListBox.DoubleTapped += TexturesListBox_DoubleTapped;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        // Method opening a dialog box and collecting a list of files in the selected folder
        private async Task OpenFolderAsync()
        {
            var folderDialog = new OpenFolderDialog();
            var selectedFolder = await folderDialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".ozj", ".ozt", ".ozb", ".ozd", ".ozp" };

                // We get all files with the selected extension.
                var files = Directory.GetFiles(selectedFolder, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(f => supportedExtensions.Contains(
                                             Path.GetExtension(f).ToLowerInvariant()))
                                     .ToList();

                // We create a collection of DisplayedFile objects to display the short name
                var displayedFiles = files.Select(path => new DisplayedFile
                {
                    FullPath = path,
                    FileName = Path.GetFileName(path) // only the name with the extension
                })
                .ToList();

                // We set ItemsSource to the DisplayedFile object collection
                if (_texturesListBox != null)
                {
                    _texturesListBox.ItemsSource = displayedFiles;
                }
            }
        }

        private async void TexturesListBox_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (_texturesListBox?.SelectedItem is DisplayedFile selectedFile)
            {
                // We use the full path to load the image
                var loader = new CustomImageLoader();
                var bitmap = await loader.InitAsync(selectedFile.FullPath);

                if (_atlasCanvas != null && bitmap != null)
                {
                    _atlasCanvas.LoadedImage = bitmap;
                    _atlasCanvas.InvalidateVisual();
                }
            }
        }

        private async Task LoadImageAsync()
        {
            // Disable obsolete warning for OpenFileDialog
#pragma warning disable CS0618
            var dialog = new OpenFileDialog();
            dialog.Filters = new List<FileDialogFilter>
    {
        new FileDialogFilter { Name = "Images", Extensions = new List<string> { "png", "jpg", "jpeg", "bmp" } }
    };
#pragma warning restore CS0618

            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                Bitmap bitmap;
                // Create the bitmap outside the using block to ensure the stream is disposed after bitmap is fully loaded
                using (var stream = File.OpenRead(result[0]))
                {
                    // Create bitmap from stream
                    bitmap = new Bitmap(stream);
                }
                // Set the loaded image and force redraw of the AtlasCanvas
                _atlasCanvas!.LoadedImage = bitmap;
                _atlasCanvas!.InvalidateVisual();
                _atlasCanvas!.Regions.Clear();
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
    }
}