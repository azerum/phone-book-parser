using System;
using System.Threading;
using System.Threading.Tasks;

namespace Library
{
    public abstract class DisposableWithCancellation : IDisposable, IAsyncDisposable
    {
        private readonly CancellationTokenSource source = new();
        protected CancellationToken DisposeToken => source.Token;

        public void Dispose()
        {
            source.Cancel();
            DoDispose();
        }

        public ValueTask DisposeAsync()
        {
            source.Cancel();
            return DoDisposeAsync();
        }

        protected abstract void DoDispose();
        protected abstract ValueTask DoDisposeAsync();

        protected CancellationToken LinkToDisposeToken(CancellationToken token)
        {
            var s = CancellationTokenSource
                .CreateLinkedTokenSource(source.Token, token);

            return s.Token;
        }
    }
}
