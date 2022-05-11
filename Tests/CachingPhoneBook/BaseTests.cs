using System;
using System.IO;
using Library;
using Library.Caching;
using NUnit.Framework;

namespace Tests.CachingPhoneBook
{
    [TestFixture]
    public abstract class BaseTests
    {
        private const string dbPath = "cache.db";
        private static readonly string connectionString = $"Data Source={dbPath}";

        protected CachingSqlitePhoneBook cachingBook;

        [SetUp]
        public void CreateNewCachingBook()
        {
            if (File.Exists(dbPath))
            {
                File.WriteAllBytes(dbPath, Array.Empty<byte>());
            }

            cachingBook = CachingSqlitePhoneBook.Open(
                GetInnerForNextTest(),
                connectionString
            );
        }

        protected abstract IPhoneBook GetInnerForNextTest();
    }
}
