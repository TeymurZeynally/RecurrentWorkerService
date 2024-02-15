using RecurrentWorkerService.Distributed.Configuration.Settings;

namespace RecurrentWorkerService.Configuration.Builders;

public class DistributedMultiiterationWorkerSettingsBuilder 
{
	public DistributedMultiiterationWorkerSettingsBuilder SetMultiIterationOnNodeMaxDuration(TimeSpan mexDuration)
	{
		_mltiIterationOnNodeMaxDuration = mexDuration;
		return this;
	}

	internal new MultiiterationWorkerSettings Build()
	{
		return new MultiiterationWorkerSettings { MultiIterationOnNodeMaxDuration = _mltiIterationOnNodeMaxDuration };
	}

	private TimeSpan _mltiIterationOnNodeMaxDuration = TimeSpan.FromMinutes(1);
}