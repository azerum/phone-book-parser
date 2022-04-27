using System;
using System.Collections.Generic;
using System.Linq;

namespace Library
{
    public static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<R> SelectAsyncAndMerge<T, R>(
            this IAsyncEnumerable<T> values,
            Func<T, IAsyncEnumerable<R>> selector
        )
        {
            List<IAsyncEnumerable<R>> streams = new();

            await foreach (T v in values)
            {
                streams.Add(selector(v));
            }

            //We are forced to use Merge(params) overload, as
            //Merge(IEnumerable) overload doesn't exploit concurrency,
            //that is, it works similary to Concat()

            var results = AsyncEnumerableEx.Merge(streams.ToArray());

            await foreach (R r in results)
            {
                yield return r;
            }
        }
    }
}
