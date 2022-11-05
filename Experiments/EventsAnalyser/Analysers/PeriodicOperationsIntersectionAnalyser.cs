using EventsAnalyser.Builders;
using EventsAnalyser.Queries;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;

namespace EventsAnalyser.Analysers;

internal class PeriodicOperationsIntersectionAnalyser
{
	private readonly QueryApi _queryApi;

	public PeriodicOperationsIntersectionAnalyser(QueryApi queryApi)
	{
		_queryApi = queryApi;
	}

	public async Task<bool> Analyse(Interval interval, TimeSpan period, PayloadType payload, string identity)
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

		Console.WriteLine(query);
			
		var previous = default(PeriodicOperationDuration);

		await foreach (var operation in _queryApi.QueryAsyncEnumerable<PeriodicOperationDuration>(query, "TZ").ConfigureAwait(false))
		{
			if (previous != null)
			{
				var previousOperationEndTimestamp = previous.DateTimeOffset + previous.Duration;
				if (operation.DateTimeOffset < previousOperationEndTimestamp)
				{
					return false;
				}
			}
				
			previous = operation;
		}

		previous.Should().NotBeNull();

		return true;
	}
}