using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<IEnumerable<T>> ToEnumerableAsync<T>(
            this IAsyncEnumerable<T> asyncEnumerable
        )
        {
            List<T> values = new();

            await foreach (T v in asyncEnumerable)
            {
                values.Add(v);
            }

            return values;
        }
    }
}
