using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Prioritization.Aggregators;
using RecurrentWorkerService.Distributed.Prioritization.Calculators;
using RecurrentWorkerService.Distributed.Prioritization.Indicators;
using RecurrentWorkerService.Distributed.Prioritization.Services;
using RecurrentWorkerService.Distributed.Registration;

namespace RecurrentWorkerService.Distributed.Prioritization.Registration;

public static class DistributedWorkersBuilderExtensions
{
	public static IDistributedWorkersBuilder AddPrioritization(this IDistributedWorkersBuilder builder, Action<DistributedWorkersPrioritizationBuilder>? prioritizationBuilder = null)
	{
		var assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName();
		var activitySource = new ActivitySource(assembly.Name!, assembly.Version?.ToString());

		prioritizationBuilder?.Invoke(new DistributedWorkersPrioritizationBuilder(builder.Services));

		builder.Services.AddSingleton<IPriorityCalculator, PriorityCalculator>();
		builder.Services.AddSingleton<IComputedPriorityAggregator, ComputedPriorityAggregator>(
			r => new ComputedPriorityAggregator(
				builder.NodeId,
				activitySource,
				r.GetRequiredService<ILogger<ComputedPriorityAggregator>>()));
		builder.Services.AddSingleton<IPriorityChangesAggregator, PriorityChangesAggregator>(
			r => new PriorityChangesAggregator(
				r.GetRequiredService<IPersistence>(),
				r.GetRequiredService<IPriorityCalculator>(),
				builder.NodeId,
				activitySource));
		builder.Services.AddSingleton<IPriorityCalculator, PriorityCalculator>();
		builder.Services.AddSingleton<IPriorityManager, PriorityManager>();

		builder.Services.AddHostedService(r => new PriorityService(
			r.GetRequiredService<IPersistence>(),
			r.GetRequiredService<IComputedPriorityAggregator>(),
			r.GetRequiredService<IPriorityChangesAggregator>(),
			r.GetServices<IdentityPriorityIndicator>().ToArray(),
			r.GetServices<NodePriorityIndicator>().ToArray(),
			r.GetRequiredService<ILogger<PriorityService>>()));

		return builder;
	}
}