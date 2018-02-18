using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    internal static class SchedulerAssertions
    {
        public static void Assert(TimeSpan initialDelay, TimeSpan period,
            Func<CancellationToken, Task> action, Func<IFailureContext, CancellationToken, Task> onException,
            TimeSpan maxBackoffPeriod)
        {            
            if (initialDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(initialDelay), initialDelay,
                    "Initial delay should be greater or equal to 0");
            }

            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), period, "Period should be greater than 0");
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (onException == null)
            {
                throw new ArgumentNullException(nameof(onException));
            }

            if (maxBackoffPeriod < period)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBackoffPeriod), maxBackoffPeriod,
                    "Maximum backoff period should be greater or equal to period");
            }
        }
    }
}