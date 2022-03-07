namespace RecurrentWorkerService.Distributed.Prioritization.Providers;

internal interface IPriorityProvider
{
	byte[] GetPrioritiesAsc(string identity);

	byte GetPriority(string identity);

	void UpdatePriorityInformation((string Identity, long NodeId, byte Priority)[] priorities);
}