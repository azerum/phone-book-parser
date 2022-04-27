using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public class CachingDbBootstrap
    {
        private readonly SqliteConnection connection;

        public CachingDbBootstrap(SqliteConnection connection)
        {
            this.connection = connection;
        }

        public void InitRegionTable()
        {
            var transaction = connection.BeginTransaction();

            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Region(
                Id INT PRIMARY KEY,
                Url TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                UNIQUE(Url)
            )
            ";

            command.ExecuteNonQuery();

            command.CommandText =
            @"
            CREATE UNIQUE INDEX IF NOT EXISTS UQIX_Region_DisplayName
            ON Region (DisplayName)
            ";

            command.ExecuteNonQuery();

            transaction.Commit();
        }

        public void InitProvinceTable()
        {
            var transaction = connection.BeginTransaction();

            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Province(
                Id INT PRIMARY KEY,
                Url TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                RegionId INT NOT NULL,
                UNIQUE(Url),
                FOREIGN KEY (RegionId) REFERENCES Region (Id)
            )
            ";

            command.ExecuteNonQuery();

            command.CommandText =
            @"
            CREATE UNIQUE INDEX IF Not EXISTS UQIX_Province_DisplayName
            ON Province (DisplayName)
            ";

            command.ExecuteNonQuery();

            transaction.Commit();
        }

        public void InitCityTable()
        {
            var transaction = connection.BeginTransaction();

            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS City(
                Id INT PRIMARY KEY,
                Url TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                ProvinceId INT NOT NULL,
                UNIQUE(Url),
                FOREIGN KEY (ProvinceId) REFERENCES Province (Id)
            )
            ";

            command.ExecuteNonQuery();

            command.CommandText =
            @"
            CREATE UNIQUE INDEX IF Not EXISTS UQIX_City_DisplayName
            ON City (DisplayName)
            ";

            command.ExecuteNonQuery();

            transaction.Commit();
        }
    }
}
