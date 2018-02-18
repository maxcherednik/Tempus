using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    internal class TestScheduledTask : IScheduledTask
    {
        public TestScheduledTask(DateTimeOffset dueTime, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod,
            Func<DateTimeOffset> currentDateTime)
        {
            OnException = onException;
            Action = action;
            DueTime = dueTime;

            FailureContext = new FailureContext(period, maxBackoffPeriod, currentDateTime);
        }

        public DateTimeOffset DueTime { get; private set; }

        public Func<CancellationToken, Task> Action { get; }

        public Func<IFailureContext, CancellationToken, Task> OnException { get; }

        public bool CancelRequested { get; private set; }

        public FailureContext FailureContext { get; }

        public Task Cancel()
        {
            CancelRequested = true;
            return Task.FromResult(0);
        }

        internal void SetNextDueTime()
        {
            DueTime = DueTime.Add(FailureContext.CurrentPeriod);
        }
    }
}