using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.Configuration.Builders;
using RecurrentWorkerService.Distributed.Persistence;
using RecurrentWorkerService.Distributed.Schedules;
using RecurrentWorkerService.Distributed.Services;
using RecurrentWorkerService.Distributed.Services.Calculators;
using RecurrentWorkerService.Distributed.Services.Hosts;
using RecurrentWorkerService.Services;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Distributed.Registration;

public class DistributedWorkersRegistrationBuilder
{
	private readonly IServiceCollection _services;

	public DistributedWorkersRegistrationBuilder(IServiceCollection services)
	{
		_services = services;
		_services.AddSingleton<IExecutionDateCalculator, ExecutionDateCalculator>();
		_services.AddHostedService<HeartbeatHostService>();
	}

	public DistributedWorkersRegistrationBuilder AddDistributedRecurrentWorker<TWorker>(
		string identity,
		Action<DistributedRecurrentScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IRecurrentWorker
	{
		var scheduleBuilder = new DistributedRecurrentScheduleBuilder(new DistributedRecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedRecurrentWorkerService(
			s.GetService<ILogger<DistributedRecurrentWorkerService>>()!,
			() => s.GetService<TWorker>()!,
			scheduleBuilder.Build(),
			s.GetService<IExecutionDateCalculator>()!,
			s.GetService<IPersistence>()!,
			identity));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedRecurrentWorker(
		string identity,
		Func<IServiceProvider, IRecurrentWorker> implementationFactory,
		Action<DistributedRecurrentScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new DistributedRecurrentScheduleBuilder(new DistributedRecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedRecurrentWorkerService(
			s.GetService<ILogger<DistributedRecurrentWorkerService>>()!,
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetService<IExecutionDateCalculator>()!,
			s.GetService<IPersistence>()!,
			identity));
		return this;
	}
}