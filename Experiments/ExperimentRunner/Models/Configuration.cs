namespace ExperimentRunner.Models;

public class Configuration
{
    public string PlanExecutionsFile { get; init; } = default!;
    
    public TimeSpan DelayBetweenTests { get; init; }
    
    public ExperimentPlanItem[] Plan { get; init; } = default!;
}
