using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace Library.Caching
{
    public class CachingSqlitePhoneBook : IPhoneBook
    {
        private readonly IPhoneBook inner;

        private readonly string connectionString;
        private readonly SqliteCompiler compiler;

        private CachingSqlitePhoneBook(
            IPhoneBook inner,
            string connectionString
        )
        {
            this.inner = inner;

            this.connectionString = connectionString;
            compiler = new();
        }

        public static CachingSqlitePhoneBook Open(
            IPhoneBook inner,
            string connectionString
        )
        {
            using SqliteConnection connection = new(connectionString);
            connection.Open();

            CacheDbBuilder dbBuilder = new(connection);
            dbBuilder.EnsureAllTablesAreCreated();

            return new(inner, connectionString);
        }

        private readonly Query selectRegions = new Query()
            .Select("Url", "DisplayName")
            .From("Regions");

        private readonly Query selectProvinces = new Query()
            .Select(
                "p.Url as PUrl",
                "p.DisplayName as PDisplayName",
                "r.Url as RUrl",
                "r.DisplayName as RDisplayName"
            )
            .From("Provinces as p")
            .Join("Regions as r", "p.RegionId", "r.Id");

        private readonly Query selectCities = new Query()
            .Select(
                "c.Url as CUrl",
                "c.DisplayName as CDisplayName",
                "p.Url as PUrl",
                "p.DisplayName as PDisplayName",
                "r.Url as RUrl",
                "r.DisplayName as RDisplayName"
            )
            .From("Cities as c")
            .Join("Provinces as p", "c.ProvinceId", "p.Id")
            .Join("Regions as r", "p.RegionId", "r.Id");

        public async IAsyncEnumerable<Region> GetAllRegions(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            using var db = await OpenDb();

            var dynamics = await db
                .FromQuery(selectRegions)
                .GetAsync(cancellationToken: cancellationToken);

            var cachedRegions = dynamics.Select(Mappers.ToRegion);

            if (cachedRegions.Any())
            {
                foreach (Region r in cachedRegions)
                {
                    yield return r;
                }

                yield break;
            }

            var regions = inner.GetAllRegions(cancellationToken);

            string[] columns = { "Url", "DisplayName" };
            List<object[]> values = new();

            await foreach (Region r in regions)
            {
                values.Add(new[] { r.Url, r.DisplayName });
                yield return r;
            }

            await db.Connection.InTransaction(
                tx => db.Query("Regions").InsertAsync(
                    columns,
                    values,
                    tx,
                    cancellationToken: cancellationToken
                )
            );
        }

        public async IAsyncEnumerable<Province> GetAllProvincesInRegion(
            Region region,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            using var db = await OpenDb();

            var dynamics = await db
                .FromQuery(selectProvinces)
                .Where("r.DisplayName", "=", region.DisplayName)
                .GetAsync(cancellationToken: cancellationToken);

            var cachedProvinces = dynamics.Select(Mappers.ToProvince);

            if (cachedProvinces.Any())
            {
                foreach (Province p in cachedProvinces)
                {
                    yield return p;
                }

                yield break;
            }

            var provinces =
                CacheAllProvincesInRegion(db, region, cancellationToken);

            await foreach (Province p in provinces)
            {
                yield return p;
            }
        }

        private async IAsyncEnumerable<Province> CacheAllProvincesInRegion(
            QueryFactory db,
            Region region,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            int? regionId = await db
                .Query("Regions")
                .Select("Id")
                .Where("DisplayName", "=", region.DisplayName)
                .FirstAsync<int?>(cancellationToken: cancellationToken);

            if (regionId == null)
            {

            }

            Console.WriteLine(regionId);

            var provinces =
                inner.GetAllProvincesInRegion(region, cancellationToken);

            string[] columns = { "Url", "DisplayName", "RegionId" };
            List<object[]> values = new();

            await foreach (Province p in provinces)
            {
                values.Add(new object[] { p.Url, p.DisplayName, regionId });
                yield return p;
            }

            await db.Connection.InTransaction(tx =>
            {
                return db.Query("Provinces").InsertAsync(
                    columns,
                    values,
                    tx,
                    cancellationToken: cancellationToken
                );
            });
        }

        public async IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<FoundRecord> SearchInAll(
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.CompletedTask;
            yield break;
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

        private async Task<QueryFactory> OpenDb()
        {
            SqliteConnection connection = new(connectionString);
            await connection.OpenAsync();

            return new(connection, compiler);
        }
    }
}
