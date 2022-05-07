using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching.DbTables
{
    public class ProvincesTable : DbTable<Province>
    {
        public ProvincesTable(CacheDb db)
            : base(db, "Provinces") { }

        protected override async Task Insert(
            Province entity,
            SqliteTransaction? transaction = null
        )
        {
            int regionId = await db.Regions.SelectIdOrInsert(entity.Region);
            await InsertWithRegionId(entity, regionId);
        }

        public Task InsertWithRegionId(
            Province entity,
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
                entity.Url,
                entity.DisplayName,
                RegionId = regionId
            };

            return db.Connection.ExecuteAsync(sql, param, transaction);
        }

        public Task InsertMany(
            IEnumerable<Province> entities,
            int regionId,
            CancellationToken cancellationToken = default
        )
        {
            return DoForEachWithTransaction(
                entities,
                (p, tx) => InsertWithRegionId(p, regionId, tx),
                cancellationToken
            );
        }
    }
}
