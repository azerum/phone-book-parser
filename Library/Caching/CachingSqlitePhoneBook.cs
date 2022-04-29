using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public class CachingSqlitePhoneBook : IPhoneBook
    {
        private readonly IPhoneBook inner;
        private readonly CacheDb db;

        public CachingSqlitePhoneBook(
            IPhoneBook inner,
            CacheDb db
        )
        {
            this.inner = inner;
            this.db = db;
        }

        public static CachingSqlitePhoneBook Create(
            IPhoneBook inner,
            string connectionString
        )
        {
            SqliteConnection connection = new(connectionString);
            connection.Open();

            CacheDb db = new(connection);
            db.EnsureAllTablesAreCreated();

            return new(inner, db);
        }

        public async IAsyncEnumerable<Region> GetAllRegions(
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
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
            var provinces = await db.Provinces.SelectAllInRegion(region);

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

            await db.Provinces.InsertMany(provinces, cancellationToken);
        }

        public async IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var cities = await db.Cities.SelectAllInProvince(province);

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

        public IAsyncEnumerable<FoundRecord> SearchInAll(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
