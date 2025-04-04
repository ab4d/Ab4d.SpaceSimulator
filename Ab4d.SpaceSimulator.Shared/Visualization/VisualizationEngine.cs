using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SpaceSimulator.Physics;
using Ab4d.SpaceSimulator.Utilities;

namespace Ab4d.SpaceSimulator.Visualization;

public class VisualizationEngine
{
    public const float ViewUnitScale = 1f / 1e9f; // 1 unit in 3D view space is 1 million km = 1e9 m

    public const float MinOrbitScreenSize = 20; // If moon or planet's orbit is smaller than 20 pixels, then hide the planet or moon. This is used when using actual body sizes.

    public readonly SceneView SceneView;
    public readonly TargetPositionCamera Camera;
    public readonly GesturesCameraController CameraController;

    public readonly GroupNode RootNode = new();
    public readonly List<Light> Lights = new();
    public readonly List<CelestialBodyView> CelestialBodyViews = [];

    // Scale factor for scaling celestial body dimensions
    private float _celestialBodyScaleFactor = 1f;

    public BitmapTextCreator? BitmapTextCreator { get; set; }

    public bool IsSimulationPaused { get; set; } = true; // Initially paused

    private bool _showOrbits = true;
    public bool ShowOrbits
    {
        get => _showOrbits;
        set
        {
            _showOrbits = value; 
            UpdateVisibleObjects();
        }
    }

    private bool _showTrails = true;
    public bool ShowTrails
    {
        get => _showTrails;
        set
        {
            _showTrails = value; 
            UpdateVisibleObjects();
        }
    }

    private bool _showNames = true;
    public bool ShowNames
    {
        get => _showNames;
        set
        {
            _showNames = value; 
            UpdateVisibleObjects();
        }
    }


    // Tracked celestial body
    private CelestialBodyView? _trackedCelestialBody = null;

    public VisualizationEngine(SceneView sceneView, TargetPositionCamera camera, GesturesCameraController cameraController)
    {
        SceneView = sceneView;
        Camera = camera;
        CameraController = cameraController;

        camera.CameraChanged += (sender, args) =>
        {
            // When simulation is not paused, then on each update we will update the scale of body spheres and positions and alignment of body names.
            // But when the simulation is paused, then we need to update that on each camera move.
            if (IsSimulationPaused)
            {
                foreach (var celestialBodyView in CelestialBodyViews)
                    celestialBodyView.Update(dataChange: false);

                UpdateCameraNearAndFarPlanes();
            }
        };
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


    private bool _isMinSizeLimited = true;
    public bool IsMinSizeLimited
    {
        get => _isMinSizeLimited;
        set
        {
            _isMinSizeLimited = value;
            Update(false);
        }
    }

    // Minimum pixel size for displayed celestial bodies
    private float _minScreenSize = 40f;
    public float MinScreenSize
    {
        get => _minScreenSize;
        set
        {
            _minScreenSize = value;
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

        UpdateCameraNearAndFarPlanes();
    }

    public void TrackCelestialBody(string? name)
    {
        _trackedCelestialBody = null;

        // Look up by name...
        if (name != null)
            _trackedCelestialBody = CelestialBodyViews.FirstOrDefault(c => c.Name == name);

        if (_trackedCelestialBody == null)
        {
            // Free mode
            Camera.RotationCenterPosition = null;

            // Zoom to and rotate around the position behind the mouse / pointer.
            CameraController.ZoomMode = CameraZoomMode.PointerPosition;
            CameraController.RotateAroundPointerPosition = true;
        }
        else
        {
            // Centered to tracked celestial body
            var center = _trackedCelestialBody.SphereNode.CenterPosition;
            Camera.RotationCenterPosition = center;
            Camera.TargetPosition = center;

            var distanceFactor = _trackedCelestialBody.CelestialBody.Type == CelestialBodyType.Star ? 1000 : 300;

            Camera.Distance = (float)_trackedCelestialBody.CelestialBody.Radius * distanceFactor * ViewUnitScale;

            // Zoom to and rotate around selected celestial body
            CameraController.ZoomMode = CameraZoomMode.ViewCenter;
            CameraController.RotateAroundPointerPosition = false;
        }
    }

    public void CreateNameSceneNodes()
    {
        foreach (var celestialBodyView in CelestialBodyViews)
            CreateNameSceneNodes(celestialBodyView);
    }

    private void CreateNameSceneNodes(CelestialBodyView celestialBodyView)
    {
        if (BitmapTextCreator == null)
            return;

        //var textPosition = celestialBodyView.SphereNode.CenterPosition + Vector3.UnitX * celestialBodyView.SphereNode.Radius * 1.5f;

        var textNode = BitmapTextCreator.CreateTextNode(text: celestialBodyView.Name,
                                                        position: Vector3.Zero, 
                                                        positionType: PositionTypes.Left, // left align text to the position
                                                        textDirection: Vector3.UnitX, 
                                                        upDirection: Vector3.UnitY, 
                                                        fontSize: 1,
                                                        textColor: Colors.White,
                                                        isSolidColorMaterial: true);
        if (this.ShowNames)
            textNode.Visibility = celestialBodyView.SphereNode.Visibility;
        else
            textNode.Visibility = SceneNodeVisibility.Hidden;

        celestialBodyView.NameSceneNode = textNode;
        celestialBodyView.UpdateNameSceneNode();

        RootNode.Add(textNode);
    }

    private void UpdateVisibleObjects()
    {
        foreach (var celestialBodyView in CelestialBodyViews)
            celestialBodyView.Update(dataChange: false);
    }

    private void UpdateCameraNearAndFarPlanes()
    {
        // Manually calculate camera's near and far planes
        SceneView.CalculateCameraPlanes(Camera, out var nearPlaneDistance, out var farPlaneDistance);

        // Now we get the closest celestial body:
        var cameraPosition = Camera.GetCameraPosition();

        float minDistanceSquared = float.MaxValue;
        int minDistanceIndex = -1;

        for (var i = 0; i < CelestialBodyViews.Count; i++)
        {
            var celestialBodyView = CelestialBodyViews[i];
            var distanceToCamera = (celestialBodyView.SphereNode.CenterPosition - cameraPosition).LengthSquared();
            if (distanceToCamera == 0) // skip hidden objects
                continue;

            if (distanceToCamera > 0 && minDistanceSquared > distanceToCamera)
            {
                minDistanceSquared = distanceToCamera;
                minDistanceIndex = i;
            }
        }


        if (minDistanceIndex > 0) // Is there any celestial body found
        {
            var minDistance = MathF.Sqrt(minDistanceSquared);
            minDistance -= CelestialBodyViews[minDistanceIndex].SphereNode.Radius;

            // We have two special cases for NearPlaneDistance:
            // 1) very close to the planet / moon, we set NearPlaneDistance to 1000 km
            // 2) close to the planet / moon, we set NearPlaneDistance to 100,000 km
            if (minDistance < 1e9 * ViewUnitScale)  // < 1 mio km
                nearPlaneDistance = 1_000_000 * ViewUnitScale; // = 1000 km => 0.001
            else if (minDistance < 1e11 * ViewUnitScale) // < 100 mio km
                nearPlaneDistance = 100_000_000 * ViewUnitScale; // = 100,000 km => 0.1
        }

        Camera.NearPlaneDistance = nearPlaneDistance;
        Camera.FarPlaneDistance = farPlaneDistance;
    }
}
