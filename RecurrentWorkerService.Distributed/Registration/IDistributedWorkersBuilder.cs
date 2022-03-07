using Microsoft.Extensions.DependencyInjection;

namespace RecurrentWorkerService.Distributed.Registration;
public interface IDistributedWorkersBuilder
{
	public long NodeId { get; }

	public string ServiceName { get; }

	public IServiceCollection Services { get; }
}
