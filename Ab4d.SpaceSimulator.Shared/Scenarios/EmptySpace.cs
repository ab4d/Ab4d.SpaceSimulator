using Ab4d.SpaceSimulator.Physics;
using Ab4d.SpaceSimulator.Visualization;

namespace Ab4d.SpaceSimulator.Scenarios;

public class EmptySpace : IScenario
{
    public string GetName()
    {
        return "Empty space";
    }

    public void SetupScenario(PhysicsEngine physicsEngine, VisualizationEngine visualizationEngine,
        PlanetTextureLoader planetTextureLoader)
    {
        // No-op
    }

    public string? GetDefaultView()
    {
        return null; // None available
    }

    public float? GetDefaultCameraDistance()
    {
        return null; // Use whatever fallback the calling code uses.
    }

    public int[]? GetSimulationSpeedIntervals()
    {
        return null; // No custom intervals
    }

    public (double, int)? GetSimulationStepSettings()
    {
        return null; // No custom settings
    }
}
