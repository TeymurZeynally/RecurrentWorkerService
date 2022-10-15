namespace ExperimentRunner.Models;

public class ExperimentPlanItem
{
    public string Name { get; init; }
    
    public TimeSpan TestDuration { get; init; }
    
    public DockerApplication[] Storages { get; init; }
    
    public DockerApplication[] Applications { get; init; }
}
