using System;
using System.Threading.Tasks;
using Library.Caching.DbTables;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public class CacheDb : IAsyncDisposable, IDisposable
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

        public void Close()
        {
            Connection.Close();
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
