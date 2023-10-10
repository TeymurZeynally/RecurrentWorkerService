using Microsoft.Extensions.Configuration;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Schedules.WorkloadScheduleModels;
using System.Diagnostics;

namespace RecurrentWorkerService.Configuration.Builders;

public class WorkloadScheduleBuilder : ScheduleBuilder
{
	internal WorkloadScheduleBuilder(WorkloadSchedule schedule) : base(schedule)
	{
		_schedule = schedule;
	}

	public WorkloadScheduleBuilder SetRange(TimeSpan periodFrom, TimeSpan periodTo)
	{
		_schedule.PeriodFrom = periodFrom;
		_schedule.PeriodTo = periodTo;

		return this;
	}

	public WorkloadScheduleBuilder SetStrategies(Action<WorkloadStrategyBuilder> config)
	{
		var builder = new WorkloadStrategyBuilder();
		config(builder);
		_schedule.Strategies = builder.Build();
		return this;
	}

	public WorkloadScheduleBuilder FromConfigSection(IConfigurationSection configurationSection)
	{
		var schedule = configurationSection.Get<WorkloadSchedule>();
		Debug.Assert(schedule != null, "Config section does not contain data for schedule");

		_schedule.RetryOnFailDelay = schedule.RetryOnFailDelay;
		_schedule.PeriodFrom = schedule.PeriodFrom;
		_schedule.PeriodTo = schedule.PeriodTo;
		_schedule.Strategies = schedule.Strategies;

		return this;
	}

	internal new WorkloadSchedule Build()
	{
		Debug.Assert(_schedule.PeriodFrom >= TimeSpan.Zero, "PeriodFrom can not be negative");
		Debug.Assert(_schedule.PeriodTo >= TimeSpan.Zero, "PeriodTo can not be negative");
		Debug.Assert(_schedule.RetryOnFailDelay == null || _schedule.RetryOnFailDelay >= TimeSpan.Zero, "RetryOnFailDelay can not be negative");
		Debug.Assert(_schedule.Strategies != null, "Strategies can not be null");
		Debug.Assert(!_schedule.Strategies.Any(x => x is { Action: StrategyAction.Divide, ActionCoefficient: 0 }), "Strategy can not contain division by zero");

		return _schedule;
	}

	private readonly WorkloadSchedule _schedule;
}