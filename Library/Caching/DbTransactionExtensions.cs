using System;
using System.Data;
using System.Threading.Tasks;

namespace Library.Caching
{
    internal static class DbConnectionExtensions
    {
        public static async Task InTransaction(
            this IDbConnection connection,
            Func<IDbTransaction, Task> action
        )
        {
            var transaction = connection.BeginTransaction();

            try
            {
                await action(transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
