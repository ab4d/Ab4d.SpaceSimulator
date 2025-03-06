using System.Collections.Generic;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SpaceSimulator.Visualization;

public class VisualizationEngine
{
    public readonly GroupNode RootNode = new();
    private readonly List<CelestialBodyView> _celestialBodyVisualizations = [];

    // Scale factor for scaling celestial body dimensions
    private float _celestialBodyScaleFactor = 20f;

    public float CelestialBodyScaleFactor
    {
        get => _celestialBodyScaleFactor;
        set
        {
            _celestialBodyScaleFactor = value;
            // Force update
            Update();
        }
    }

    public void AddCelestialBodyVisualization(CelestialBodyView celestialBodyVisualization)
    {
        _celestialBodyVisualizations.Add(celestialBodyVisualization);
        celestialBodyVisualization.RegisterNodes(RootNode);
    }

    public void Update()
    {
        foreach (var visualization in _celestialBodyVisualizations)
        {
            visualization.Update();
        }
    }
}
