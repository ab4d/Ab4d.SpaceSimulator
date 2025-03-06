using System.Collections.Generic;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SpaceSimulator.Visualization;

public class VisualizationEngine
{
    public readonly Camera Camera;

    public readonly GroupNode RootNode = new();
    public readonly List<CelestialBodyView> CelestialBodyViews = [];

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

    public void AddCelestialBodyVisualization(CelestialBodyView celestialBodyView)
    {
        CelestialBodyViews.Add(celestialBodyView);
        celestialBodyView.RegisterNodes(RootNode);
    }

    public void Update(bool dataChange)
    {
        // Update celestial body views / visualizations
        foreach (var celestialBodyView in CelestialBodyViews)
        {
            celestialBodyView.Update(dataChange);
        }
    }
}
