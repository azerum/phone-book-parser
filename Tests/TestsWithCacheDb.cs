using System;
using System.IO;
using Library.Caching;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestsWithCacheDb
    {
        private const string testDbPath = "cache.db";
        protected CacheDb db;

        [SetUp]
        public void CreateNewDb()
        {
            //Truncate the old DB file if it exists
            if (File.Exists(testDbPath))
            {
                File.WriteAllBytes(testDbPath, Array.Empty<byte>());
            }

            db = CacheDb.Open($"Data Source={testDbPath}");
            db.EnsureAllTablesAreCreated();
        }

        [TearDown]
        public void CloseDb()
        {
            db.Close();
        }
    }
}
