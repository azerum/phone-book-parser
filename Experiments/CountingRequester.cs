using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Io;
using AngleSharp.Io.Network;

namespace Experiments
{
    public class CountingRequester : HttpClientRequester 
    {
        private int requestsCount;
        public int RequestsCount => requestsCount;

        protected override async Task<IResponse> PerformRequestAsync(
            Request request,
            CancellationToken cancel
            )
        {
            try
            {
                return await base.PerformRequestAsync(request, cancel);
            }
            finally
            {
                Interlocked.Increment(ref requestsCount);
            }
        }
    }
}
