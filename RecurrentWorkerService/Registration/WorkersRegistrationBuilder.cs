﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Configuration.Builders;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services;
using RecurrentWorkerService.Services.Calculators;
using RecurrentWorkerService.Workers;

namespace RecurrentWorkerService.Registration;

public class WorkersRegistrationBuilder
{
	private readonly IServiceCollection _services;

	internal WorkersRegistrationBuilder(IServiceCollection services)
	{
		_services = services;
	}

	public WorkersRegistrationBuilder AddRecurrentWorker<TWorker>(Action<RecurrentScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IRecurrentWorker
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.TryAddSingleton<RecurrentWorkerExecutionDelayCalculator>();
		_services.AddTransient<IWorkerService>(s => new Services.RecurrentWorkerService(
			s.GetRequiredService<ILogger<Services.RecurrentWorkerService>>(),
			s.GetRequiredService<TWorker>,
			scheduleBuilder.Build(),
			s.GetRequiredService<RecurrentWorkerExecutionDelayCalculator>()));
		return this;
	}

	public WorkersRegistrationBuilder AddRecurrentWorker(
		Func<IServiceProvider, IRecurrentWorker> implementationFactory,
		Action<RecurrentScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new RecurrentScheduleBuilder(new RecurrentSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.TryAddSingleton<RecurrentWorkerExecutionDelayCalculator>();
		_services.AddTransient<IWorkerService>(s => new Services.RecurrentWorkerService(
			s.GetRequiredService<ILogger<Services.RecurrentWorkerService>>(),
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetRequiredService<RecurrentWorkerExecutionDelayCalculator>()));
		return this;
	}

	public WorkersRegistrationBuilder AddCronWorker<TWorker>(Action<CronScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, ICronWorker
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.TryAddSingleton<CronWorkerExecutionDelayCalculator>();
		_services.AddTransient<IWorkerService>(s => new CronWorkerService(
			s.GetRequiredService<ILogger<CronWorkerService>>(),
			s.GetRequiredService<TWorker>,
			scheduleBuilder.Build(),
			s.GetRequiredService<CronWorkerExecutionDelayCalculator>()));
		return this;
	}

	public WorkersRegistrationBuilder AddCronWorker(
		Func<IServiceProvider, ICronWorker> implementationFactory,
		Action<CronScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new CronScheduleBuilder(new CronSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.TryAddSingleton<CronWorkerExecutionDelayCalculator>();
		_services.AddTransient<IWorkerService>(s => new CronWorkerService(
			s.GetRequiredService<ILogger<CronWorkerService>>(),
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetRequiredService<CronWorkerExecutionDelayCalculator>()));
		return this;
	}

	public WorkersRegistrationBuilder AddWorkloadWorker<TWorker>(Action<WorkloadScheduleBuilder> scheduleBuilderAction)
		where TWorker : class, IWorkloadWorker
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.AddTransient<TWorker>();
		_services.TryAddSingleton<WorkloadWorkerExecutionDelayCalculator>();
		_services.AddTransient<IWorkerService>(s => new WorkloadWorkerService(
			s.GetRequiredService<ILogger<WorkloadWorkerService>>(),
			s.GetRequiredService<TWorker>,
			scheduleBuilder.Build(),
			s.GetRequiredService<WorkloadWorkerExecutionDelayCalculator>()));
		return this;
	}

	public WorkersRegistrationBuilder AddWorkloadWorker(
		Func<IServiceProvider, IWorkloadWorker> implementationFactory,
		Action<WorkloadScheduleBuilder> scheduleBuilderAction)
	{
		var scheduleBuilder = new WorkloadScheduleBuilder(new WorkloadSchedule());
		scheduleBuilderAction(scheduleBuilder);

		_services.TryAddSingleton<WorkloadWorkerExecutionDelayCalculator>();
		_services.AddTransient<IWorkerService>(s => new WorkloadWorkerService(
			s.GetRequiredService<ILogger<WorkloadWorkerService>>(),
			() => implementationFactory(s),
			scheduleBuilder.Build(),
			s.GetRequiredService<WorkloadWorkerExecutionDelayCalculator>()));
		return this;
	}
}