# Tempus

This is a simple async/await, unit-test friendly timer implementation.

### Why we are here
It appears that the timers .net platform provides out of the box are slightly outdated. The API of these timers never changed since the very beginning.

There are 2 major issues with the API:
1. It is event-based

   In the era of the asynchronous programming, when nearly all the codebase top to bottom is asynchronous, event handlers do not fit this paradigm. Async void is not an option!
   
2. There is no interface abstraction
   
   Yeah, there is no way to unit test our periodic logic.
   
Hence, here we are. This timer is to address these issues.

### What it does
* It ticks! More or less precisely. Underneath Task.Delay() is used.
* Provides a unit-test friendly IScheduler abstraction and its impementation TestScheduler which allows us to time travel

### What it does not do
* It does not persist any kind of state
* It does not do exclusive inter process/system/machine execution

## Examples
Let's configure a periodic task which will be pinging an external service every 5 seconds:

```C#
public class EchoService
{
    private readonly IScheduledTask _periodTask;

    public EchoService(IScheduler scheduler, IExternalService externalService)
    {
        _periodTask = scheduler.Schedule(TimeSpan.FromSeconds(5), async token =>
            {
                await externalService.Ping(token);
            },
            (context, token) =>
            {
                Console.WriteLine(DateTime.Now + " " +
                                  $"Exception: {context.Exception}. " +
                                  $"First failure at: {context.FirstFailureDateTime} " +
                                  $"Fail count: {context.FailCount} " +
                                  $"Period: {context.Period} "+
                                  $"Current period: {context.CurrentPeriod} "+
                                  $"Max period: {context.MaxPeriod}");
                return Task.CompletedTask;
            });
    }

    public async Task Stop()
    {
        await _periodTask.Cancel();
    }
}
```

Here we don't know what can go wrong with our external service, hence no specific exception handling logic - print every exception.

In case of unhandled exception the scheduler will keep executing the task with the period specified:
```
04/07/2018 12:35:17 Normal. Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 1 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:22 Normal. Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 2 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:27 Normal. Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 3 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:32 Normal. Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 4 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:37 Normal. Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 5 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:42 Normal. Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 6 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
```

Seems like a known situation - Service unavailable - no need to print the stack trace!

```C#
public class EchoService
{
    private readonly IScheduledTask _periodTask;

    public EchoService(IScheduler scheduler, IExternalService externalService)
    {
        _periodTask = scheduler.Schedule(TimeSpan.FromSeconds(5), async token =>
            {
                await externalService.Ping(token);
            },
            (context, token) =>
            {
                if (context.Exception is ExternalServiceUnavailableException)
                {
                    Console.WriteLine(DateTime.Now + " " +
                                      $"Warning - Service unavailable. " +
                                      $"First failure at: {context.FirstFailureDateTime} " +
                                      $"Fail count: {context.FailCount} " +
                                      $"Period: {context.Period} "+
                                      $"Current period: {context.CurrentPeriod} "+
                                      $"Max period: {context.MaxPeriod}");
                }
                else
                {
                    Console.WriteLine(DateTime.Now + " " +
                                      $"Exception: {context.Exception.Message}. " +
                                      $"First failure at: {context.FirstFailureDateTime} " +
                                      $"Fail count: {context.FailCount} " +
                                      $"Period: {context.Period} "+
                                      $"Current period: {context.CurrentPeriod} "+
                                      $"Max period: {context.MaxPeriod}");
                }
            });
    }

    public async Task Stop()
    {
        await _periodTask.Cancel();
    }
}
```

Again in case of `ExternalServiceUnavailableException` the scheduler will keep executing the task with the period specified:
```
04/07/2018 12:35:17 Warning - Service unavailable. First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 1 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:22 Warning - Service unavailable. First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 2 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:27 Warning - Service unavailable. First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 3 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:32 Warning - Service unavailable. First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 4 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:37 Warning - Service unavailable. First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 5 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
04/07/2018 12:35:42 Warning - Service unavailable. First failure at: 04/07/2018 12:35:17 +02:00 Fail count: 6 Period: 00:00:05 Current period: 00:00:05 Max period: 00:00:05
```

Important part here is that `ExternalServiceUnavailableException` is still considered as an unhandled exception, cause we let it flow into the exception logging part. It is logged differently from any other exceptions, but the exception context is collecting the information about all the consequent exceptions happened recently.

If we would like to avoid this behaviour, we can actually handle the `ExternalServiceUnavailableException` in the body of the scheduled action:
```C#
public class EchoService
{
    private readonly IScheduledTask _periodTask;

    public EchoService(IScheduler scheduler, IExternalService externalService)
    {
        _periodTask = scheduler.Schedule(TimeSpan.FromSeconds(5), async token =>
            {
                try
                {
                    await externalService.Ping(token);
                }
                catch (ExternalServiceUnavailableException)
                {
                    Console.WriteLine(DateTime.Now + " Warning - Service unavailable");
                }
            },
            (context, token) =>
            {
                Console.WriteLine(DateTime.Now + " " +
                                  $"Exception: {context.Exception}. " +
                                  $"First failure at: {context.FirstFailureDateTime} " +
                                  $"Fail count: {context.FailCount} " +
                                  $"Period: {context.Period} "+
                                  $"Current period: {context.CurrentPeriod} "+
                                  $"Max period: {context.MaxPeriod}");
                return Task.CompletedTask;
            });
    }

    public async Task Stop()
    {
        await _periodTask.Cancel();
    }
}
```

In case of `ExternalServiceUnavailableException` no unhadled exceptions happened and the output will be like this:
```
04/07/2018 12:35:17 Warning - Service unavailable
04/07/2018 12:35:22 Warning - Service unavailable
04/07/2018 12:35:27 Warning - Service unavailable
04/07/2018 12:35:32 Warning - Service unavailable
04/07/2018 12:35:37 Warning - Service unavailable
04/07/2018 12:35:42 Warning - Service unavailable
```

There is another option for unhandled exceptions - exponential backoff. Let's take the first example and configure it to backoff exponentially to max 75 seconds:
```C#
public class EchoService
{
    private readonly IScheduledTask _periodTask;

    public EchoService(IScheduler scheduler, IExternalService externalService)
    {
        _periodTask = scheduler.Schedule(TimeSpan.FromSeconds(5), async token =>
            {
                await externalService.Ping(token);
            },
            (context, token) =>
            {
                Console.WriteLine(DateTime.Now + " " +
                                  $"Exception: {context.Exception}. " +
                                  $"First failure at: {context.FirstFailureDateTime} " +
                                  $"Fail count: {context.FailCount} " +
                                  $"Period: {context.Period} "+
                                  $"Current period: {context.CurrentPeriod} "+
                                  $"Max period: {context.MaxPeriod}");
                return Task.CompletedTask;
            },
            TimeSpan.FromSeconds(75));
    }

    public async Task Stop()
    {
        await _periodTask.Cancel();
    }
}
```

In case of unhandled exception the scheduler will keep executing the task but with ever increasing period up to the maximum period specified. Look at the timings here - execution periods here are: 5, 10, 20, 40, 75, 75 seconds:
```
04/07/2018 13:39:11 Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 13:39:11 +02:00 Fail count: 1 Period: 00:00:05 Current period: 00:00:05 Max period: 00:01:15
04/07/2018 13:39:16 Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 13:39:11 +02:00 Fail count: 2 Period: 00:00:05 Current period: 00:00:10 Max period: 00:01:15
04/07/2018 13:39:26 Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 13:39:11 +02:00 Fail count: 3 Period: 00:00:05 Current period: 00:00:20 Max period: 00:01:15
04/07/2018 13:39:46 Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 13:39:11 +02:00 Fail count: 4 Period: 00:00:05 Current period: 00:00:40 Max period: 00:01:15
04/07/2018 13:40:26 Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 13:39:11 +02:00 Fail count: 5 Period: 00:00:05 Current period: 00:01:15 Max period: 00:01:15
04/07/2018 13:41:41 Exception: Service unavailable(stack trace here). First failure at: 04/07/2018 13:39:11 +02:00 Fail count: 6 Period: 00:00:05 Current period: 00:01:15 Max period: 00:01:15
```

## Unit testing

```C#
[Fact]
public async Task EchoServiceOnceConstructedShouldPingExternalServicePeriodically()
{
    // setup
    var testScheduler = new TestScheduler();

    var externalServiceMock = new Mock<IExternalService>();


    // call
    var echoService = new EchoService(testScheduler, externalServiceMock.Object);

    await testScheduler.AdvanceBy(TimeSpan.FromSeconds(16));


    // check
    externalServiceMock.Verify(service => service.Ping(It.IsAny<CancellationToken>()), Times.Exactly(3));

    await echoService.Stop();
}
```
