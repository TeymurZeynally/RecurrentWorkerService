using Microsoft.Extensions.DependencyInjection;

namespace RecurrentWorkerService.Distributed.Registration;

internal class DistributedWorkersBuilder : IDistributedWorkersBuilder
{
	public DistributedWorkersBuilder(IServiceCollection services, long nodeId, string serviceName)
	{
		NodeId = nodeId;
		ServiceName = serviceName;
		Services = services;
	}

	public long NodeId { get; }

	public string ServiceName { get; }

	public IServiceCollection Services { get; }
}

