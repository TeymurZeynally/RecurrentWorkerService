namespace RecurrentWorkerService.Distributed.Prioritization.Calculators;

internal class RecurrentExecutionDelayCalculator : IRecurrentExecutionDelayCalculator
{
	public TimeSpan CalculateExecutionDelay(TimeSpan period, DateTimeOffset now)
	{
		return new DateTimeOffset(period.Ticks * (now.Ticks / period.Ticks + 1), TimeSpan.Zero) - now;
	}
}