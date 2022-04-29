using System;
using System.Threading;
using System.Threading.Tasks;

namespace Library
{
    public abstract class CancellingDisposable : IDisposable, IAsyncDisposable
    {
        private readonly CancellationTokenSource source = new();

        protected CancellationToken LinkToDisposeToken(CancellationToken token)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(
                source.Token,
                token
            );

            return linked.Token;
        }

        public void Dispose()
        {
            source.Cancel();
            GC.SuppressFinalize(this);

            DoDispose();
        }

        public ValueTask DisposeAsync()
        {
            source.Cancel();
            GC.SuppressFinalize(this);

            return DoDisposeAsync();
        }

        protected abstract void DoDispose();
        protected abstract ValueTask DoDisposeAsync();
    }
}
