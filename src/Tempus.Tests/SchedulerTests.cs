using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Tempus.Tests
{
    public class SchedulerTests
    {
        [Fact]
        public async Task ShouldTick()
        {
            var scheduler = new Scheduler();

            var autoResetEvent = new AutoResetEvent(false);

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                                                   (ct) =>
                                                   {
                                                       autoResetEvent.Set();
                                                       return Task.CompletedTask;
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       return Task.CompletedTask;
                                                   });

            var intTimerCounter = 0;

            while (intTimerCounter < 5)
            {
                var signaled = autoResetEvent.WaitOne(70);
                signaled.Should().BeTrue();
                intTimerCounter++;
            }

            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task ShouldTickAfterInitialDelayAndThenWithPeriod()
        {
            var scheduler = new Scheduler();

            var autoResetEvent = new AutoResetEvent(false);

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                                                   TimeSpan.FromMilliseconds(300),
                                                   (ct) =>
                                                   {
                                                       autoResetEvent.Set();
                                                       return Task.CompletedTask;
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       return Task.CompletedTask;
                                                   });

            var signaledInitialy = autoResetEvent.WaitOne(280);
            signaledInitialy.Should().BeFalse("Should not be ticking before initial delay");

            var intTimerCounter = 0;

            while (intTimerCounter < 5)
            {
                var signaled = autoResetEvent.WaitOne(70);
                signaled.Should().BeTrue();
                intTimerCounter++;
            }

            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task ShouldTickEvenAfterException()
        {
            var scheduler = new Scheduler();

            var autoResetEvent = new AutoResetEvent(false);

            Task Throwing() => throw new InvalidOperationException("Boom");

            Exception catchedException = null;

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                                                   (ct) =>
                                                   {
                                                       return Throwing();
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       catchedException = ex;
                                                       autoResetEvent.Set();
                                                       return Task.CompletedTask;
                                                   });

            var intTimerCounter = 0;

            while (intTimerCounter < 5)
            {
                var signaled = autoResetEvent.WaitOne(100);
                signaled.Should().BeTrue();

                catchedException.Should().BeOfType<InvalidOperationException>();

                intTimerCounter++;
            }

            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task ShouldTickEvenAfterExceptionInsideExceptionHandler()
        {
            var scheduler = new Scheduler();

            var autoResetEvent = new AutoResetEvent(false);

            Task Throwing() => throw new InvalidOperationException("Boom");

            Exception catchedException = null;

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                                                   (ct) =>
                                                   {
                                                       return Throwing();
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       catchedException = ex;
                                                       autoResetEvent.Set();
                                                       return Throwing();
                                                   });

            var intTimerCounter = 0;

            while (intTimerCounter < 5)
            {
                var signaled = autoResetEvent.WaitOne(100);
                signaled.Should().BeTrue();

                catchedException.Should().BeOfType<InvalidOperationException>();

                intTimerCounter++;
            }

            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task ShouldNotTickAfterCancel()
        {
            var scheduler = new Scheduler();

            var autoResetEvent = new AutoResetEvent(false);

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                                                   (ct) =>
                                                   {
                                                       autoResetEvent.Set();
                                                       return Task.CompletedTask;
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       return Task.CompletedTask;
                                                   });

            var intTimerCounter = 0;

            while (intTimerCounter < 5)
            {
                var signaled = autoResetEvent.WaitOne(70);
                signaled.Should().BeTrue();
                intTimerCounter++;
            }

            await scheduledTask.Cancel();

            var signaledAfterCancel = autoResetEvent.WaitOne(70);
            signaledAfterCancel.Should().BeFalse();
        }

        [Fact]
        public async Task CancelShouldWaitIfActionIsInProgress()
        {
            var scheduler = new Scheduler();

            var autoResetEvent = new AutoResetEvent(false);

            var timerNotifiedForCancellation = new TaskCompletionSource<bool>();

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                                                   async (ct) =>
                                                   {
                                                       autoResetEvent.Set();
                                                       await timerNotifiedForCancellation.Task;
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       return Task.CompletedTask;
                                                   });

            var signaled = autoResetEvent.WaitOne(70);
            signaled.Should().BeTrue();

            var actionTask = scheduledTask.Cancel();

            await Task.Delay(500);

            actionTask.IsCompleted.Should().BeFalse();

            timerNotifiedForCancellation.SetResult(true);

            await actionTask;
        }
    }
}
