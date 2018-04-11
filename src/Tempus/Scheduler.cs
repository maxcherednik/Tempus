using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    /// <summary>
    /// Represents an object that schedules units of work.
    /// </summary>
    public class Scheduler : IScheduler
    {
        /// <summary>
        /// Represents a notion of time for this scheduler
        /// </summary>
        public DateTimeOffset Now => DateTimeOffset.Now;

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
            ScheduleInternal(TimeSpan.Zero, period, action, onException, period);

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
            ScheduleInternal(TimeSpan.Zero, period, action, onException, maxBackoffPeriod);

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

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var task = Task.Run(async () =>
            {
                using (cancellationTokenSource)
                {
                    var failureContext = new FailureContext(period, maxBackoffPeriod, () => Now);

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