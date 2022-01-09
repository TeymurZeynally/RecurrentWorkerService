using RecurrentWorkerService.Schedules;

namespace RecurrentWorkerService.Configuration.Builders;

public class CronScheduleBuilder : ScheduleBuilder
{
	internal CronScheduleBuilder(CronSchedule schedule) : base(schedule)
	{
		_schedule = schedule;
	}

	public CronScheduleBuilder SetCronExpression(string expression)
	{
		_schedule.Expression = expression;
		return this;
	}

	internal new CronSchedule Build()
	{
		return _schedule;
	}

	private readonly CronSchedule _schedule;
}