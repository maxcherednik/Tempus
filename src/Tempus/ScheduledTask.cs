using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    internal class ScheduledTask : IScheduledTask
    {
        private readonly Task _task;

        private readonly CancellationTokenSource _cancellationTokenSource;

        public ScheduledTask(Task task, CancellationTokenSource cancellationTokenSource)
        {
            _task = task;
            _cancellationTokenSource = cancellationTokenSource;
        }

        public Task Cancel()
        {
            _cancellationTokenSource.Cancel();

            return _task;
        }
    }
}
