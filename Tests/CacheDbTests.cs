using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Library;
using Library.Caching;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Tests
{
    [TestFixture]
    public class CacheDbTests
    {
        private const string testDbPath = "cache.db";

        private CacheDb db;

        [SetUp]
        public void CreateNewDb()
        {
            //Truncate the old DB file if it exists
            if (File.Exists(testDbPath))
            {
                File.WriteAllBytes(testDbPath, Array.Empty<byte>());
            }

            SqliteConnection connection = new($"Data Source={testDbPath}");
            connection.Open();

            db = new(connection);
            db.EnsureAllTablesAreCreated();
        }

        [TearDown]
        public void CloseDbConnection()
        {
            db.Connection.Close();
            db = null;
        }

        [Test]
        public async Task ProvincesTableSelectAllInRegion_Works()
        {
            Region region = new("https://example.com", "Example");

            List<Province> expectedProvinces = new(2);

            for (int i = 0; i < expectedProvinces.Capacity; ++i)
            {
                Province p = new(region, $"https://example.com/{i}", $"Province{i}");
                expectedProvinces.Add(p);
            }

            await db.Provinces.InsertMany(expectedProvinces);

            var provinces = await db.Provinces.SelectAllInRegion(region);

            Assert.That(
                provinces,
                Is.EquivalentTo(expectedProvinces).Using<Province>(
                    (a, b)
                    => a.Url == b.Url
                    && a.DisplayName == b.DisplayName
                    && a.Region == b.Region
                )
            );
        }
    }
}
