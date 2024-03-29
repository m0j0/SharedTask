﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharedTask
{
    public sealed class SharedTask<T> : IDisposable
    {
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

        public SharedTask(Func<Task<T>> getTask) : this(token => getTask())
        {
        }

        public Task<T> GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            lock (_nullifyContinuation)
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
            lock (_nullifyContinuation)
            {
                Clean();
            }
        }

        public bool IsRunning()
        {
            lock (_nullifyContinuation)
            {
                return _task != null;
            }
        }

        private void Cancel(object state)
        {
            lock (_nullifyContinuation)
            {
                var cancellationToken = (CancellationToken) state;
                _cancellationTokens.Remove(cancellationToken);

                if (_cancellationTokens.Count == 0)
                {
                    _cancellationTokenSource.Cancel();
                    Clean();
                }
            }
        }

        private void Nullify(Task<T> task)
        {
            lock (_nullifyContinuation)
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

    public sealed class SharedTask : IDisposable
    {
        private static readonly object EmptyObject = new object();

        private readonly SharedTask<object> _sharedTask;

        public SharedTask(Func<CancellationToken, Task> getTask)
        {
            _sharedTask = new SharedTask<object>(async token =>
            {
                await getTask(token);
                return EmptyObject;
            });
        }

        public SharedTask(Func<Task> getTask)
        {
            _sharedTask = new SharedTask<object>(async token =>
            {
                await getTask();
                return EmptyObject;
            });
        }

        public Task GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            return _sharedTask.GetOrCreateAsync(cancellationToken);
        }

        public void Dispose()
        {
            _sharedTask.Dispose();
        }
    }
}