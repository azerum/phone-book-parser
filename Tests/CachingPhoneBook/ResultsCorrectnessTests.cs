using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library;
using NUnit.Framework;

namespace Tests.CachingPhoneBook
{
    public class ResultsCorrectnessTests : BaseTests
    {
        class FakePhoneBook : IPhoneBook
        {
            private readonly List<Region> regions;
            private readonly List<Province> provinces;

            private readonly List<City> cities;
            private readonly List<City> searchedCities;

            public IEnumerable<City> Cities => cities;
            public IEnumerable<City> SearchedCities => searchedCities;

            public bool SearchInAllWasCalled { get; private set; } = false;
            public bool SearchInRegionWasCalled { get; private set; } = false;
            public bool SearchInProvinceWasCalled { get; private set; } = false;

            public FakePhoneBook()
            {
                regions = CreateRegions();
                provinces = CreateProvinces(regions);

                cities = CreateCities(provinces);
                searchedCities = new();
            }

            private static List<Region> CreateRegions()
            {
                List<Region> regions = new(4);

                for (int i = 0; i < regions.Capacity; ++i)
                {
                    regions.Add(new($"https://region/{i}", $"Region {i}"));
                }

                return regions;
            }

            private static List<Province> CreateProvinces(List<Region> regions)
            {
                List<Province> provinces = new();
                int provinceNo = 0;

                for (int i = 0; i < regions.Count; ++i)
                {
                    Region r = regions[i];

                    for (int j = 0; j < i + 1; ++j)
                    {
                        provinces.Add(new(
                            r,
                            $"{r.Url}/province/{provinceNo}",
                            $"Province {provinceNo}"
                        ));

                        ++provinceNo;
                    }
                }

                return provinces;
            }

            private static List<City> CreateCities(List<Province> provinces)
            {
                List<City> cities = new();
                int cityNo = 0;

                for (int i = 0; i < provinces.Count; ++i)
                {
                    Province p = provinces[i];

                    for (int j = 0; j < i + 1; ++j)
                    {
                        cities.Add(new(
                            p,
                            $"{p.Url}/city/{cityNo}",
                            $"City {cityNo}"
                        ));

                        ++cityNo;
                    }
                }

                return cities;
            }

            public IAsyncEnumerable<Region> GetAllRegions(
                CancellationToken cancellationToken = default
            )
            {
                return regions.ToAsyncEnumerable();
            }

            public IAsyncEnumerable<Province> GetAllProvincesInRegion(
                Region region,
                CancellationToken cancellationToken = default
            )
            {
                return provinces
                    .Where(p => p.Region.Equals(region))
                    .ToAsyncEnumerable();
            }

            public IAsyncEnumerable<City> GetAllCitiesInProvince(
                Province province,
                CancellationToken cancellationToken = default
            )
            {
                return cities
                    .Where(c => c.Province.Equals(province))
                    .ToAsyncEnumerable();
            }

            public IAsyncEnumerable<FoundRecord> SearchInAll(
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                SearchInAllWasCalled = true;
                return AsyncEnumerable.Empty<FoundRecord>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInRegion(
                Region region,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                SearchInRegionWasCalled = true;
                return AsyncEnumerable.Empty<FoundRecord>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInProvince(
                Province province,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                SearchInProvinceWasCalled = true;
                return AsyncEnumerable.Empty<FoundRecord>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInCity(
                City city,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                //There might be duplicated in the list if
                //CachingSqlitePhoneBook searches in the same city
                //twice. This would be considered a bad implementation
                //and tests will fail, as tests will compare SearchedCities
                //to Cities
                searchedCities.Add(city);

                return AsyncEnumerable.Empty<FoundRecord>();
            }
        }

        private FakePhoneBook fakePhoneBook;
        private readonly SearchCriteria criteria = new("Иванов");

        protected override IPhoneBook GetInnerForNextTest()
        {
            fakePhoneBook = new();
            return fakePhoneBook;
        }

        [Test]
        public void GetAllRegions_SimpleCall()
        {
            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.GetAllRegions()
            );
        }

        [Test]
        public async Task GetAllRegions_AfterGetAllProvincesInRegion()
        {
            Region region = await GetFirstRegionFromInnerAsync();
            await cachingBook.GetAllProvincesInRegion(region).Consume();

            GetAllRegions_SimpleCall();
        }

        [Test]
        public async Task GetAllRegions_AfterSearchInRegion()
        {
            Region region = await GetFirstRegionFromInnerAsync();
            await cachingBook.SearchInRegion(region, criteria).Consume();

            GetAllRegions_SimpleCall();
        }

        [Test]
        public async Task GetAllProvincesInRegion_SimpleCall()
        {
            Region region = await GetFirstRegionFromInnerAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.GetAllProvincesInRegion(region)
            );
        }

        [Test]
        public async Task GetAllProvincesInRegion_AfterGetAllCitiesInProvince()
        {
            Province province = await GetFirstProvinceFromInnerAsync();
            await cachingBook.GetAllCitiesInProvince(province).Consume();

            await GetAllProvincesInRegion_SimpleCall();
        }

        [Test]
        public async Task GetAllProvincesInRegion_AfterSearchInProvince()
        {
            Province province = await GetFirstProvinceFromInnerAsync();
            await cachingBook.SearchInProvince(province, criteria).Consume();

            await GetAllProvincesInRegion_SimpleCall();
        }

        [Test]
        public async Task GetAllCitiesInProvince_SimpleCall()
        {
            Province province = await GetFirstProvinceFromInnerAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.GetAllCitiesInProvince(province)
            );
        }

        [Test]
        public async Task GetAllCitiesInProvince_AfterSearchInCity()
        {
            City city = await GetFirstCityFromInnerAsync();
            await cachingBook.SearchInCity(city, criteria).Consume();

            await GetAllCitiesInProvince_SimpleCall();
        }

        [Test]
        public void SearchInAll_ReturnsSameResultAsInner()
        {
            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.SearchInAll(criteria)
            );
        }

        [Test]
        public async Task SearchInAll_DoesNotCallInner()
        {
            await cachingBook.SearchInAll(criteria).Consume();
            Assert.IsFalse(fakePhoneBook.SearchInAllWasCalled);
        }

        [Test]
        public async Task SearchInAll_CallsSearchInCityWithEachCity()
        {
            await cachingBook.SearchInAll(criteria).Consume();

            Assert.That(
                fakePhoneBook.SearchedCities,
                Is.EquivalentTo(fakePhoneBook.Cities)
            );
        }

        [Test]
        public async Task SearchInRegion_ReturnsSameResultAsInner()
        {
            Region region = await GetFirstRegionFromInnerAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.SearchInRegion(region, criteria)
            );
        }

        [Test]
        public async Task SearchInRegion_DoesNotCallInner()
        {
            Region region = await GetFirstRegionFromInnerAsync();
            await cachingBook.SearchInRegion(region, criteria).Consume();

            Assert.IsFalse(fakePhoneBook.SearchInRegionWasCalled);
        }

        [Test]
        public async Task SearchInRegion_CallsSearchInCityWithEachCityInRegion()
        {
            Region region = await GetFirstRegionFromInnerAsync();
            await cachingBook.SearchInRegion(region, criteria).Consume();

            var citiesInRegion = fakePhoneBook.Cities
                .Where(c => c.Province.Region.Equals(region));

            Assert.That(
                fakePhoneBook.SearchedCities,
                Is.EquivalentTo(citiesInRegion)
            );
        }

        [Test]
        public async Task SearchInProvince_ReturnsSameResultsAsInner()
        {
            Province province = await GetFirstProvinceFromInnerAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.SearchInProvince(province, criteria)
            );
        }

        [Test]
        public async Task SearchInProvince_DoesNotCallInner()
        {
            Province province = await GetFirstProvinceFromInnerAsync();
            await cachingBook.SearchInProvince(province, criteria).Consume();

            Assert.IsFalse(fakePhoneBook.SearchInProvinceWasCalled);
        }

        [Test]
        public async Task SearchInProvince_CallsSearchInCityWithEachCityInProvince()
        {
            Province province = await GetFirstProvinceFromInnerAsync();
            await cachingBook.SearchInProvince(province, criteria).Consume();

            var citiesInProvince = fakePhoneBook.Cities
                .Where(c => c.Province.Equals(province));

            Assert.That(
                fakePhoneBook.SearchedCities,
                Is.EquivalentTo(citiesInProvince)
            );
        }

        [Test]
        public async Task SearchInCity_ReturnsSameResultAsInner()
        {
            City city = await GetFirstCityFromInnerAsync();

            AssertMethodReturnsSameResultsAsInner(
                phoneBook => phoneBook.SearchInCity(city, criteria)
            );
        }

        private void AssertMethodReturnsSameResultsAsInner<T>(
            Func<IPhoneBook, IAsyncEnumerable<T>> method
        )
        {
            var fromInner = method(fakePhoneBook).ToEnumerable();

            //First method call is supposed to call the method of inner
            //and cache Regions, Provinces and Cities. Second method
            //call should fetch them from cache. We need to test that
            //both calls return correct results

            var fromFirstCall = method(cachingBook).ToEnumerable();
            var fromSecondCall = method(cachingBook).ToEnumerable();

            Assert.That(fromFirstCall, Is.EquivalentTo(fromInner));
            Assert.That(fromSecondCall, Is.EquivalentTo(fromInner));
        }

        private ValueTask<Region> GetFirstRegionFromInnerAsync()
        {
            return fakePhoneBook.GetAllRegions().FirstAsync();
        }

        private async ValueTask<Province> GetFirstProvinceFromInnerAsync()
        {
            Region region = await GetFirstRegionFromInnerAsync();
            return await fakePhoneBook.GetAllProvincesInRegion(region).FirstAsync();
        }

        private async ValueTask<City> GetFirstCityFromInnerAsync()
        {
            Province province = await GetFirstProvinceFromInnerAsync();
            return await fakePhoneBook.GetAllCitiesInProvince(province).FirstAsync();
        }
    }
}
