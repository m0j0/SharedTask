using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SharedTask.Tests
{
    [TestFixture]
    public class UpdateServiceTests
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

        [Test]
        [Repeat(10)]
        public async Task TaskCachingSingleTaskTest()
        {
            var task1 = _task.GetOrCreateAsync();
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_started, Is.EqualTo(1));
            Assert.That(_finished, Is.EqualTo(1));
            Assert.That(task1.Result, Is.EqualTo(ExpectedResult));
            Assert.That(task2.Result, Is.EqualTo(ExpectedResult));
        }

        [Test]
        [Repeat(10)]
        public async Task TaskCachingSingleTaskWithDelayTest()
        {
            var task1 = _task.GetOrCreateAsync();
            await Task.Delay(SmallDelay);
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_started, Is.EqualTo(1));
        }

        [Test]
        [Repeat(10)]
        public async Task TaskCachingMultipleTasksTest()
        {
            var task = _task.GetOrCreateAsync();

            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                var task2 = _task.GetOrCreateAsync();
                tasks.Add(task2);
            }

            await task;
            await Task.WhenAll(tasks);

            Assert.That(_started, Is.EqualTo(1));
        }

        [Test]
        [Repeat(10)]
        public async Task TaskCachingMultipleTasksWithDelayTest()
        {
            var task = _task.GetOrCreateAsync();
            await Task.Delay(Delay - 2 * SmallDelay);

            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var task2 = _task.GetOrCreateAsync();
                tasks.Add(task2);
            }

            await task;
            await Task.WhenAll(tasks);

            Assert.That(_started, Is.EqualTo(1));
        }

        [Test]
        [Repeat(10)]
        public async Task TaskCachingDifferentTasksTest()
        {
            var task1 = _task.GetOrCreateAsync();
            await Task.Delay(Delay + SmallDelay);
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_started, Is.EqualTo(2));
        }

        [Test]
        [Repeat(10)]
        public async Task TaskCachingNullifyingFieldTest()
        {
            Assert.That(_task.IsStateEmpty(), Is.True);
            var task = _task.GetOrCreateAsync();

            Assert.That(_task.IsStateEmpty(), Is.False);

            await Task.Delay(Delay / 2);

            Assert.That(_task.IsStateEmpty(), Is.False);

            await Task.Delay(Delay);

            Assert.That(_task.IsStateEmpty(), Is.True);
        }

        private async Task<int> GetNumberAsync(CancellationToken cancellationToken)
        {
            _started++;
            await Task.Delay(Delay, cancellationToken);
            _finished++;
            return ExpectedResult;
        }
    }
}