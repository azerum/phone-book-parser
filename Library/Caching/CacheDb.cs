using System;
using System.Data;
using Library.Caching.DbTables;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public class CacheDb
    {
        public SqliteConnection Connection { get; }

        public RegionsTable Regions { get; }
        public ProvincesTable Provinces { get; }
        public CitiesTable Cities { get; }

        public CacheDb(SqliteConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                throw new ArgumentException(
                   "'connection' parameter must be an open connection"
                );
            }

            Connection = connection;

            Regions = new(this);
            Provinces = new(this);
            Cities = new(this);
        }

        public void EnsureAllTablesAreCreated()
        {
            var transaction = Connection.BeginTransaction();

            try
            {
                Regions.EnsureIsCreated(transaction);
                Provinces.EnsureIsCreated(transaction);
                Cities.EnsureIsCreated(transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
