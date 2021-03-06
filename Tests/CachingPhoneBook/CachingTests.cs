using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.CachingPhoneBook
{
    public class CachingTests : TestsBase
    {
        class FakePhoneBook : IPhoneBook
        {
            //Shorter name to use in the class code
            private readonly Dictionary<string, int> callsCount;

            public Dictionary<string, int> CallsCountExceptSearchInCity => callsCount;

            public FakePhoneBook()
            {
                callsCount = new();

                callsCount.Add(nameof(this.GetAllRegions), 0);
                callsCount.Add(nameof(this.GetAllProvincesInRegion), 0);
                callsCount.Add(nameof(this.GetAllCitiesInProvince), 0);

                callsCount.Add(nameof(this.SearchInAll), 0);
                callsCount.Add(nameof(this.SearchInRegion), 0);
                callsCount.Add(nameof(this.SearchInProvince), 0);
            }

            public IAsyncEnumerable<Region> GetAllRegions(
                CancellationToken cancellationToken = default
            )
            {
                ++callsCount[nameof(this.GetAllRegions)];
                return AsyncEnumerable.Empty<Region>();
            }

            public IAsyncEnumerable<Province> GetAllProvincesInRegion(
                Region region,
                CancellationToken cancellationToken = default
            )
            {
                ++callsCount[nameof(this.GetAllProvincesInRegion)];
                return AsyncEnumerable.Empty<Province>();
            }

            public IAsyncEnumerable<City> GetAllCitiesInProvince(
                Province province,
                CancellationToken cancellationToken = default
            )
            {
                ++callsCount[nameof(this.GetAllCitiesInProvince)];
                return AsyncEnumerable.Empty<City>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInAll(
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                ++callsCount[nameof(this.SearchInAll)];
                return AsyncEnumerable.Empty<FoundRecord>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInRegion(
                Region region,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                ++callsCount[nameof(this.SearchInRegion)];
                return AsyncEnumerable.Empty<FoundRecord>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInProvince(
                Province province,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                ++callsCount[nameof(this.SearchInProvince)];
                return AsyncEnumerable.Empty<FoundRecord>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInCity(
                City city,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                return AsyncEnumerable.Empty<FoundRecord>();
            }
        }

        private FakePhoneBook fakePhoneBook;

        protected override IPhoneBook GetInnerForNextTest()
        {
            fakePhoneBook = new();
            return fakePhoneBook;
        }

        [Test]
        public async Task DoesNotCallAnyMethodsOfInnerExceptSearchInCityMoreThanOnce()
        {
            foreach (var method in PhoneBookMethods.AllExceptSearchInCity())
            {
                //Call the method twice to test its behaviour before and
                //after caching

                await method.Call(cachingBook).Consume();
                await method.Call(cachingBook).Consume();
            }

            foreach (var pair in fakePhoneBook.CallsCountExceptSearchInCity)
            {
                Assert.LessOrEqual(
                    pair.Value,
                    1,
                    $"{pair.Key} was called {pair.Value} time(s)"
                );
            }
        }
    }
}
