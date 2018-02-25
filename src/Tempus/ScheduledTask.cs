using System;
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

        public async Task Cancel()
        {
            _cancellationTokenSource.Cancel();

            try
            {
                await _task;
            }
            catch (OperationCanceledException)
            {
                // on purpose
            }
        }
    }
}
