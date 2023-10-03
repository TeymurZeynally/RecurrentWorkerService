using OpenTelemetry.Trace;

namespace OpenTelemetry.Exporter.InfluxLineProtocolFile.OpenTelemetry.Exporter.InfluxLineProtocolFile
{
	public static class InfluxLineProtocolFileExporterHelperExtensions
	{
		public static TracerProviderBuilder AddInfluxLineProtocolFileExporter(this TracerProviderBuilder builder, string fileName)
		{
			return builder.AddProcessor(new SimpleActivityExportProcessor(new InfluxLineProtocolFileActivityExporter(fileName)));
		}
	}
}