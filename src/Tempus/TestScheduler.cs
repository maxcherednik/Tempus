using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    public class TestScheduler : IScheduler
    {
        private readonly ConcurrentDictionary<TestScheduledTask, TestScheduledTask> _timers;

        public TestScheduler() : this(DateTimeOffset.Now)
        {
        }

        public TestScheduler(DateTimeOffset initialCurrentDateTime)
        {
            Now = initialCurrentDateTime;

            _timers = new ConcurrentDictionary<TestScheduledTask, TestScheduledTask>();
        }

        public DateTimeOffset Now { get; private set; }

        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException)
        {
            return RegisterScheduledTask(period, period, action, onException, period);
        }

        public IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException)
        {
            return RegisterScheduledTask(initialDelay, period, action, onException, period);
        }

        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod)
        {
            return RegisterScheduledTask(period, period, action, onException, maxBackoffPeriod);
        }

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

        public async Task AdvanceBy(TimeSpan timeSpan)
        {
            if (timeSpan <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSpan), timeSpan, "TimeSpan should be greater than 0");
            }
            
            Now = Now.Add(timeSpan);

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
                        if (timer.DueTime <= Now)
                        {
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