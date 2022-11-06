﻿using EventsAnalyser.Analysers;
using EventsAnalyser.Calculators.Models;
using EventsAnalyser.Helpers;
using EventsAnalyser.Queries.Models;
using FluentAssertions;
using InfluxDB.Client;

using Interval = EventsAnalyser.Queries.Models.Interval;

var planExecutionsFile = @"/home/teymur/Desktop/Dashboard/ExecutionFiles.csv";

var lines = (await File.ReadAllLinesAsync(planExecutionsFile).ConfigureAwait(false))
	.Select(x => x.Split(","))
	.Select(x => (Interval: new Interval(){ StartTimeStamp = DateTimeOffset.Parse(x[0]), EndTimeStamp = DateTimeOffset.Parse(x[1]) }, Name: x[2]))
	.ToArray();


var options = new InfluxDBClientOptions.Builder()
	.Url("http://localhost:8086")
	.AuthenticateToken(@"S05hCwUie9qDicNHHQAJoGfrIH6tzOapmZjHKQLdvgBR-_fR3nQnwUUd5rzriRceNAZO5kclNymCSpvnmSI6lA==")
	.TimeOut(TimeSpan.FromMinutes(10))
	.Build();
var influxDbClient = InfluxDBClientFactory.Create(options);

var queryApi = influxDbClient.GetQueryApi();


var configurationOfWorkers = new[]
{
	(Id: "Cron-Immediate", Payload: PayloadType.Immediate, Period: TimeSpan.FromMinutes(1)),
	(Id: "Cron-Fast", Payload: PayloadType.Fast, Period: TimeSpan.FromMinutes(1)),
	(Id: "Cron-Slow", Payload: PayloadType.Slow, Period: TimeSpan.FromMinutes(1)),
	(Id: "Recurrent-Immediate", Payload: PayloadType.Immediate, Period: TimeSpan.FromTicks(1)),
	(Id: "Recurrent-Fast", Payload: PayloadType.Fast, Period: TimeSpan.FromSeconds(1)),
	(Id: "Recurrent-Slow", Payload: PayloadType.Slow, Period: TimeSpan.FromSeconds(1)),
};




foreach (var line in lines)
{
	Console.WriteLine();
	Console.WriteLine(line.Name);
	Console.WriteLine();	
	
	List<AnalysisResult> periodicOperationsReults = new List<AnalysisResult>();
	List<AnalysisResult> libAndCodeOperationsReults = new List<AnalysisResult>();
	foreach (var config in configurationOfWorkers)
	{
		var periodicOperationsIntersectionResult = await new PeriodicOperationsIntersectionAnalyser(queryApi).Analyse(line.Interval, config.Period, config.Payload, config.Id);
		var periodicOperationsResult = await new PeriodicOperationsAnalyser(queryApi).Analyse(line.Interval, config.Period, config.Payload, config.Id);
		var libAndCodeOperationsAnalyserResult = await new LibAndCodeOperationsAnalyser(queryApi).Analyse(line.Interval, "DistributedRecurrentWorkerService",  config.Payload, config.Id);

		periodicOperationsIntersectionResult.Should().BeTrue();
		periodicOperationsReults.Add(periodicOperationsResult);
		libAndCodeOperationsReults.Add(libAndCodeOperationsAnalyserResult);
	}

	PrintCsv("PeriodicOperations", periodicOperationsReults.ToArray());
	Console.WriteLine();
	PrintCsv("LibAndCodeOperations", libAndCodeOperationsReults.ToArray());
	Console.WriteLine();

	var persistenceOperationsDurationResults = await new PersistenceOperationsDurationAnalyser(queryApi).Analyse(line.Interval);
	PrintCsv("PersistenceOperationsDuration", persistenceOperationsDurationResults);
	Console.WriteLine();

	var prioritiesReceiveTimestampResult = await new PrioritiesReceiveTimestampAnalyser(queryApi).Analyse(line.Interval, "UpdatePriorityInformation");
	PrintCsv("PrioritiesReceive", prioritiesReceiveTimestampResult);
	Console.WriteLine();

}


void PrintCsv(string name, params AnalysisResult[] analysisResults)
{
	Console.WriteLine("Name,Parameter,Count,Max,Mean,MeanErrorless,StandardDeviation,Variance,Error,Errors");
	foreach (var analysisResult in analysisResults)
	{
		Console.Write(name + ",");
		Console.Write(analysisResult.Parameter + ",");
		Console.Write(TimeSpanHelper.FromNanoseconds(analysisResult.Count) + ",");
		Console.Write(TimeSpanHelper.FromNanoseconds(analysisResult.Max) + ",");
		Console.Write(TimeSpanHelper.FromNanoseconds(analysisResult.Mean) + ",");
		Console.Write(TimeSpanHelper.FromNanoseconds(analysisResult.MeanErrorless) + ",");
		Console.Write(TimeSpanHelper.FromNanoseconds(analysisResult.StandardDeviation) + ",");
		Console.Write(analysisResult.Variance + ",");
		Console.Write(TimeSpanHelper.FromNanoseconds(analysisResult.Error) + ",");
		Console.Write(analysisResult.Errors.Count);
		Console.WriteLine();
	}
}


void Print(AnalysisResult analysisResult)
{
	Console.WriteLine(@$"Parameter: {analysisResult.Parameter}");
	Console.WriteLine(@$"Count: {TimeSpanHelper.FromNanoseconds(analysisResult.Count)}");
	Console.WriteLine(@$"Max: {TimeSpanHelper.FromNanoseconds(analysisResult.Max)}");
	Console.WriteLine(@$"Mean: {TimeSpanHelper.FromNanoseconds(analysisResult.Mean)}");
	Console.WriteLine(@$"MeanErrorless: {TimeSpanHelper.FromNanoseconds(analysisResult.MeanErrorless)}");
	Console.WriteLine(@$"StandardDeviation: {TimeSpanHelper.FromNanoseconds(analysisResult.StandardDeviation)}");
	Console.WriteLine(@$"Variance: {analysisResult.Variance}");
	Console.WriteLine(@$"Error: {TimeSpanHelper.FromNanoseconds(analysisResult.Error)}");
	Console.WriteLine(@$"Errors: {analysisResult.Errors.Count}: {string.Join(",", analysisResult.Errors.Select(x => $"({x.Value} {x.TraceId})"))}");
	Console.WriteLine();
}
