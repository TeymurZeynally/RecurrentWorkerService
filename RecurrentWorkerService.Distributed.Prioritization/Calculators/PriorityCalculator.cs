namespace RecurrentWorkerService.Distributed.Prioritization.Calculators;

internal class PriorityCalculator : IPriorityCalculator
{
	public Task<byte> GetFailuresPriorityAsync(DateTimeOffset[] failuresHistory, CancellationToken cancellationToken)
	{
		return Task.FromResult(Convert.ToByte(Math.Min(byte.MaxValue, failuresHistory.Length)));
	}

	public Task<byte> GetIdentityPriorityAsync(byte[] indicators, CancellationToken cancellationToken)
	{
		return Task.FromResult(indicators.Max());
	}

	public Task<byte> GetNodePriorityAsync(byte[] indicators, CancellationToken cancellationToken)
	{
		return Task.FromResult(indicators.Max());
	}
}