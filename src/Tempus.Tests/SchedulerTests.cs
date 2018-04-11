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
                                                   ct =>
                                                   {
                                                       autoResetEvent.Set();
                                                       return Task.CompletedTask;
                                                   },
                                                   (failureContext, ct) => Task.CompletedTask);

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

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(300),
                                                   TimeSpan.FromMilliseconds(50),
                                                   ct =>
                                                   {
                                                       autoResetEvent.Set();
                                                       return Task.CompletedTask;
                                                   },
                                                   (failureContext, ct) => Task.CompletedTask);

            var signaledInitially = autoResetEvent.WaitOne(280);
            signaledInitially.Should().BeFalse("Should not be ticking before initial delay");

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

            IFailureContext actualFailureContext = null;

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                                                   ct => Throwing(),
                                                   (failureContext, ct) =>
                                                   {
                                                       actualFailureContext = failureContext;
                                                       autoResetEvent.Set();
                                                       return Task.CompletedTask;
                                                   });

            var intTimerCounter = 0;

            while (intTimerCounter < 5)
            {
                var signaled = autoResetEvent.WaitOne(100);
                signaled.Should().BeTrue();

                actualFailureContext.Exception.Should().BeOfType<InvalidOperationException>();

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

            IFailureContext actualFailureContext = null;

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                                                   ct => Throwing(),
                                                   (failureContext, ct) =>
                                                   {
                                                       actualFailureContext = failureContext;
                                                       autoResetEvent.Set();
                                                       return Throwing();
                                                   });

            var intTimerCounter = 0;

            while (intTimerCounter < 5)
            {
                var signaled = autoResetEvent.WaitOne(100);
                signaled.Should().BeTrue();

                actualFailureContext.Exception.Should().BeOfType<InvalidOperationException>();

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
                                                   ct =>
                                                   {
                                                       autoResetEvent.Set();
                                                       return Task.CompletedTask;
                                                   },
                                                   (failureContext, ct) => Task.CompletedTask);

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
                                                   async ct =>
                                                   {
                                                       autoResetEvent.Set();
                                                       await timerNotifiedForCancellation.Task;
                                                   },
                                                   (failureContext, ct) => Task.CompletedTask);

            var signaled = autoResetEvent.WaitOne(70);
            signaled.Should().BeTrue();

            var actionTask = scheduledTask.Cancel();

            await Task.Delay(500);

            actionTask.IsCompleted.Should().BeFalse();

            timerNotifiedForCancellation.SetResult(true);

            await actionTask;
        }

        [Fact]
        public async Task ShouldBackoffAfterException()
        {
            var scheduler = new Scheduler();

            Task Throwing() => throw new InvalidOperationException("Boom");

            IFailureContext actualFailureContext = null;

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                ct => Throwing(),
                (failureContext, ct) =>
                {
                    actualFailureContext = failureContext;
                    return Task.CompletedTask;
                }, TimeSpan.FromMilliseconds(1000));

            await Task.Delay(50 + 50 + 100 + 200 + 400 + 800 + 1000 + 1000);
            
            await scheduledTask.Cancel();
            
            // assert
            
            actualFailureContext.FailCount.Should().Be(7);
            actualFailureContext.Period.Should().Be(TimeSpan.FromMilliseconds(50));
            actualFailureContext.MaxPeriod.Should().Be(TimeSpan.FromMilliseconds(1000));
            actualFailureContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(1000));
        }
        
        [Fact]
        public async Task ShouldTickWithNormalPeriodAfterExceptionResolved()
        {
            var scheduler = new Scheduler();
            
            var shouldThrow = true;

            var counter = 0;

            Task Throwing()
            {
                if (shouldThrow)
                {
                    throw new InvalidOperationException("Boom");
                }
                
                counter++;

                return Task.CompletedTask;
            }

            var scheduledTask = scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                ct => Throwing(),
                (failureContext, ct) => Task.CompletedTask, 
                TimeSpan.FromMilliseconds(1000));

            await Task.Delay(50 + 50 + 100 + 200 + 400 + 800 + 1000);

            shouldThrow = false;
            
            await Task.Delay(1100);
            
            await scheduledTask.Cancel();
            
            counter.Should().BeGreaterOrEqualTo(15);
        }
    }
}
