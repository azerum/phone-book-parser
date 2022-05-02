using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public static class TransactionExtensions
    {
        public static async Task RollbackOnException(
            this SqliteTransaction transaction,
            Func<Task> action
        )
        {
            try
            {
                await action();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
