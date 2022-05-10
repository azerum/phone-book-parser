using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public class CachingSqlitePhoneBook : IPhoneBook
    {
        private readonly IPhoneBook inner;
        private readonly string connectionString;

        private CachingSqlitePhoneBook(
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
            SqliteConnection connection = new(connectionString);
            connection.Open();

            using var db = CacheDb.OpenAsync(connectionString).Result;
            db.EnsureAllTablesAreCreated();

            return new(inner, connectionString);
        }

        private Task<CacheDb> OpenDb()
        {
            return CacheDb.OpenAsync(connectionString);
        }

        public async IAsyncEnumerable<Region> GetAllRegions(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            using var db = await OpenDb();

            if (await db.AreAllRegionsCached(cancellationToken))
            {
                var cachedRegions = await db.SelectAllRegions(cancellationToken);

                foreach (Region r in cachedRegions)
                {
                    yield return r;
                }

                yield break;
            }

            var regions = await inner
                .GetAllRegions(cancellationToken)
                .ToListAsync(CancellationToken.None);

            await db.CacheAllRegions(regions, cancellationToken);

            foreach (Region r in regions)
            {
                yield return r;
            }
        }

        public async IAsyncEnumerable<Province> GetAllProvincesInRegion(
            Region region,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            using var db = await OpenDb();
            var regionInfo = await db.SelectRegionInfo(region, cancellationToken);

            long regionId;

            if (regionInfo != null)
            {
                regionId = regionInfo.Id;

                if (regionInfo.AllProvincesAreCached)
                {
                    var cachedProvinces = await db
                        .SelectProvincesByRegionId(regionId, cancellationToken);

                    foreach (Province p in cachedProvinces)
                    {
                        yield return p;
                    }

                    yield break;
                }
            }
            else
            {
                regionId = await db.InsertRegion(region, cancellationToken);
            }

            var provinces = await inner
                .GetAllProvincesInRegion(region, cancellationToken)
                .ToListAsync(CancellationToken.None);

            await db.CacheAllProvincesInRegionWithId(
                provinces,
                regionId,
                cancellationToken
            );

            foreach (Province p in provinces)
            {
                yield return p;
            }
        }

        public async IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            using var db = await OpenDb();

            var provinceInfo =
                await db.SelectProvinceInfo(province, cancellationToken);

            long provinceId;

            if (provinceInfo != null)
            {
                provinceId = provinceInfo.Id;

                if (provinceInfo.AllCitiesAreCached)
                {
                    var cachedCities = await db
                        .SelectCitiesByProvinceId(provinceId, cancellationToken);

                    foreach (City c in cachedCities)
                    {
                        yield return c;
                    }

                    yield break;
                }
            }
            else
            {
                provinceId = await db.InsertProvince(province, cancellationToken);
            }

            var cities = await inner
                .GetAllCitiesInProvince(province, cancellationToken)
                .ToListAsync(CancellationToken.None);

            await db.CacheAllCitiesInProvinceWithId(
                cities,
                provinceId,
                cancellationToken
            );

            foreach (City c in cities)
            {
                yield return c;
            }
        }

        public async IAsyncEnumerable<FoundRecord> SearchInAll(
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            using var db = await OpenDb();

            await EnsureAllRegionsAreCached(db, cancellationToken);

            await EnsureAllCachedRegionsHaveAllProvincesCached(
                db,
                cancellationToken
            );

            await EnsureAllCachedProvincesHaveAllCitiesCached(
                db,
                cancellationToken
            );

            var results = SearchInEachCachedCity(db, criteria, cancellationToken);

            await foreach (FoundRecord r in results)
            {
                yield return r;
            }
        }

        private async Task EnsureAllRegionsAreCached(
            CacheDb db,
            CancellationToken cancellationToken
        )
        {
            if (!await db.AreAllRegionsCached(cancellationToken))
            {
                var regions = await inner
                    .GetAllRegions(cancellationToken)
                    .ToListAsync(CancellationToken.None);

                await db.CacheAllRegions(regions, cancellationToken);
            }
        }

        private async Task EnsureAllCachedRegionsHaveAllProvincesCached(
            CacheDb db,
            CancellationToken cancellationToken
        )
        {
            var regionsWithNotAllProvinces = await db
                .SelectRegionsWithNotAllProvincesCached(cancellationToken);

            if (regionsWithNotAllProvinces.Any())
            {
                foreach (var (id, region) in regionsWithNotAllProvinces)
                {
                    await CacheAllProvincesInRegion(
                        db,
                        region,
                        id,
                        cancellationToken
                    );
                }
            }
        }

        private async Task EnsureAllCachedProvincesHaveAllCitiesCached(
            CacheDb db,
            CancellationToken cancellationToken
        )
        {
            var provincesWithNotAllCities = await db
                .SelectProvincesWithNotAllCitiesCached(cancellationToken);

            if (provincesWithNotAllCities.Any())
            {
                foreach (var (id, province) in provincesWithNotAllCities)
                {
                    await CacheAllCitiesInProvince(
                        db,
                        province,
                        id,
                        cancellationToken
                    );
                }
            }
        }

        public async IAsyncEnumerable<FoundRecord> SearchInRegion(
            Region region,
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            using var db = await OpenDb();

            var regionInfo = await db.SelectRegionInfo(region, cancellationToken);

            long regionId =
                regionInfo?.Id
                ?? await db.InsertRegion(region, cancellationToken);

            bool allProvincesAreCached = regionInfo?.AllProvincesAreCached ?? false;

            if (!allProvincesAreCached)
            {
                await CacheAllProvincesInRegion(
                    db,
                    region,
                    regionId,
                    cancellationToken
                );
            }

            await EnsureAllCachedProvincesHaveAllCitiesCached(db, cancellationToken);

            var results = SearchInEachCachedCity(db, criteria, cancellationToken);

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
            using var db = await OpenDb();

            var provinceInfo = await db
                .SelectProvinceInfo(province, cancellationToken);

            long provinceId =
                provinceInfo?.Id
                ?? await db.InsertProvince(province, cancellationToken);

            bool allCitiesAreCached = provinceInfo?.AllCitiesAreCached ?? false;

            if (!allCitiesAreCached)
            {
                await CacheAllCitiesInProvince(
                    db,
                    province,
                    provinceId,
                    cancellationToken
                );
            }

            var results = SearchInEachCachedCity(db, criteria, cancellationToken);

            await foreach (FoundRecord r in results)
            {
                yield return r;
            }
        }

        private async Task CacheAllProvincesInRegion(
            CacheDb db,
            Region region,
            long regionId,
            CancellationToken cancellationToken
        )
        {
            var provinces = await inner
                .GetAllProvincesInRegion(region, cancellationToken)
                .ToListAsync(CancellationToken.None);

            await db.CacheAllProvincesInRegionWithId(
                provinces,
                regionId,
                cancellationToken
            );
        }

        private async Task CacheAllCitiesInProvince(
            CacheDb db,
            Province province,
            long provinceId,
            CancellationToken cancellationToken
        )
        {
            var cities = await inner
                .GetAllCitiesInProvince(province, cancellationToken)
                .ToListAsync(CancellationToken.None);

            await db.CacheAllCitiesInProvinceWithId(
                cities,
                provinceId,
                cancellationToken
            );
        }

        private async IAsyncEnumerable<FoundRecord> SearchInEachCachedCity(
            CacheDb db,
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var enumerable = await db.SelectAllCities(cancellationToken);
            var asyncEnumerable = enumerable.ToAsyncEnumerable();

            var results = asyncEnumerable.SelectAsyncAndMerge(
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
    }
}
