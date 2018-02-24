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

            var timerExecutionCounter = 0;

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                                                   ct =>
                                                   {
                                                       timerExecutionCounter++;
                                                       return Task.CompletedTask;
                                                   },
                                                   (failureContext, ct) => Task.CompletedTask);

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(30));


            timerExecutionCounter.Should().Be(0);


            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task WhenAdvancePeriodIsABitGreaterThanTimePeriodTimerShouldTickOnce()
        {
            var testScheduler = new TestScheduler();

            var timerExecutionCounter = 0;

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                                                   ct =>
                                                   {
                                                       timerExecutionCounter++;
                                                       return Task.CompletedTask;
                                                   },
                                                   (failureContext, ct) => Task.CompletedTask);

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(51));


            timerExecutionCounter.Should().Be(1);


            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task WhenAdvancePeriodIs11TimesGreaterThanTimePeriodTimerShouldTick11Times()
        {
            var testScheduler = new TestScheduler();

            var timerExecutionCounter = 0;

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                                                   ct =>
                                                   {
                                                       timerExecutionCounter++;
                                                       return Task.CompletedTask;
                                                   },
                                                   (failureContext, ct) => Task.CompletedTask);

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(551));


            timerExecutionCounter.Should().Be(11);


            await scheduledTask.Cancel();
        }

        [Fact]
        public async Task ShouldNotTickAfterCancel()
        {
            var testScheduler = new TestScheduler();

            var timerExecutionCounter = 0;

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                                                   ct =>
                                                   {
                                                       timerExecutionCounter++;
                                                       return Task.CompletedTask;
                                                   },
                                                   (failureContext, ct) => Task.CompletedTask);

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(51));


            timerExecutionCounter.Should().Be(1);


            await scheduledTask.Cancel();

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(50));

            timerExecutionCounter.Should().Be(1);
        }
        
        [Fact]
        public async Task ShouldCallOnExceptionInCaseOfError()
        {
            var testScheduler = new TestScheduler();
            
            IFailureContext actualFailureContext = null;
            
            Task Throwing() => throw new InvalidOperationException("Boom");

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                ct => Throwing(),
                (failureContext, ct) =>
                {
                    actualFailureContext = failureContext;
                    return Task.CompletedTask;
                });

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(51));

            actualFailureContext.Should().NotBeNull();
            actualFailureContext.Exception.Should().NotBeNull();
            actualFailureContext.FailCount.Should().Be(1);

            await scheduledTask.Cancel();
        }
        
        [Fact]
        public async Task ShouldBackoffInCaseOfSeveralExceptions()
        {
            var testScheduler = new TestScheduler();
            
            IFailureContext actualFailureContext = null;
            
            Task Throwing() => throw new InvalidOperationException("Boom");

            var scheduledTask = testScheduler.Schedule(TimeSpan.FromSeconds(50),
                ct => Throwing(),
                (failureContext, ct) =>
                {
                    actualFailureContext = failureContext;
                    return Task.CompletedTask;
                }, TimeSpan.FromSeconds(180));

            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(51));
            
            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(51));

            actualFailureContext.Should().NotBeNull();
            actualFailureContext.Exception.Should().NotBeNull();
            actualFailureContext.FailCount.Should().Be(2);
            actualFailureContext.CurrentPeriod.Should().Be(TimeSpan.FromSeconds(100));
            
            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(51));
            
            actualFailureContext.FailCount.Should().Be(2);
            
            await testScheduler.AdvanceBy(TimeSpan.FromSeconds(50));
            
            actualFailureContext.Exception.Should().NotBeNull();
            actualFailureContext.FailCount.Should().Be(3);
            actualFailureContext.CurrentPeriod.Should().Be(TimeSpan.FromSeconds(180));
            
            

            await scheduledTask.Cancel();
        }
        
        [Fact]
        public async Task ShouldTickWithNormalPeriodAfterExceptionResolved()
        {
            var scheduler = new TestScheduler();
            
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
            
            IFailureContext actualFailureContext = null;

            var scheduledTask = scheduler.Schedule(TimeSpan.FromSeconds(50),
                ct => Throwing(),
                (failureContext, ct) =>
                {
                    actualFailureContext = failureContext;
                    return Task.CompletedTask;
                }, TimeSpan.FromSeconds(180));

            await scheduler.AdvanceBy(TimeSpan.FromSeconds(51));
            
            await scheduler.AdvanceBy(TimeSpan.FromSeconds(51));
            
            await scheduler.AdvanceBy(TimeSpan.FromSeconds(101));
            
            await scheduler.AdvanceBy(TimeSpan.FromSeconds(181));

            shouldThrow = false;
            
            await scheduler.AdvanceBy(TimeSpan.FromSeconds(181));
            
            actualFailureContext.Exception.Should().BeNull();
            actualFailureContext.FailCount.Should().Be(0);
            actualFailureContext.CurrentPeriod.Should().Be(TimeSpan.FromSeconds(50));
            
            await scheduler.AdvanceBy(TimeSpan.FromSeconds(51));

            counter.Should().Be(2);
            
            await scheduledTask.Cancel();
        }
        
        [Fact]
        public async Task ShouldEmulateNotionOfNow()
        {
            var initPoint = DateTimeOffset.Now.AddYears(-1);
            var scheduler = new TestScheduler(initPoint);

            var timeDiff = TimeSpan.FromDays(51);
            

            await scheduler.AdvanceBy(timeDiff);

            scheduler.Now.Should().Be(initPoint + timeDiff);
        }
        
        [Fact]
        public void ShouldFailWhenAdvancePeriodIsLessOrEqualToZero()
        {
            var scheduler = new TestScheduler();

            Func<Task> advanceAction = () => scheduler.AdvanceBy(TimeSpan.FromDays(-51));

            advanceAction.ShouldThrow<ArgumentOutOfRangeException>();
        }
    }
}
