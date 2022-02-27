using RecurrentWorkerService.Extensions;
using RecurrentWorkerService.Schedules;

namespace RecurrentWorkerService.Services.Calculators;

internal class RecurrentWorkerExecutionDelayCalculator
{
	public TimeSpan Calculate(RecurrentSchedule schedule, TimeSpan elapsed, bool isError)
	{
		if (isError && schedule.RetryOnFailDelay != null)
		{
			return schedule.RetryOnFailDelay.Value;
		}

		var delay = schedule.Period - elapsed;
		return TimeSpanExtensions.Max(delay, TimeSpan.Zero);
	}
}