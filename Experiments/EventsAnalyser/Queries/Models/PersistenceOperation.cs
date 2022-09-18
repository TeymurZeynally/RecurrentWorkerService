using EventsAnalyser.Helpers;
using InfluxDB.Client.Core;

namespace EventsAnalyser.Queries.Models
{
	internal class PersistenceOperation
	{
		[Column("duration_nano")]
		public long DurationNanoseconds { get; set; }

		[Column("name")]
		public string Name { get; set; }


		[Column("trace_id")]
		public string TraceId { get; set; }

		public TimeSpan Duration => TimeSpanHelper.FromNanoseconds(DurationNanoseconds);
	}
}
