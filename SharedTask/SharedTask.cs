using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedTask
{
    public sealed class SharedTask<T> : IDisposable
    {
        //
        private readonly object _lock = new object();
        //

        private readonly Func<CancellationToken, Task<T>> _getTask;
        private readonly Action<Task<T>> _nullifyContinuation;
        private readonly Action<object> _cancelCallback;
        private readonly List<CancellationToken> _cancellationTokens;
        private readonly List<CancellationTokenRegistration> _cancellationTokenRegistrations;

        private Task<T> _task;
        private CancellationTokenSource _cancellationTokenSource;

        public SharedTask(Func<CancellationToken, Task<T>> getTask)
        {
            _getTask = getTask;
            _nullifyContinuation = Nullify;
            _cancelCallback = Cancel;
            _cancellationTokens = new List<CancellationToken>();
            _cancellationTokenRegistrations = new List<CancellationTokenRegistration>();
        }

        public Task<T> GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_task != null &&
                    _task.IsCompleted)
                {
                    Clean();
                }

                if (_task == null ||
                    _task.IsCompleted)
                {

                    _cancellationTokenSource = new CancellationTokenSource();
                    _task = GetTaskInternalAsync(_cancellationTokenSource.Token);
                    _task.ContinueWith(_nullifyContinuation, _cancellationTokenSource.Token);
                }
                
                _cancellationTokens.Add(cancellationToken);
                _cancellationTokenRegistrations.Add(cancellationToken.Register(_cancelCallback, cancellationToken));

                return _task;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                Clean();
            }
            
        }

        internal bool IsStateEmpty()
        {
            return _task == null;
        }

        private void Cancel(object state)
        {
            lock (_lock)
            {
                var cancellationToken = (CancellationToken) state;
                _cancellationTokens.Remove(cancellationToken);

                if (_cancellationTokens.Count == 0)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }

        private void Nullify(Task task)
        {
            lock (_lock)
            {
                if (task != _task)
                {
                    return;
                }

                Clean();
            }
        }

        private void Clean()
        {
            _task?.Dispose();
            _task = null;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _cancellationTokens.Clear();

            foreach (var cancellationTokenRegistration in _cancellationTokenRegistrations)
            {
                cancellationTokenRegistration.Dispose();
            }
            _cancellationTokenRegistrations.Clear();
        }

        private async Task<T> GetTaskInternalAsync(CancellationToken cancellationToken)
        {
            // to execute lock scope very fast
            await Task.Yield();

            return await _getTask(cancellationToken);
        }
    }
}