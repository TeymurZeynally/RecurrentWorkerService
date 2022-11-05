﻿using EventsAnalyser.Builders;
using EventsAnalyser.Calculators;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Helpers;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;

namespace EventsAnalyser.Analysers;

internal class PrioritiesReceiveTimestampAnalyser
{
	private readonly QueryApi _queryApi;

	public PrioritiesReceiveTimestampAnalyser(QueryApi queryApi)
	{
		_queryApi = queryApi;
	}

	public async Task<AnalysisResult> Analyse(Interval interval, string priorityOperationName)
	{
		var parameters = QueryParametersBuilder.Build(new
		{
			priorityOperationName,
			startTimeStamp = interval.StartTimeStamp.UtcDateTime.ToString("O"),
			endTimeStamp = interval.EndTimeStamp.UtcDateTime.ToString("O"),
		});
		var query = parameters + await File.ReadAllTextAsync("Queries/QueryPrioritiesReceiveTimestamp.txt").ConfigureAwait(false);
			
		Console.WriteLine(query);
			
		var operations = await _queryApi.QueryAsync<PrioritiesReceiveData>(query, "TZ").ConfigureAwait(false);
		
		operations.Count.Should().NotBe(0);
		
		return AnalysisResultCalculator.Calculate(
			priorityOperationName,
			operations.GroupBy(x => x.PriorityEventId)
				.Select(v => ((double)GetMaxDelta(v.ToArray()), v.Key)).ToArray());
	}

	private long GetMaxDelta(PrioritiesReceiveData[] data)
	{
		//Console.WriteLine(data.Length);
		long max = 0;
		for (int i = 0; i < data.Length; i++)
		for (int j = 0; j < data.Length; j++)
			if (i != j)
			{
				var diff = Math.Abs(data[i].DateTimeOffset.Ticks - data[j].DateTimeOffset.Ticks);
				if (diff > max)
				{
					max = diff;
				}
			}
			
		//Console.WriteLine($"{TimeSpan.FromTicks(max)} {TimeSpanHelper.FromNanoseconds(TimeSpanHelper.ToNanoseconds(max))}");

		return TimeSpanHelper.ToNanoseconds(max);
	}
}