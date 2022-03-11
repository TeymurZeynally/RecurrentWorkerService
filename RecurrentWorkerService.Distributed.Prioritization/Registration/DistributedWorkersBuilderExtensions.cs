using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Prioritization.Calculators;
using RecurrentWorkerService.Distributed.Prioritization.Providers;
using RecurrentWorkerService.Distributed.Prioritization.Services;
using RecurrentWorkerService.Distributed.Registration;

namespace RecurrentWorkerService.Distributed.Prioritization.Registration;

public static class DistributedWorkersBuilderExtensions
{
	public static IDistributedWorkersBuilder AddBasicPrioritization(this IDistributedWorkersBuilder builder)
	{
		builder.Services.AddSingleton<IPriorityCalculator, PriorityCalculator>();
		builder.Services.AddSingleton<IRecurrentExecutionDelayCalculator, RecurrentExecutionDelayCalculator>();
		builder.Services.AddSingleton<IPriorityProvider, PriorityProvider>(r => new PriorityProvider(builder.NodeId));
		builder.Services.AddSingleton<IPriorityCalculator, PriorityCalculator>();
		builder.Services.AddSingleton<IPriorityManager, PriorityManager>();

		builder.Services.AddHostedService(r => new PriorityService(
			r.GetService<IPersistence>()!,
			r.GetService<IPriorityProvider>()!,
			r.GetService<ILogger<PriorityService>>()!));

		return builder;
	}
}