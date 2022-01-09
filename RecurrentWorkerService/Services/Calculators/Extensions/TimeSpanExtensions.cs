namespace RecurrentWorkerService.Services.Calculators.Extensions;

internal static class TimeSpanExtensions
{
	public static TimeSpan Max(TimeSpan firstTimeSpan, TimeSpan secondTimeSpan)
	{
		return firstTimeSpan > secondTimeSpan ? firstTimeSpan: secondTimeSpan;
	}
}