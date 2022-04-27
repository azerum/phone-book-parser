using System;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Io;
using AngleSharp.Io.Network;

namespace Library.Parsing
{
    public class RateLimitingRequester : HttpClientRequester
    {
        private readonly SemaphoreSlim semaphore;
        private readonly TimeSpan intervalBetweenRequests;

        public RateLimitingRequester(TimeSpan intervalBetweenRequests)
        {
            semaphore = new(1, 1);
            this.intervalBetweenRequests = intervalBetweenRequests;
        }

        protected override async Task<IResponse> PerformRequestAsync(
            Request request,
            CancellationToken cancel
        )
        {
            await semaphore.WaitAsync(cancel);

            try
            {
                await Task.Delay(intervalBetweenRequests, cancel);
                return await base.PerformRequestAsync(request, cancel);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
