using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    internal class TestScheduledTask : IScheduledTask
    {
        public TestScheduledTask(DateTime dueTime, TimeSpan period, Func<CancellationToken, Task> action, Func<Exception, CancellationToken, Task> onException)
        {
            OnException = onException;
            Action = action;
            Period = period;
            DueTime = dueTime;
        }

        public DateTime DueTime { get; private set; }

        public TimeSpan Period { get; }

        public Func<CancellationToken, Task> Action { get; }

        public Func<Exception, CancellationToken, Task> OnException { get; }

        public bool CancelRequested { get; private set; }

        public Task Cancel()
        {
            CancelRequested = true;
            return Task.FromResult(0);
        }

        internal void SetNextDueTime()
        {
            DueTime = DueTime.Add(Period);
        }
    }
}
