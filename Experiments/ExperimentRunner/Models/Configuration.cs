namespace ExperimentRunner.Models;

public class Configuration
{
    public string PlanExecutionsFile { get; init; }
    
    public TimeSpan DelayBetweenTests { get; init; }
    
    public ExperimentPlanItem[] Plan { get; init; }
}
