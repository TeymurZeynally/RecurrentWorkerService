using EventsAnalyser.Helpers;
using InfluxDB.Client.Core;

namespace EventsAnalyser.Queries.Models
{
	internal class LibAndCodeOperationsDuration
	{
		[Column("code_duration")]
		public long CodeDurationNano { get; set; }

		[Column("lock_duration")]
		public long LockDurationNano { get; set; }

		[Column("lib_duration")]
		public long LibDurationNano { get; set; }

		[Column("trace_id")]
		public string TraceId { get; set; }

		public TimeSpan CodeDuration => TimeSpanHelper.FromNanoseconds(CodeDurationNano);

		public TimeSpan LibDuration => TimeSpanHelper.FromNanoseconds(LibDurationNano);

		public TimeSpan LockDuration => TimeSpanHelper.FromNanoseconds(LockDurationNano);
	}
}
