using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public static class ConnectionExtensions
    {
        public static void EnsureAllTablesAreCreated(
            this SqliteConnection connection
        )
        {
            var transaction = connection.BeginTransaction();

            try
            {
                connection.EnsureRegionsTableIsCreated(transaction);
                connection.EnsureProvincesTableIsCreated(transaction);
                connection.EnsureCitiesTableIfCreated(transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static void EnsureRegionsTableIsCreated(
            this SqliteConnection connection,
            SqliteTransaction transaction
        )
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Regions(
                Id INTEGER PRIMARY KEY,
                Url TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                UNIQUE(Url)
            )
            ";

            command.ExecuteNonQuery();

            command.CommandText =
            @"
            CREATE UNIQUE INDEX IF NOT EXISTS UQIX_Regions_DisplayName
            ON Regions (DisplayName)
            ";

            command.ExecuteNonQuery();
        }

        private static void EnsureProvincesTableIsCreated(
            this SqliteConnection connection,
            SqliteTransaction transaction
        )
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Provinces(
                Id INTEGER PRIMARY KEY,
                Url TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                RegionId INT NOT NULL,
                UNIQUE(Url),
                FOREIGN KEY (RegionId) REFERENCES Regions (Id)
            )
            ";

            command.ExecuteNonQuery();

            command.CommandText =
            @"
            CREATE UNIQUE INDEX IF Not EXISTS UQIX_Provinces_DisplayName
            ON Provinces (DisplayName)
            ";

            command.ExecuteNonQuery();
        }

        private static void EnsureCitiesTableIfCreated(
            this SqliteConnection connection,
            SqliteTransaction transaction
        )
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Cities(
                Id INTEGER PRIMARY KEY,
                Url TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                ProvinceId INT NOT NULL,
                UNIQUE(Url),
                FOREIGN KEY (ProvinceId) REFERENCES Provinces (Id)
            )
            ";

            command.ExecuteNonQuery();

            command.CommandText =
            @"
            CREATE UNIQUE INDEX IF NOT EXISTS UQIX_Cities_DisplayName
            ON Cities (DisplayName)
            ";

            command.ExecuteNonQuery();
        }

        public static async Task<IEnumerable<Region>> SelectAllRegions(
            this SqliteConnection connection
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

        public static async Task<IEnumerable<Province>> SelectAllProvincesInRegion(
            this SqliteConnection connection,
            Region region
        )
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

            var dynamics = await connection.QueryAsync(sql, region);

            return dynamics.Select(d =>
            {
                Region r = new(d.RUrl, d.RDisplayName);
                Province p = new(r, d.PUrl, d.PDisplayName);

                return p;
            });
        }

        public static Task Insert(
            this SqliteConnection connection,
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

        public static Task Insert(
            this SqliteConnection connection,
            Province province,
            int regionId,
            SqliteTransaction? transaction = null
        )
        {
            string sql =
            @"
            INSERT INTO Provinces (Url, DisplayName, RegionId)
            VALUES (@Url, @DisplayName, @RegionId)
            ";

            var param = new
            {
                province.Url,
                province.DisplayName,
                RegionId = regionId
            };

            return connection.ExecuteAsync(sql, param, transaction);
        }

        public static Task Insert(
            this SqliteConnection connection,
            City city,
            int provinceId,
            SqliteTransaction? transaction = null
        )
        {
            string sql =
            @"
            INSERT INTO Cities (Url, DisplayName, ProvinceId)
            VALUES (@Url, @DisplayName, @ProvinceId)
            ";

            var param = new
            {
                city.Url,
                city.DisplayName,
                ProvinceId = provinceId
            };

            return connection.ExecuteAsync(sql, param, transaction);
        }

        public static Task InsertMany(
            this SqliteConnection connection,
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

        public static Task InsertMany(
            this SqliteConnection connection,
            IEnumerable<Province> provinces,
            int regionId,
            CancellationToken cancellationToken = default
        )
        {
            return DoInsertMany(
                connection,
                provinces,
                (conn, p, tx) => conn.Insert(p, regionId, tx),
                cancellationToken
            );
        }

        public static Task InsertMany(
            this SqliteConnection connection,
            IEnumerable<City> cities,
            int provinceId,
            CancellationToken cancellationToken = default
        )
        {
            return DoInsertMany(
                connection,
                cities,
                (conn, c, tx) => conn.Insert(c, provinceId, tx),
                cancellationToken
            );
        }

        private static async Task DoInsertMany<T>(
            SqliteConnection connection,
            IEnumerable<T> values,
            Func<SqliteConnection, T, SqliteTransaction, Task> insert,
            CancellationToken cancellationToken = default
        )
        {
            var transaction = connection.BeginTransaction();

            try
            {
                foreach (T v in values)
                {
                    await insert(connection, v, transaction);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
        }
    }
}
