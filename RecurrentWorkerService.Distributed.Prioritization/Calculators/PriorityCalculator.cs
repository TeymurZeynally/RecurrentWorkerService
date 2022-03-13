namespace RecurrentWorkerService.Distributed.Prioritization.Calculators;

internal class PriorityCalculator : IPriorityCalculator
{
	public Task<byte> GetPriorityAsync(DateTimeOffset[] failuresHistory, CancellationToken cancellationToken)
	{
		return Task.FromResult(Convert.ToByte(Math.Min(byte.MaxValue, failuresHistory.Length)));
	}

	public Task<byte> GetNodePriorityAsync(byte[][] indicators, CancellationToken cancellationToken)
	{
		return Task.FromResult(indicators.Last().Max());
	}
}