﻿using Application.Helpers;
using RecurrentWorkerService.Workers;

namespace Application;

internal class RecurrentWorker2 : IRecurrentWorker
{
	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		Console.WriteLine($"{DateTimeOffset.UtcNow} RecurrentWorker2 Start");
		if (!FailHelper.IsFail())
		{
			await Task.CompletedTask;
			throw new Exception("FAIL");
		}

	}
}