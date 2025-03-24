using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    private string[] _selectionNames = ["custom"]; // Populated once scenario is set up

    private PlanetTextureLoader? _planetTextureLoader;

    private bool _isVerticalView;

    public MainView()
    {
        InitializeComponent();

        ViewCenterComboBox.ItemsSource = _selectionNames;
        ViewCenterComboBox.SelectionChanged += ViewCenterComboBox_OnSelectionChanged;
        ViewCenterComboBox.SelectedIndex = 0;

        _simulationSpeedIntervals = new int[] { 0, 10, 100, 600, 3600, 6 * 3600, 24 * 3600, 10 * 24 * 3600, 30 * 24 * 3600, 100 * 24 * 3600 };

        SimulationSpeedSlider.Value = 0; // initially paused

        SimulationSpeedSlider.Maximum = _simulationSpeedIntervals.Length - 1;
        SpeedInfoTextBlock.Text = "";

        SetupCameraController();

        // Create physics and visualization engine
        _physicsEngine = new PhysicsEngine();
        Debug.Assert(_camera != null, nameof(_camera) + " != null");
        _visualizationEngine = new VisualizationEngine(MainSceneView.SceneView, _camera, _cameraController);

        // Create scene
        var solarSystem = new SolarSystemScenario();

        MainSceneView.Scene.RootNode.Add(_visualizationEngine.RootNode);

        MainSceneView.GpuDeviceCreated += (sender, args) =>
        {
            _planetTextureLoader = new PlanetTextureLoader(args.GpuDevice);
            solarSystem.SetupScenario(_physicsEngine, _visualizationEngine, _planetTextureLoader);

            // Call async method from sync context:
            _ = InitializeBitmapTextCreatorAsync();

            // Setup lights
            MainSceneView.Scene.SetAmbientLight(0.2f);

            foreach (var oneLight in _visualizationEngine.Lights)
                MainSceneView.Scene.Lights.Add(oneLight);


            // Populate the list for ViewCenterComboBox
            var selectionNames = new List<string>();
            selectionNames.Add("custom");

            int selectedIndex = 0;

            for (var i = 0; i < _visualizationEngine.CelestialBodyViews.Count; i++)
            {
                var bodyView = _visualizationEngine.CelestialBodyViews[i];
                if (selectedIndex == 0 && bodyView.Type == CelestialBodyType.Star)
                    selectedIndex = i + 1; // skip 'custom'
                        
                selectionNames.Add(bodyView.Name);
            }

            _selectionNames = selectionNames.ToArray();
            ViewCenterComboBox.ItemsSource = _selectionNames;
            ViewCenterComboBox.SelectedIndex = selectedIndex;
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

                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));

                Grid.SetColumn(TimePanel, 0);
                Grid.SetColumnSpan(TimePanel, 3);
                Grid.SetColumn(ViewCenterPanel, 0);
                Grid.SetColumn(SettingsPanel, 2);
                
                Grid.SetRow(TimePanel, 0);
                Grid.SetRow(ViewCenterPanel, 1);
                Grid.SetRow(SettingsPanel, 1);
            }
            else
            {
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                BottomOptionsGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Auto));      
                
                Grid.SetRow(TimePanel, 0);
                Grid.SetRow(ViewCenterPanel, 0);
                Grid.SetRow(SettingsPanel, 0);
                
                Grid.SetColumn(TimePanel, 0);
                Grid.SetColumnSpan(TimePanel, 1);
                Grid.SetColumn(ViewCenterPanel, 1);
                Grid.SetColumn(SettingsPanel, 3);
            }

            _isVerticalView = isVerticalView;
        }

        _visualizationEngine?.Update(false);
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
            
            IsAutomaticNearPlaneDistanceCalculation = false,
            IsAutomaticFarPlaneDistanceCalculation = false
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

    private void ShowSettingsPanels(StackPanel panelToShow)
    {
        SettingBorder.IsVisible = true;

        // First hide all panels
        foreach (var childPanel in SettingsGrid.Children.OfType<StackPanel>())
            childPanel.IsVisible = false;

        // Now show selected panel
        panelToShow.IsVisible = true;
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

    private void Scenario1Button_OnClick(object? sender, RoutedEventArgs e)
    {
        AddInfoMessage("Scenario 1 started", Colors.Orange);
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
            SimulationSettingsButton.IsChecked = false;
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
            ViewSettingsButton.IsChecked = false;
            ShowSettingsPanels(SimulationSettingsPanel);
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
