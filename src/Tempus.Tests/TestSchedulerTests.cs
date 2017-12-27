using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Tempus.Tests
{
    public class TestSchedulerTests
    {
        [Fact]
        public async Task WhenAdvancePeriodIsLessThanTimePeriodTimerShouldNotTick()
        {
            var testScheduler = new TestScheduler();

            int timerExecutionCounter = 0;

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                                                   (ct) =>
                                                   {
                                                       timerExecutionCounter++;
                                                       return Task.CompletedTask;
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       return Task.CompletedTask;
                                                   });

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(30));


            timerExecutionCounter.Should().Be(0);


            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task WhenAdvancePeriodIsABitGreaterThanTimePeriodTimerShouldTickOnce()
        {
            var testScheduler = new TestScheduler();

            int timerExecutionCounter = 0;

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                                                   (ct) =>
                                                   {
                                                       timerExecutionCounter++;
                                                       return Task.CompletedTask;
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       return Task.CompletedTask;
                                                   });

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(51));


            timerExecutionCounter.Should().Be(1);


            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task WhenAdvancePeriodIs11TimesGreaterThanTimePeriodTimerShouldTick11Times()
        {
            var testScheduler = new TestScheduler();

            int timerExecutionCounter = 0;

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                                                   (ct) =>
                                                   {
                                                       timerExecutionCounter++;
                                                       return Task.CompletedTask;
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       return Task.CompletedTask;
                                                   });

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(551));


            timerExecutionCounter.Should().Be(11);


            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task ShouldNotTickAfterCancel()
        {
            var testScheduler = new TestScheduler();

            int timerExecutionCounter = 0;

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                                                   (ct) =>
                                                   {
                                                       timerExecutionCounter++;
                                                       return Task.CompletedTask;
                                                   },
                                                   (ex, ct) =>
                                                   {
                                                       return Task.CompletedTask;
                                                   });

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(51));


            timerExecutionCounter.Should().Be(1);


            await scheduledTask.Cancel();

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(50));

            timerExecutionCounter.Should().Be(1);
        }
    }
}
