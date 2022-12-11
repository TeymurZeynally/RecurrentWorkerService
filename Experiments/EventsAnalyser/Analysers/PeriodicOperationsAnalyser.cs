using EventsAnalyser.Builders;
using EventsAnalyser.Calculators;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;

namespace EventsAnalyser.Analysers;

internal class PeriodicOperationsAnalyser
{
	private readonly QueryApi _queryApi;

	public PeriodicOperationsAnalyser(QueryApi queryApi)
	{
		_queryApi = queryApi;
	}

	public async Task<AnalysisResult> Analyse(Interval interval, TimeSpan period, PayloadType payload, string identity)
	{
		var name = $"{payload}Payload.ExecuteAsync";
		var parameters = QueryParametersBuilder.Build(
			new
			{
				name,
				identity,
				startTimeStamp = interval.StartTimeStamp.UtcDateTime.ToString("O"),
				endTimeStamp = interval.EndTimeStamp.UtcDateTime.ToString("O"),
			});
		var query = parameters + await File.ReadAllTextAsync("Queries/QueryOperationsTimeAndDuration.txt").ConfigureAwait(false);

		//Console.WriteLine(query);

		var previous = default(PeriodicOperationDuration);
		var deltas = new List<TimeSpan>();

		var data = await Cache.Cache.Get<PeriodicOperationDuration>(
			query,
			async () => await _queryApi.QueryAsync<PeriodicOperationDuration>(query, "KSS")).ConfigureAwait(false);

		foreach (var operation in data)
		{
			if (previous != null)
			{
				var expectedDate = previous.DateTimeOffset + (previous.Duration > period ? previous.Duration : period);

				deltas.Add(operation.DateTimeOffset - expectedDate);

				//Console.WriteLine($"{previous.DateTimeOffset} | {previous.Duration} | {expectedDate} | {operation.DateTimeOffset} | {operation.Duration}  | {deltas.Last()}");
			}


			previous = operation;
		}
		
		deltas.Count.Should().NotBe(0);
		
		
		return AnalysisResultCalculator.Calculate(identity, deltas.Select(v => (v.TotalNanoseconds, string.Empty)).ToArray());
	}
}