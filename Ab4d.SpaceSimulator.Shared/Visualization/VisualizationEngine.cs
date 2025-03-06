using System.Collections.Generic;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SpaceSimulator.Visualization;

public class VisualizationEngine
{
    public readonly Camera Camera;

    public readonly GroupNode RootNode = new();
    private readonly List<CelestialBodyView> _celestialBodyVisualizations = [];

    // Scale factor for scaling celestial body dimensions
    private float _celestialBodyScaleFactor = 20f;

    // Minimum pixel size for displayed celestial bodies
    private bool _enableMinimumPixelSize = true;
    private float _minimumPixelSize = 20f;

    public VisualizationEngine(Camera camera)
    {
        Camera = camera;
    }

    public float CelestialBodyScaleFactor
    {
        get => _celestialBodyScaleFactor;
        set
        {
            _celestialBodyScaleFactor = value;
            Update(false);
        }
    }

    public bool EnableMinimumPixelSize
    {
        get => _enableMinimumPixelSize;
        set
        {
            _enableMinimumPixelSize = value;
            Update(false);
        }
    }

    public float MinimumPixelSize
    {
        get => _minimumPixelSize;
        set
        {
            _minimumPixelSize = value;
            Update(false);
        }
    }

    public void AddCelestialBodyVisualization(CelestialBodyView celestialBodyVisualization)
    {
        _celestialBodyVisualizations.Add(celestialBodyVisualization);
        celestialBodyVisualization.RegisterNodes(RootNode);
    }

    public void Update(bool dataChange)
    {
        foreach (var visualization in _celestialBodyVisualizations)
        {
            visualization.Update(dataChange);
        }
    }
}
