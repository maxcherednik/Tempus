using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tempus
{
    public class Scheduler : IScheduler
    {
        public IScheduledTask Schedule(TimeSpan period, Func<CancellationToken, Task> action, Func<Exception, CancellationToken, Task> onException)
        {
            return Schedule(period, TimeSpan.Zero, action, onException);
        }

        public IScheduledTask Schedule(TimeSpan period, TimeSpan initialDelay, Func<CancellationToken, Task> action, Func<Exception, CancellationToken, Task> onException)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var task = Task.Run(async () =>
            {
                using (cancellationTokenSource)
                {
                    try
                    {
                        if(initialDelay > TimeSpan.Zero)
                        {
                            await Task.Delay(initialDelay, cancellationToken);
                            await CallAction(action, onException, cancellationToken);
                        }

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(period, cancellationToken);
                            await CallAction(action, onException, cancellationToken);
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

        private static async Task CallAction(Func<CancellationToken, Task> action, Func<Exception, CancellationToken, Task> onException, CancellationToken cancellationToken)
        {
            try
            {
                await action(cancellationToken);
            }
            catch (Exception e)
            {
                try
                {
                    await onException(e, cancellationToken);
                }
                catch
                {
                    // dont do anything if we failed to log
                }
            }
        }
    }
}
