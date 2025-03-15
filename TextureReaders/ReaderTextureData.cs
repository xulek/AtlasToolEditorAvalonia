namespace AtlasToolEditorAvalonia.TextureReaders
{
    public class ReaderTextureData
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public byte Components { get; set; }
        public byte[] Data { get; set; } = new byte[0];
    }
}
