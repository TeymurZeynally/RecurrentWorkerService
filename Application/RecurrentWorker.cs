using Application.Helpers;
using RecurrentWorkerService.Workers;

namespace Application;

internal class RecurrentWorker : IRecurrentWorker
{
	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		Console.WriteLine($"{DateTimeOffset.UtcNow} RecurrentWorker Start");

		if (!FailHelper.IsFail())
		{
			await Task.CompletedTask;
			throw new Exception("FAIL");
		}

		Console.WriteLine($"{DateTimeOffset.UtcNow} RecurrentWorker End");
	}

}