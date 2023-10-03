using EventsAnalyser.Builders;
using EventsAnalyser.Calculators;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;
using RecurrentWorkerService.Extensions;
using RecurrentWorkerService.Schedules;
using RecurrentWorkerService.Services.Calculators;

namespace EventsAnalyser.Analysers;

internal class PeriodicWorkloadOperationsAnalyser
{
	private readonly QueryApi _queryApi;

	public PeriodicWorkloadOperationsAnalyser(QueryApi queryApi)
	{
		_queryApi = queryApi;
	}

	public async Task<AnalysisResult> Analyse(Interval interval, WorkloadSchedule schedule, PayloadType payload, string identity)
	{
		var name = $"WorkloadWorker.ExecuteAsync-{payload}Payload";
		var parameters = QueryParametersBuilder.Build(
			new
			{
				worker_name = name,
				identity,
				startTimeStamp = interval.StartTimeStamp.UtcDateTime.ToString("O"),
				endTimeStamp = interval.EndTimeStamp.UtcDateTime.ToString("O"),
			});
		var query = parameters + await File.ReadAllTextAsync("Queries/QueryWorkloadOperationsTimeAndDuration.txt").ConfigureAwait(false);

		Console.WriteLine(query);

		var calcualtor = new WorkloadWorkerExecutionDelayCalculator();

		var deltas = new List<TimeSpan>();

		var previous = default(WorkloadOperationDuration);

		var data = await Cache.Cache.Get<WorkloadOperationDuration>(
			query,
			async () => await _queryApi.QueryAsync<WorkloadOperationDuration>(query, "TZ")).ConfigureAwait(false);

		foreach (var operation in data)
		{
			if (previous != null)
			{
				var previousEndDate = previous.DateTimeOffset + previous.Duration;
				var actualDelay = operation.DateTimeOffset - previousEndDate;

				var delay = calcualtor.Calculate(schedule, previous.LastDelay, previous.Workload, false);
				delay = TimeSpanExtensions.Max(delay - previous.Duration, TimeSpan.Zero);

				var delta = actualDelay - delay;

				deltas.Add(delta);
				//Console.WriteLine(delta);
			}

			previous = operation;
		}

		deltas.Count.Should().NotBe(0);

		return AnalysisResultCalculator.Calculate(identity, deltas.Select(v => (v.TotalNanoseconds, string.Empty)).ToArray());
	}
}