using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching.DbTables
{
    public abstract class DbTable<TEntity> where TEntity : SearchScope
    {
        protected readonly CacheDb db;
        protected readonly string tableName;

        public DbTable(CacheDb db, string tableName)
        {
            this.db = db;
            this.tableName = tableName;
        }

        public async Task<int> SelectIdOrInsert(
            TEntity entity,
            CancellationToken cancellationToken = default
        )
        {
            string select =
            $@"
            SELECT Id
            FROM [{tableName}]
            WHERE DisplayName = @DisplayName
            ";

            var param = new
            {
                TableName = tableName,
                entity.DisplayName
            };

            int? maybeId =
                await db.Connection.ExecuteScalarAsync<int?>(select, param);

            cancellationToken.ThrowIfCancellationRequested();

            if (maybeId != null)
            {
                return (int)maybeId;
            }

            await Insert(entity);
            cancellationToken.ThrowIfCancellationRequested();

            return await db.Connection.ExecuteScalarAsync<int>(
                "SELECT last_insert_rowid()"
            );
        }

        protected abstract Task Insert(
            TEntity entity,
            SqliteTransaction? transaction = null
        );

        protected async Task DoForEachWithTransaction(
            IEnumerable<TEntity> entities,
            Func<TEntity, SqliteTransaction, Task> action,
            CancellationToken cancellationToken = default
        )
        {
            var transaction = db.Connection.BeginTransaction();

            try
            {
                foreach (TEntity e in entities)
                {
                    await action(e, transaction);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
        }
    }
}
