using EventsAnalyser.Builders;
using EventsAnalyser.Calculators;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;
using NCrontab;

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
		var isCron = identity.Contains("Cron", StringComparison.OrdinalIgnoreCase);
		if (period.TotalMinutes != 1 && isCron) throw new NotSupportedException("Cron with period more than minute is nit supported by this analyser"); 


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
			async () => await _queryApi.QueryAsync<PeriodicOperationDuration>(query, "TZ")).ConfigureAwait(false);

		foreach (var operation in data)
		{
			if (previous != null)
			{
				var expectedDate = default(DateTimeOffset);

				var delta = default(TimeSpan);
				if (isCron)
				{
					var previousEnd = previous.DateTimeOffset + previous.Duration;
					expectedDate = (DateTimeOffset)CrontabSchedule.Parse("* * * * *").GetNextOccurrence(previousEnd.UtcDateTime);
					delta = operation.DateTimeOffset - expectedDate;
				}
				else
				{
					expectedDate = previous.DateTimeOffset + (previous.Duration > period ? previous.Duration : period);
					delta = operation.DateTimeOffset - expectedDate;
				}

				deltas.Add(delta);
			}

			previous = operation;
		}


		
		deltas.Count.Should().NotBe(0);
		
		
		return AnalysisResultCalculator.Calculate(identity, deltas.Select(v => (v.TotalNanoseconds, string.Empty)).ToArray());
	}
}