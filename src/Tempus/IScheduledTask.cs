using System.Threading.Tasks;

namespace Tempus
{
    /// <summary>
    /// Represents Scheduler task abstraction
    /// </summary>
    public interface IScheduledTask
    {
        /// <summary>
        /// Cancels this scheduled task
        /// </summary>
        /// <returns>Returns Task of cancel operation. 
        /// This Task will be completed once scheduled task is done
        /// </returns>
        Task Cancel();
    }
}
