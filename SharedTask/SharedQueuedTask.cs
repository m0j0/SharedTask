using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharedTask
{
    public sealed class SharedQueuedTask<T> : IDisposable
    {
        public SharedQueuedTask(Func<CancellationToken, Task<T>> getTask)
        {
        }

        public SharedQueuedTask(Func<Task<T>> getTask) : this(token => getTask())
        {
        }

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
        public SharedQueuedTask(Func<CancellationToken, Task> getTask)
        {
        }

        public SharedQueuedTask(Func<Task> getTask)
        {
        }

        public Task GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}