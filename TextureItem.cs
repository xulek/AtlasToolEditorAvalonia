using Avalonia;
using Avalonia.Media.Imaging;

namespace AtlasToolEditorAvalonia
{
    // Represents a texture region with a Z value.
    public class TextureItem
    {
        public string Name { get; set; } = "";
        public Bitmap? Image { get; set; }
        public Rect Bounds { get; set; }
        public int Z { get; set; } = 0;
        public override string ToString() => $"{Name} (Z: {Z})";
    }
}
