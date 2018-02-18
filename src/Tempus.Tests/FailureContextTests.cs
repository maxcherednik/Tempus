using System;
using FluentAssertions;
using Xunit;

namespace Tempus.Tests
{
    public class FailureContextTests
    {
        [Fact]
        public void ShouldHaveDefaultValuesAfterCreation()
        {
            var normalPeriod = TimeSpan.FromMilliseconds(50);
            var maxPeriod = TimeSpan.FromSeconds(2);

            var failContext = new FailureContext(normalPeriod, maxPeriod, () => DateTime.Now);

            failContext.Period.Should().Be(normalPeriod);
            failContext.CurrentPeriod.Should().Be(normalPeriod);
            failContext.MaxPeriod.Should().Be(maxPeriod);

            failContext.FailCount.Should().Be(0);
            failContext.Exception.Should().BeNull();
        }

        [Fact]
        public void ShouldReactToFirstFailure()
        {
            var normalPeriod = TimeSpan.FromMilliseconds(50);
            var maxPeriod = TimeSpan.FromSeconds(2);

            var exception = new InvalidOperationException("Boom");

            var failContext = new FailureContext(normalPeriod, maxPeriod, () => DateTime.Now);
            failContext.SetException(exception);


            failContext.Period.Should().Be(normalPeriod);
            failContext.CurrentPeriod.Should().Be(normalPeriod);
            failContext.MaxPeriod.Should().Be(maxPeriod);

            failContext.FailCount.Should().Be(1);
            failContext.Exception.Should().Be(exception);
        }

        [Fact]
        public void ShouldReactToSecondFailure()
        {
            var normalPeriod = TimeSpan.FromMilliseconds(50);
            var maxPeriod = TimeSpan.FromSeconds(2);

            var exception1 = new InvalidOperationException("Boom");

            var exception2 = new InvalidCastException("Boom2");

            var failContext = new FailureContext(normalPeriod, maxPeriod, () => DateTime.Now);
            failContext.SetException(exception1);

            var timeOfFirstFailure = failContext.FirstFailureDateTime;

            // call
            failContext.SetException(exception2);

            // assert
            failContext.FirstFailureDateTime.Should().Be(timeOfFirstFailure);


            failContext.Period.Should().Be(normalPeriod);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(100));
            failContext.MaxPeriod.Should().Be(maxPeriod);

            failContext.FailCount.Should().Be(2);
            failContext.Exception.Should().Be(exception2);
        }

        [Fact]
        public void ShouldReactToThirdFailure()
        {
            var normalPeriod = TimeSpan.FromMilliseconds(50);
            var maxPeriod = TimeSpan.FromSeconds(2);

            var exception1 = new InvalidOperationException("Boom");

            var exception2 = new InvalidCastException("Boom2");

            var exception3 = new IndexOutOfRangeException("Boom3");

            var failContext = new FailureContext(normalPeriod, maxPeriod, () => DateTime.Now);
            failContext.SetException(exception1);

            var timeOfFirstFailure = failContext.FirstFailureDateTime;

            failContext.SetException(exception2);

            // call

            failContext.SetException(exception3);

            // assert
            failContext.FirstFailureDateTime.Should().Be(timeOfFirstFailure);


            failContext.Period.Should().Be(normalPeriod);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(200));
            failContext.MaxPeriod.Should().Be(maxPeriod);

            failContext.FailCount.Should().Be(3);
            failContext.Exception.Should().Be(exception3);
        }

        [Fact]
        public void ShouldNotExceedMaxBackoffPeriodAfterSeveralFailures()
        {
            var normalPeriod = TimeSpan.FromMilliseconds(50);
            var maxPeriod = TimeSpan.FromSeconds(1);

            var exception = new InvalidOperationException("Boom");

            var failContext = new FailureContext(normalPeriod, maxPeriod, () => DateTime.Now);

            failContext.SetException(exception);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(50));

            failContext.SetException(exception);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(100));

            failContext.SetException(exception);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(200));

            failContext.SetException(exception);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(400));

            failContext.SetException(exception);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(800));

            failContext.SetException(exception);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(1000));

            failContext.SetException(exception);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(1000));
        }

        [Fact]
        public void ShouldRecoverToNormalPeriodAfterReset()
        {
            var normalPeriod = TimeSpan.FromMilliseconds(50);
            var maxPeriod = TimeSpan.FromSeconds(1);

            var exception = new InvalidOperationException("Boom");

            var failContext = new FailureContext(normalPeriod, maxPeriod, () => DateTime.Now);

            failContext.SetException(exception);
            failContext.SetException(exception);
            failContext.SetException(exception);
            failContext.SetException(exception);
            failContext.SetException(exception);
            failContext.SetException(exception);
            failContext.SetException(exception);
            failContext.CurrentPeriod.Should().Be(TimeSpan.FromMilliseconds(1000));

            failContext.Reset();

            failContext.Period.Should().Be(normalPeriod);
            failContext.CurrentPeriod.Should().Be(normalPeriod);
            failContext.MaxPeriod.Should().Be(maxPeriod);

            failContext.FailCount.Should().Be(0);
            failContext.Exception.Should().BeNull();
        }
    }
}