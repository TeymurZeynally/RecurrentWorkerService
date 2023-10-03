using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using RecurrentWorkerService.Distributed.Configuration.Builders;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Services;
using RecurrentWorkerService.Distributed.Services.Hosts;

namespace RecurrentWorkerService.Distributed.Registration;

public static class DistributedWorkersServiceBuilder
{
	public static IDistributedWorkersBuilder AddDistributedWorkers(
		this IServiceCollection services,
		string serviceName,
		Action<DistributedWorkersRegistrationBuilder> workerRegistrationBuilderAction,
		Action<DistributedWorkersSettingsBuilder>? settingsBuilderAction = null)
	{
		return AddDistributedWorkers(
			services,
			serviceName,
			Math.Abs(BitConverter.ToInt64(RandomNumberGenerator.GetBytes(64))),
			workerRegistrationBuilderAction,
			settingsBuilderAction);
	}

	public static IDistributedWorkersBuilder AddDistributedWorkers(
		this IServiceCollection services,
		string serviceName,
		long nodeId,
		Action<DistributedWorkersRegistrationBuilder> workerRegistrationBuilderAction,
		Action<DistributedWorkersSettingsBuilder>? settingsBuilderAction = null)
	{
		var workersSettingsBuilder = new DistributedWorkersSettingsBuilder();
		settingsBuilderAction?.Invoke(workersSettingsBuilder);

		services.AddSingleton(workersSettingsBuilder.Build());

		var workersRegistrationBuilder = new DistributedWorkersRegistrationBuilder(services, nodeId);
		workerRegistrationBuilderAction(workersRegistrationBuilder);

		services.AddHostedService(s => new DistributedWorkerHostedService(s.GetServices<IDistributedWorkerService>()));

		services.AddSingleton<IPriorityManager, NullPriorityManager>();
		return new DistributedWorkersBuilder(services, nodeId, serviceName);
	}
}