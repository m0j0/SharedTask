using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SharedTask.Tests
{
    [TestFixture]
    public class NonGenericSharedTaskTests
    {
        private const int Delay = 200;
        private const int SmallDelay = 50;

        private SharedTask _task;
        private int _started;
        private int _finished;

        [SetUp]
        public void SetUp()
        {
            _started = 0;
            _finished = 0;
            _task = new SharedTask(RunAsync);
        }

        [TearDown]
        public void TearDown()
        {
            _task.Dispose();
        }

        [Test]
        public async Task SingleTaskTest()
        {
            var task1 = _task.GetOrCreateAsync();
            var task2 = _task.GetOrCreateAsync();

            await Task.WhenAll(task1, task2);

            Assert.That(_started, Is.EqualTo(1));
            Assert.That(_finished, Is.EqualTo(1));
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            _started++;
            await Task.Delay(Delay, cancellationToken);
            _finished++;
        }
    }
}