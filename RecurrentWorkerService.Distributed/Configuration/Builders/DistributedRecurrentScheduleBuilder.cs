using RecurrentWorkerService.Configuration.Builders;
using RecurrentWorkerService.Distributed.Schedules;

namespace RecurrentWorkerService.Distributed.Configuration.Builders;

public class DistributedRecurrentScheduleBuilder : RecurrentScheduleBuilder
{
	internal DistributedRecurrentScheduleBuilder(DistributedRecurrentSchedule schedule) : base(schedule)
	{
		_schedule = schedule;
	}

	public DistributedRecurrentScheduleBuilder SetExecutionCount(int executionCount)
	{
		_schedule.ExecutionCount = executionCount;
		return this;
	}

	internal new DistributedRecurrentSchedule Build()
	{
		return _schedule;
	}

	private readonly DistributedRecurrentSchedule _schedule;
}