using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching.DbTables
{
    public class CitiesTable : DbTable<City>
    {
        public override string TableName => "Cities";

        public CitiesTable(CacheDb db, SqliteConnection connection)
            : base(db, connection) { }

        public override void EnsureIsCreated(
            SqliteTransaction? transaction = null
        )
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
            $@"
            CREATE TABLE IF NOT EXISTS {TableName}(
                Id INTEGER PRIMARY KEY,
                Url TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                ProvinceId INT NOT NULL,
                UNIQUE(Url),
                FOREIGN KEY (ProvinceId) REFERENCES {db.Provinces.TableName} (Id)
            )
            ";

            command.ExecuteNonQuery();

            command.CommandText =
            $@"
            CREATE UNIQUE INDEX IF NOT EXISTS UQIX_{TableName}_DisplayName
            ON {TableName} (DisplayName)
            ";

            command.ExecuteNonQuery();
        }

        protected override string InsertSql =>
        $@"
        INSERT INTO {TableName} (Url, DisplayName, ProvinceId)
        VALUES (@Url, @DisplayName, @ProvinceId)
        ";

        protected override async ValueTask<object> MakeInsertParameters(City obj)
        {
            int provinceId = await db.Provinces.SelectIdOrInsert(obj.Province);

            return new
            {
                obj.Url,
                obj.DisplayName,
                ProvinceId = provinceId
            };
        }

        public async Task<IEnumerable<City>> SelectAll()
        {
            string sql =
            $@"
            SELECT
                c.Url as CUrl,
                c.DisplayName as CDisplayName,
                p.Url as PUrl,
                p.DisplayName  as PDisplayName,
                r.Url as RUrl,
                r.DisplayName as RDisplayName
            FROM
                {TableName} as c
            INNER JOIN
                {db.Provinces.TableName} as p
            ON
                c.ProvinceId = p.Id
            INNER JOIN
                {db.Regions.TableName} as r
            ON
                p.RegionId = r.Id
            ";

            var dynamics = await connection.QueryAsync(sql);

            return dynamics.Select(d =>
            {
                Region region = new(d.RUrl, d.RDisplayName);
                Province province = new(region, d.PUrl, d.PDisplayName);
                City city = new(province, d.CUrl, d.CDisplayName);

                return city;
            });
        }

        public async Task<IEnumerable<City>> SelectAllInRegion(Region region)
        {
            string sql =
            $@"
            SELECT
                c.Url as CUrl,
                c.DisplayName as CDisplayName,
                p.Url as PUrl,
                p.DisplayName  as PDisplayName,
                r.Url as RUrl,
                r.DisplayName as RDisplayName
            FROM
                {TableName} as c
            INNER JOIN
                {db.Provinces.TableName} as p
            ON
                c.ProvinceId = p.Id
            INNER JOIN
                {db.Regions.TableName} as r
            ON
                p.RegionId = r.Id
            WHERE
                r.DisplayName = @DisplayName
            ";

            var dynamics = await connection.QueryAsync(sql, region);

            return dynamics.Select(d =>
            {
                Region region = new(d.RUrl, d.RDisplayName);
                Province province = new(region, d.PUrl, d.PDisplayName);
                City city = new(province, d.CUrl, d.CDisplayName);

                return city;
            });
        }

        public async Task<IEnumerable<City>> SelectAllInProvince(Province province)
        {
            string sql =
            $@"
            SELECT
                c.Url, c.DisplayName
            FROM
                {TableName} as c
            INNER JOIN
                {db.Provinces.TableName} as p
            ON
                c.ProvinceId = p.Id
            WHERE
                p.DisplayName = @DisplayName
            ";

            var dynamics = await connection.QueryAsync(sql, province);

            return dynamics.Select(d => new City(province, d.Url, d.DisplayName));
        }
    }
}
