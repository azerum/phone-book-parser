using System;
using System.IO;

namespace Tests
{
    public static class FileExtensions
    {
        public static void TruncateIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.WriteAllBytes(path, Array.Empty<byte>());
            }
        }
    }
}
