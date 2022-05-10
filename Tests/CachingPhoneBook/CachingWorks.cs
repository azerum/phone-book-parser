using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library;
using NUnit.Framework;

namespace Tests.CachingPhoneBook
{
    public class CachingWorks : BaseTests
    {
        class FakePhoneBook : IPhoneBook
        {
            public readonly Dictionary<string, int> CallsCount;

            public FakePhoneBook()
            {
                CallsCount = new();

                CallsCount.Add(nameof(this.GetAllRegions), 0);
                CallsCount.Add(nameof(this.GetAllProvincesInRegion), 0);
                CallsCount.Add(nameof(this.GetAllCitiesInProvince), 0);

                CallsCount.Add(nameof(this.SearchInAll), 0);
                CallsCount.Add(nameof(this.SearchInRegion), 0);
                CallsCount.Add(nameof(this.SearchInProvince), 0);
                CallsCount.Add(nameof(this.SearchInCity), 0);
            }

            public IAsyncEnumerable<Region> GetAllRegions(
                CancellationToken cancellationToken = default
            )
            {
                ++CallsCount[nameof(this.GetAllRegions)];
                return AsyncEnumerable.Empty<Region>();
            }

            public IAsyncEnumerable<Province> GetAllProvincesInRegion(
                Region region,
                CancellationToken cancellationToken = default
            )
            {
                ++CallsCount[nameof(this.GetAllProvincesInRegion)];
                return AsyncEnumerable.Empty<Province>();
            }

            public IAsyncEnumerable<City> GetAllCitiesInProvince(
                Province province,
                CancellationToken cancellationToken = default
            )
            {
                ++CallsCount[nameof(this.GetAllCitiesInProvince)];
                return AsyncEnumerable.Empty<City>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInAll(
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                ++CallsCount[nameof(this.SearchInAll)];
                return AsyncEnumerable.Empty<FoundRecord>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInRegion(
                Region region,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                ++CallsCount[nameof(this.SearchInRegion)];
                return AsyncEnumerable.Empty<FoundRecord>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInProvince(
                Province province,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                ++CallsCount[nameof(this.SearchInProvince)];
                return AsyncEnumerable.Empty<FoundRecord>();
            }

            public IAsyncEnumerable<FoundRecord> SearchInCity(
                City city,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                ++CallsCount[nameof(this.SearchInProvince)];
                return AsyncEnumerable.Empty<FoundRecord>();
            }
        }

        private readonly FakePhoneBook fakePhoneBook = new();
        protected override IPhoneBook Inner => fakePhoneBook;

        [Test]
        public async Task DoesNotCallAnyInnerPhoneBookMethodMoreThanOnce()
        {
            foreach (var method in MethodsToTest())
            {
                await method(cachingBook).Consume();
            }

            foreach (var pair in fakePhoneBook.CallsCount)
            {
                Assert.LessOrEqual(
                    pair.Value,
                    1,
                    $"{pair.Key} was called {pair.Value} time(s)"
                );
            }
        }

        public static List<Func<IPhoneBook, IAsyncEnumerable<object>>> MethodsToTest()
        {
            List<Func<IPhoneBook, IAsyncEnumerable<object>>> results = new();

            Region region = new("https://example.com", "Dummy");
            Province province = new(region, "https://example.com/p", "Dummy");
            City city = new(province, "https://example.com/p/c", "Dummy");

            SearchCriteria criteria = new("Dummy");

            results.Add(phoneBook => phoneBook.GetAllRegions());
            results.Add(phoneBook => phoneBook.GetAllProvincesInRegion(region));
            results.Add(phoneBook => phoneBook.GetAllCitiesInProvince(province));

            //results.Add(phoneBook => phoneBook.SearchInAll(criteria));
            results.Add(phoneBook => phoneBook.SearchInRegion(region, criteria));
            results.Add(phoneBook => phoneBook.SearchInProvince(province, criteria));
            results.Add(phoneBook => phoneBook.SearchInCity(city, criteria));

            return results;
        }
    }
}
