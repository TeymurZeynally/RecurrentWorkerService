// See https://aka.ms/new-console-template for more information

using EventsAnalyser.Analysers;
using EventsAnalyser.Helpers;
using InfluxDB.Client;


var options = new InfluxDBClientOptions.Builder()
	.Url("http://localhost:8086")
	.AuthenticateToken(@"OyjJEJ9mARG6V8FVgI-EcsQaLvpxzfq3Rx2IQ8zjI5H-nYe4H8yHotdQtF3zZKIhtxe9eWnHP-3uX04CkUCsHg==")
	.TimeOut(TimeSpan.FromMinutes(10))
	.Build();
var influxDbClient = InfluxDBClientFactory.Create(options);

var queryApi = influxDbClient.GetQueryApi();


/*
var result = await new PeriodicOperationsValidator(queryApi).Validate(TimeSpan.FromSeconds(1), PayloadType.Fast, "Recurrent-Fast");

foreach (var r in result)
{
	Console.WriteLine(r);
	
}

*/


/*
var result = await new LibAndCodeOperationsValidator(queryApi).Validate("DistributedRecurrentWorkerService", PayloadType.Fast, "Recurrent-Fast");

foreach (var r in result)
{
	Console.WriteLine(r);

}

*/

var results = await new PersistenceOperationsDurationAnalyser(queryApi).Analyse();

foreach (var result in results)
{
	Console.WriteLine(@$"Parameter: {result.Parameter}");
	Console.WriteLine(@$"Mean: {TimeSpanHelper.FromNanoseconds(result.Mean)}");
	Console.WriteLine(@$"StandardDeviation: {TimeSpanHelper.FromNanoseconds(result.StandardDeviation)}");
	Console.WriteLine(@$"Variance: {result.Variance}");
	Console.WriteLine(@$"Error: {TimeSpanHelper.FromNanoseconds(result.Error)}");
	Console.WriteLine(@$"Errors: {result.Errors.Count}: {string.Join(",", result.Errors.Select(x => $"({x.Value} {x.TraceId})"))}");
	Console.WriteLine();
}