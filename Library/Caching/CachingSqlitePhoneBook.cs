using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
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
            using SqliteConnection connection = new(connectionString);

            using CacheDb db = CacheDb.Open(connectionString);
            db.EnsureAllTablesAreCreated();

            return new(inner, connectionString);
        }

        private const string selectProvinces =
        @"
        SELECT
            p.Url as PUrl,
            p.DisplayName as PDisplayName,
            r.Url as RUrl,
            r.DisplayName as RDisplayName
        FROM
            Provinces as p
        INNER JOIN
            Regions as r
        ON
            p.RegionId = r.Id
        ";

        private const string selectCities =
        @"
        SELECT
            c.Url as CUrl,
            c.DisplayName as CDisplayName,
            p.Url as PUrl,
            p.DisplayName as PDisplayName,
            r.Url as RUrl,
            r.DisplayName as RDisplayName
        FROM
            Cities as c
        INNER JOIN
            Provinces as p
        ON
            c.ProvinceId = p.Id
        INNER JOIN
            Regions as r
        ON
            p.RegionId = r.Id
        ";

        public async IAsyncEnumerable<Region> GetAllRegions(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var db = OpenDb();

            var dynamics = await db.Connection.QueryAsync(@"
                SELECT Url, DisplayName
                FROM Regions
            ");

            var regions = dynamics.Select(Selectors.ToRegion);

            if (regions.Any())
            {
                foreach (Region r in regions)
                {
                    yield return r;
                }

                yield break;
            }

            var newRegions = inner.GetAllRegions(cancellationToken);
            List<Region> toInsert = new();

            await foreach (Region r in newRegions)
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

            string selectProvincesInRegion =
            $@"
            {selectProvinces}
            WHERE
                r.DisplayName = @DisplayName
            ";

            var dynamics =
                await db.Connection.QueryAsync(selectProvincesInRegion, region);

            var provinces = dynamics.Select(Selectors.ToProvince);

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

            int regionId =
                await db.Regions.SelectIdOrInsert(region, cancellationToken);

            await db.Provinces.InsertMany(provinces, regionId, cancellationToken);
        }


        public async IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var db = OpenDb();

            string selectCitiesInProvince =
            $@"
            {selectCities}
            WHERE
                p.DisplayName = @DisplayName
            ";

            var dynamics =
                await db.Connection.QueryAsync(selectCitiesInProvince, province);

            var cities = dynamics.Select(Selectors.ToCity);

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

            int provinceId =
                await db.Provinces.SelectIdOrInsert(province, cancellationToken);

            await db.Cities.InsertMany(toInsert, provinceId, cancellationToken);
        }

        #region Caching note
        //Considering the interface, there are handy properties we can use
        //when caching models:
        //- If there is any region cached, that all regions are cached
        //
        //- If there is any province in a region cached, then all provinces
        //  in that region are cached
        //
        //- If there is any city in a province cached, then all cities in
        //  that province are cached
        //
        //However, if any province is cached, it *does not* mean that
        //*all* provinces are cached, that is, there could be regions
        //without cached provinces at all. Similary, there could
        //be provinces with none cities cached yet
        //
        //This means that we can't simple select all cities from the DB,
        //and if result is not empty, this would be the all cached cities.
        //Instead, we need first query all regions that don't have any cached
        //province and cache their provinces. Then we need to query all provinces
        //that don't have any cached cities and cached their cities. Only
        //then we can be sure that all cities are cached and are in DB
        #endregion

        public async IAsyncEnumerable<FoundRecord> SearchInAll(
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var db = OpenDb();

            await EnsureAllProvincesInAllRegionsAllCached(db, cancellationToken);
            await EnsureAllCitiesInAllProvincesAreCached(db, cancellationToken);

            var dynamics = await db.Connection.QueryAsync(selectCities);
            var cities = dynamics.Select(Selectors.ToCity).ToAsyncEnumerable();

            var results = cities.SelectAsyncAndMerge(
                c => inner.SearchInCity(c, criteria, cancellationToken)
            );

            await foreach (FoundRecord r in results)
            {
                yield return r;
            }
        }

        private async Task EnsureAllProvincesInAllRegionsAllCached(
            CacheDb db,
            CancellationToken cancellationToken
        )
        {
            string selectRegionsWithoutProvinces =
            @"
            SELECT
                r.Id,
                r.Url,
                r.DisplayName
            FROM
                Regions as r
            LEFT OUTER JOIN
                Provinces as p
            ON
                p.RegionId = r.id
            WHERE
                p.Id IS NULL
            ";

            var dynamics =
                await db.Connection.QueryAsync(selectRegionsWithoutProvinces);

            var regionsWithIds = dynamics.Select(d => {
                int id = d.Id;
                Region region = Selectors.ToRegion(d);

                return (id, region);
            });

            if (!regionsWithIds.Any())
            {
                return;
            }

            foreach (var (id, region) in regionsWithIds)
            {
                List<Province> toInsert = new();

                var provinces =
                    inner.GetAllProvincesInRegion(region, cancellationToken);

                await foreach (Province p in provinces)
                {
                    toInsert.Add(p);
                }

                await db.Provinces.InsertMany(toInsert, id, cancellationToken);
            }
        }

        private async Task EnsureAllCitiesInAllProvincesAreCached(
            CacheDb db,
            CancellationToken cancellationToken
        )
        {
            string selectProvincesWithoutCities =
            $@"
            {selectCities}
            LEFT OUTER JOIN
                Cities as c
            ON
                c.ProvinceId = p.Id
            WHERE
                c.Id IS NULL
            ";

            var dynamics =
                await db.Connection.QueryAsync(selectProvincesWithoutCities);

            var provincesWithIds = dynamics.Select(d =>
            {
                int id = d.Id;
                Province province = Selectors.ToProvince(d);

                return (id, province);
            });

            if (!provincesWithIds.Any())
            {
                return;
            }

            foreach (var (id, province) in provincesWithIds)
            {
                List<City> toInsert = new();

                var cities =
                    inner.GetAllCitiesInProvince(province, cancellationToken);

                await foreach (City c in cities)
                {
                    toInsert.Add(c);
                }

                await db.Cities.InsertMany(toInsert, id, cancellationToken);
            }
        }

        public IAsyncEnumerable<FoundRecord> SearchInRegion(
            Region region,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<FoundRecord> SearchInProvince(
            Province province,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
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
