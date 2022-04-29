using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching.DbTables
{
    public class ProvincesTable : DbTable<Province>
    {
        public override string TableName => "Provinces";

        protected override string InsertSql =>
        $@"
        INSERT INTO {TableName} (Url, DisplayName, RegionId)
        VALUES (@Url, @DisplayName, @RegionId)
        ";

        public ProvincesTable(CacheDb db) : base(db) { }

        public override void EnsureIsCreated(
            SqliteTransaction? transaction = null
        )
        {
            var command = db.Connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
            $@"
            CREATE TABLE IF NOT EXISTS {TableName}(
                Id INTEGER PRIMARY KEY,
                Url TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                RegionId INT NOT NULL,
                UNIQUE(Url),
                FOREIGN KEY (RegionId) REFERENCES {db.Regions.TableName} (Id)
            )
            ";

            command.ExecuteNonQuery();

            command.CommandText =
            $@"
            CREATE UNIQUE INDEX IF Not EXISTS UQIX_{TableName}_DisplayName
            ON {TableName} (DisplayName)
            ";

            command.ExecuteNonQuery();
        }

        protected override async ValueTask<object> MakeInsertParameters(Province obj)
        {
            int regionId = await db.Regions.SelectIdOrInsert(obj.Region);

            return new
            {
                obj.Url,
                obj.DisplayName,
                RegionId = regionId
            };
        }

        public async Task<IEnumerable<Province>> SelectAllInRegion(Region region)
        {
            string sql =
            $@"
            SELECT
                p.Url, p.DisplayName
            FROM
                {TableName} as p
            INNER JOIN
                {db.Regions.TableName} as r
            ON
                p.RegionId = r.Id
            WHERE
                r.DisplayName = @DisplayName
            ";

            var dynamics = await db.Connection.QueryAsync(sql, region);

            return dynamics.Select(d => new Province(region, d.Url, d.DisplayName));
        }
    }
}
