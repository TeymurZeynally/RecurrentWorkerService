using System.Diagnostics;
using Microsoft.Extensions.Configuration;
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
		_schedule.CronExpression = expression;
		return this;
	}

	public CronScheduleBuilder FromConfigSection(IConfigurationSection configurationSection)
	{
		var schedule = configurationSection.Get<CronSchedule>();
		Debug.Assert(schedule != null, "Config section does not contain data for schedule");

		_schedule.RetryOnFailDelay = schedule.RetryOnFailDelay;
		_schedule.CronExpression = schedule.CronExpression;

		return this;
	}

	internal new CronSchedule Build()
	{
		Debug.Assert(_schedule.CronExpression != null, "Cron expression is not provided");
		Debug.Assert(_schedule.RetryOnFailDelay == null || _schedule.RetryOnFailDelay >= TimeSpan.Zero, "RetryOnFailDelay can not be negative");

		return _schedule;
	}

	private readonly CronSchedule _schedule;
}