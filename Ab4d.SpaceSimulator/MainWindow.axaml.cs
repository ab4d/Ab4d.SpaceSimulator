using System;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SpaceSimulator
{
    public partial class MainWindow : Window
    {
        private PointerCameraController? _pointerCameraController;

        private TargetPositionCamera? _camera;

        public MainWindow()
        {
            InitializeComponent();

            SetupCameraController();
            CreateTestScene();
        }

        private void SetupCameraController()
        {
            _camera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -30,
                Distance = 500,
                ViewWidth = 500,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Always
            };

            MainSceneView.SceneView.Camera = _camera;


            _pointerCameraController = new PointerCameraController(MainSceneView)
            {
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.PointerPosition,
                RotateAroundPointerPosition = true
            };
        }

        private void CreateTestScene()
        {
            var boxModel = new BoxModelNode(centerPosition: new Vector3(0, 0, 0), 
                size: new Vector3(80, 40, 60), 
                name: "Gold BoxModel")
            {
                Material = StandardMaterials.Gold,
                //Material = new StandardMaterial(Colors.Gold),
                //Material = new StandardMaterial(diffuseColor: new Color3(1f, 0.84313726f, 0f))
            };

            MainSceneView.Scene.RootNode.Add(boxModel);
        }

        private void ShowDeviceCreateFailedError(Exception ex)
        {
            var errorTextBlock = new TextBlock()
            {
                Text = "Error creating VulkanDevice:\r\n" + ex.Message,
                Foreground = Brushes.Red,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            RootGrid.Children.Add(errorTextBlock);
        }
    }
}