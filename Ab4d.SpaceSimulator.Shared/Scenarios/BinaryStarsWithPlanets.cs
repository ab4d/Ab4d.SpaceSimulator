using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SpaceSimulator.Physics;
using Ab4d.SpaceSimulator.Visualization;

namespace Ab4d.SpaceSimulator.Scenarios;

/// <summary>
/// A toy example with binary stars.
/// </summary>
public class BinaryStarsWithPlanets : IScenario
{
    public string GetName()
    {
        return "Binary stars";
    }

    public void SetupScenario(PhysicsEngine physicsEngine, VisualizationEngine visualizationEngine,
        PlanetTextureLoader planetTextureLoader)
    {
        // First star
        var color = Colors.Yellow;
        var body = new CelestialBody()
        {
            Name = "Star #1",
            Type = CelestialBodyType.Star,
            Position = new Vector3d(0, 0, -0.5 * Constants.AstronomicalUnit), // meters
            Mass = Constants.MassOfSun, // kg
            Radius = Constants.DiameterOfSun / 2.0, // meters
            HasOrbit = false,
            Velocity = new Vector3d(0, 20 * 1e3, 0), // m/s
        };
        body.Initialize(); // Set up trajectory tracker, etc.
        physicsEngine.AddBody(body);

        // Visualization
        StandardMaterialBase material = new SolidColorMaterial(color, name: $"star1-Material");
        var view = new CelestialBodyView(visualizationEngine, body, material);
        view.OrbitColor = color;

        visualizationEngine.AddCelestialBodyVisualization(view);

        // Second star
        color = Colors.LightGoldenrodYellow;
        body = new CelestialBody()
        {
            Name = "Star #2",
            Type = CelestialBodyType.Star,
            Position = new Vector3d(0, 0, 0.5 * Constants.AstronomicalUnit), // meters
            Mass = Constants.MassOfSun, // kg
            Radius = Constants.DiameterOfSun / 2.0, // meters
            HasOrbit = false,
            Velocity = new Vector3d(0, -20 * 1e3, 0), // m/s
        };
        body.Initialize(); // Set up trajectory tracker, etc.
        physicsEngine.AddBody(body);

        // Visualization
        material = new SolidColorMaterial(color, name: $"star2-Material");
        view = new CelestialBodyView(visualizationEngine, body, material);
        view.OrbitColor = color;

        visualizationEngine.AddCelestialBodyVisualization(view);

        // First planet
        color = Colors.Red;
        body = new CelestialBody()
        {
            Name = "Planet #1",
            Type = CelestialBodyType.Planet,
            Position = new Vector3d(0, 0.1 * Constants.AstronomicalUnit, -0.1 * Constants.AstronomicalUnit), // meters
            Mass = Constants.MassOfEarth, // kg
            Radius = Constants.DiameterOfEarth / 2.0, // meters
            HasOrbit = false,
            Velocity = new Vector3d(0, 0, 0), // m/s
        };
        body.Initialize(); // Set up trajectory tracker, etc.
        physicsEngine.AddBody(body);

        // Visualization
        material = new StandardMaterial(color, name: $"planet1-Material");
        view = new CelestialBodyView(visualizationEngine, body, material);
        view.OrbitColor = color;

        visualizationEngine.AddCelestialBodyVisualization(view);
    }

    public string? GetDefaultView()
    {
        return null; // No default view
    }
}
