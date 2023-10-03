using System.Globalization;
using System.Text;

namespace InfluxLineProtocol
{
	public static class InfluxLine
	{
		public static string Create(string measurement, IReadOnlyDictionary<string, object?> fields, DateTime timestamp)
			=> Create(measurement, new Dictionary<string, string>(), fields, new DateTimeOffset(timestamp));
		
		public static string Create(string measurement, IReadOnlyDictionary<string, object?> fields, DateTimeOffset timestamp)
			=> Create(measurement, new Dictionary<string, string>(), fields, timestamp);

		public static string Create(string measurement, IReadOnlyDictionary<string, string> tags, IReadOnlyDictionary<string, object?> fields, DateTime timestamp)
			=> Create(measurement, tags, fields, new DateTimeOffset(timestamp));

		public static string Create(string measurement, IReadOnlyDictionary<string, string> tags, IReadOnlyDictionary<string, object?> fields, DateTimeOffset timestamp)
		{
			if(!fields.Any()) throw new ArgumentException("Influx line protocol should have at least one field", nameof(fields));

			return $"{measurement}{(tags.Any() ? $",{ToTagsString(tags)}" : string.Empty)} {ToFieldsString(fields)} {GetUnixNanoseconds(timestamp):0}";
		}

		private static string ToTagsString(IReadOnlyDictionary<string, string> tags)
		 => string.Join(",", tags
			 .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
			 .Select(x => $"{InfluxEscape(x.Key)}={InfluxEscape(x.Value)}"));

		private static string ToFieldsString(IReadOnlyDictionary<string, object?> fields)
		{
			var stringBuilder = new StringBuilder();

			var first = true;
			foreach (var field in fields)
			{
				var valueString = "\"\"";

				if (field.Value != null)
				{
					valueString = field.Value switch
					{
						int => $"{field.Value}i",
						long => $"{field.Value}i",
						uint => $"{field.Value}u",
						ulong => $"{field.Value}u",
						float => $"{fields.Values}",
						double => $"{fields.Values}",
						bool => field.Value.ToString(),
						TimeSpan span => span.TotalNanoseconds.ToString("", CultureInfo.InvariantCulture),
						_ => $"\"{field.Value}\"",
					};
				}
				
				stringBuilder.Append($"{(first ? string.Empty : ",")}{field.Key}={valueString}");

				first = false;
			}

			return stringBuilder.ToString();
		}


		private static double GetUnixNanoseconds(DateTimeOffset dateTimeOffset)
			=> (dateTimeOffset - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalNanoseconds;

		private static string InfluxEscape(string input)
			=> input.Replace(@"\", @"\\").Replace(" ", @"\ ").Replace("\"", "\\\"").Replace(",", @"\,");
	}
}