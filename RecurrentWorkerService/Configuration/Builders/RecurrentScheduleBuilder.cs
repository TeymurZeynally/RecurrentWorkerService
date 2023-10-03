using Microsoft.Extensions.Configuration;
using RecurrentWorkerService.Schedules;
using System.Diagnostics;

namespace RecurrentWorkerService.Configuration.Builders;

public class RecurrentScheduleBuilder : ScheduleBuilder
{
	internal RecurrentScheduleBuilder(RecurrentSchedule schedule) : base(schedule)
	{
		_schedule = schedule;
	}

	public RecurrentScheduleBuilder SetPeriod(TimeSpan period)
	{
		_schedule.Period = period;
		return this;
	}

	public RecurrentScheduleBuilder FromConfigSection(IConfigurationSection configurationSection)
	{
		var schedule = configurationSection.Get<RecurrentSchedule>();
		Debug.Assert(schedule != null, "Config section does not contain data for schedule");

		_schedule.RetryOnFailDelay = schedule.RetryOnFailDelay;
		_schedule.Period = schedule.Period;

		return this;
	}

	internal new RecurrentSchedule Build()
	{
		Debug.Assert(_schedule.Period < TimeSpan.Zero, "Period can not be negative");
		Debug.Assert(_schedule.RetryOnFailDelay != null && _schedule.RetryOnFailDelay < TimeSpan.Zero, "RetryOnFailDelay can not be negative");

		return _schedule;
	}

	private readonly RecurrentSchedule _schedule;
}