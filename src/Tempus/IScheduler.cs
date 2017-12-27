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
        /// Schedules the specified action to be executed with the specified period
        /// </summary>
        /// <returns>IScheduledTask which can be used to cancel the sheduled action</returns>
        /// <param name="period">Execution period</param>
        /// <param name="action">Action to be executed</param>
        /// <param name="onException">Func which will be called in case of unhandled exception</param>
        IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action, Func<Exception, CancellationToken, Task> onException);

        /// <summary>
        /// Schedules the specified action to be executed with the specified period
        /// </summary>
        /// <returns>IScheduledTask which can be used to cancel the sheduled action</returns>
        /// <param name="period">Execution eriod</param>
        /// <param name="initialDelay">Initial delay before the first execution</param>
        /// <param name="action">Action to be executed</param>
        /// <param name="onException">Func which will be called in case of unhandled exception</param>
        IScheduledTask Schedule(TimeSpan period, TimeSpan initialDelay, Func<CancellationToken, Task> action, Func<Exception, CancellationToken, Task> onException);
    }
}
