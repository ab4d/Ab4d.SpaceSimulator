using System.Collections.Generic;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SpaceSimulator.VisualizationEngine;

public class VisualizationEngine
{
    public readonly GroupNode RootNode = new();
    private readonly List<CelestialBodyVisualization> _celestialBodyVisualizations = [];

    public void AddCelestialBodyVisualization(CelestialBodyVisualization celestialBodyVisualization)
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
