using Microsoft.Extensions.DependencyInjection;
using RecurrentWorkerService.Services;
using RecurrentWorkerService.Services.Hosts;

namespace RecurrentWorkerService.Registration;

public static class HostedServiceExtensions
{
	public static IServiceCollection AddWorkers(
		this IServiceCollection services,
		Action<WorkersRegistrationBuilder> workersRegistrationBuilderAction)
	{
		var workersRegistrationBuilder = new WorkersRegistrationBuilder(services);
		workersRegistrationBuilderAction(workersRegistrationBuilder);

		services.AddHostedService(s => new WorkerHostedService(s.GetServices<IWorkerService>()));
		return services;
	}
}