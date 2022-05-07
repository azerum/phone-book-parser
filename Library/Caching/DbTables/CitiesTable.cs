using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching.DbTables
{
    public class CitiesTable : DbTable<City>
    {
        public CitiesTable(CacheDb db)
            : base(db, "Cities") { }

        protected override async Task Insert(
            City entity,
            SqliteTransaction? transaction = null
        )
        {
            int provinceId =
                await db.Provinces.SelectIdOrInsert(entity.Province);

            await InsertWithProvinceId(entity, provinceId, transaction);
        }

        public Task InsertWithProvinceId(
            City entity,
            int provinceId,
            SqliteTransaction? transaction = null
        )
        {
            string sql =
            @"
            INSERT INTO Cities (Url, DisplayName, ProvinceId)
            VALUES (@Url, @DisplayName, @ProvinceId)
            ";

            var param = new
            {
                entity.Url,
                entity.DisplayName,
                ProvinceId = provinceId
            };

            return db.Connection.ExecuteAsync(sql, param, transaction);
        }

        public Task InsertMany(
            IEnumerable<City> entities,
            int provinceId,
            CancellationToken cancellationToken = default
        )
        {
            return DoForEachWithTransaction(
                entities,
                (c, tx) => InsertWithProvinceId(c, provinceId, tx),
                cancellationToken
            );
        }
    }
}
