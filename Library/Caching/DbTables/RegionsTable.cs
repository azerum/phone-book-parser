using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching.DbTables
{
    public class RegionsTable : DbTable<Region>
    {
        public RegionsTable(CacheDb db)
            : base(db, "Regions") { }

        protected override Task Insert(
            Region entity,
            SqliteTransaction? transaction = null
        )
        {
            string sql =
            @"
            INSERT INTO Regions (Url, DisplayName)
            VALUES (@Url, @DisplayName)
            ";

            return db.Connection.ExecuteAsync(sql, entity, transaction);
        }

        public Task InsertMany(
            IEnumerable<Region> entities,
            CancellationToken cancellationToken = default
        )
        {
            return DoForEachWithTransaction(entities, Insert, cancellationToken);
        }
    }
}
