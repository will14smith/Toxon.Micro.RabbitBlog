using System;
using System.IO;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool
{
    public class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory(string rootDirectory, string hint = null)
        {
            Path = System.IO.Path.Combine(rootDirectory, $"{hint}-{DateTime.UtcNow:yyyyMMMMddHHmmss}");
            Directory.CreateDirectory(Path);
        }

        public override string ToString()
        {
            return Path;
        }

        public void Dispose()
        {
            // TODO Directory.Delete(Path, true);
        }
    }
}
