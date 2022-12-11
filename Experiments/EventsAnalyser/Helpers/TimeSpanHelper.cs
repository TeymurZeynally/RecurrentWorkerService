namespace EventsAnalyser.Helpers;

internal static class TimeSpanHelper
{
	public static TimeSpan FromNanoseconds(long nanoseconds)
		=> new(nanoseconds / TimeSpan.NanosecondsPerTick);

	public static TimeSpan FromNanoseconds(double nanoseconds)
		=> new((long)nanoseconds / TimeSpan.NanosecondsPerTick);
}