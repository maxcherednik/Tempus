using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Tempus.Tests
{
    public class SchedulerCreationTests
    {
        private static IEnumerable<object[]> GetSchedulers()
        {
            yield return new object[] { new Scheduler() };
            yield return new object[] { new TestScheduler() };
        }
        
        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void ShouldFailIfInitalDelayLessThenZero(IScheduler scheduler)
        {
            Action action = () => scheduler.Schedule(TimeSpan.FromMilliseconds(-100),
                TimeSpan.FromSeconds(1),
                ct => Task.CompletedTask,
                (failureContext, ct) => Task.CompletedTask);

            action.ShouldThrow<ArgumentOutOfRangeException>();
        }
        
        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void ShouldFailIfPeriodLessThenZero(IScheduler scheduler)
        {
            Action action = () => scheduler.Schedule(TimeSpan.FromMilliseconds(-100),
                ct => Task.CompletedTask,
                (failureContext, ct) => Task.CompletedTask);

            action.ShouldThrow<ArgumentOutOfRangeException>();
        }
        
        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void ShouldFailIfPeriodEqualToZero(IScheduler scheduler)
        {
            Action action = () => scheduler.Schedule(TimeSpan.Zero,
                ct => Task.CompletedTask,
                (failureContext, ct) => Task.CompletedTask);

            action.ShouldThrow<ArgumentOutOfRangeException>();
        }
        
        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void ShouldFailIfPeriodGreaterThanMaxBackoffPeriod(IScheduler scheduler)
        {
            Action action = () => scheduler.Schedule(TimeSpan.FromSeconds(50),
                ct => Task.CompletedTask,
                (failureContext, ct) => Task.CompletedTask,
                TimeSpan.FromSeconds(5));

            action.ShouldThrow<ArgumentOutOfRangeException>();
        }
        
        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void ShouldFailIfActionIsNotProvided(IScheduler scheduler)
        {
            Action action = () => scheduler.Schedule(TimeSpan.FromMilliseconds(50), 
                null,
                (failureContext, ct) => Task.CompletedTask);

            action.ShouldThrow<ArgumentNullException>();
        }
        
        [Theory]
        [MemberData(nameof(GetSchedulers))]
        public void ShouldFailIfExceptionHandlerIsNotProvided(IScheduler scheduler)
        {
            Action action = () => scheduler.Schedule(TimeSpan.FromMilliseconds(50),
                ct => Task.CompletedTask,
                null);

            action.ShouldThrow<ArgumentNullException>();
        }
    }
}