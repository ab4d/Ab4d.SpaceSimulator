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

    /// <summary>
    /// Returns the camera distance that should be used to set up default camera view in case when scenario does not
    /// specify a celestial body on which the view should be cenetered by default (i.e., when the GetDefaultView()
    /// implementation returns null). In such case, the view is reset to look at (0, 0, 0) with specified distance
    /// (which should be in display units).
    /// </summary>
    /// <returns></returns>
    public float? GetDefaultCameraDistance();

    /// <summary>
    /// Returns scenario-specific simulation speed intervals, if applicable.
    /// </summary>
    /// <returns></returns>
    public int[]? GetSimulationSpeedIntervals();

    /// <summary>
    /// Return simulation step settings; the maximum discrete simulation step (in seconds), and the number of maximum
    /// iterations for dynamic scaling. If simulation speed exceeds the specified step value by factor of N, the step
    /// is dynamically adjusted so that N iterations (sub-steps) are performed. This dynamic scaling can be disabled
    /// by setting the value of N to 0.
    /// </summary>
    /// <returns></returns>
    public (double, int)? GetSimulationStepSettings();
}
