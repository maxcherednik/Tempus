using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tempus.Tests")]

namespace Tempus
{
    internal class FailureContext : IFailureContext
    {
        private readonly Func<DateTimeOffset> _currentDateTime;

        public FailureContext(TimeSpan period, TimeSpan maxBackoffPeriod, Func<DateTimeOffset> currentDateTime)
        {
            _currentDateTime = currentDateTime;
            
            Period = period;

            CurrentPeriod = period;

            MaxPeriod = maxBackoffPeriod;
        }

        public DateTimeOffset FirstFailureDateTime { get; private set; }

        public int FailCount { get; private set; }

        public Exception Exception { get; private set; }

        public TimeSpan Period { get; }

        public TimeSpan CurrentPeriod { get; private set; }

        public TimeSpan MaxPeriod { get; }

        public void SetException(Exception e)
        {
            if (FailCount == 0)
            {
                FirstFailureDateTime = _currentDateTime();
            }

            FailCount++;

            Exception = e;

            if (Period == MaxPeriod) return;

            var newExecutionPeriod = TimeSpan.FromTicks(Period.Ticks * (long) Math.Pow(2, FailCount - 1));

            CurrentPeriod = newExecutionPeriod > MaxPeriod ? MaxPeriod : newExecutionPeriod;
        }

        public void Reset()
        {
            FailCount = 0;

            FirstFailureDateTime = DateTimeOffset.MinValue;

            Exception = null;

            CurrentPeriod = Period;
        }
    }
}