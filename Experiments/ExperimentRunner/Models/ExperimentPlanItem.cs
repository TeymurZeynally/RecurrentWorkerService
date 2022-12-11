namespace ExperimentRunner.Models;

public class ExperimentPlanItem
{
    public string Name { get; init; } = default!;
    
    public TimeSpan TestDuration { get; init; } = default!;
    
    public DockerApplication[] Storages { get; init; } = default!;
    
    public DockerApplication[] Applications { get; init; } = default!;
}
