using EventsAnalyser.Builders;
using EventsAnalyser.Calculators;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;

namespace EventsAnalyser.Analysers;

internal class LibAndCodeOperationsAnalyser
{
	private readonly QueryApi _queryApi;

	public LibAndCodeOperationsAnalyser(QueryApi queryApi)
	{
		_queryApi = queryApi;
	}

	public async Task<AnalysisResult> Analyse(Interval interval, string name, PayloadType payload, string identity)
	{
		var payloadName = $"{payload}Payload.ExecuteAsync";
		var parameters = QueryParametersBuilder.Build(
			new
			{
				name, payload = payloadName,
				identity,
				startTimeStamp = interval.StartTimeStamp.UtcDateTime.ToString("O"),
				endTimeStamp = interval.EndTimeStamp.UtcDateTime.ToString("O"),
			});
		var query = parameters + await File.ReadAllTextAsync("Queries/QueryLibAndCodeOperationsDuration.txt").ConfigureAwait(false);

		//Console.WriteLine(query);

		var results = new List<TimeSpan>();
		var data = await Cache.Cache.Get<LibAndCodeOperationsDuration>(
			query,
			async () => await _queryApi.QueryAsync<LibAndCodeOperationsDuration>(query, "TZ")).ConfigureAwait(false);

		foreach (var operation in data)
		{
			results.Add(operation.LibDuration - operation.LockDuration - operation.CodeDuration);

			if (results.Last() > TimeSpan.FromSeconds(1))
			{
				//Console.WriteLine(operation.TraceId);
			}

			//Console.WriteLine($"{operation.CodeDuration} | {operation.LibDuration} | {operation.LockDuration} | {operation.LibDuration - operation.LockDuration - operation.CodeDuration}");
		}

		results.Count.Should().NotBe(0);
		
		return AnalysisResultCalculator.Calculate(identity, results.Select(v => (v.TotalNanoseconds, string.Empty)).ToArray());
	}
}