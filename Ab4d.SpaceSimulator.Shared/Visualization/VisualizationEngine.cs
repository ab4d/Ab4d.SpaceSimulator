using System.Collections.Generic;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SpaceSimulator.Visualization;

public class VisualizationEngine
{
    public readonly GroupNode RootNode = new();
    private readonly List<CelestialBodyView> _celestialBodyVisualizations = [];

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
