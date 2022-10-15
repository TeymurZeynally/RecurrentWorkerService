namespace EventsAnalyser.Helpers
{
	internal static class TimeSpanHelper
	{
		public static System.TimeSpan FromNanoseconds(long nanoseconds)
			=> new(nanoseconds / 100);

		public static System.TimeSpan FromNanoseconds(double nanoseconds)
			=> new((long)nanoseconds / 100);
		
		public static long ToNanoseconds(TimeSpan timeSpan)
			=> timeSpan.Ticks * 100;
		
		public static long ToNanoseconds(long ticks)
			=> ticks * 100;
	}
}
