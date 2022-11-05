using EventsAnalyser.Builders;
using EventsAnalyser.Calculators;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Queries;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;

namespace EventsAnalyser.Analysers;

internal class PersistenceOperationsDurationAnalyser
{
	private readonly QueryApi _queryApi;

	public PersistenceOperationsDurationAnalyser(QueryApi queryApi)
	{
		_queryApi = queryApi;
	}

	public async Task<AnalysisResult[]> Analyse(Interval interval)
	{
		var parameters = QueryParametersBuilder.Build(new
		{
			startTimeStamp = interval.StartTimeStamp.UtcDateTime.ToString("O"),
			endTimeStamp = interval.EndTimeStamp.UtcDateTime.ToString("O"),
		});
		var query = parameters + await File.ReadAllTextAsync("Queries/QueryPersistenceOperationsDuration.txt").ConfigureAwait(false);

		Console.WriteLine(query);

		var operations = await _queryApi.QueryAsync<PersistenceOperation>(query, "TZ").ConfigureAwait(false);

		operations.Count.Should().NotBe(0);
		
		return operations
			.GroupBy(x => x.Name)
			.Select(x => AnalysisResultCalculator.Calculate(x.Key, x.Select(v => ((double)v.DurationNanoseconds, v.TraceId)).ToArray()))
			.ToArray();
	}
}