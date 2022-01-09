namespace RecurrentWorkerService.Distributed.Services.Calculators;

internal interface IExecutionDateCalculator
{
	public (long N, DateTimeOffset ExecutionDate) CalculateNextExecutionDate(TimeSpan period, DateTimeOffset now);

	public (long N, DateTimeOffset ExecutionDate) CalculateCurrentExecutionDate(TimeSpan period, DateTimeOffset now);
}