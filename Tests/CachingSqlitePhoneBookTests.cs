using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Library;
using Library.Caching;
using Library.Parsing;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class CachingSqlitePhoneBookTests
    {
        private const string dbPath = "cache.db";

        private static readonly string connectionString =
            $"Data Source={dbPath}";

        private readonly ParsingSitePhoneBook inner = new();
        protected CachingSqlitePhoneBook cachingBook;

        [OneTimeSetUp]
        public void CreateCachingPhoneBook()
        {
            //Truncate the DB file if it exists
            if (File.Exists(dbPath))
            {
                File.WriteAllBytes(dbPath, Array.Empty<byte>());
            }

            cachingBook = CachingSqlitePhoneBook.Open(
                inner,
                connectionString
            );
        }

        [Test]
        public void GetAllRegions_ReturnsSameResultsAsInner()
        {
            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.GetAllRegions()
            );
        }

        [Test]
        public async Task GetAllProvincesInRegion_ReturnsSameResultsAsInner()
        {
            Region region = await GetFirstRegionAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.GetAllProvincesInRegion(region)
            );
        }

        [Test]
        public async Task GetAllCitiesInProvince_ReturnsSameResultsAsInner()
        {
            Province province = await GetFirstProvinceAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.GetAllCitiesInProvince(province)
            );
        }

        private readonly SearchCriteria criteria = new("Иванов");

        [Test]
        public async Task SearchInRegion_ReturnsSameResultsAsInner()
        {
            Region region = await GetFirstRegionAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.SearchInRegion(region, criteria)
            );
        }

        [Test]
        public async Task SearchInProvince_ReturnsSameResultsAsInner()
        {
            Province province = await GetFirstProvinceAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.SearchInProvince(province, criteria)
            );
        }

        [Test]
        public async Task SearchInCity_ReturnsSameResultsAsInner()
        {
            City city = await GetFirstCityAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.SearchInCity(city, criteria)
            );
        }

        private void AssertMethodReturnsSameResultsAsInner<T>(
            Func<IPhoneBook, IAsyncEnumerable<T>> method
        )
        {
            var fromCaching = method(cachingBook).ToEnumerable();
            var fromInner = method(inner).ToEnumerable();

            Assert.That(fromCaching, Is.EquivalentTo(fromInner));
        }

        private ValueTask<Region> GetFirstRegionAsync()
        {
            return inner.GetAllRegions().FirstAsync();
        }

        private async ValueTask<Province> GetFirstProvinceAsync()
        {
            Region region = await GetFirstRegionAsync();

            return await inner
                .GetAllProvincesInRegion(region)
                .FirstAsync();
        }

        private async ValueTask<City> GetFirstCityAsync()
        {
            Province province = await GetFirstProvinceAsync();

            return await inner
                .GetAllCitiesInProvince(province)
                .FirstAsync();
        }
    }
}
