using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedTask
{
    public sealed class SharedTask
    {
        private readonly Func<CancellationToken, Task> _getTask;
        private readonly object _lock = new object();
        
        private Task _task;

        public SharedTask(Func<CancellationToken, Task> getTask)
        {
            _getTask = getTask;
        }

        public Task GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_task == null ||
                    _task.IsCompleted)
                {
                    _task = GetTaskInternalAsync(default);
                    _task.ContinueWith(task =>
                    {
                        lock (_lock)
                        {
                            _task = null;
                        }
                    });
                }

                return _task;
            }
        }

        private async Task GetTaskInternalAsync(CancellationToken cancellationToken)
        {
            // to execute lock scope very fast
            await Task.Yield();

            await _getTask(cancellationToken);
        }
    }
}