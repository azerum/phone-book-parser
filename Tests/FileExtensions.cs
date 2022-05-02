using System;
using System.IO;

namespace Tests
{
    public static class FileExtensions
    {
        public static void Truncate(string path)
        {
            if (File.Exists(path))
            {
                File.WriteAllBytes(path, Array.Empty<byte>());
            }
        }
    }
}
