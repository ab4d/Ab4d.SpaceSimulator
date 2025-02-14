using System;
using System.Collections.Generic;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Colors = Avalonia.Media.Colors;

namespace Ab4d.SpaceSimulator.Shared;

public partial class MainView : UserControl
{
    private PointerCameraController? _pointerCameraController;

    private TargetPositionCamera? _camera;

    private bool _isPlaying;
    private bool _isInternalChange;
    private readonly int[] _simulationSpeedIntervals;

    private List<TextBlock> _allMessages = new();
    private const int MaxShownInfoMessagesCount = 5;

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

        _simulationSpeedIntervals = new int[] { 0, 1, 10, 60, 600, 3600, 6 * 3600, 24 * 3600, 7 * 24 * 3600, 30 * 24 * 3600 };

        SimulationSpeedSlider.Maximum = _simulationSpeedIntervals.Length - 1;
        SpeedInfoTextBlock.Text = "";

        SetupCameraController();
        CreateTestScene();

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
        if (simulationSpeed <= 0)
        {
            //_gravitySimulator.StopSimulation();
            SpeedInfoTextBlock.Text = "";
        }
        else
        {
            //double oneSimulationStepDuration = simulationSpeed / SimulationStepsPerSecond;

            //if (!_gravitySimulator.IsSimulationStarted)
            //{
            //    _gravitySimulator.StartSimulation(SimulationStepsPerSecond, oneSimulationStepDuration);
            //}
            //else
            //{
            //    _gravitySimulator.SimulationStepsPerSecond = SimulationStepsPerSecond;
            //    _gravitySimulator.OneSimulationStepDuration = oneSimulationStepDuration;
            //}

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

            SpeedInfoTextBlock.Text = string.Format("+{0:0.0} {1}/s", infoValue, infoUnit);
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
        //string timeText = "Time: ";

        //if (_gravitySimulator.TimeInDays > 1)
        //    timeText += string.Format("+{0:0} days ", _gravitySimulator.TimeInDays);

        //int timeWithinDay = (int)_gravitySimulator.TimeInSeconds % (24 * 60 * 60);
        //int hours = timeWithinDay / (60 * 60);
        //int minutes = (timeWithinDay % (60 * 60)) / 60;
        //int seconds = timeWithinDay % 60;

        //timeText += string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);

        //SimulationTimeTextBlock.Text = timeText;
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
            PlayPauseButton.Content = "\u23f5";
            _isPlaying = false;
        }
        else
        {
            PlayPauseButton.Content = "\u23f8";
            _isPlaying = true;
        }
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