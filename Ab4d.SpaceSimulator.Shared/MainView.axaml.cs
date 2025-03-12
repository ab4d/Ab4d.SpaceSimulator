using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    private readonly int[] _simulationSpeedIntervals;
    private double _simulationSpeed;

    private DateTime _previousUpdateTime = DateTime.Now;

    private List<TextBlock> _allMessages = new();
    private const int MaxShownInfoMessagesCount = 5;

    private readonly PhysicsEngine? _physicsEngine;
    private readonly VisualizationEngine? _visualizationEngine;

    private string[] _selectionNames = ["none"]; // Populated once scenario is set up

    private PlanetTextureLoader? _planetTextureLoader;

    private bool _isVerticalView;

    public MainView()
    {
        InitializeComponent();

        // Uncomment when ObjectsScaleMethods enum is available:
        //ScaleTypeComboBox.ItemsSource = Enum.GetNames(typeof(ObjectsScaleMethods));
        //ScaleTypeComboBox.SelectionChanged += ScaleTypeComboBoxOnSelectionChanged;
        //ScaleTypeComboBox.SelectedIndex = 2;

        ViewCenterComboBox.ItemsSource = _selectionNames;
        ViewCenterComboBox.SelectionChanged += ViewCenterComboBox_OnSelectionChanged;
        ViewCenterComboBox.SelectedIndex = 0;

        _simulationSpeedIntervals = new int[] { 0, 1, 10, 60, 600, 3600, 6 * 3600, 24 * 3600, 7 * 24 * 3600, 30 * 24 * 3600 };

        SimulationSpeedSlider.Value = 8.13; // 10 days / second

        SimulationSpeedSlider.Maximum = _simulationSpeedIntervals.Length - 1;
        SpeedInfoTextBlock.Text = "";

        SetupCameraController();

        // Create physics and visualization engine
        _physicsEngine = new PhysicsEngine();
        Debug.Assert(_camera != null, nameof(_camera) + " != null");
        _visualizationEngine = new VisualizationEngine(_camera, _cameraController);

        // Create scene
        var solarSystem = new SolarSystemScenario();

        MainSceneView.Scene.RootNode.Add(_visualizationEngine.RootNode);

        MainSceneView.GpuDeviceCreated += (sender, args) =>
        {
            _planetTextureLoader = new PlanetTextureLoader(args.GpuDevice);
            solarSystem.SetupScenario(_physicsEngine, _visualizationEngine, _planetTextureLoader);


            // Setup lights
            MainSceneView.Scene.SetAmbientLight(0.2f);

            foreach (var oneLight in _visualizationEngine.Lights)
                MainSceneView.Scene.Lights.Add(oneLight);


            // Populate the list for ViewCenterComboBox
            _selectionNames = new string[_visualizationEngine.CelestialBodyViews.Count + 1];
            var idx = 1;
            foreach (var bodyView in _visualizationEngine.CelestialBodyViews)
            {
                _selectionNames[idx++] = bodyView.CelestialBody.Name;
            }
            _selectionNames[0] = "none";
            ViewCenterComboBox.ItemsSource = _selectionNames;
        };

        MainSceneView.SceneUpdating += (sender, args) =>
        {
            var now = DateTime.Now;

            if (!_isPlaying)
            {
                _previousUpdateTime = now;
                _visualizationEngine.Update(false); // Update without data change
                return;
            }

            // Compute elapsed real-time, then scale it with simulation speed (simulation time step per real-time second)
            // to obtain the simulation time delta
            var timeDelta = now - _previousUpdateTime;
            _previousUpdateTime = now; // Store for next call

            var scaledTimeDelta = timeDelta * _simulationSpeed;

            _physicsEngine.Simulate(scaledTimeDelta.TotalSeconds);
            _visualizationEngine.Update(true);

            UpdateShownSimulationTime();
        };

        // Initial UI update
        UpdateShownSimulationTime();
        SetSimulationSpeed(GetSimulationSpeed());

        ScaleFactorSlider.Value = _visualizationEngine.CelestialBodyScaleFactor;
        MinimumSizeCheckBox.IsChecked = _visualizationEngine.EnableMinimumPixelSize;
        MinimumSizeSlider.Value = _visualizationEngine.MinimumPixelSize;
        UpdateShownScaleFactor();
        UpdateShownMinimumPixelSize();

        // In case when VulkanDevice cannot be created, show an error message
        // If this is not handled by the user, then SharpEngineSceneView will show its own error message
        MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
        {
            ShowDeviceCreateFailedError(args.Exception); // Show error message
            args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
        };

        this.SizeChanged += (sender, args) => OnViewSizeChanged(args);
    }

    private void OnViewSizeChanged(SizeChangedEventArgs args)
    {
        bool isVerticalView = args.NewSize.Width < 900 && args.NewSize.Height > 500;

        if (isVerticalView != _isVerticalView)
        {
            BottomOptionsGrid.ColumnDefinitions.Clear();
            BottomOptionsGrid.RowDefinitions.Clear();

            if (isVerticalView)
            {
                BottomOptionsGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
                BottomOptionsGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));

                Grid.SetColumn(TimePanel, 0);
                Grid.SetColumn(SpeedTimerPanel, 0);
                Grid.SetColumn(ViewCenterPanel, 0);
                Grid.SetColumn(SettingsPanel, 0);
                
                Grid.SetRow(TimePanel, 0);
                Grid.SetRow(SpeedTimerPanel, 1);
                Grid.SetRow(ViewCenterPanel, 2);
                Grid.SetRow(SettingsPanel, 4);
            }
            else
            {
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));      
                
                Grid.SetRow(TimePanel, 0);
                Grid.SetRow(SpeedTimerPanel, 0);
                Grid.SetRow(ViewCenterPanel, 0);
                Grid.SetRow(SettingsPanel, 0);
                
                Grid.SetColumn(TimePanel, 0);
                Grid.SetColumn(SpeedTimerPanel, 1);
                Grid.SetColumn(ViewCenterPanel, 2);
                Grid.SetColumn(SettingsPanel, 4);
            }

            _isVerticalView = isVerticalView;
        }
    }

    [MemberNotNull(nameof(_camera))]
    [MemberNotNull(nameof(_cameraController))]
    private void SetupCameraController()
    {
        _camera = new TargetPositionCamera()
        {
            Heading = -115,
            Attitude = -30,
            Distance = 850,
            ViewWidth = 500,
            TargetPosition = new Vector3(0, 0, 0),
            ShowCameraLight = ShowCameraLightType.Never,
            
            NearPlaneDistance = 10_000_000f * VisualizationEngine.ViewUnitScale,    // 10.000 km
            IsAutomaticNearPlaneDistanceCalculation = false,
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
            IsScrollGestureEnabled        = true,
            RotateCameraWithScrollGesture = true, // When true, then dragging with one finger will rotate the camera (this is the default)
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
            //ScenariosButton.IsChecked = false;
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
            SpeedInfoTextBlock.Text = "Paused";
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

            SpeedInfoTextBlock.Text = $"+{infoValue:0.0} {infoUnit}/s";
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
        if (_physicsEngine == null)
            return;

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

    private void UpdateShownScaleFactor()
    {
        if (_visualizationEngine == null)
            return;

        var value = _visualizationEngine.CelestialBodyScaleFactor;
        ScaleFactorTextBlock.Text = $"Dimension scaling: {value:F0} x";
    }

    private void UpdateShownMinimumPixelSize()
    {
        if (_visualizationEngine == null)
            return;

        var value = _visualizationEngine.MinimumPixelSize;
        MinimumSizeTextBlock.Text = $"View size: {value:F0}";
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
        if (!this.IsLoaded)
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

    private void SettingsButton_OnClick(object? sender, RoutedEventArgs e)
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
        int idx = ViewCenterComboBox.SelectedIndex;
        string? name = null;
        if (idx > 0 && idx <= _selectionNames.Length)
        {
            name = _selectionNames[idx];
        }

        _visualizationEngine?.TrackCelestialBody(name);
    }

    private void Scenario1Button_OnClick(object? sender, RoutedEventArgs e)
    {
        AddInfoMessage("Scenario 1 started", Colors.Orange);
    }

    private void ScaleFactorSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_visualizationEngine == null)
            return;

        var value = ScaleFactorSlider.Value;
        _visualizationEngine.CelestialBodyScaleFactor = (float)value;
        UpdateShownScaleFactor();
    }

    private void MinimumSizeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_visualizationEngine == null)
            return;

        var value = MinimumSizeSlider.Value;
        _visualizationEngine.MinimumPixelSize = (float)value;
        UpdateShownMinimumPixelSize();
    }

    private void MinimumSizeCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_visualizationEngine == null)
            return;

        _visualizationEngine.EnableMinimumPixelSize = MinimumSizeCheckBox.IsChecked ?? false;
    }
}
