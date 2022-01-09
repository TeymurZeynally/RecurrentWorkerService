namespace RecurrentWorkerService.Distributed.Services.Calculators;

internal class ExecutionDateCalculator : IExecutionDateCalculator
{
	public (long N, DateTimeOffset ExecutionDate) CalculateCurrentExecutionDate(TimeSpan period, DateTimeOffset now)
	{
		var numberOfFullPeriodsBetweenDates = now.Ticks / period.Ticks;
		var result = new DateTimeOffset(period.Ticks * numberOfFullPeriodsBetweenDates, TimeSpan.Zero);

		return (numberOfFullPeriodsBetweenDates, result);
	}

	public (long N, DateTimeOffset ExecutionDate) CalculateNextExecutionDate(TimeSpan period, DateTimeOffset now)
	{
		var numberOfFullPeriodsBetweenDates = now.Ticks / period.Ticks + 1;
		var result = new DateTimeOffset(period.Ticks * numberOfFullPeriodsBetweenDates, TimeSpan.Zero);

		return (numberOfFullPeriodsBetweenDates, result);
	}
}