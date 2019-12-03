using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedTask
{
    public sealed class SharedTask<T> : IDisposable
    {
        private readonly Func<CancellationToken, Task<T>> _getTask;
        private readonly object _lock = new object();
        private readonly Action<Task, object> _nullifyContinuation;

        private volatile Task<T> _task;
        private CancellationTokenSource _cancellationTokenSource;
        private List<CancellationToken> _cancellationTokens = new List<CancellationToken>();

        public SharedTask(Func<CancellationToken, Task<T>> getTask)
        {
            _getTask = getTask;
            _nullifyContinuation = Nullify;
        }

        public Task<T> GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            /*var task = _task;
            if (task != null &&
                !task.IsCompleted)
            {
                return task;
            }*/

            lock (_lock)
            {
                if (_task == null ||
                    _task.IsCompleted)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();

                    _task = GetTaskInternalAsync(_cancellationTokenSource.Token);
                    _task.ContinueWith(_nullifyContinuation, _task, _cancellationTokenSource.Token);
                }

                //if (cancellationToken.CanBeCanceled)
                {
                    _cancellationTokens.Add(cancellationToken);
                    cancellationToken.Register(Cancel, cancellationToken);
                }

                return _task;
            }
        }

        private void Cancel(object state)
        {
            var cancellationToken = (CancellationToken) state;
            _cancellationTokens.Remove(cancellationToken);
            if (_cancellationTokens.Count == 0)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        internal bool IsStateEmpty()
        {
            return _task == null;
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

        public void Dispose()
        {
            
        }
    }
}