namespace ExperimentalApplication.Payloads
{
	internal interface IPayload
	{
		Task ExecuteAsync(CancellationToken cancellationToken);
	}
}
