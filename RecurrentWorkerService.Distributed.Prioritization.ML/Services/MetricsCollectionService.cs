using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Prioritization.ML.Repository;
using RecurrentWorkerService.Distributed.Prioritization.ML.Repository.Models;
using RecurrentWorkerService.Distributed.Prioritization.ML.Services.Models;
using RecurrentWorkerService.Distributed.Prioritization.SystemMetrics;
using RecurrentWorkerService.Distributed.Prioritization.ML.Clients;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Services;

internal class MetricsCollectionService : BackgroundService
{
	public MetricsCollectionService(
		MetricCollectionSettings settings,
		IMetricsRepository metricsRepository,
		IMetricsFacade metricsFacade,
		MLServerClient client,
		ILogger<MetricsCollectionService> logger)
	{
		_settings = settings;
		_metricsRepository = metricsRepository;
		_metricsFacade = metricsFacade;
		_client = client;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Task.WhenAll(
			CollectSystemMetrics(stoppingToken),
			SaveSystemMetrics(stoppingToken),
			TrainModel(stoppingToken));
	}

	private async Task CollectSystemMetrics(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var cpu = _metricsFacade.GetUsedCpu();
				var mem = _metricsFacade.GetUsedRam();
				var net = _metricsFacade.GetReceivedBytes() + _metricsFacade.GetSentBytes();

				_systemInfo.Add((cpu, mem, net));

				await Task.Delay(_settings.CollectionInterval, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.LogWarning(e, "System metrics collection problem. Retrying...");
			}
		}
	}

	private async Task SaveSystemMetrics(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var metrics = _systemInfo.ToArray();
				_systemInfo.Clear();

				// TODO: Use Median
				_metricsRepository.Add(new Metrics
				{
					Cpu = (float)metrics.Select(x => x.Cpu).Average(),
					Memory = (float)metrics.Select(x => x.Mem).Average(),
					Network = (float)metrics.Select(x => x.Net).Average()
				});

				await Task.Delay(_settings.ForecastInterval, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.LogWarning(e, "System metrics save problem. Retrying...");
			}
		}
	}

	private async Task TrainModel(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var metrics = _metricsRepository.GetAll();

				var stream = await _client.DownloadOnnxModel(metrics, cancellationToken).ConfigureAwait(false);

				var newModel = Path.Combine(_settings.ModelsDirectory, Guid.NewGuid().ToString());
				await using var fileStream = new FileStream(newModel, FileMode.OpenOrCreate);
				await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

				await Task.Delay(_settings.ModelTrainInterval, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.LogWarning(e, "Model training problem. Retrying...");
			}
		}
	}


	private readonly ConcurrentBag<(double Cpu, double Mem, double Net)> _systemInfo = new();
	private readonly MetricCollectionSettings _settings;
	private readonly IMetricsRepository _metricsRepository;
	private readonly IMetricsFacade _metricsFacade;
	private readonly MLServerClient _client;
	private readonly ILogger<MetricsCollectionService> _logger;
}