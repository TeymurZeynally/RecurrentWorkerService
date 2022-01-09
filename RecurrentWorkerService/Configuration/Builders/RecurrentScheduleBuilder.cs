using RecurrentWorkerService.Schedules;

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

	internal new RecurrentSchedule Build()
	{
		return _schedule;
	}

	private readonly RecurrentSchedule _schedule;
}