using System;

namespace Tempus
{
    /// <summary>
    /// Represents information about scheduled task's failure
    /// </summary>
    public interface IFailureContext
    {
        /// <summary>
        /// Gets the time of the first exception occurrence
        /// </summary>
        DateTimeOffset FirstFailureDateTime { get; }

        /// <summary>
        /// Gets the number of consecutive errors
        /// </summary>
        int FailCount { get; }

        /// <summary>
        /// Gets the exception occured during scheduled task execution
        /// </summary>
        Exception Exception { get; }

        /// <summary>
        /// Gets the normal scheduler period
        /// </summary>
        TimeSpan Period { get; }

        /// <summary>
        /// Gets the current scheduler period.
        /// In case of exponential backoff scheduler, the period will be increasing
        /// with every consecutive failure up to MaxPeriod value.
        /// </summary>
        TimeSpan CurrentPeriod { get; }

        /// <summary>
        /// Gets the max period.
        /// In case of exponential backoff scheduler, this is maximum period of 
        /// a scheduled task.
        /// </summary>
        TimeSpan MaxPeriod { get; }
    }
}