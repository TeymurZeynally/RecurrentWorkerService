using EventsAnalyser.Helpers;
using InfluxDB.Client.Core;

namespace EventsAnalyser.Queries.Models;

internal class WorkloadOperationDuration
{
	[Column("duration_nano")]
	public long DurationNanoseconds { get; set; }

	[Column("workload")]
	public byte Workload { get; set; }

	[Column("last-delay")]
	public long LastDelayNanoseconds { get; set; }

	[Column(IsTimestamp = true)]
	public DateTime DateTime { get; set; }

	public DateTimeOffset DateTimeOffset => new(DateTime);

	public TimeSpan Duration => TimeSpanHelper.FromNanoseconds(DurationNanoseconds);

	public TimeSpan LastDelay => TimeSpanHelper.FromNanoseconds(LastDelayNanoseconds);
}