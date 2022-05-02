using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Library.Caching
{
    public class CachingSqlitePhoneBook : IPhoneBook
    {
        private readonly IPhoneBook inner;
        private readonly string connectionString;

        public CachingSqlitePhoneBook(
            IPhoneBook inner,
            string connectionString
        )
        {
            this.inner = inner;
            this.connectionString = connectionString;
        }

        public static CachingSqlitePhoneBook Open(
            IPhoneBook inner,
            string connectionString
        )
        {
            using var db = CacheDb.Open(connectionString);
            db.EnsureAllTablesAreCreated();

            return new(inner, connectionString);
        }

        public async IAsyncEnumerable<Region> GetAllRegions(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var db = OpenDb();

            var regions = await db.Regions.SelectAll();
            cancellationToken.ThrowIfCancellationRequested();

            if (regions.Any())
            {
                foreach (Region r in regions)
                {
                    yield return r;
                }

                yield break;
            }

            List<Region> toInsert = new();

            await foreach (Region r in inner.GetAllRegions(cancellationToken))
            {
                toInsert.Add(r);
                yield return r;
            }

            await db.Regions.InsertMany(toInsert, cancellationToken);
        }

        public async IAsyncEnumerable<Province> GetAllProvincesInRegion(
            Region region,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var db = OpenDb();

            var provinces = await db.Provinces.SelectAllInRegion(region);
            cancellationToken.ThrowIfCancellationRequested();

            if (provinces.Any())
            {
                foreach (Province p in provinces)
                {
                    yield return p;
                }

                yield break;
            }

            var newProvinces =
                inner.GetAllProvincesInRegion(region, cancellationToken);

            List<Province> toInsert = new();

            await foreach (Province p in newProvinces)
            {
                toInsert.Add(p);
                yield return p;
            }

            await db.Provinces.InsertMany(toInsert, cancellationToken);
        }

        public async IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var db = OpenDb();

            var cities = await db.Cities.SelectAllInProvince(province);
            cancellationToken.ThrowIfCancellationRequested();

            if (cities.Any())
            {
                foreach (City c in cities)
                {
                    yield return c;
                }

                yield break;
            }

            var newCities =
                inner.GetAllCitiesInProvince(province, cancellationToken);

            List<City> toInsert = new();

            await foreach (City c in newCities)
            {
                toInsert.Add(c);
                yield return c;
            }

            await db.Cities.InsertMany(toInsert, cancellationToken);
        }

        public async IAsyncEnumerable<FoundRecord> SearchInAll(
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var db = OpenDb();

            var citiesEnumerable = await db.Cities.SelectAll();
            var cities = citiesEnumerable.ToAsyncEnumerable();

            cancellationToken.ThrowIfCancellationRequested();

            if (!citiesEnumerable.Any())
            {
                cities = GetAllRegions(cancellationToken)
                    .SelectAsyncAndMerge(r => GetAllProvincesInRegion(r, cancellationToken))
                    .SelectAsyncAndMerge(p => GetAllCitiesInProvince(p, cancellationToken));
            }

            var results = cities.SelectAsyncAndMerge(
                c => inner.SearchInCity(c, criteria, cancellationToken)
            );

            await foreach (FoundRecord r in results)
            {
                yield return r;
            }
        }

        public async IAsyncEnumerable<FoundRecord> SearchInRegion(
            Region region,
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var db = OpenDb();

            var citiesEnumerable = await db.Cities.SelectAllInRegion(region);
            var cities = citiesEnumerable.ToAsyncEnumerable();

            if (!citiesEnumerable.Any())
            {
                cities = GetAllProvincesInRegion(region, cancellationToken)
                    .SelectAsyncAndMerge(p => GetAllCitiesInProvince(p, cancellationToken));
            }

            var results = cities.SelectAsyncAndMerge(
                c => inner.SearchInCity(c, criteria, cancellationToken)
            );

            await foreach (FoundRecord r in results)
            {
                yield return r;
            }
        }

        public async IAsyncEnumerable<FoundRecord> SearchInProvince(
            Province province,
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var db = OpenDb();

            var citiesEnumerable = await db.Cities.SelectAllInProvince(province);
            var cities = citiesEnumerable.ToAsyncEnumerable();

            if (!citiesEnumerable.Any())
            {
                cities = GetAllCitiesInProvince(province, cancellationToken);
            }

            var results = cities.SelectAsyncAndMerge(
                c => inner.SearchInCity(c, criteria, cancellationToken)
            );

            await foreach (FoundRecord r in results)
            {
                yield return r;
            }
        }

        public IAsyncEnumerable<FoundRecord> SearchInCity(
            City city,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            return inner.SearchInCity(city, criteria, cancellationToken);
        }

        private CacheDb OpenDb()
        {
            return CacheDb.Open(connectionString);
        }
    }
}
