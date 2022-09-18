namespace EventsAnalyser.Helpers
{
	internal static class TimeSpanHelper
	{
		public static System.TimeSpan FromNanoseconds(long nanoseconds)
			=> new(nanoseconds / 100);

		public static System.TimeSpan FromNanoseconds(double nanoseconds)
			=> new((long)nanoseconds / 100);
	}
}
