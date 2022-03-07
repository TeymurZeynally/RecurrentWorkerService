using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Configuration.Builders;
using RecurrentWorkerService.Distributed.Interfaces.Persistence;
using RecurrentWorkerService.Distributed.Interfaces.Prioritization;
using RecurrentWorkerService.Distributed.Services;
using RecurrentWorkerService.Distributed.Services.Calculators;
using RecurrentWorkerService.Distributed.Services.Hosts;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services.Calculators;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Distributed.Registration;

public class DistributedWorkersRegistrationBuilder
{
	private readonly IServiceCollection _services;

	public DistributedWorkersRegistrationBuilder(IServiceCollection services)
	{
		_services = services;

		_services.AddSingleton<RecurrentWorkerExecutionDateCalculator>();
		_services.AddSingleton<CronWorkerExecutionDateCalculator>();
		_services.AddSingleton<WorkloadWorkerExecutionDelayCalculator>();

		_services.AddHostedService<HeartbeatHostService>();
	}

	public DistributedWorkersRegistrationBuilder AddDistributedRecurrentWorker<TWorker>(
		string identity,
		Action<RecurrentScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IRecurrentWorker
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedRecurrentWorkerService(
			s.GetService<ILogger<DistributedRecurrentWorkerService>>()!,
			() => s.GetService<TWorker>()!,
			scheduleBuilder.Build(),
			s.GetService<RecurrentWorkerExecutionDateCalculator>()!,
			s.GetService<IPersistence>()!,
			s.GetService<IPriorityManager>()!,
			identity));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedRecurrentWorker(
		string identity,
		Func<IServiceProvider, IRecurrentWorker> implementationFactory,
		Action<RecurrentScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedRecurrentWorkerService(
			s.GetService<ILogger<DistributedRecurrentWorkerService>>()!,
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetService<RecurrentWorkerExecutionDateCalculator>()!,
			s.GetService<IPersistence>()!,
			s.GetService<IPriorityManager>()!,
			identity));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedCronWorker<TWorker>(
		string identity,
		Action<CronScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, ICronWorker
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedCronWorkerService(
			s.GetService<ILogger<DistributedCronWorkerService>>()!,
			() => s.GetService<TWorker>()!,
			scheduleBuilder.Build(),
			s.GetService<CronWorkerExecutionDateCalculator>()!,
			s.GetService<IPersistence>()!,
			s.GetService<IPriorityManager>()!,
			identity));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedCronWorker(
		string identity,
		Func<IServiceProvider, ICronWorker> implementationFactory,
		Action<CronScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedCronWorkerService(
			s.GetService<ILogger<DistributedCronWorkerService>>()!,
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetService<CronWorkerExecutionDateCalculator>()!,
			s.GetService<IPersistence>()!,
			s.GetService<IPriorityManager>()!,
			identity));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedWorkloadWorker<TWorker>(
		string identity,
		Action<WorkloadScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IWorkloadWorker
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.AddTransient<IDistributedWorkerService>(s => new DistributedWorkloadWorkerService(
			s.GetService<ILogger<DistributedWorkloadWorkerService>>()!,
			() => s.GetService<TWorker>()!,
			scheduleBuilder.Build(),
			s.GetService<WorkloadWorkerExecutionDelayCalculator>()!,
			s.GetService<IPersistence>()!,
			s.GetService<IPriorityManager>()!,
			identity));
		return this;
	}

	public DistributedWorkersRegistrationBuilder AddDistributedWorkloadWorker(
		string identity,
		Func<IServiceProvider, IWorkloadWorker> implementationFactory,
		Action<WorkloadScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<IDistributedWorkerService>(s => new DistributedWorkloadWorkerService(
			s.GetService<ILogger<DistributedWorkloadWorkerService>>()!,
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetService<WorkloadWorkerExecutionDelayCalculator>()!,
			s.GetService<IPersistence>()!,
			s.GetService<IPriorityManager>()!,
			identity));
		return this;
	}
}