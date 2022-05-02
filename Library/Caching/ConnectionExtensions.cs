using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
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
                connection.EnsureRootTableIsCreated(transaction);
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

        private static void EnsureRootTableIsCreated(
            this SqliteConnection connection,
            SqliteTransaction transaction
        )
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = "DROP TABLE IF EXISTS Root";
            command.ExecuteNonQuery();

            command.CommandText =
            @"
            CREATE TABLE Root(
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                AllRegionsAreCached INT NOT NULL
            );
            ";

            command.ExecuteNonQuery();

            command.CommandText =
            @"
            INSERT INTO Root (Id, AllRegionsAreCached)
            VALUES (1, 0)
            ";

            command.ExecuteNonQuery();
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
                AllProvincesAreCached INT DEFAULT 0 NOT NULL,
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
                AllCitiesAreCached INT DEFAULT 0 NOT NULL,
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
    }
}
