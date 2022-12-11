namespace ExperimentRunner.Models;

public class DockerApplication
{
    public string Name { get; init; } = default!;
    
    public NetworkParameters? NetworkSettings { get; init; } = default!;
}
