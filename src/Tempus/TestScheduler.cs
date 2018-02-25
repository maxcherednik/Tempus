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

        /// <inheritdoc />
        public DateTimeOffset Now { get; private set; }

        /// <inheritdoc />
        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException)
        {
            return RegisterScheduledTask(period, period, action, onException, period);
        }

        /// <inheritdoc />
        public IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException)
        {
            return RegisterScheduledTask(initialDelay, period, action, onException, period);
        }

        /// <inheritdoc />
        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod)
        {
            return RegisterScheduledTask(period, period, action, onException, maxBackoffPeriod);
        }

        /// <inheritdoc />
        public IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod)
        {
            return RegisterScheduledTask(initialDelay, period, action, onException, maxBackoffPeriod);
        }

        private IScheduledTask RegisterScheduledTask(TimeSpan initialDelay, TimeSpan period,
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