using System.Collections.Generic;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SpaceSimulator.VisualizationEngine;

public class VisualizationEngine
{
    public readonly GroupNode RootNode = new();
    private readonly List<CelestialBodyView> _celestialBodyVisualizations = [];

    public void AddCelestialBodyVisualization(CelestialBodyView celestialBodyVisualization)
    {
        _celestialBodyVisualizations.Add(celestialBodyVisualization);
        RootNode.Add(celestialBodyVisualization.SceneNode);
        if (celestialBodyVisualization.TrajectoryNode != null)
        {
            RootNode.Add(celestialBodyVisualization.TrajectoryNode);
        }
    }

    public void Update()
    {
        foreach (var visualization in _celestialBodyVisualizations)
        {
            visualization.Update();
        }
    }
}
