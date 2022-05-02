using System;
using System.Threading.Tasks;
using Library.Caching.DbTables;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public class CacheDb : IAsyncDisposable, IDisposable
    {
        private readonly SqliteConnection connection;
        private bool disposed;

        private readonly RegionsTable regions;
        private readonly ProvincesTable provinces;
        private readonly CitiesTable cities;

        public RegionsTable Regions
        {
            get
            {
                ThrowIfDisposed();
                return regions;
            }
        }

        public ProvincesTable Provinces
        {
            get
            {
                ThrowIfDisposed();
                return provinces;
            }
        }

        public CitiesTable Cities
        {
            get
            {
                ThrowIfDisposed();
                return cities;
            }
        }

        private CacheDb(SqliteConnection connection)
        {
            this.connection = connection;
            disposed = false;

            regions = new(this, connection);
            provinces = new(this, connection);
            cities = new(this, connection);
        }

        public static CacheDb Open(string connectionString)
        {
            SqliteConnection connection = new(connectionString);
            connection.Open();

            return new(connection);
        }

        public void EnsureAllTablesAreCreated()
        {
            ThrowIfDisposed();

            var transaction = connection.BeginTransaction();

            try
            {
                regions.EnsureIsCreated(transaction);
                provinces.EnsureIsCreated(transaction);
                cities.EnsureIsCreated(transaction);

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
            connection.Close();
        }

        public Task CloseAsync()
        {
            return connection.CloseAsync();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Close();
        }

        public ValueTask DisposeAsync()
        {
            if (disposed)
            {
                return ValueTask.CompletedTask;
            }

            disposed = true;
            return new(CloseAsync());
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("CacheDb");
            }
        }
    }
}
