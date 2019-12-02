using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedTask
{
    public sealed class SharedTask<T>
    {
        private readonly Func<CancellationToken, Task<T>> _getTask;
        private readonly object _lock = new object();
        private readonly Action<Task, object> _nullifyContinuation;
        
        private Task<T> _task;

        public SharedTask(Func<CancellationToken, Task<T>> getTask)
        {
            _getTask = getTask;
            _nullifyContinuation = Nullify;
        }

        public Task<T> GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_task == null ||
                    _task.IsCompleted)
                {
                    _task = GetTaskInternalAsync(default);
                    _task.ContinueWith(_nullifyContinuation, _task, cancellationToken);
                }

                return _task;
            }
        }

        internal Task<T> GetStateAsync()
        {
            lock (_lock)
            {
                return _task;
            }
        }

        private void Nullify(Task task, object state)
        {
            var originalValue = (Task) state;
            var fieldValue = _task;

            if (originalValue != fieldValue)
            {
                return;
            }

            lock (_lock)
            {
                if (originalValue != _task)
                {
                    return;
                }

                _task = null;
            }
        }

        private async Task<T> GetTaskInternalAsync(CancellationToken cancellationToken)
        {
            // to execute lock scope very fast
            await Task.Yield();

            return await _getTask(cancellationToken);
        }
    }
}