using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace Application;

internal class MemoryLoadWorker : IWorkloadWorker
{
	private readonly ILogger<MemoryLoadWorker> _logger;

	public MemoryLoadWorker(ILogger<MemoryLoadWorker> logger)
	{
		_logger = logger;
	}

	public async Task<Workload> ExecuteAsync(CancellationToken cancellationToken)
	{
		using var _ = _logger.BeginScope("Workload worker");
		_logger.LogInformation("Start");

		var random = new Random();

		var memoryLoadGB = random.Next(2, 5);
		var memoryLoadBytes = memoryLoadGB * 1024u * 1024u * 1024u;

		var list = new List<byte[]>();
		for (long i = 0; i < memoryLoadGB; i++)
		{
			list.Add(new byte[1024 * 1024 * 1024]); // Change the size here.
			//await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
		}

		await Task.Delay(TimeSpan.FromSeconds(memoryLoadGB), cancellationToken);

		_logger.LogInformation("End");

		return Workload.FromDoneItems(memoryLoadGB, 5);
	}
}