using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Library;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace Tests
{
    public class CachingTests : TestsWithCachingSqlitePhoneBook
    {
        private SqliteConnection connection;

        [SetUp]
        public void OpenConnection()
        {
            connection = new(connectionString);
            connection.Open();
        }

        [TearDown]
        public void CloseConnection()
        {
            connection.Close();
            connection.Dispose();
        }

        [Test]
        public async Task GetAllRegions_CachesResults()
        {
            var returnedRegions =
                await cachingBook.GetAllRegions().ToEnumerableAsync();

            string sql =
            @"
            SELECT Url, DisplayName
            FROM Regions
            ";

            var dynamics = await connection.QueryAsync(sql);

            var cachedRegions =
                dynamics.Select(d => new Region(d.Url, d.DisplayName));

            Assert.That(cachedRegions, Is.EquivalentTo(returnedRegions));
        }

        [Test]
        public async Task GetAllProvincesInRegion_CachesResults()
        {
            Region region = await GetFirstRegionAsync();

            var returnedProvinces = await cachingBook
                .GetAllProvincesInRegion(region)
                .ToEnumerableAsync();

            string sql =
            @"
            SELECT
                p.Url as PUrl,
                p.DisplayName as PDisplayName,
                r.Url as RUrl,
                r.DisplayName as RDisplayName
            FROM
                Provinces as p
            INNER JOIN
                Regions as r
            ON
                p.RegionId = r.Id
            ";

            var dynamics = await connection.QueryAsync(sql);

            var cachedRegions = dynamics.Select(d =>
            {
                Region r = new(d.RUrl, d.RDisplayName);
                Province p = new(r, d.PUrl, d.PDisplayName);

                return p;
            });

            Assert.That(cachedRegions, Is.EquivalentTo(returnedProvinces));
        }
    }
}
