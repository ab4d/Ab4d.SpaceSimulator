using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SpaceSimulator.Utilities;

namespace Ab4d.SpaceSimulator.Visualization;

public class VisualizationEngine
{
    public const float ViewUnitScale = 1f / 1e9f; // 1 unit in 3D view space is 1 million km = 1e9 m
    
    public readonly TargetPositionCamera Camera;
    public readonly GesturesCameraController CameraController;

    public readonly GroupNode RootNode = new();
    public readonly List<Light> Lights = new();
    public readonly List<CelestialBodyView> CelestialBodyViews = [];

    // Scale factor for scaling celestial body dimensions
    private float _celestialBodyScaleFactor = 20f;

    // Minimum pixel size for displayed celestial bodies
    private bool _enableMinimumPixelSize = true;
    private float _minimumPixelSize = 20f;

    // Tracked celestial body
    private CelestialBodyView? _trackedCelestialBody = null;

    public VisualizationEngine(TargetPositionCamera camera, GesturesCameraController cameraController)
    {
        Camera = camera;
        CameraController = cameraController;
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

        // If we are tracking a celestial body, update the zoom/rotation center and the camera target position
        if (_trackedCelestialBody != null)
        {
            var center = _trackedCelestialBody.SphereNode.CenterPosition;
            Camera.RotationCenterPosition = center;
            Camera.TargetPosition = center;
        }
    }

    public void TrackCelestialBody(string? name)
    {
        _trackedCelestialBody = null;

        // Look up by name...
        if (name != null)
        {
            // TODO: keep a dictionary and perform a dictionary look-up
            foreach (var celestialBodyView in CelestialBodyViews)
            {
                if (name == celestialBodyView.CelestialBody.Name)
                {
                    _trackedCelestialBody = celestialBodyView;
                    break;
                }
            }
        }

        if (_trackedCelestialBody == null)
        {
            // Free mode
            Camera.RotationCenterPosition = null;

            CameraController.ZoomMode = CameraZoomMode.PointerPosition;
            CameraController.RotateAroundPointerPosition = true;
        }
        else
        {
            // Centered to tracked celestial body
            var center = _trackedCelestialBody.SphereNode.CenterPosition;
            Camera.RotationCenterPosition = center;
            Camera.TargetPosition = center;

            CameraController.ZoomMode = CameraZoomMode.CameraRotationCenterPosition;
            CameraController.RotateAroundPointerPosition = false;
        }
    }
}
