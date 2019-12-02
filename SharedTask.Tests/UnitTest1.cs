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

        private SharedTask _task;
        private int _called;

        [SetUp]
        public void SetUp()
        {
            _called = 0;
            _task = new SharedTask(GetNumberAsync);
        }

        [Test]
        [Repeat(10)]
        public async Task TaskCachingSingleTaskTest()
        {
            var task1 = _task.GetOrCreateAsync();
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_called, Is.EqualTo(1));
        }

        [Test]
        [Repeat(10)]
        public async Task TaskCachingSingleTaskWithDelayTest()
        {
            var task1 = _task.GetOrCreateAsync();
            await Task.Delay(SmallDelay);
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_called, Is.EqualTo(1));
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

            Assert.That(_called, Is.EqualTo(1));
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

            Assert.That(_called, Is.EqualTo(1));
        }

        [Test]
        [Repeat(10)]
        public async Task TaskCachingDifferentTasksTest()
        {
            var task1 = _task.GetOrCreateAsync();
            await Task.Delay(Delay + SmallDelay);
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_called, Is.EqualTo(2));
        }


        //[Test]
        //[Repeat(10)]
        //public async Task TaskCachingNullifyingFieldTest()
        //{
        //    Assert.That(_task, Is.Null);
        //    var task = UpdateService.GetOrCreateTask(ref _task, () => GetNumberWithDelayAsync(100, 500), NullifyFieldTest);

        //    Assert.That(_task, Is.Not.Null);

        //    await Task.Delay(1000);

        //    Assert.That(_task, Is.Null);
        //}

        private async Task<int> GetNumberAsync(CancellationToken cancellationToken)
        {
            _called++;
            await Task.Delay(Delay, cancellationToken);
            return ExpectedResult;
        }
    }
}