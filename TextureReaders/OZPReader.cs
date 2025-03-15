using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;

namespace AtlasToolEditorAvalonia.TextureReaders
{
    public class OZPReader : BaseReader<ReaderTextureData>
    {
        public const int MAX_WIDTH = 1024;
        public const int MAX_HEIGHT = 1024;

        protected override ReaderTextureData Read(byte[] buffer)
        {
            if (buffer[0] == 137 && buffer[1] == 'P' && buffer[2] == 'N' && buffer[3] == 'G')
                return this.ReadPNG(buffer[4..]);

            throw new ApplicationException($"Invalid file format");
        }

        private ReaderTextureData ReadPNG(byte[] buffer)
        {
            using var image = Image.Load<Rgba32>(buffer);

            int width = image.Width;
            int height = image.Height;

            var data = new byte[width * height * 4];
            image.CopyPixelDataTo(data);

            return new ReaderTextureData
            {
                Width = width,
                Height = height,
                Components = 4,
                Data = data
            };
        }
    }
}
