using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharedTask
{
    public sealed class SharedQueuedTask<T> : IDisposable
    {
        public Task<T> GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }

    public sealed class SharedQueuedTask : IDisposable
    {
        public Task GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}