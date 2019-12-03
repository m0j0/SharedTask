using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SharedTask.Tests
{
    [TestFixture]
    public class SharedTaskTests
    {
        private const int ExpectedResult = 100;
        private const int Delay = 500;
        private const int SmallDelay = 50;

        private SharedTask<int> _task;
        private int _started;
        private int _finished;

        [SetUp]
        public void SetUp()
        {
            _started = 0;
            _finished = 0;
            _task = new SharedTask<int>(GetNumberAsync);
        }

        [TearDown]
        public void TearDown()
        {
            _task.Dispose();
        }

        [Test]
        [Repeat(10)]
        public async Task SingleTaskTest()
        {
            var task1 = _task.GetOrCreateAsync();
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_started, Is.EqualTo(1));
            Assert.That(_finished, Is.EqualTo(1));
            Assert.That(task1.Result, Is.EqualTo(ExpectedResult));
            Assert.That(task2.Result, Is.EqualTo(ExpectedResult));

            await Task.Delay(1);
            Assert.That(_task.IsRunning(), Is.True);
        }

        [Test]
        [Repeat(10)]
        public async Task SingleTaskWithDelayTest()
        {
            var task1 = _task.GetOrCreateAsync();
            await Task.Delay(SmallDelay);
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_started, Is.EqualTo(1));
            Assert.That(_finished, Is.EqualTo(1));
            Assert.That(task1.Result, Is.EqualTo(ExpectedResult));
            Assert.That(task2.Result, Is.EqualTo(ExpectedResult));

            await Task.Delay(1);
            Assert.That(_task.IsRunning(), Is.True);
        }

        [Test]
        [Repeat(10)]
        public async Task MultipleTasksTest()
        {
            var task = _task.GetOrCreateAsync();

            var tasks = new List<Task<int>>();
            for (int i = 0; i < 5; i++)
            {
                var task2 = _task.GetOrCreateAsync();
                tasks.Add(task2);
            }

            await task;
            await Task.WhenAll(tasks);

            Assert.That(_started, Is.EqualTo(1));
            Assert.That(_finished, Is.EqualTo(1));
            Assert.That(task.Result, Is.EqualTo(ExpectedResult));
            foreach (var task2 in tasks)
            {
                Assert.That(task2.Result, Is.EqualTo(ExpectedResult));
            }

            await Task.Delay(1);
            Assert.That(_task.IsRunning(), Is.True);
        }

        [Test]
        [Repeat(10)]
        public async Task MultipleTasksWithDelayTest()
        {
            var task = _task.GetOrCreateAsync();
            await Task.Delay(Delay - 2 * SmallDelay);

            var tasks = new List<Task<int>>();
            for (int i = 0; i < 10; i++)
            {
                var task2 = _task.GetOrCreateAsync();
                tasks.Add(task2);
            }

            await task;
            await Task.WhenAll(tasks);

            Assert.That(_started, Is.EqualTo(1));
            Assert.That(_finished, Is.EqualTo(1));
            Assert.That(task.Result, Is.EqualTo(ExpectedResult));
            foreach (var task2 in tasks)
            {
                Assert.That(task2.Result, Is.EqualTo(ExpectedResult));
            }

            await Task.Delay(1);
            Assert.That(_task.IsRunning(), Is.True);
        }

        [Test]
        [Repeat(10)]
        public async Task DifferentTasksTest()
        {
            var task1 = _task.GetOrCreateAsync();
            await Task.Delay(Delay + SmallDelay);
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_started, Is.EqualTo(2));
            Assert.That(_finished, Is.EqualTo(2));
            Assert.That(task1.Result, Is.EqualTo(ExpectedResult));
            Assert.That(task2.Result, Is.EqualTo(ExpectedResult + 1));

            await Task.Delay(1);
            Assert.That(_task.IsRunning(), Is.True);
        }

        [Test]
        [Repeat(10)]
        public async Task NullifyingFieldTest()
        {
            Assert.That(_task.IsRunning(), Is.True);

            var task = _task.GetOrCreateAsync();

            Assert.That(_task.IsRunning(), Is.False);

            await Task.Delay(Delay / 2);

            Assert.That(_task.IsRunning(), Is.False);

            await Task.Delay(Delay);

            Assert.That(_task.IsRunning(), Is.True);
        }

        [Test]
        [Repeat(10)]
        public async Task CancelTaskTest()
        {
            Task<int> task = null;
            Exception exception = null;

            try
            {
                var cancellationTokenSource = new CancellationTokenSource(Delay / 2);

                task = _task.GetOrCreateAsync(cancellationTokenSource.Token);
                await task;
            }
            catch (OperationCanceledException e)
            {
                exception = e;
            }

            Assert.That(_started, Is.EqualTo(1));
            Assert.That(_finished, Is.EqualTo(0));
            Assert.That(task.IsCanceled, Is.True);
            Assert.That(exception, Is.Not.Null);

            await Task.Delay(1);
            Assert.That(_task.IsRunning(), Is.True);
        }
        
        [Test]
        [Repeat(10)]
        public async Task TwoTasksCancelTest()
        {
            Task<int> task1 = null;
            Task<int> task2 = null;
            Exception exception = null;

            try
            {
                var cancellationTokenSource1 = new CancellationTokenSource(Delay / 2);
                task1 = _task.GetOrCreateAsync(cancellationTokenSource1.Token);

                var cancellationTokenSource2 = new CancellationTokenSource(Delay / 2);
                task2 = _task.GetOrCreateAsync(cancellationTokenSource2.Token);

                await Task.WhenAll(task1, task2);
            }
            catch (OperationCanceledException e)
            {
                exception = e;
            }

            Assert.That(_started, Is.EqualTo(1));
            Assert.That(_finished, Is.EqualTo(0));
            Assert.That(task1.IsCanceled, Is.True);
            Assert.That(task2.IsCanceled, Is.True);
            Assert.That(exception, Is.Not.Null);

            await Task.Delay(1);
            Assert.That(_task.IsRunning(), Is.True);
        }

        [Test]
        [Repeat(10)]
        public async Task TwoTasksDoNotCancelTest()
        {
            Task<int> task1 = null;
            Task<int> task2 = null;
            Exception exception = null;

            try
            {
                var cancellationTokenSource = new CancellationTokenSource(Delay / 2);
                task1 = _task.GetOrCreateAsync(cancellationTokenSource.Token);
                task2 = _task.GetOrCreateAsync();

                await Task.WhenAll(task1, task2);
            }
            catch (OperationCanceledException e)
            {
                exception = e;
            }

            Assert.That(_started, Is.EqualTo(1));
            Assert.That(_finished, Is.EqualTo(1));
            Assert.That(task1.Result, Is.EqualTo(ExpectedResult));
            Assert.That(task2.Result, Is.EqualTo(ExpectedResult));
            Assert.That(exception, Is.Null);

            await Task.Delay(1);
            Assert.That(_task.IsRunning(), Is.True);
        }

        private async Task<int> GetNumberAsync(CancellationToken cancellationToken)
        {
            _started++;
            await Task.Delay(Delay, cancellationToken);
            _finished++;
            return ExpectedResult + (_finished - 1);
        }
    }
}