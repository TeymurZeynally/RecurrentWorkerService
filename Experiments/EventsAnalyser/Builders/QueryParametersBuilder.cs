namespace EventsAnalyser.Builders;

internal static class QueryParametersBuilder
{
	public static string Build(KeyValuePair<string, string>[] parameters)
	{
		return string.Join(null, parameters.Select(x => $"{x.Key} = \"{x.Value}\"{Environment.NewLine}"));
	}

	public static string Build(object parameters)
	{
		return Build(
			parameters
				.GetType()
				.GetProperties()
				.Select(property => new KeyValuePair<string,string>(property.Name, property.GetValue(parameters)?.ToString()!))
				.ToArray());
	}
}