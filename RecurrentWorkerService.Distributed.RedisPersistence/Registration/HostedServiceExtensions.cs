using Microsoft.Extensions.DependencyInjection;
using RecurrentWorkerService.Distributed.Configuration.Builders;
using RecurrentWorkerService.Distributed.Persistence;
using RecurrentWorkerService.Distributed.Registration;
using StackExchange.Redis;

namespace RecurrentWorkerService.Distributed.RedisPersistence.Registration;

public static class HostedServiceExtensions
{
	public static IServiceCollection AddDistributedWorkers(
		this IServiceCollection services,
		string serviceName,
		IDatabase redisDatabase,
		Action<DistributedWorkersRegistrationExtensions> workerRegistrationBuilderAction,
		Action<DistributedWorkersSettingsBuilder>? settingsBuilderAction = null)
	{
		var nodeId = Guid.NewGuid().ToString();
		

		var workersSettingsBuilder = new DistributedWorkersSettingsBuilder();
		settingsBuilderAction?.Invoke(workersSettingsBuilder);

		services.AddSingleton(redisDatabase);
		services.AddTransient<IPersistence>(
			s => new Persistence.RedisPersistence(s.GetService<IDatabase>()!, serviceName, nodeId));
		services.AddSingleton(workersSettingsBuilder.Build());


		var workersRegistrationBuilder = new DistributedWorkersRegistrationExtensions(services);
		workerRegistrationBuilderAction(workersRegistrationBuilder);

		return services;
	}
}