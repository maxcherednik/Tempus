using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    public class TestScheduler : IScheduler
    {
        private DateTime _currentDateTime;

        private ConcurrentDictionary<TestScheduledTask, TestScheduledTask> _timers;

        public TestScheduler()
        {
            _currentDateTime = DateTime.UtcNow;

            _timers = new ConcurrentDictionary<TestScheduledTask, TestScheduledTask>();
        }

        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action, Func<Exception, CancellationToken, Task> onException)
        {
            var t = new TestScheduledTask(_currentDateTime.Add(period), period, action, onException);

            _timers.TryAdd(t, null);

            return t;
        }

        public IScheduledTask Schedule(TimeSpan period, TimeSpan initialDelay, Func<CancellationToken, Task> action, Func<Exception, CancellationToken, Task> onException)
        {
            var t = new TestScheduledTask(_currentDateTime.Add(initialDelay), period, action, onException);

            _timers.TryAdd(t, null);

            return t;
        }

        public async Task AdvanceBy(TimeSpan timeSpan)
        {
            _currentDateTime = _currentDateTime.Add(timeSpan);

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
                        if (timer.DueTime <= _currentDateTime)
                        {
                            await CallAction(timer.Action, timer.OnException, CancellationToken.None);
                            timer.SetNextDueTime();
                            timerWasSignaled = true;
                        }
                    }

                    foreach (var cancelledTimer in cancelledTimers)
                    {
                        _timers.TryRemove(cancelledTimer, out _);
                    }
                }
            }
            while (timerWasSignaled);
        }

        private static async Task CallAction(Func<CancellationToken, Task> action, Func<Exception, CancellationToken, Task> onException, CancellationToken cancellationToken)
        {
            try
            {
                await action(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                try
                {
                    await onException(e, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // dont do anything if we failed to log
                }
            }
        }
    }
}
