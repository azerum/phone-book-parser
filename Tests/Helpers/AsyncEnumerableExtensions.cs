using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Helpers
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task Consume<T>(this IAsyncEnumerable<T> values)
        {
            await foreach (T v in values)
            {
                _ = v;
            }
        }
    }
}
