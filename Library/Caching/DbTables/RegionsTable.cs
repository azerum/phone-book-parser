using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching.DbTables
{
    public class RegionsTable : DbTable<Region>
    {
        public override string TableName => "Regions";

        public RegionsTable(CacheDb db, SqliteConnection connection)
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
                UNIQUE(Url)
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
        INSERT INTO {TableName} (Url, DisplayName)
        VALUES (@Url, @DisplayName)
        ";

        protected override ValueTask<object> MakeInsertParameters(Region obj)
        {
            return new ValueTask<object>(obj);
        }

        public async Task<IEnumerable<Region>> SelectAll(
            SqliteTransaction? transaction = null
        )
        {
            var dynamics = await connection.QueryAsync(
                $"SELECT Url, DisplayName FROM {TableName}",
                transaction
            );

            return dynamics.Select(d => new Region(d.Url, d.DisplayName));
        }
    }
}
