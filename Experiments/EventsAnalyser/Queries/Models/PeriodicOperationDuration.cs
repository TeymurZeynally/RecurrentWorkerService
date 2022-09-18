using EventsAnalyser.Helpers;
using InfluxDB.Client.Core;

namespace EventsAnalyser.Queries.Models
{
	internal class PeriodicOperationDuration
	{
		[Column("duration_nano")]
		public long DurationNanoseconds { get; set; }

		[Column(IsTimestamp = true)]
		public DateTime DateTime { get; set; }

		public DateTimeOffset DateTimeOffset => new(DateTime);

		public TimeSpan Duration => TimeSpanHelper.FromNanoseconds(DurationNanoseconds);
	}
}
