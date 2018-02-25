using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    /// <summary>
    /// Represents scheduler which can be used for unit testing
    /// </summary>
    public class TestScheduler : IScheduler
    {
        private readonly ConcurrentDictionary<TestScheduledTask, TestScheduledTask> _timers;

        /// <summary>
        /// Initializes a new instance of the TestScheduler class with initial point in time at DateTimeOffset.Now
        /// </summary>
        public TestScheduler() : this(DateTimeOffset.Now)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TestScheduler class with initial point in time specified
        /// </summary>
        /// <param name="initialCurrentDateTime">Initial point in time</param>
        public TestScheduler(DateTimeOffset initialCurrentDateTime)
        {
            Now = initialCurrentDateTime;

            _timers = new ConcurrentDictionary<TestScheduledTask, TestScheduledTask>();
        }

        /// <summary>
        /// Represents a notion of time for this scheduler
        /// </summary>
        public DateTimeOffset Now { get; private set; }

        /// <summary>
        /// Schedules the specified action to be executed with the specified period
        /// </summary>
        /// <returns>IScheduledTask which can be used to cancel the sheduled action</returns>
        /// <param name="period">Execution period</param>
        /// <param name="action">Action to be executed</param>
        /// <param name="onException">Func which will be called in case of unhandled exception</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when period is less or equal to 0</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when action or onException parameters are not provided</exception>
        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException) =>
            ScheduleInternal(period, period, action, onException, period);

        /// <summary>
        /// Schedules the specified action to be executed with the specified period
        /// </summary>
        /// <returns>IScheduledTask which can be used to cancel the sheduled action</returns>
        /// <param name="initialDelay">Initial delay before the first execution</param>
        /// <param name="period">Execution eriod</param>
        /// <param name="action">Action to be executed</param>
        /// <param name="onException">Func which will be called in case of unhandled exception</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when initialDelay is less then 0 or period is less or equal to 0</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when action or onException parameters are not provided</exception>
        public IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException) =>
            ScheduleInternal(initialDelay, period, action, onException, period);

        /// <summary>
        /// Schedules the specified action to be executed with the specified period.
        /// In case of consecutive exceptions execution period will be exponentially increased up to maxBackoffPeriod
        /// </summary>
        /// <returns>IScheduledTask which can be used to cancel the sheduled action</returns>
        /// <param name="period">Execution period</param>
        /// <param name="action">Action to be executed</param>
        /// <param name="onException">Func which will be called in case of unhandled exception</param>
        /// <param name="maxBackoffPeriod">Execution period's maximum value to which scheduled task backs off in case of consecutive exceptions</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when period is less or equal to 0 or maxBackoffPeriod is less then period</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when action or onException parameters are not provided</exception>
        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod) =>
            ScheduleInternal(period, period, action, onException, maxBackoffPeriod);

        /// <summary>
        /// Schedules the specified action to be executed with the specified period.
        /// In case of consecutive exceptions execution period will be exponentially increased up to maxBackoffPeriod
        /// </summary>
        /// <returns>IScheduledTask which can be used to cancel the sheduled action</returns>
        /// <param name="initialDelay">Initial delay before the first execution</param>
        /// <param name="period">Execution eriod</param>
        /// <param name="action">Action to be executed</param>
        /// <param name="onException">Func which will be called in case of unhandled exception</param>
        /// <param name="maxBackoffPeriod">Execution period's maximum value to which scheduled task backs off in case of consecutive exceptions</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when initialDelay is less then 0 or period is less or equal to 0 or maxBackoffPeriod is less then period</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when action or onException parameters are not provided</exception>
        public IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod) =>
            ScheduleInternal(initialDelay, period, action, onException, maxBackoffPeriod);

        private IScheduledTask ScheduleInternal(TimeSpan initialDelay, TimeSpan period,
            Func<CancellationToken, Task> action, Func<IFailureContext, CancellationToken, Task> onException,
            TimeSpan maxBackoffPeriod)
        {
            SchedulerAssertions.Assert(initialDelay, period, action, onException, maxBackoffPeriod);

            var t = new TestScheduledTask(Now.Add(initialDelay), period, action, onException,
                maxBackoffPeriod, () => Now);

            _timers.TryAdd(t, null);

            return t;
        }

        /// <summary>
        /// Advances the scheduler's clock by the specified relative time, running all work scheduled for that timespan.
        /// </summary>
        /// <param name="period">Period</param>
        /// <returns>Task, that will be completed once all the work scheduled will be executed</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when period is less or equal to 0</exception>
        public async Task AdvanceBy(TimeSpan period)
        {
            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), period, "Period should be greater than 0");
            }


            var newNow = Now.Add(period);

            bool timerWasSignaled;
            do
            {
                timerWasSignaled = false;

                var cancelledTimers = new List<TestScheduledTask>();

                foreach (var timerPair in _timers)
                {
                    var timer = timerPair.Key;

                    if (timer.CancelRequested)
                    {
                        cancelledTimers.Add(timer);
                    }
                    else
                    {
                        if (timer.DueTime <= newNow)
                        {
                            Now = timer.DueTime;
                            await CallAction(timer);
                            timer.SetNextDueTime();
                            timerWasSignaled = true;
                        }
                    }

                    foreach (var cancelledTimer in cancelledTimers)
                    {
                        _timers.TryRemove(cancelledTimer, out _);
                    }
                }
            } while (timerWasSignaled);

            Now = newNow;
        }

        private static async Task CallAction(TestScheduledTask testScheduledTask)
        {
            try
            {
                await testScheduledTask.Action(CancellationToken.None).ConfigureAwait(false);
                testScheduledTask.FailureContext.Reset();
            }
            catch (Exception ex)
            {
                try
                {
                    testScheduledTask.FailureContext.SetException(ex);
                    await testScheduledTask.OnException(testScheduledTask.FailureContext, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // dont do anything if we failed to log
                }
            }
        }
    }
}