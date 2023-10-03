using System.Diagnostics;
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
		var assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName();
		var activitySource = new ActivitySource(assembly.Name!, assembly.Version?.ToString());

		builder.Services.AddSingleton(channelBase);
		builder.Services.AddSingleton<IPersistence>(
			s => new Persistence.EtcdPersistence(
				s.GetRequiredService<ILogger<Persistence.EtcdPersistence>>(),
				s.GetRequiredService<ChannelBase>(),
				builder.ServiceName,
				builder.NodeId,
				activitySource));

		return builder;
	}
}