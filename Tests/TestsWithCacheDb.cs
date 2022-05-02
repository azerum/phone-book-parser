using System.Collections.Generic;
using System.IO;
using Library;
using Library.Caching;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestsWithCacheDb
    {
        private const string dbPath = "cache.db";
        protected CacheDb db;

        [SetUp]
        public void CreateDb()
        {
            FileExtensions.TruncateIfExists(dbPath);

            db = CacheDb.Open($"Data Source={dbPath}");
            db.EnsureAllTablesAreCreated();
        }

        [TearDown]
        public void CloseDb()
        {
            db.Close();
        }

        public static (Region, IEnumerable<Province>, IEnumerable<City>)
            CreateRegionWithProvincesAndCities(
                string regionName,
                int provincesCount,
                int citiesCountPerProvince
            )
        {
            Region r = new($"https://{regionName}", regionName);

            List<Province> provinces = new(provincesCount);
            List<City> cities = new(provincesCount * citiesCountPerProvince);

            for (int i = 0; i < provincesCount; ++i)
            {
                Province p = new(
                    r,
                    $"{r.Url}/province/{i}",
                    $"{r.DisplayName}_Province_{i}"
                );

                provinces.Add(p);

                for (int j = 0; j < citiesCountPerProvince; ++j)
                {
                    City c = new(
                        p,
                        $"{p.Url}/city/{j}",
                        $"{p.DisplayName}_City_{j}"
                    );

                    cities.Add(c);
                }
            }

            return (r, provinces, cities);
        }
    }
}
