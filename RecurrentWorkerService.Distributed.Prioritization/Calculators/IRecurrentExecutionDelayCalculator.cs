namespace RecurrentWorkerService.Distributed.Prioritization.Calculators;

internal interface IRecurrentExecutionDelayCalculator
{
	TimeSpan CalculateExecutionDelay(TimeSpan period, DateTimeOffset now);
}