using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.SpaceSimulator.Physics;
using Ab4d.SpaceSimulator.Utilities;
using Avalonia.Platform;

namespace Ab4d.SpaceSimulator.Visualization;

public class VisualizationEngine
{
    public const float ViewUnitScale = 1f / 1e9f; // 1 unit in 3D view space is 1 million km = 1e9 m

    public const float MinOrbitScreenSize = 20; // If moon or planet's orbit is smaller than 20 pixels, then hide the planet or moon. This is used when using actual body sizes.

    public float OrbitColorMultiplier = 0.3f;
    public float TrailColorMultiplier = 1.0f;

    public readonly SceneView SceneView;
    public readonly TargetPositionCamera Camera;
    public readonly GesturesCameraController CameraController;

    public readonly GroupNode RootNode = new();
    public readonly List<Light> Lights = new();
    public readonly List<CelestialBodyView> CelestialBodyViews = [];

    // Scale factor for scaling celestial body dimensions
    private float _celestialBodyScaleFactor = 1f;

    private bool _isMilkyWayLoadingStarted;
    private SphereModelNode? _milkyWaySphereNode;
    private FirstPersonCamera? _milkyWayCamera;

    public BitmapTextCreator? BitmapTextCreator { get; set; }

    public bool IsSimulationPaused { get; set; } = true; // Initially paused

    private bool _showMilkyWay = true;
    public bool ShowMilkyWay
    {
        get => _showMilkyWay;
        set
        {
            _showMilkyWay = value;
            UpdateMilkyWay();
        }
    }

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

        RootNode.Transform = new AxisAngleRotateTransform(Vector3.UnitX, -90);

        camera.CameraChanged += (sender, args) =>
        {
            // When simulation is not paused, then on each update we will update the scale of body spheres and positions and alignment of body names.
            // But when the simulation is paused, then we need to update that on each camera move.
            if (IsSimulationPaused)
            {
                UpdateCameraNearAndFarPlanes();

                foreach (var celestialBodyView in CelestialBodyViews)
                    celestialBodyView.UpdateVisualization();
            }

            if (_milkyWayCamera != null)
            {
                _milkyWayCamera.Heading = Camera.Heading;
                _milkyWayCamera.Attitude = Camera.Attitude;
            }
        };
    }

    public void Reset()
    {
        // Remove all visualization nodes from root node
        RootNode.Clear();

        // Clear internal list of visualization nodes
        _trackedCelestialBody = null;
        CelestialBodyViews.Clear();

        // Clear lights
        Lights.Clear();

        // Re-add Milky Way node, if available.
        if (_milkyWaySphereNode != null)
            RootNode.Add(_milkyWaySphereNode);
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
        // If applicable, update parts of celestial body visualization that are based solely on the physical properties
        // (position, orientation, trail, etc.). Properties such as dynamic scale and visibility of children (moons)
        // need to be updated later, after we update camera (whose position might in turn depend on position of
        // celestial body, which we update here).
        if (dataChange)
        {
            foreach (var celestialBodyView in CelestialBodyViews)
            {
                celestialBodyView.UpdatePhysicalProperties();
            }
        }

        // If we are tracking a celestial body, update the zoom/rotation center and the camera target position
        if (_trackedCelestialBody != null)
        {
            var center = _trackedCelestialBody.SphereNode.CenterPosition;
            Camera.RotationCenterPosition = center;
            Camera.TargetPosition = center;
        }

        UpdateCameraNearAndFarPlanes();

        // Finally, update the rest of celestial body visualization; perform dynamic scaling (based on distance from
        // camera), show/hide children (i.e., moons), etc.
        foreach (var celestialBodyView in CelestialBodyViews)
        {
            celestialBodyView.UpdateVisualization();
        }
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

        // Check if name is explicitly hidden on celestial body visualization object.
        if (!celestialBodyView.ShowName)
            textNode.Visibility = SceneNodeVisibility.Hidden;

        celestialBodyView.NameSceneNode = textNode;
        celestialBodyView.UpdateNameSceneNode();

        RootNode.Add(textNode);
    }

    private void UpdateVisibleObjects()
    {
        // Darken the orbit when trails are also shown so the trail is better visible
        OrbitColorMultiplier = ShowTrails ? 0.3f : 0.5f;

        foreach (var celestialBodyView in CelestialBodyViews)
        {
            celestialBodyView.UpdateOrbitColor();
            celestialBodyView.UpdateVisualization(); // Update just visualization
        }
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


        if (minDistanceIndex != -1) // Is there any celestial body found
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

    public void UpdateMilkyWay()
    {
        if (_showMilkyWay)
        {
            if (_milkyWaySphereNode != null)
            {
                _milkyWaySphereNode.Visibility = SceneNodeVisibility.Visible;
                return;
            }

            if (!_isMilkyWayLoadingStarted && SceneView.GpuDevice != null)
            {
                _isMilkyWayLoadingStarted = true;
                _ = LoadMilkyWayTextureAsync(); // call async code from non-async method
            }
        }
        else
        {
            if (_milkyWaySphereNode != null)
                _milkyWaySphereNode.Visibility = SceneNodeVisibility.Hidden;
        }
    }

    private async Task LoadMilkyWayTextureAsync()
    {
        try
        {
            var assetsPath = $"avares://{typeof(PlanetTextureLoader).Assembly.GetName().Name}/Assets/";
            var textureFileName = "MilkyWay.png";

            var bitmapStream = AssetLoader.Open(new Uri(assetsPath + textureFileName));

            if (bitmapStream != null && SceneView.GpuDevice != null)
            {
                var gpuImage = await TextureLoader.CreateTextureAsync(bitmapStream, textureFileName, SceneView.GpuDevice);

                    // When the texture is loaded, create the sphere model node
                    var textureMaterial = new SolidColorMaterial(gpuImage);
                    //textureMaterial = new SolidColorMaterial(Colors.SkyBlue);

                    var backgroundRenderingLayer = SceneView.Scene.BackgroundRenderingLayer;

                    _milkyWaySphereNode = new SphereModelNode("BackgroundMilkyWaySphere")
                    {
                        Radius = 10,
                        BackMaterial = textureMaterial,                         // Set milkyway texture as the back material so it is visible from inside the sphere
                        Transform = new YawPitchRollRotateTransform(0, 0, -75), // Spiral arms of the Milky Way are rotated by 75 degrees to the solar system plane
                        CustomRenderingLayer = backgroundRenderingLayer         // Pot the sphere in the background rendering layer (See below)
                    };

                    if (backgroundRenderingLayer != null)
                    {
                        // The sphere is put into the background rendering layer so it can be rendered first (behind all other objects)
                        // and this allows using custom camera so we do not need to have very big sphere radius that would "hurt" the camera's depth resolution.
                        _milkyWayCamera = new FirstPersonCamera("MilkyWayCamera")
                        {
                            CameraPosition = new Vector3(0, 0, 0), // Camera is inside the sphere
                            FieldOfView = 60,                      // Use a little bit wider filed of view to get better details and not to increase the size of the pixels on the Milky Way texture
                            Heading = Camera.Heading,
                            Attitude = Camera.Attitude,
                        };

                        backgroundRenderingLayer.CustomCamera = _milkyWayCamera;

                        // Because we use a different camera, we need to reset the depth buffer (the depth values from the _milkyWaySphereNode are not compatible with the rest of the scene)
                        backgroundRenderingLayer.ClearDepthStencilBufferAfterRendering = true;
                    }

                    if (!_showMilkyWay) // Maybe user turned off showing the Milky Way while loading
                        _milkyWaySphereNode.Visibility = SceneNodeVisibility.Hidden;

                    RootNode.Add(_milkyWaySphereNode);
            }
        }
        catch (FileNotFoundException)
        {
            // pass
        }
    }
}
