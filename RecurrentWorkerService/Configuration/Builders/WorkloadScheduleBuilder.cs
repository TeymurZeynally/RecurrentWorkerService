using RecurrentWorkerService.Schedules;

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

	internal new WorkloadSchedule Build()
	{
		return _schedule;
	}

	private readonly WorkloadSchedule _schedule;
}