using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    /// <inheritdoc />
    public class Scheduler : IScheduler
    {
        /// <inheritdoc />
        public DateTimeOffset Now => DateTimeOffset.Now;

        /// <inheritdoc />
        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException)
        {
            return ScheduleInternal(TimeSpan.Zero, period, action, onException, period);
        }

        /// <inheritdoc />
        public IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException)
        {
            return ScheduleInternal(initialDelay, period, action, onException, period);
        }

        /// <inheritdoc />
        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod)
        {
            return ScheduleInternal(TimeSpan.Zero, period, action, onException, maxBackoffPeriod);
        }

        /// <inheritdoc />
        public IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod)
        {
            return ScheduleInternal(initialDelay, period, action, onException, maxBackoffPeriod);
        }

        private static IScheduledTask ScheduleInternal(TimeSpan initialDelay, TimeSpan period,
            Func<CancellationToken, Task> action, Func<IFailureContext, CancellationToken, Task> onException,
            TimeSpan maxBackoffPeriod)
        {
            SchedulerAssertions.Assert(initialDelay, period, action, onException, maxBackoffPeriod);

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var task = Task.Run(async () =>
            {
                using (cancellationTokenSource)
                {
                    var failureContext = new FailureContext(period, maxBackoffPeriod, () => DateTime.Now);

                    if (initialDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(initialDelay, cancellationToken);
                        await CallAction(action, onException, cancellationToken, failureContext);
                    }

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(failureContext.CurrentPeriod, cancellationToken);
                        await CallAction(action, onException, cancellationToken, failureContext);
                    }
                }
            }, cancellationToken);

            return new ScheduledTask(task, cancellationTokenSource);
        }

        private static async Task CallAction(Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, CancellationToken cancellationToken,
            FailureContext failureContext)
        {
            try
            {
                await action(cancellationToken);
                failureContext.Reset();
            }
            catch (Exception e)
            {
                try
                {
                    failureContext.SetException(e);
                    await onException(failureContext, cancellationToken);
                }
                catch
                {
                    // dont do anything if we failed to log
                }
            }
        }
    }
}