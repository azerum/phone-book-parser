using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public abstract class BaseQueries<T> where T : SearchScope
    {
        private readonly SqliteConnection connection;

        protected abstract string[] InsertFields { get; }

        public BaseQueries(SqliteConnection connection)
        {
            this.connection = connection;
        }

        public Task InsertOrIgnoreAsync(
            T obj,
            SqliteTransaction? transaction = null
        )
        {
            
        }
    }
}
