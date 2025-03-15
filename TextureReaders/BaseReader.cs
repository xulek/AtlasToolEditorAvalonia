using System.IO;
using System.Threading.Tasks;

namespace AtlasToolEditorAvalonia.TextureReaders
{
    public abstract class BaseReader<T>
    {
        public async Task<T> Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}", path);

            var buffer = await File.ReadAllBytesAsync(path);

            return Read(buffer);
        }

        protected abstract T Read(byte[] buffer);
    }
}
