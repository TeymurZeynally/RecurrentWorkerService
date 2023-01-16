using EventsAnalyser.Helpers;
using InfluxDB.Client.Core;

namespace EventsAnalyser.Queries.Models;

internal class PrioritiesReceiveData
{
	[Column(IsTimestamp = true)]
	public DateTime DateTime { get; set; }

	[Column("name")]
	public string Name { get; set; }
		
	[Column("trace_id")]
	public string TraceId { get; set; }
		
	[Column("span_id")]
	public string SpanId { get; set; }
		
	[Column("node")]
	public string Node { get; set; }
		
	[Column("priority_event_id")]
	public string PriorityEventId { get; set; }

	public DateTimeOffset DateTimeOffset => new(DateTime);
}