namespace ExperimentRunner.Models;

public class DockerApplication
{
    public string Name { get; init; }
    
    public NetworkParameters? NetworkSettings { get; init; }
}
