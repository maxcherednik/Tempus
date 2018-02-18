using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    public class Scheduler : IScheduler
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
        
        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException)
        {
            return ScheduleInternal(TimeSpan.Zero, period, action, onException, period);
        }

        public IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException)
        {
            return ScheduleInternal(initialDelay, period, action, onException, period);
        }

        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod)
        {
            return ScheduleInternal(TimeSpan.Zero, period, action, onException, maxBackoffPeriod);
        }

        public IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action,
            Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod)
        {
            return ScheduleInternal(initialDelay, period, action, onException, maxBackoffPeriod);
        }

        private IScheduledTask ScheduleInternal(TimeSpan initialDelay, TimeSpan period,
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
                    try
                    {
                        var failureContext = new FailureContext(period, maxBackoffPeriod,() => DateTime.Now);

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
                    catch (OperationCanceledException)
                    {
                        // on purpose
                    }
                }
            });

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