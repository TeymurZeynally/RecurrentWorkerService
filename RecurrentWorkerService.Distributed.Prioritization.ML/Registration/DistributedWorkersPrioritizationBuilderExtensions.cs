using Microsoft.Extensions.DependencyInjection;
using RecurrentWorkerService.Distributed.Prioritization.ML.Clients;
using RecurrentWorkerService.Distributed.Prioritization.ML.Indicators.ML;
using RecurrentWorkerService.Distributed.Prioritization.ML.Repository;
using RecurrentWorkerService.Distributed.Prioritization.ML.Services;
using RecurrentWorkerService.Distributed.Prioritization.ML.Services.Models;
using RecurrentWorkerService.Distributed.Prioritization.Registration;
using RecurrentWorkerService.Distributed.Prioritization.SystemMetrics;
using RecurrentWorkerService.Distributed.Prioritization.SystemMetrics.Metrics;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Registration;

public static class DistributedWorkersPrioritizationBuilderExtensions
{
	public static IDistributedWorkersPrioritizationBuilder AddMLPrioritization(
		this IDistributedWorkersPrioritizationBuilder builder,
		string mlDirectoryPath,
		Action<HttpClient> configureClient,
		Action<DistributedWorkersMLPrioritizationBuilder>? prioritizationBuilder = null)
	{
		prioritizationBuilder?.Invoke(new DistributedWorkersMLPrioritizationBuilder(builder));

		var pathToMetrics = Path.Combine(mlDirectoryPath, "metrics.json");
		var collectionSettings = new MetricCollectionSettings
		{
			CollectionInterval = TimeSpan.FromSeconds(1),
			ForecastInterval = TimeSpan.FromMinutes(1),
			ModelTrainInterval = TimeSpan.FromDays(1),
			ModelsDirectory = mlDirectoryPath,
		};
		var metricsCapacity = (int)(collectionSettings.ModelTrainInterval * 2 / collectionSettings.ForecastInterval);

		builder.Services.AddSingleton<PriorityModel>(_ => new PriorityModel(mlDirectoryPath));
		builder.Services.AddSingleton<MetricCollectionSettings>(_ => collectionSettings);
		builder.Services.AddSingleton<IMetricsRepository, MetricsRepository>(_ => new MetricsRepository(pathToMetrics, metricsCapacity));

		builder.Services.AddSingleton<ICpuMetrics, CpuMetrics>();
		builder.Services.AddSingleton<IRamMetrics, RamMetrics>();
		builder.Services.AddSingleton<INetworkMetrics, NetworkMetrics>();
		builder.Services.AddSingleton<IMetricsFacade, MetricsFacade>();

		builder.Services.AddHttpClient<MLServerClient>(configureClient);

		builder.Services.AddHostedService<MetricsCollectionService>();

		return builder;
	}
}