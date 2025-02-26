using System.Collections.Generic;
using Ab4d.SharpEngine.SceneNodes;
using Avalonia;

namespace Ab4d.SpaceSimulator.VisualizationEngine;

public class VisualizationEngine
{
    public readonly GroupNode RootNode = new();
    private readonly List<CelestialBody> _celestialBodies = [];

    public void AddCelestialBody(CelestialBody celestialBody)
    {
        _celestialBodies.Add(celestialBody);
        RootNode.Add(celestialBody.SceneNode);
    }

    public void Update()
    {
        foreach (var celestialBody in _celestialBodies)
        {
            celestialBody.Update();
        }
    }
}
