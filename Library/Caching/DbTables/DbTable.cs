﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching.DbTables
{
    public abstract class DbTable<T> where T : SearchScope
    {
        public abstract string TableName { get; }
        protected abstract string InsertSql { get; }

        protected readonly CacheDb db;
        protected readonly SqliteConnection connection;

        public DbTable(CacheDb db, SqliteConnection connection)
        {
            this.db = db;
            this.connection = connection;
        }

        public abstract void EnsureIsCreated(
            SqliteTransaction? transaction = null
        );

        public async Task<int> SelectIdOrInsert(
            T obj,
            SqliteTransaction? transaction = null
        )
        {
            string select =
            $@"
            SELECT Id
            FROM {TableName}
            WHERE DisplayName = @DisplayName
            ";

            int? id = await connection.ExecuteScalarAsync<int?>(
                select,
                new { obj.DisplayName },
                transaction
            );

            if (id.HasValue)
            {
                return id.Value;
            }

            string insertAndGetId =
            $@"
            {InsertSql};
            SELECT last_insert_rowid();
            ";

            var param = await MakeInsertParameters(obj);

            return await connection.ExecuteScalarAsync<int>(
                insertAndGetId,
                param,
                transaction
            );
        }

        public async Task Insert(T obj, SqliteTransaction? transaction = null)
        {
            var param = await MakeInsertParameters(obj);

            _ = await connection.ExecuteAsync(
                InsertSql,
                param,
                transaction
            );
        }

        public async Task InsertMany(
            IEnumerable<T> objects,
            CancellationToken cancellationToken = default
        )
        {
            var transaction = connection.BeginTransaction();

            try
            {
                foreach (T obj in objects)
                {
                    await Insert(obj, transaction);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        protected abstract ValueTask<object> MakeInsertParameters(T obj);
    }
}
