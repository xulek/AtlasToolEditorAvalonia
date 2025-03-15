using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AtlasToolEditorAvalonia.TextureReaders;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AtlasToolEditorAvalonia
{
    public class CustomImageLoader
    {
        public async Task<WriteableBitmap> InitAsync(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            ReaderTextureData textureData;

            switch (ext)
            {
                case ".ozj":
                    {
                        var reader = new OZJReader();
                        textureData = await reader.Load(filePath);
                    }
                    break;
                case ".ozt":
                    {
                        var reader = new OZTReader();
                        textureData = await reader.Load(filePath);
                    }
                    break;
                case ".ozb":
                    {
                        var reader = new OZBReader();
                        var texture = await reader.Load(filePath);
                        textureData = new ReaderTextureData
                        {
                            Width = texture.Width,
                            Height = texture.Height,
                            Components = 4,
                            Data = texture.Data.SelectMany(x => new byte[] { x.R, x.G, x.B, x.A }).ToArray()
                        };
                    }
                    break;
                case ".ozd":
                    {
                        var reader = new OZDReader();
                        textureData = await reader.Load(filePath);
                    }
                    break;
                case ".ozp":
                    {
                        var reader = new OZPReader();
                        textureData = await reader.Load(filePath);
                    }
                    break;
                default:
                    throw new NotImplementedException($"Extension {ext} not supported");
            }

            return ConvertToWriteableBitmap(textureData);
        }

        private WriteableBitmap ConvertToWriteableBitmap(ReaderTextureData textureData)
        {
            int width = (int)textureData.Width;
            int height = (int)textureData.Height;
            var pixelSize = new PixelSize(width, height);
            var dpi = new Vector(96, 96);
            var wb = new WriteableBitmap(pixelSize, dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);

            using (var fb = wb.Lock())
            {
                int rowBytes = fb.RowBytes;
                int totalBytes = rowBytes * height;
                
                byte[] buffer = new byte[totalBytes];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcIndex = (y * width + x) * textureData.Components;
                        byte r = textureData.Data[srcIndex];
                        byte g = textureData.Data[srcIndex + 1];
                        byte b = textureData.Data[srcIndex + 2];
                        
                        byte a = textureData.Components == 4 ? textureData.Data[srcIndex + 3] : (byte)255;

                        int destIndex = y * rowBytes + x * 4;
                        
                        buffer[destIndex] = b;         // Blue
                        buffer[destIndex + 1] = g;     // Green
                        buffer[destIndex + 2] = r;     // Red
                        buffer[destIndex + 3] = a;     // Alpha
                    }
                }
                
                Marshal.Copy(buffer, 0, fb.Address, totalBytes);
            }

            return wb;
        }
    }
}
