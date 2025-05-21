using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using System.Threading.Tasks;
using Ab4d.SpaceSimulator.Physics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Colors = Avalonia.Media.Colors;
using Ab4d.SpaceSimulator.Utilities;
using Ab4d.SpaceSimulator.Visualization;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.SpaceSimulator.Scenarios;
using Avalonia.Platform;

namespace Ab4d.SpaceSimulator.Shared;

public partial class MainView : UserControl
{
    private GesturesCameraController? _cameraController;

    private TargetPositionCamera? _camera;

    private bool _isPlaying;
    private double _simulationSpeed;

    private int[] _simulationSpeedIntervals;

    private DateTime _previousUpdateTime = DateTime.Now;

    private List<TextBlock> _allMessages = new();
    private const int MaxShownInfoMessagesCount = 5;

    private readonly PhysicsEngine? _physicsEngine;
    private readonly VisualizationEngine? _visualizationEngine;

    private string[] _selectionNames = ["custom"]; // Populated once scenario is set up

    private IScenario? _selectedScenario;

    private PlanetTextureLoader? _planetTextureLoader;

    private bool _isVerticalView;

    public MainView()
    {
        InitializeComponent();

        ViewCenterComboBox.ItemsSource = _selectionNames;
        ViewCenterComboBox.SelectionChanged += ViewCenterComboBox_OnSelectionChanged;
        ViewCenterComboBox.SelectedIndex = 0;

        // Initialize scenario list and populate the UI
        var scenarios = new IScenario[] {
            new SolarSystem(),
            new Trappist1System(),
            new BinaryStarsWithPlanets(),
            new EmptySpace(),
        };
        _selectedScenario = scenarios[0];

        foreach (var scenario in scenarios)
        {
            var button = new RadioButton
            {
                GroupName = "ScenariosRadioButtonGroup",
                Content = scenario.GetName(),
            };
            ScenarioListStackPanel.Children.Add(button);

            if (scenario == _selectedScenario)
                button.IsChecked = true;

            button.IsCheckedChanged += (sender, args) =>
            {
                var isChecked = button.IsChecked ?? false;
                if (!isChecked)
                    return;
                _selectedScenario = scenario;
                SetupScenario(_selectedScenario);
            };
        }

        _simulationSpeedIntervals = [0]; // Will be initialized when scenario is set up.

        SimulationSpeedSlider.Value = 0; // initially paused

        SimulationSpeedSlider.Maximum = _simulationSpeedIntervals.Length - 1;
        SpeedInfoTextBlock.Text = "";

        MainSceneView.CreateOptions.EnableStandardValidation = true;
        Log.LogLevel = LogLevels.Warn;
        Log.IsLoggingToDebugOutput = true;


        SetupCameraController();

        // Create physics and visualization engine
        _physicsEngine = new PhysicsEngine();
        Debug.Assert(_camera != null, nameof(_camera) + " != null");
        _visualizationEngine = new VisualizationEngine(MainSceneView.SceneView, _camera, _cameraController);

        // Create scene

        MainSceneView.Scene.RootNode.Add(_visualizationEngine.RootNode);

        MainSceneView.GpuDeviceCreated += (sender, args) =>
        {
            _planetTextureLoader = new PlanetTextureLoader(args.GpuDevice);

            // Setup initial scenario - do this before starting InitializeBitmapTextCreatorAsync(), so that the nodes
            // are ready when the async task below completes (and tries to create name nodes).
            if (_selectedScenario != null)
                SetupScenario(_selectedScenario);

            // Call async method from sync context; this will also create name nodes for celestial bodies once the bitmap
            // text creator is ready. (This is necessary for delayed text node initialization for the initial scenario,
            // which happens during application start-up).
            _ = InitializeBitmapTextCreatorAsync();

            _visualizationEngine.UpdateMilkyWay();
        };

        MainSceneView.SceneViewInitialized += (sender, args) =>
        {
            // After the size of SceneView is known, we can set the sizes of the bodies
            _visualizationEngine.Update(false);
        };

        MainSceneView.SceneUpdating += (sender, args) =>
        {
            // SceneUpdating is called cca 60 times per second

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
            _visualizationEngine.Update(true);

            UpdateShownSimulationTime();
        };


        // Initial UI update
        UpdateShownSimulationTime();
        SetSimulationSpeed(GetSimulationSpeed());

        if (_visualizationEngine.IsMinSizeLimited)
            MinScreenSizeSlider.Value = _visualizationEngine.MinScreenSize;
        else
            MinScreenSizeSlider.Value = 0;

        UseActualSizeCheckBox.IsChecked = !_visualizationEngine.IsMinSizeLimited;

        UpdateMinScreenSizeTextBlock();


        // In case when VulkanDevice cannot be created, show an error message
        // If this is not handled by the user, then SharpEngineSceneView will show its own error message
        MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
        {
            ShowDeviceCreateFailedError(args.Exception); // Show error message
            args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
        };

        this.SizeChanged += (sender, args) => OnViewSizeChanged(args);
    }

    private void SetupScenario(Scenarios.IScenario scenario)
    {
        Debug.Assert(_physicsEngine != null, nameof(_physicsEngine) + " != null");
        Debug.Assert(_visualizationEngine != null, nameof(_visualizationEngine) + " != null");
        Debug.Assert(_planetTextureLoader != null, nameof(_planetTextureLoader) + " != null");

        // Ensure simulation is stopped
        SimulationSpeedSlider.Value = 0;

        // Setup simulation speed intervals
        _simulationSpeedIntervals = scenario.GetSimulationSpeedIntervals() ??
                                    [0, 10, 100, 600, 3600, 6 * 3600, 24 * 3600, 10 * 24 * 3600, 30 * 24 * 3600, 100 * 24 * 3600]; // Defaults
        SimulationSpeedSlider.Maximum = _simulationSpeedIntervals.Length - 1;

        // Reset physics engine and visualization engine
        _physicsEngine.Reset();
        _visualizationEngine.Reset();

        // Setup scenario
        scenario.SetupScenario(_physicsEngine, _visualizationEngine, _planetTextureLoader);

        // Create name scene nodes - this requires _visualizationEngine.BitmapTextCreator to be initialized and set.
        // During initial scenario setup (part of application start up) this will not be the case yet (SetupScenario
        // is called before bitmap text creator is initialized in an async task, and text nodes are created post-hoc
        // in that task as well). During subsequent scenario switches/restarts, however, the bitmap text creator should
        // be available, and this call ends up creating the name nodes.
        _visualizationEngine.CreateNameSceneNodes();

        // Setup lights
        MainSceneView.Scene.SetAmbientLight(0.2f);

        foreach (var oneLight in _visualizationEngine.Lights)
            MainSceneView.Scene.Lights.Add(oneLight);

        // Populate the list for ViewCenterComboBox
        var selectionNames = new List<string>();
        selectionNames.Add("custom");

        var defaultView = scenario.GetDefaultView();
        var selectedIndex = 0;

        for (var i = 0; i < _visualizationEngine.CelestialBodyViews.Count; i++)
        {
            var bodyView = _visualizationEngine.CelestialBodyViews[i];
            if (defaultView != null && bodyView.Name == defaultView)
                selectedIndex = i + 1; // skip 'custom'

            selectionNames.Add(bodyView.Name);
        }

        _selectionNames = selectionNames.ToArray();
        ViewCenterComboBox.ItemsSource = _selectionNames;
        ViewCenterComboBox.SelectedIndex = selectedIndex;

        // If scenario did not provide default view (the celestial body to center view on), check if it provides
        // default camera target and distance and apply them.
        if (defaultView == null && _camera != null)
        {
            var defaultDistance = scenario.GetDefaultCameraDistance() != null ? scenario.GetDefaultCameraDistance().Value : 850;
            _camera.TargetPosition = new Vector3(0, 0, 0); // Center
            _camera.Distance = defaultDistance;
        }

        // Simulation time needs to be explicitly updated (since we stopped simulation).
        UpdateShownSimulationTime();

        // Force the update of world-bounding-box on the scene's root node, which is used to update far and near plane
        // distance in VisualizationEngine.UpdateCameraNearAndFarPlanes(), called by VisualizationEngine.Update().
        // This ensures that near and far plane are set correctly in the frame that immediately follows the scenario
        // switch.
        MainSceneView.Scene.RootNode.Update();
        _visualizationEngine.Update(dataChange: true);
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
                // Vertical layout: N x 3 grid:
                //  - first row: time panel (spans all three columns)
                //  - second row: view-center panel (first column), empty (second column), settings panel (third column)
                BottomOptionsGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));

                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));

                Grid.SetRow(TimePanel, 0);
                Grid.SetColumn(TimePanel, 0);
                Grid.SetColumnSpan(TimePanel, 3);

                Grid.SetRow(ViewCenterPanel, 1);
                Grid.SetColumn(ViewCenterPanel, 0);
                Grid.SetColumnSpan(ViewCenterPanel, 1);

                Grid.SetRow(SettingsPanel, 1);
                Grid.SetColumn(SettingsPanel, 2);
                Grid.SetColumnSpan(SettingsPanel, 1);
            }
            else
            {
                // Horizontal layout: 1 x 4 grid: time panel (first column), view-center panel (second column), empty (third column), settings panel (fourth column).
                // NOTE: this layout should match what is originally defined in the axaml file.
                BottomOptionsGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Auto));

                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));

                Grid.SetRow(TimePanel, 0);
                Grid.SetColumn(TimePanel, 0);
                Grid.SetColumnSpan(TimePanel, 1);

                Grid.SetRow(ViewCenterPanel, 0);
                Grid.SetColumn(ViewCenterPanel, 1);
                Grid.SetColumnSpan(ViewCenterPanel, 1);

                Grid.SetRow(SettingsPanel, 0);
                Grid.SetColumn(SettingsPanel, 3);
                Grid.SetColumnSpan(SettingsPanel, 1);
            }

            _isVerticalView = isVerticalView;
        }

        _visualizationEngine?.Update(false);
    }

    [MemberNotNull(member: nameof(_camera))]
    [MemberNotNull(member: nameof(_cameraController))]
    private void SetupCameraController()
    {
        _camera = new TargetPositionCamera()
        {
            Heading = -115,
            Attitude = -30,
            Distance = 850,
            ViewWidth = 500,
            TargetPosition = new Vector3(x: 0, y: 0, z: 0),
            ShowCameraLight = ShowCameraLightType.Never,

            IsAutomaticNearPlaneDistanceCalculation = false,
            IsAutomaticFarPlaneDistanceCalculation = false
        };

        // After the "powered by AB4D" logo is hidden, start the camera rotation
        // This is stopped on first manual camera rotation - see below the CameraRotateStarted event handler
        MainSceneView.SceneView.OnLicenseLogoRemoved = () =>
            _camera.StartRotation(headingChangeInSecond: 2, attitudeChangeInSecond: 0, accelerationSpeed: 1.01f, easingFunction: EasingFunctions.QuadraticEaseInFunction);

        MainSceneView.SceneView.Camera = _camera;


        // Create GesturesCameraController that is defined in Common folder.
        // It can also recognize pinch and scroll gestures.
        _cameraController = new GesturesCameraController(sharpEngineSceneView: MainSceneView)
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

        // Stop initial camera rotation
        _cameraController.CameraRotateStarted += (sender, args) => _camera?.StopRotation();
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

    private void ShowSettingsPanels(StackPanel panelToShow)
    {
        SettingBorder.IsVisible = true;

        // First hide all panels
        foreach (var childPanel in SettingsGrid.Children.OfType<StackPanel>())
            childPanel.IsVisible = false;

        // Now show selected panel
        panelToShow.IsVisible = true;
    }

    private void UncheckOtherSettingsButtons(ToggleButton activeButton)
    {
        // Uncheck other buttons in the settings panel.
        foreach (var button in SettingsPanel.Children.OfType<ToggleButton>())
        {
            if (button != activeButton)
                button.IsChecked = false;
        }
    }

    private void HideSettingsPanel()
    {
        SettingBorder.IsVisible = false;
    }

    private void SetSimulationSpeed(double simulationSpeed)
    {
        _simulationSpeed = simulationSpeed;

        if (simulationSpeed <= 0)
        {
            SpeedInfoTextBlock.Text = "  Paused";
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

        SetMaxSimulationStep();
    }

    private void SetMaxSimulationStep()
    {
        if (_physicsEngine == null)
            return;

        if (AutoMaxSimulationTimeStepCheckBox.IsChecked ?? false)
        {
            _physicsEngine.MaxSimulationTimeStep = Math.Max(3600, _simulationSpeed / 100); // Use 1 hour (3600 s) or run at least 100 sub-steps if simulationSpeed is bigger than 100 hours

            var usedTimeStep = Math.Min(_simulationSpeed, _physicsEngine.MaxSimulationTimeStep);
            SimulationTimeStepValueTextBlock.Text = usedTimeStep.ToString("N0"); // number with thousands separator and no decimals
        }
        else
        {
            if (Int32.TryParse(SimulationTimeStepTextBox.Text, out var simulationTime))
                _physicsEngine.MaxSimulationTimeStep = simulationTime;
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

    private void UpdateMinScreenSizeTextBlock()
    {
        if (_visualizationEngine == null)
            return;

        var value = _visualizationEngine.MinScreenSize;
        MinScreenSizeTextBlock.Text = $"Min screen size: {value:F0} px";
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

    private async Task InitializeBitmapTextCreatorAsync()
    {
        var bitmapTextCreator = await BitmapTextCreator.GetDefaultBitmapTextCreatorAsync(MainSceneView.Scene);

        if (_visualizationEngine != null)
        {
            _visualizationEngine.BitmapTextCreator = bitmapTextCreator;
            _visualizationEngine.CreateNameSceneNodes();
        }
    }

    private void SimulationSpeedSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!this.IsLoaded)
            return;

        var simulationSpeed = GetSimulationSpeed();

        _isPlaying = simulationSpeed > 0;

        if (_visualizationEngine != null)
            _visualizationEngine.IsSimulationPaused = !_isPlaying;

        SetSimulationSpeed(simulationSpeed);

        UpdateShownSimulationTime();
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

    private void ScenarioRestartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // Restart the selected scenario
        if (_selectedScenario == null)
            return;
        SetupScenario(_selectedScenario);
    }

    private void UseActualSizeCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_visualizationEngine == null)
            return;

        if (UseActualSizeCheckBox.IsChecked ?? false)
        {
            MinScreenSizeTextBlock.IsVisible = false;
            MinScreenSizeSlider.IsVisible = false;
        }
        else
        {
            MinScreenSizeTextBlock.IsVisible = true;
            MinScreenSizeSlider.IsVisible = true;
        }

        UpdateMinScreenSize();
    }

    private void MinScreenSizeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_visualizationEngine == null)
            return;

        UpdateMinScreenSize();
    }

    private void UpdateMinScreenSize()
    {
        if (_visualizationEngine == null)
            return;


        var sliderValue = (float)MinScreenSizeSlider.Value;
        bool useMinScreenSize = sliderValue > 1 && !(UseActualSizeCheckBox.IsChecked ?? false);

        _visualizationEngine.IsMinSizeLimited = useMinScreenSize;
        _visualizationEngine.MinScreenSize = useMinScreenSize ? sliderValue : 0;

        UpdateMinScreenSizeTextBlock();
    }

    private void AutoMaxSimulationTimeStepCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        bool isAutoTimeStep = AutoMaxSimulationTimeStepCheckBox.IsChecked ?? false;

        SimulationTimeStepValueTextBlock.IsVisible = isAutoTimeStep;
        SimulationTimeStepTextBox.IsVisible = !isAutoTimeStep;

        if (isAutoTimeStep)
            SetMaxSimulationStep();
    }

    private void ViewSettingsButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (ViewSettingsButton.IsChecked ?? false)
        {
            UncheckOtherSettingsButtons(ViewSettingsButton);
            ShowSettingsPanels(ViewSettingsPanel);
        }
        else
        {
            HideSettingsPanel();
        }
    }

    private void SimulationSettingsButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (SimulationSettingsButton.IsChecked ?? false)
        {
            UncheckOtherSettingsButtons(SimulationSettingsButton);
            ShowSettingsPanels(SimulationSettingsPanel);
        }
        else
        {
            HideSettingsPanel();
        }
    }

    private void ScenariosButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (ScenariosButton.IsChecked ?? false)
        {
            UncheckOtherSettingsButtons(ScenariosButton);
            ShowSettingsPanels(ScenarioSettingsPanel);
        }
        else
        {
            HideSettingsPanel();
        }
    }

    private void SimulationTimeStepTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        SetMaxSimulationStep();
    }

    private void ShowMilkyWayCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_visualizationEngine != null)
            _visualizationEngine.ShowMilkyWay = ShowMilkyWayCheckBox.IsChecked ?? false;
    }

    private void ShowOrbitsCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_visualizationEngine != null)
            _visualizationEngine.ShowOrbits = ShowOrbitsCheckBox.IsChecked ?? false;
    }

    private void ShowTrailsCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_visualizationEngine != null)
            _visualizationEngine.ShowTrails = ShowTrailsCheckBox.IsChecked ?? false;
    }

    private void ShowNamesCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_visualizationEngine != null)
            _visualizationEngine.ShowNames = ShowNamesCheckBox.IsChecked ?? false;
    }
}
