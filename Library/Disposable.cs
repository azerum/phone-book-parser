using System;
using System.Threading.Tasks;

namespace Library
{
    public abstract class Disposable : IDisposable
    {
        protected bool Disposed { get; private set; }

        //https://codecrafter.blogspot.com/2010/01/better-idisposable-pattern.html
        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;

            CleanUpManagedResources();
            CleanUpNativeResources();

            GC.SuppressFinalize(this);
        }

        protected void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("this");
            }
        }

        protected virtual void CleanUpManagedResources() { }
        protected virtual void CleanUpNativeResources() { }
    }
}
