using System;
using System.Numerics;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.RenderingLayers;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.Vulkan;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Colors = Avalonia.Media.Colors;

namespace Ab4d.SpaceSimulator.Shared;

public partial class TestCustomCameraWindow : Window
{
    private readonly SharpEngineSceneView _sharpEngineSceneView;
    private PointerCameraController? _pointerCameraController;
    private TargetPositionCamera? _targetPositionCamera;

    private RenderingLayer? _customRenderingLayer;
    private TargetPositionCamera? _spaceshipCamera;

    private TranslateTransform _spaceshipCenterTransform;
    private Vector3 _spaceshipPosition;
    private SphereModelNode _sun;
    private SphereModelNode _earth;
    private SphereModelNode _moon;

    public TestCustomCameraWindow()
    {
        InitializeComponent();

        _sharpEngineSceneView = new SharpEngineSceneView(PresentationTypes.SharedTexture); // SharedTexture is also the default presentation type so we could also create the SharpEngineSceneView without that parameter
        _sharpEngineSceneView.BackgroundColor = Colors.Aqua;

        RootGrid.Children.Add(_sharpEngineSceneView);

        SetupCustomRenderingLayer();
        SetupPointerCameraController();

        CreateTestScene();
    }

    private void SetupCustomRenderingLayer()
    {
        _spaceshipCamera = new TargetPositionCamera("RenderingLayerCustomCamera")
        {
            NearPlaneDistance = 0.001f,  // 1 m
            FarPlaneDistance = 10,       // 10 km

            IsAutomaticNearPlaneDistanceCalculation = false,
            IsAutomaticFarPlaneDistanceCalculation = false
        };

        _customRenderingLayer = new RenderingLayer("CustomRenderingLayer");
     
        _customRenderingLayer.CustomCamera = _spaceshipCamera;
        _customRenderingLayer.CustomAmbientLightColor = new Color3(0.4f);
        _customRenderingLayer.ClearDepthStencilBufferBeforeRendering = true;

        var scene = _sharpEngineSceneView.Scene;

        if (scene.OverlayRenderingLayer != null)
            scene.AddRenderingLayerBefore(_customRenderingLayer, scene.OverlayRenderingLayer);
    }

    private void SetupPointerCameraController()
    {
        // Define the camera
        _targetPositionCamera = new TargetPositionCamera
        {
            Heading = 5,
            Attitude = -5,
            Distance = 0.5f,
            TargetPosition = new Vector3(0, 0, 0),
            ShowCameraLight = ShowCameraLightType.Auto,

            NearPlaneDistance = 10,
            FarPlaneDistance = 149_597_870 * 2, // 2 AU

            IsAutomaticNearPlaneDistanceCalculation = false,
            IsAutomaticFarPlaneDistanceCalculation = false
        };

        _targetPositionCamera.CameraChanged += TargetPositionCameraOnCameraChanged;

        _sharpEngineSceneView.SceneView.Camera = _targetPositionCamera;


        // PointerCameraController use pointer or mouse to control the camera

        //_pointerCameraController = new PointerCameraController(_sharpEngineSceneView.SceneView, SceneViewBorder) // We could also create PointerCameraController by SceneView and custom EventSourceElement
        _pointerCameraController = new PointerCameraController(_sharpEngineSceneView)
        {
            RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
            MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
            QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default

            RotateAroundPointerPosition = false,
            ZoomMode = CameraZoomMode.ViewCenter,
        };
    }

    private void TargetPositionCameraOnCameraChanged(object? sender, EventArgs e)
    {
        UpdateSpaceshipCamera();
    }

    private void UpdateSpaceshipCamera()
    {
        if (_targetPositionCamera == null || _spaceshipCamera == null)
            return;

        _spaceshipCamera.TargetPosition = _targetPositionCamera.TargetPosition;
        _spaceshipCamera.Heading        = _targetPositionCamera.Heading;
        _spaceshipCamera.Attitude       = _targetPositionCamera.Attitude;
        _spaceshipCamera.Distance       = _targetPositionCamera.Distance;
        _spaceshipCamera.AspectRatio    = _targetPositionCamera.AspectRatio;
    }

    private void CreateTestScene()
    {
        var scene = _sharpEngineSceneView.Scene;

        // All units are in kilometers

        _sun = new SphereModelNode("Sun")
        {
            CenterPosition = new Vector3(0, 0, 0),
            Radius = 696340, // 696,340 km
            Material = StandardMaterials.Yellow
        };

        scene.RootNode.Add(_sun);


        _earth = new SphereModelNode("Earth")
        {
            CenterPosition = new Vector3(10_000_000, 0, 149_597_870), // 1 AU = 149,597,870.7 km
            Radius = 6371,                                   // 6,371 km
            Material = StandardMaterials.Blue
        };

        scene.RootNode.Add(_earth);


        _moon = new SphereModelNode("Moon")
        {
            CenterPosition = _earth.CenterPosition + new Vector3(0, 0, 384_400), // 384,400 km from the Earth
            Radius = 1737,                                                       // 1,737 km
            Material = StandardMaterials.Gray
        };

        scene.RootNode.Add(_moon);


        var spaceship = new BoxModelNode("Spaceship")
        {
            Position = new Vector3(0, 0, 0),      // Spaceship is at the center of the scene
            Size = new Vector3(0.01f, 0.01f, 0.1f),  // 100 meters long, 10 meters wide and 10 meters high
            Material = StandardMaterials.Silver,
            CustomRenderingLayer = _customRenderingLayer,
        };

        scene.RootNode.Add(spaceship);
        
        var spaceshipEngine = new BoxModelNode("SpaceshipEngine")
        {
            Position = spaceship.Position + new Vector3(0, 0, 0.052f),     
            Size = new Vector3(0.01f, 0.01f, 0.004f),  // 4 meters long, 10 meters wide and 10 meters high
            Material = StandardMaterials.Red,
            CustomRenderingLayer = _customRenderingLayer,
        };

        scene.RootNode.Add(spaceshipEngine);


        _spaceshipPosition = new Vector3(0, -1800, 0) - _moon.CenterPosition;
        _spaceshipCenterTransform = new TranslateTransform(_spaceshipPosition);

        _sun.Transform = _spaceshipCenterTransform;
        _earth.Transform = _spaceshipCenterTransform;
        _moon.Transform = _spaceshipCenterTransform;


        if (_targetPositionCamera != null)
        {
            _targetPositionCamera.TargetPosition = spaceship.Position;
            _targetPositionCamera.Distance = 1f; // 1000 meters from spaceship

            UpdateSpaceshipCamera();
            
            //if (_renderingLayerCustomCamera != null)
            //{
            //    _renderingLayerCustomCamera.TargetPosition = _targetPositionCamera.TargetPosition;
            //    _renderingLayerCustomCamera.Heading = _targetPositionCamera.Heading;
            //    _renderingLayerCustomCamera.Attitude = _targetPositionCamera.Attitude;
            //    _renderingLayerCustomCamera.Distance = _targetPositionCamera.Distance;

            //    _renderingLayerCustomCamera.NearPlaneDistance = 0.1f;
            //    _renderingLayerCustomCamera.FarPlaneDistance = 1000;
            //}
        }
    }
}