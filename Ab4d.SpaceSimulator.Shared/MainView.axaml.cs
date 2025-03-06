using System;
using System.Collections.Generic;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using Ab4d.SpaceSimulator.Physics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Colors = Avalonia.Media.Colors;
using Ab4d.SpaceSimulator.Utilities;
using Ab4d.SpaceSimulator.Visualization;

namespace Ab4d.SpaceSimulator.Shared;

public partial class MainView : UserControl
{
    private GesturesCameraController? _cameraController;

    private TargetPositionCamera? _camera;

    private bool _isPlaying;
    private bool _isInternalChange;
    private readonly int[] _simulationSpeedIntervals;
    private double _simulationSpeed;

    private DateTime _previousUpdateTime = DateTime.Now;

    private List<TextBlock> _allMessages = new();
    private const int MaxShownInfoMessagesCount = 5;

    private readonly PhysicsEngine _physicsEngine = new();
    private readonly VisualizationEngine _visualizationEngine = new();

    public MainView()
    {
        InitializeComponent();

        // Uncomment when ObjectsScaleMethods enum is available:
        //ScaleTypeComboBox.ItemsSource = Enum.GetNames(typeof(ObjectsScaleMethods));
        //ScaleTypeComboBox.SelectionChanged += ScaleTypeComboBoxOnSelectionChanged;
        //ScaleTypeComboBox.SelectedIndex = 2;

        ViewCenterComboBox.ItemsSource = new string[] { "Sol", "Earth", "Moon" }; //
        ViewCenterComboBox.SelectionChanged += ViewCenterComboBox_OnSelectionChanged;
        ViewCenterComboBox.SelectedIndex = 0;

        _simulationSpeedIntervals = new int[] { 0, 1, 10, 60, 600, 3600, 6 * 3600, 24 * 3600, 7 * 24 * 3600, 30 * 24 * 3600, 100 * 24 * 3600 };

        SimulationSpeedSlider.Maximum = _simulationSpeedIntervals.Length - 1;
        SpeedInfoTextBlock.Text = "";

        SetupCameraController();

        // Create scene
        var solarSystem = new SolarSystemScenario();
        solarSystem.SetupScenario(_physicsEngine, _visualizationEngine);

        MainSceneView.Scene.RootNode.Add(_visualizationEngine.RootNode);

        MainSceneView.SceneUpdating += (sender, args) =>
        {
            var now = DateTime.Now;

            if (!_isPlaying)
            {
                _previousUpdateTime = now;
                return;
            }

            // Compute elapsed real-time, then scale it with simulation speed (simulation time step per real-time second)
            // to obtain the simulation time delta
            var timeDelta = now - _previousUpdateTime;
            _previousUpdateTime = now; // Store for next call

            var scaledTimeDelta = timeDelta * _simulationSpeed;

            _physicsEngine.Simulate(scaledTimeDelta.TotalSeconds);
            _visualizationEngine.Update();

            UpdateShownSimulationTime();
        };

        // Initial UI update
        UpdateShownSimulationTime();
        SetSimulationSpeed(GetSimulationSpeed());

        // In case when VulkanDevice cannot be created, show an error message
        // If this is not handled by the user, then SharpEngineSceneView will show its own error message
        MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
        {
            ShowDeviceCreateFailedError(args.Exception); // Show error message
            args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
        };
    }

    private void SetupCameraController()
    {
        _camera = new TargetPositionCamera()
        {
            Heading = -40,
            Attitude = -30,
            Distance = 5,
            ViewWidth = 500,
            TargetPosition = new Vector3(0, 0, 0),
            ShowCameraLight = ShowCameraLightType.Always,

            // TODO: this breaks the zoom on Linux
            /*NearPlaneDistance = 100_000f / (float)PhysicsEngine.Constants.AstronomicalUnit, // 100 km in AU
            IsAutomaticNearPlaneDistanceCalculation = false,*/
        };

        MainSceneView.SceneView.Camera = _camera;


        // Create GesturesCameraController that is defined in Common folder.
        // It can also recognize pinch and scroll gestures.
        _cameraController = new GesturesCameraController(MainSceneView)
        {
            RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                          // this is already the default value but is still set up here for clarity
            MoveCameraConditions   = PointerAndKeyboardConditions.Disabled,
            QuickZoomConditions    = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default
                
            ZoomMode = CameraZoomMode.PointerPosition,
            RotateAroundPointerPosition = true,

            IsPinchGestureEnabled         = true,
            IsScrollGestureEnabled        = false,
            RotateCameraWithScrollGesture = false, // When true, then dragging with one finger will rotate the camera (this is the default)
            RotateWithPinchGesture        = true,  // When true, we can rotate the camera with two fingers (false by default)
        };
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

    private void ShowOptionPanels(Border panelToShow)
    {
        if (panelToShow == ScenariosBorder)
        {
            ScenariosBorder.IsVisible = true;
        }
        else
        {
            ScenariosBorder.IsVisible = false;
            ScenariosButton.IsChecked = false;
        }

        if (panelToShow == SettingBorder)
        {
            SettingBorder.IsVisible = true;
        }
        else
        {
            SettingBorder.IsVisible = false;
            SettingButton.IsChecked = false;
        }
    }

    private void SetSimulationSpeed(double simulationSpeed)
    {
        _simulationSpeed = simulationSpeed;

        if (simulationSpeed <= 0)
        {
            SpeedInfoTextBlock.Text = "Speed: paused";
        }
        else
        {
            double infoValue;
            string infoUnit;

            if (simulationSpeed < 60)
            {
                infoValue = simulationSpeed;
                infoUnit = "s";
            }
            else if (simulationSpeed < 60 * 60)
            {
                infoValue = simulationSpeed / 60;
                infoUnit = "min";
            }
            else if (simulationSpeed < 24 * 60 * 60)
            {
                infoValue = simulationSpeed / (60 * 60);
                infoUnit = "h";
            }
            else
            {
                infoValue = simulationSpeed / (24 * 60 * 60);
                infoUnit = "days";
            }

            SpeedInfoTextBlock.Text = $"Speed: +{infoValue:0.0} {infoUnit}/s";
        }
    }

    private double GetSimulationSpeed()
    {
        double sliderValue = SimulationSpeedSlider.Value;

        double simulationSpeedInSeconds;


        int lowIndex = (int) Math.Floor(sliderValue);
        int highIndex = (int) Math.Ceiling(sliderValue);

        if (lowIndex == highIndex)
        {
            simulationSpeedInSeconds = _simulationSpeedIntervals[lowIndex];
        }
        else
        {
            int lowInterval = _simulationSpeedIntervals[lowIndex];
            int highInterval = _simulationSpeedIntervals[highIndex];
            simulationSpeedInSeconds = lowInterval + (double) (highInterval - lowInterval) * (double) (sliderValue - lowIndex);
        }

        return simulationSpeedInSeconds;
    }

    private void UpdateShownSimulationTime()
    {
        var simulationTime = _physicsEngine.SimulationTime;
        var timeText = "Time: ";

        if (simulationTime >= Physics.Constants.SecondsInDay)
        {
            var days = (int)Math.Floor(simulationTime / Physics.Constants.SecondsInDay);
            timeText += $"+{days:0} day(s) ";
        }

        var timeWithinDay = (int)simulationTime % (Physics.Constants.SecondsInDay);
        var hours = timeWithinDay / (60 * 60);
        var minutes = (timeWithinDay % (60 * 60)) / 60;
        var seconds = timeWithinDay % 60;

        timeText += $"{hours:00}:{minutes:00}:{seconds:00}";

        SimulationTimeTextBlock.Text = timeText;
    }

    private void AddInfoMessage(string message)
    {
        AddInfoMessage(message, Avalonia.Media.Colors.White, isBold: false);
    }

    private void AddInfoMessage(string message, Color color, bool isBold = false)
    {
        var textBlock = new TextBlock()
        {
            Text = message,
            Foreground = new SolidColorBrush(color),
            TextWrapping = TextWrapping.Wrap,
            FontWeight = isBold ? FontWeight.Bold : FontWeight.Regular,
        };

        InfoMessagesPanel.Children.Add(textBlock);

        _allMessages.Add(textBlock);
        if (_allMessages.Count > MaxShownInfoMessagesCount)
        {
            InfoMessagesPanel.Children.RemoveAt(0);
            _allMessages.RemoveAt(0);
        }
    }

    private void SimulationSpeedSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!this.IsLoaded || _isInternalChange)
            return;

        var simulationSpeed = GetSimulationSpeed();
        SetSimulationSpeed(simulationSpeed);

        UpdateShownSimulationTime();
    }

    private void PlayPauseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isPlaying)
        {
            PlayPauseButton.Content = "Start";
            _isPlaying = false;
        }
        else
        {
            PlayPauseButton.Content = "Stop";
            _isPlaying = true;
        }

        UpdateShownSimulationTime();
    }

    private void ScenariosButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ScenariosBorder.IsVisible)
            ScenariosBorder.IsVisible = false;
        else
            ShowOptionPanels(ScenariosBorder);
    }

    private void SettingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SettingBorder.IsVisible)
            SettingBorder.IsVisible = false;
        else
            ShowOptionPanels(SettingBorder);
    }

    private void ScaleTypeComboBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {

    }

    private void ViewCenterComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {

    }

    private void Scenario1Button_OnClick(object? sender, RoutedEventArgs e)
    {
        AddInfoMessage("Scenario 1 started", Colors.Orange);
    }
}
