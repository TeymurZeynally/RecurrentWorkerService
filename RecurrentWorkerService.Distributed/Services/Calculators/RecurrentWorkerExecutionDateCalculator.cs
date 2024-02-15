namespace RecurrentWorkerService.Distributed.Services.Calculators;

internal class RecurrentWorkerExecutionDateCalculator
{
	public (long N, DateTimeOffset ExecutionDate) CalculateCurrentExecutionDate(TimeSpan period, DateTimeOffset now)
	{
		if (period == TimeSpan.Zero)
		{
			var dateTime = DateTimeOffset.UtcNow;
			return (dateTime.Ticks, dateTime);
		}

		var numberOfFullPeriodsBetweenDates = now.Ticks / period.Ticks;
		var result = new DateTimeOffset(period.Ticks * numberOfFullPeriodsBetweenDates, TimeSpan.Zero);

		return (numberOfFullPeriodsBetweenDates, result);
	}

	public (long N, DateTimeOffset ExecutionDate) CalculateNextExecutionDate(TimeSpan period, DateTimeOffset now)
	{
		if(period == TimeSpan.Zero)
		{
			var dateTime = DateTimeOffset.UtcNow.AddTicks(1);
			return (dateTime.Ticks, dateTime);
		}

		var numberOfFullPeriodsBetweenDates = now.Ticks / period.Ticks + 1;
		var result = new DateTimeOffset(period.Ticks * numberOfFullPeriodsBetweenDates, TimeSpan.Zero);

		return (numberOfFullPeriodsBetweenDates, result);
	}
}