using System;
using System.Threading.Tasks;
using Dapper;
using Library.Caching.DbTables;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public sealed class CacheDb : IDisposable, IAsyncDisposable
    {
        public SqliteConnection Connection { get; }

        public RegionsTable Regions { get; }
        public ProvincesTable Provinces { get; }
        public CitiesTable Cities { get; }

        private CacheDb(SqliteConnection connection)
        {
            Connection = connection;

            Regions = new(this);
            Provinces = new(this);
            Cities = new(this);
        }

        public static CacheDb Open(string connectionString)
        {
            SqliteConnection connection = new(connectionString);
            connection.Open();

            return new(connection);
        } 

        public void EnsureAllTablesAreCreated()
        {
            var transaction = Connection.BeginTransaction();

            try
            {
                EnsureRegionsTableIsCreated(transaction);
                EnsureProvincesTableIsCreated(transaction);
                EnsureCitiesTableIfCreated(transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void EnsureRegionsTableIsCreated(SqliteTransaction transaction)
        {
            var command = Connection.CreateCommand();
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

        private void EnsureProvincesTableIsCreated(
            SqliteTransaction transaction
        )
        {
            var command = Connection.CreateCommand();
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

        private void EnsureCitiesTableIfCreated(
            SqliteTransaction transaction
        )
        {
            var command = Connection.CreateCommand();
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

        public void Dispose()
        {
            Connection.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return Connection.DisposeAsync();
        }
    }
}
