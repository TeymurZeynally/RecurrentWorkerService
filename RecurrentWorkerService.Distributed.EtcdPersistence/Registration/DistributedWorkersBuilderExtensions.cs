using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Registration;

namespace RecurrentWorkerService.Distributed.EtcdPersistence.Registration;

public static class DistributedWorkersBuilderExtensions
{
	public static IDistributedWorkersBuilder AddEtcdPersistence(
		this IDistributedWorkersBuilder builder, ChannelBase channelBase)
	{
		builder.Services.AddSingleton(channelBase);
		builder.Services.AddSingleton<IPersistence>(
			s => new Persistence.EtcdPersistence(
				s.GetService<ILogger<Persistence.EtcdPersistence>>()!,
				s.GetService<ChannelBase>()!,
				builder.ServiceName,
				builder.NodeId));

		return builder;
	}
}