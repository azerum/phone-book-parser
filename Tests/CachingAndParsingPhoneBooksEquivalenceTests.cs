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
    public class CachingAndParsingPhoneBooksEquivalenceTests
    {
        private const string dbPath = "cache.db";

        private readonly ParsingSitePhoneBook parsingBook = new();
        private CachingSqlitePhoneBook cachingBook;

        [OneTimeSetUp]
        public void CreateDb()
        {
            if (File.Exists(dbPath))
            {
                File.WriteAllBytes(dbPath, Array.Empty<byte>());
            }

            cachingBook = CachingSqlitePhoneBook.Open(
                parsingBook,
                $"Data Source={dbPath}"
            );
        }

        [Test]
        public void GetAllRegions_ReturnSameResult()
        {
            AssertMethodResultsAreEquivalent(
                cachingBook,
                parsingBook,
                phoneBook => phoneBook.GetAllRegions()
            );
        }

        [Test]
        public async Task GetAllProvincesInRegion_ReturnSameResults()
        {
            Region region = await GetFirstRegionAsync();

            AssertMethodResultsAreEquivalent(
                cachingBook,
                parsingBook,
                phoneBook => phoneBook.GetAllProvincesInRegion(region)
            );
        }

        [Test]
        public async Task GetAllCitiesInProvince_ReturnSameResults()
        {
            Province province = await GetFirstProvinceAsync();

            AssertMethodResultsAreEquivalent(
                cachingBook,
                parsingBook,
                phoneBook => phoneBook.GetAllCitiesInProvince(province)
            );
        }

        private readonly SearchCriteria criteria = new("Иванов");

        [Test]
        public async Task SearchInRegion_ReturnSameResult()
        {
            Region region = await GetFirstRegionAsync();

            AssertMethodResultsAreEquivalent(
                cachingBook,
                parsingBook,
                phoneBook => phoneBook.SearchInRegion(region, criteria)
            );
        }

        [Test]
        public async Task SearchInProvince_ReturnSameResult()
        {
            Province province = await GetFirstProvinceAsync();

            AssertMethodResultsAreEquivalent(
                cachingBook,
                parsingBook,
                phoneBook => phoneBook.SearchInProvince(province, criteria)
            );
        }

        [Test]
        public async Task SearchInCity_ReturnSameResult()
        {
            City city = await GetFirstCityAsync();

            AssertMethodResultsAreEquivalent(
                cachingBook,
                parsingBook,
                phoneBook => phoneBook.SearchInCity(city, criteria)
            );
        }

        private ValueTask<Region> GetFirstRegionAsync()
        {
            return parsingBook.GetAllRegions().FirstAsync();
        }

        private async ValueTask<Province> GetFirstProvinceAsync()
        {
            Region region = await GetFirstRegionAsync();

            return await parsingBook
                .GetAllProvincesInRegion(region)
                .FirstAsync();
        }

        private async ValueTask<City> GetFirstCityAsync()
        {
            Province province = await GetFirstProvinceAsync();

            return await parsingBook
                .GetAllCitiesInProvince(province)
                .FirstAsync();
        }

        private static void AssertMethodResultsAreEquivalent<T>(
            IPhoneBook a,
            IPhoneBook b,
            Func<IPhoneBook, IAsyncEnumerable<T>> method
        )
        {
            var fromA = method(a).ToEnumerable();
            var fromB = method(b).ToEnumerable();

            Assert.That(fromA, Is.EquivalentTo(fromB));
        }
    }
}
