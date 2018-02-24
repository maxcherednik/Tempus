using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    /// <summary>
    /// Represents Scheduler abstraction
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Represents a notion of time for this scheduler
        /// </summary>
        DateTimeOffset Now { get; }
        
        /// <summary>
        /// Schedules the specified action to be executed with the specified period
        /// </summary>
        /// <returns>IScheduledTask which can be used to cancel the sheduled action</returns>
        /// <param name="period">Execution period</param>
        /// <param name="action">Action to be executed</param>
        /// <param name="onException">Func which will be called in case of unhandled exception</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when period is less or equal to 0</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when action or onException parameters are not provided</exception>
        IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action, Func<IFailureContext, CancellationToken, Task> onException);

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
        IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action, Func<IFailureContext, CancellationToken, Task> onException);
    
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
        IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action, Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod);

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
        IScheduledTask Schedule(TimeSpan initialDelay, TimeSpan period, Func<CancellationToken, Task> action, Func<IFailureContext, CancellationToken, Task> onException, TimeSpan maxBackoffPeriod);
    }
}
