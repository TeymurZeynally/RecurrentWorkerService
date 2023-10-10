
using System.Diagnostics;
using InfluxLineProtocol;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Exporter.InfluxLineProtocolFile.OpenTelemetry.Exporter.InfluxLineProtocolFile
{
	public class InfluxLineProtocolFileActivityExporter : BaseExporter<Activity>
	{
		private readonly string _fileName;

		public InfluxLineProtocolFileActivityExporter(string fileName)
		{
			_fileName = fileName;
		}

		public override ExportResult Export(in Batch<Activity> batch)
		{
			foreach (var activity in batch)
			{
				var influxTagsDictionary = new Dictionary<string, string>();
				var influxFieldsDictionary = new Dictionary<string, object?>();

				influxTagsDictionary.Add("kind", activity.Kind switch
				{
					ActivityKind.Internal => "SPAN_KIND_INTERNAL",
					ActivityKind.Client => "SPAN_KIND_CLIENT",
					ActivityKind.Server => "SPAN_KIND_SERVER",
					ActivityKind.Producer => "SPAN_KIND_PRODUCER",
					ActivityKind.Consumer => "SPAN_KIND_CONSUMER",
					_ => throw new ArgumentOutOfRangeException()
				});

				influxTagsDictionary.Add("name", activity.OperationName);
				influxTagsDictionary.Add("otel.library.name", activity.Source.Name);

				if (!string.IsNullOrWhiteSpace(activity.Source.Version))
				{
					influxTagsDictionary.Add("otel.library.version", activity.Source.Version);
				}

				if (activity.ParentSpanId != default)
				{
					influxTagsDictionary.Add("parent_span_id", activity.ParentSpanId.ToString());
				}

				influxTagsDictionary.Add("span_id", activity.SpanId.ToString());
				influxTagsDictionary.Add("trace_id", activity.TraceId.ToString());

				influxFieldsDictionary.Add("duration_nano", (long)activity.Duration.TotalNanoseconds);

				if (activity.TagObjects.Any())
				{
					foreach (var tag in activity.TagObjects)
					{
						influxFieldsDictionary.Add(tag.Key, tag.Value);
					}
				}

				var resource = ParentProvider.GetResource();
				if (resource != Resource.Empty)
				{
					foreach (var resourceAttribute in resource.Attributes)
					{
						influxFieldsDictionary.Add(resourceAttribute.Key, resourceAttribute.Value.ToString() ?? string.Empty);
					}
				}

				File.AppendAllLines(_fileName, new[] { InfluxLine.Create("spans", influxTagsDictionary, influxFieldsDictionary, activity.StartTimeUtc) });
			}

			return ExportResult.Success;
		}
	}
}
