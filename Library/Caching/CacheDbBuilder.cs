using Microsoft.Data.Sqlite;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace Library.Caching
{
    internal sealed class CacheDbBuilder
    {
        private readonly SqliteConnection connection;
        private readonly QueryFactory db;

        public CacheDbBuilder(SqliteConnection connection)
        {
            this.connection = connection;
            db = new(connection, new SqliteCompiler());
        }

        public void EnsureAllTablesAreCreated()
        {
            var transaction = connection.BeginTransaction();

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
            string createTable =
            @"
            create table if not exists Regions(
                Id integer primary key,
                Url text not null,
                DisplayName text not null,
                unique(Url)
            )
            ";

            string createIndex =
            @"
            create unique index if not exists UQIX_Regions_DisplayName
            on Regions (DisplayName)
            ";

            db.Statement(createTable, transaction);
            db.Statement(createIndex, transaction);
        }

        private void EnsureProvincesTableIsCreated(
            SqliteTransaction transaction
        )
        {
            string createTable =
            @"
            create table if not exists Provinces(
                Id integer primary key,
                Url text not null,
                DisplayName text not null,
                RegionId int not null,
                unique(Url),
                foreign key (RegionId) references Regions (Id)
            )
            ";

            string createIndex =
            @"
            create unique index if not exists UQIX_Provinces_DisplayName
            on Provinces (DisplayName)
            ";

            db.Statement(createTable, transaction);
            db.Statement(createIndex, transaction);
        }

        private void EnsureCitiesTableIfCreated(
            SqliteTransaction transaction
        )
        {
            string createTable =
            @"
            create table if not exists Cities(
                Id integer primary key,
                Url text not null,
                DisplayName text not null,
                ProvinceId int not null,
                unique(Url),
                foreign key (ProvinceId) references Provinces (Id)
            )
            ";

            string createIndex =
            @"
            create unique index if not exists UQIX_Cities_DisplayName
            on Cities (DisplayName)
            ";

            db.Statement(createTable, transaction);
            db.Statement(createIndex, transaction);
        }
    }
}
