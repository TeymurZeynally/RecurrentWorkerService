using System.Security.Cryptography;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Configuration.Builders;
using RecurrentWorkerService.Distributed.Persistence;
using RecurrentWorkerService.Distributed.Registration;
using RecurrentWorkerService.Distributed.Services;
using RecurrentWorkerService.Distributed.Services.Hosts;

namespace RecurrentWorkerService.Distributed.EtcdPersistence.Registration;

public static class HostedServiceExtensions
{
	public static IServiceCollection AddDistributedWorkers(
		this IServiceCollection services,
		string serviceName,
		ChannelBase channelBase,
		Action<DistributedWorkersRegistrationBuilder> workerRegistrationBuilderAction,
		Action<DistributedWorkersSettingsBuilder>? settingsBuilderAction = null)
	{
		var nodeId = Math.Abs(BitConverter.ToInt64(RandomNumberGenerator.GetBytes(64)));

		var workersSettingsBuilder = new DistributedWorkersSettingsBuilder();
		settingsBuilderAction?.Invoke(workersSettingsBuilder);

		services.AddSingleton(channelBase);
		services.AddSingleton<IPersistence>(
			s => new Persistence.EtcdPersistence(
				s.GetService<ILogger<Persistence.EtcdPersistence>>()!,
				s.GetService<ChannelBase>()!,
				serviceName,
				nodeId));
		services.AddSingleton(workersSettingsBuilder.Build());

		var workersRegistrationBuilder = new DistributedWorkersRegistrationBuilder(services);
		workerRegistrationBuilderAction(workersRegistrationBuilder);

		services.AddHostedService(s => new DistributedWorkerHostedService(s.GetServices<IDistributedWorkerService>()));

		return services;
	}
}