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

            connection.Open();
            connection.EnsureAllTablesAreCreated();

            return new(inner, connectionString);
        }

        public async IAsyncEnumerable<Region> GetAllRegions(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var connection = OpenConnection();

            int allRegionsAreCached = await connection.ExecuteScalarAsync<int>(@"
                SELECT AllRegionsAreCached
                FROM Root
                WHERE Id = 1
            ");

            if (allRegionsAreCached == 1)
            {
                var cachedRegions = await SelectAllRegions(connection);

                foreach (Region r in cachedRegions)
                {
                    yield return r;
                }

                yield break;
            }

            var regions = inner.GetAllRegions(cancellationToken);
            List<Region> toInsert = new();

            await foreach (Region r in regions)
            {
                toInsert.Add(r);
                yield return r;
            }

            var transaction = connection.BeginTransaction();

            string insertSql =
            @"
            INSERT INTO Regions (Url, DisplayName)
            VALUES (@Url, @DisplayName)
            ";

            string updateSql =
            @"
            UPDATE Root
            SET AllRegionsAreCached = 1
            WHERE Id = 1
            ";

            try
            {
                foreach (Region r in toInsert)
                {
                    await connection.ExecuteAsync(insertSql, r, transaction);
                }

                await connection.ExecuteAsync(updateSql, transaction);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
        }

        private static async Task<IEnumerable<Region>> SelectAllRegions(
            SqliteConnection connection
        )
        {
            string sql =
            @"
            SELECT Url, DisplayName
            FROM Regions
            ";

            var dynamics = await connection.QueryAsync(sql);

            return dynamics.Select(d => new Region(d.Url, d.DisplayName));
        }

        private static Task Insert(
            SqliteConnection connection,
            Region region,
            SqliteTransaction? transaction = null
        )
        {
            string sql =
            @"
            INSERT INTO Regions (Url, DisplayName)
            VALUES (@Url, @DisplayName)
            ";

            return connection.ExecuteAsync(sql, region, transaction);
        }

        private static Task InsertMany(
            SqliteConnection connection,
            IEnumerable<Region> regions,
            CancellationToken cancellationToken = default
        )
        {
            return DoInsertMany(
                connection,
                regions,
                Insert,
                cancellationToken
            );
        }

        private static async Task DoInsertMany<T>(
            SqliteConnection connection,
            IEnumerable<T> values,
            Func<SqliteConnection, T, SqliteTransaction, Task> insert,
            CancellationToken cancellationToken
        )
        {
            var transaction = connection.BeginTransaction();

            try
            {
                foreach (T v in values)
                {
                    await insert(connection, v, transaction);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
        }

        public async IAsyncEnumerable<Province> GetAllProvincesInRegion(
            Region region,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var connection = OpenConnection();

            dynamic d = await connection.QueryAsync(
                @"
                SELECT Id, AllProvincesAreCached
                FROM Regions
                WHERE DisplayName = @DisplayName
                ",
                region
            );

            if (d.AllProvincesAreCached == 1)
            {
                string sql =
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
                WHERE
                    r.DisplayName = @DisplayName
                ";

                var cachedProvinces = await connection.QueryAsync(sql, region);

                foreach (dynamic cp in cachedProvinces)
                {
                    Region r = new(cp.RUrl, cp.RDisplayName);
                    yield return new(r, cp.PUrl, cp.PDisplayName);
                }

                yield break;
            }

            var provinces = inner.GetAllProvincesInRegion(
                region,
                cancellationToken
            );

            List<Province> toInsert = new();

            await foreach (Province p in provinces)
            {
                toInsert.Add(p);
                yield return p;
            }

            var transaction = connection.BeginTransaction();

            string insertSql =
            @"
            INSERT INTO Provinces (Url, DisplayName, RegionId)
            VALUES (@Url, @DisplayName, @RegionId)
            ";

            string updateSql =
            @"
            UPDATE Regions
            SET AllRegionsAreCached = 1
            WHERE Id = @RegionId
            ";

            try
            {
                foreach (Province p in toInsert)
                {

                }
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
        }

        public IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
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
            return inner.SearchInCity(city, criteria, cancellationToken);
        }

        private SqliteConnection OpenConnection()
        {
            SqliteConnection connection = new(connectionString);
            connection.Open();

            return connection;
        }
    }
}
