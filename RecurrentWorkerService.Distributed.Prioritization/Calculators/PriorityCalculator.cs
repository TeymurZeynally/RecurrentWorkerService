using RecurrentWorkerService.Distributed.Prioritization.Models;

namespace RecurrentWorkerService.Distributed.Prioritization.Calculators;

internal class PriorityCalculator : IPriorityCalculator
{
	public Task<Priority> GetPriorityAsync(DateTimeOffset[] failuresHistory, CancellationToken cancellationToken)
	{
		return Task.FromResult((Priority)Convert.ToByte(Math.Min(byte.MaxValue, failuresHistory.Length)));
	}
}