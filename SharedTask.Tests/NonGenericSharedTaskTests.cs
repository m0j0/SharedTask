using System;
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

        [Test]
        public async Task SingleTaskTest()
        {
            int started = 0;
            int finished = 0;

            using (var task = new SharedTask(RunAsync))
            {
                var task1 = task.GetOrCreateAsync();
                var task2 = task.GetOrCreateAsync();

                await Task.WhenAll(task1, task2);

                Assert.That(started, Is.EqualTo(1));
                Assert.That(finished, Is.EqualTo(1));
            }
            
            async Task RunAsync()
            {
                started++;
                await Task.Delay(Delay);
                finished++;
            }
        }

        [Test]
        public async Task CancelTaskTest()
        {
            int started = 0;
            int finished = 0;

            using (var task = new SharedTask(RunAsync))
            using (var cancellationTokenSource = new CancellationTokenSource(SmallDelay))
            {
                var task1 = task.GetOrCreateAsync(cancellationTokenSource.Token);
                var task2 = task.GetOrCreateAsync();

                await Task.WhenAll(task1, task2);

                Assert.That(started, Is.EqualTo(1));
                Assert.That(finished, Is.EqualTo(1));
            }
            
            async Task RunAsync(CancellationToken cancellationToken)
            {
                started++;
                await Task.Delay(Delay, cancellationToken);
                finished++;
            }
        }
    }
}