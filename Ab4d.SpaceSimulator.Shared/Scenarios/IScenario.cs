using Ab4d.SpaceSimulator.Physics;
using Ab4d.SpaceSimulator.Visualization;

namespace Ab4d.SpaceSimulator.Scenarios;

public interface IScenario
{
    /// <summary>
    /// Return scenario name.
    /// </summary>
    /// <returns></returns>
    public string GetName();

    /// <summary>
    /// Set up the scenario; create the mass bodies and their corresponding visualizations.
    /// </summary>
    /// <param name="physicsEngine"></param>
    /// <param name="visualizationEngine"></param>
    /// <param name="planetTextureLoader"></param>
    public void SetupScenario(PhysicsEngine physicsEngine, VisualizationEngine visualizationEngine, PlanetTextureLoader planetTextureLoader);

    /// <summary>
    /// Returns the name of default view (i.e., the name of celestial body that should be centered by default).
    /// </summary>
    /// <returns></returns>
    public string? GetDefaultView();
}
