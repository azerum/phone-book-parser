using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library;
using Library.Parsing;
using NUnit.Framework;

namespace Tests.CachingPhoneBook
{
    public class ResultsAreSameAsParsingPhoneBook : BaseTests
    {
        private readonly ParsingSitePhoneBook parsingBook = new();
        protected override IPhoneBook Inner => parsingBook;

        [Test]
        public void GetAllRegions()
        {
            AssertCachingAndParsingMethodsReturnSameResults(
                phoneBook => phoneBook.GetAllRegions()
            );
        }

        [Test]
        public async Task GetAllProvincesInRegion()
        {
            Region region = await GetFirstRegionAsync();

            AssertCachingAndParsingMethodsReturnSameResults(
                phoneBook => phoneBook.GetAllProvincesInRegion(region)
            );
        }

        [Test]
        public async Task GetAllCitiesInProvince()
        {
            Province province = await GetFirstProvinceAsync();

            AssertCachingAndParsingMethodsReturnSameResults(
                phoneBook => phoneBook.GetAllCitiesInProvince(province)
            );
        }

        private readonly SearchCriteria criteria = new("Иванов");

        [Test]
        [Ignore(
            "This test takes a lot of time to run. " +
            "Comment out this attribute if you really need to run it"
        )]
        //2022-05-10 15:56 UTC+3: Test passed
        public void SearchInAll()
        {
            AssertCachingAndParsingMethodsReturnSameResults(
                phoneBook => phoneBook.SearchInAll(criteria)
            );
        }

        [Test]
        public async Task SearchInRegion()
        {
            Region region = await GetFirstRegionAsync();

            AssertCachingAndParsingMethodsReturnSameResults(
                phoneBook => phoneBook.SearchInRegion(region, criteria)
            );
        }

        [Test]
        public async Task SearchInProvince()
        {
            Province province = await GetFirstProvinceAsync();

            AssertCachingAndParsingMethodsReturnSameResults(
                phoneBook => phoneBook.SearchInProvince(province, criteria)
            );
        }

        [Test]
        public async Task SearchInCity()
        {
            City city = await GetFirstCityAsync();

            AssertCachingAndParsingMethodsReturnSameResults(
                phoneBook => phoneBook.SearchInCity(city, criteria)
            );
        }

        private void AssertCachingAndParsingMethodsReturnSameResults<T>(
            Func<IPhoneBook, IAsyncEnumerable<T>> method
        )
        {
            var fromParsing = method(parsingBook).ToEnumerable();

            //Run the method twice on cachingBook to test
            //it before (first call) and after (second call) caching

            var fromCaching1 = method(cachingBook).ToEnumerable();
            var fromCaching2 = method(cachingBook).ToEnumerable();

            Assert.That(fromCaching1, Is.EquivalentTo(fromParsing));
            Assert.That(fromCaching2, Is.EquivalentTo(fromParsing));
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
    }
}
