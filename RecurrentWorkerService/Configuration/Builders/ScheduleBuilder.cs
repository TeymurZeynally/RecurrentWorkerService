using RecurrentWorkerService.Schedules;

namespace RecurrentWorkerService.Configuration.Builders;

public class ScheduleBuilder
{
	internal ScheduleBuilder(Schedule schedule)
	{
		_schedule = schedule;
	}

	public ScheduleBuilder SetRetryOnFailDelay(TimeSpan delay)
	{
		_schedule.RetryOnFailDelay = delay;
		return this;
	}

	internal Schedule Build()
	{
		return _schedule;
	}

	private readonly Schedule _schedule;
}