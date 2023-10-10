using System.Diagnostics;
using System.Net;
using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace Application;

internal class NetLoadWorker : IWorkloadWorker
{
	private readonly ILogger<NetLoadWorker> _logger;

	public NetLoadWorker(ILogger<NetLoadWorker> logger)
	{
		_logger = logger;
	}

	public async Task<Workload> ExecuteAsync(CancellationToken cancellationToken)
	{
		using var _ = _logger.BeginScope("Workload worker");
		_logger.LogInformation("Start");

		var random = new Random();
		var sw = new Stopwatch();

		var downloadSeconds = random.Next(3, 20);


		var uri = new Uri("https://speed.hetzner.de/1GB.bin");
		var httpClient = new HttpClient(new HttpClientHandler(){Proxy = new WebProxy("http://localhost:8181")});

		var stream = await httpClient.GetStreamAsync(uri, cancellationToken);
		sw.Start();
		// work with chunks of 2KB => adjust if necessary
		const int chunkSize = 1024 * 2;
		var buffer = new byte[chunkSize];

		int totalRead = 0;
		int bytesRead;
		
		while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
		{
			totalRead += bytesRead;

			if (sw.Elapsed.TotalSeconds > downloadSeconds)
			{
				break;
			}
		}
		
		return Workload.FromDoneItems(downloadSeconds, 20);
	}
}