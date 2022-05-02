using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class CachingAndParsingPhoneBooksEquivalenceTests
        : TestsWithCachingSqlitePhoneBook
    {
        [Test]
        public void GetAllRegions_ReturnSameResults()
        {
            AssertCachingMethodResultIsSameAsParsing(
                phoneBook => phoneBook.GetAllRegions()
            );
        }

        [Test]
        public async Task GetAllProvincesInRegion_ReturnSameResults()
        {
            Region region = await GetFirstRegionAsync();

            AssertCachingMethodResultIsSameAsParsing(
                phoneBook => phoneBook.GetAllProvincesInRegion(region)
            );
        }

        [Test]
        public async Task GetAllCitiesInProvince_ReturnSameResults()
        {
            Province province = await GetFirstProvinceAsync();

            AssertCachingMethodResultIsSameAsParsing(
                phoneBook => phoneBook.GetAllCitiesInProvince(province)
            );
        }

        private readonly SearchCriteria criteria = new("Иванов");

        [Test]
        public async Task SearchInRegion_ReturnSameResults()
        {
            Region region = await GetFirstRegionAsync();

            AssertCachingMethodResultIsSameAsParsing(
                phoneBook => phoneBook.SearchInRegion(region, criteria)
            );
        }

        [Test]
        public async Task SearchInProvince_ReturnSameResults()
        {
            Province province = await GetFirstProvinceAsync();

            AssertCachingMethodResultIsSameAsParsing(
                phoneBook => phoneBook.SearchInProvince(province, criteria)
            );
        }

        [Test]
        public async Task SearchInCity_ReturnSameResults()
        {
            City city = await GetFirstCityAsync();

            AssertCachingMethodResultIsSameAsParsing(
                phoneBook => phoneBook.SearchInCity(city, criteria)
            );
        }

        private void AssertCachingMethodResultIsSameAsParsing<T>(
            Func<IPhoneBook, IAsyncEnumerable<T>> method
        )
        {
            var fromCaching = method(cachingBook).ToEnumerable();
            var fromParsing = method(parsingBook).ToEnumerable();

            Assert.That(fromCaching, Is.EquivalentTo(fromParsing));
        }
    }
}
