using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharedTask
{
    public sealed class SharedQueuedTask<T> : IDisposable
    {
        private readonly object _lock = new object();

        private Task<T> _currentTask;
        private SharedTask<T> _nextTask;

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
        private readonly SharedQueuedTask<object> _sharedQueuedTask;

        public SharedQueuedTask(Func<CancellationToken, Task> getTask)
        {
            _sharedQueuedTask = new SharedQueuedTask<object>(token =>
            {
                 getTask(token);
                 return null;
            });
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