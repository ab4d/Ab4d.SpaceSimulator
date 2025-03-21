using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SpaceSimulator.Physics;

namespace Ab4d.SpaceSimulator.Visualization;

public class CelestialBodyView
{
    private readonly VisualizationEngine _visualizationEngine;
    public readonly CelestialBody CelestialBody;
    
    public string Name => CelestialBody.Name;
    public CelestialBodyType Type => CelestialBody.Type;

    // Parent / child hierarchy
    public CelestialBodyView? Parent = null;
    public readonly List<CelestialBodyView> Children = [];

    // Celestial body sphere
    public readonly SphereModelNode SphereNode;

    // Fixed trajectory / orbit
    public readonly EllipseLineNode? OrbitNode;

    // Dynamic trajectory / trail
    private readonly TrajectoryTracker? _trajectoryTracker;
    public readonly MultiLineNode? TrajectoryNode;

    public SceneNode? NameSceneNode { get; set; }


    private Color3 _orbitColor;
    public Color3 OrbitColor
    {
        get => _orbitColor;
        set
        {
            _orbitColor = value;

            if (OrbitNode != null)
                OrbitNode.LineColor = (value * 0.5f).ToColor4(); // Make orbit color darker

            if (TrajectoryNode != null)
                TrajectoryNode.LineColor = (value * 0.7f).ToColor4();  // Trajectory color is lighter than orbit's color
        }
    }


    public CelestialBodyView(VisualizationEngine engine, CelestialBody physicsObject, Material material)
    {
        _visualizationEngine = engine;

        // Store reference to object from physics engine
        CelestialBody = physicsObject;

        // Create sphere node
        SphereNode = new SphereModelNode(name: $"{this.Name}-Sphere")
        {
            Material = material,
        };

        // Orbit ellipse
        if (CelestialBody.HasOrbit && CelestialBody.Parent != null)
        {
            var orbitColor = new Color3(0.2f, 0.2f, 0.2f);  // This is the default color that can be changed by setting OrbitColor

            var majorSemiAxis = (float)ScaleDistance(CelestialBody.OrbitRadius);
            var majorSemiAxisDir = Vector3.UnitZ;

            var minorSemiAxis = majorSemiAxis; // Approximate circular orbit
            var phi = (float)CelestialBody.OrbitalInclination * MathF.PI / 180.0f; // deg -> rad
            var minorSemiAxisDir = new Vector3(MathF.Cos(phi), MathF.Sin(phi), 0); // becomes (1, 0, 0) when phi=0

            OrbitNode = new EllipseLineNode(
                orbitColor,
                1,
                name: $"{this.Name}-OrbitEllipse")
            {
                CenterPosition = ScalePosition(CelestialBody.Parent.Position),
                WidthDirection = majorSemiAxisDir,
                Width = majorSemiAxis * 2,
                HeightDirection = minorSemiAxisDir,
                Height = majorSemiAxis * 2,
                Segments = 359, // 1-degree resolution
            };
        }

        // Trail / trajectory for objects with parent (planets and moons)
        if (CelestialBody.Parent != null)
        {
            // Create trajectory tracker
            _trajectoryTracker = new TrajectoryTracker();
            _trajectoryTracker.UpdatePosition(CelestialBody);

            // Create trajectory multi-line node
            var trajectoryColor = new Color3(0.25f, 0.25f, 0.25f); // This is the default color that can be changed by setting OrbitColor
            var initialTrajectory = GetTrajectoryTrail();
            TrajectoryNode = new MultiLineNode(
                initialTrajectory,
                true,
                trajectoryColor,
                2,
                name: $"{this.Name}-Trajectory");
        }

        // Perform initial update
        Update(true);
    }

    public void RegisterNodes(GroupNode rootNode)
    {
        rootNode.Add(SphereNode);
        if (OrbitNode != null)
        {
            rootNode.Add(OrbitNode);
        }
        if (TrajectoryNode != null)
        {
            rootNode.Add(TrajectoryNode);
        }
    }

    public void Update(bool dataChange)
    {
        // Update properties to reflect the change in underlying physical object properties (e.g., position).
        if (dataChange)
        {
            // Update position from the underlying physical object
            SphereNode.CenterPosition = ScalePosition(CelestialBody.Position);

            // Rotate around body's axis
            SphereNode.Transform = ComputeTiltAndRotationTransform();

            // Update orbit ellipse - its position
            if (OrbitNode != null && CelestialBody.Parent != null)
            {
                // NOTE: strictly speaking, we should scale using parent's ScalePosition(), in case it uses different
                // parameters...
                OrbitNode.CenterPosition = ScalePosition(CelestialBody.Parent.Position);
            }

            // Update trajectory tracker to obtain celestial body's trail
            if (_trajectoryTracker != null && TrajectoryNode != null)
            {
                _trajectoryTracker.UpdatePosition(CelestialBody);
                TrajectoryNode.Positions = GetTrajectoryTrail();
            }
        }

        var camera = _visualizationEngine.Camera;
        var viewWidth = _visualizationEngine.SceneView.Width;

        if (viewWidth > 0)
        {
            // Dynamic size change - can be triggered by other changes, such as viewport
            float sphereRadius = ScaleSize(CelestialBody.Radius);

            // Adapted from CameraUtils.GetPerspectiveScreenSize()
            var distanceVector = SphereNode.CenterPosition - camera.GetCameraPosition();
            var lookDirection = Vector3.Normalize(camera.GetLookDirection());
            var lookDirectionDistance = Vector3.Dot(distanceVector, lookDirection);
            var xScale = MathF.Tan(camera.FieldOfView * MathF.PI / 360);

            if (_visualizationEngine.IsMinSizeLimited)
            {
                var displayedSize = viewWidth * sphereRadius / (lookDirectionDistance * xScale); // We would also need to multiply sphereRadius * 2, and also multiply xScale * 2; but in this case we can skip that

                var minSize = _visualizationEngine.MinScreenSize;
                if (displayedSize < minSize)
                {
                    var correctedSize = lookDirectionDistance * xScale * minSize / viewWidth; // Inverted eq. for displayedSize

                    if (correctedSize > 0)
                        sphereRadius = correctedSize;
                }
            }

            SphereNode.Radius = sphereRadius;


            bool isBodyVisible;

            if (this.Parent != null && this.Parent.SphereNode != null)
            {
                var orbitRadius = (float)this.CelestialBody.OrbitRadius * VisualizationEngine.ViewUnitScale;
                var orbitRadiusScreenSize = (orbitRadius * viewWidth) / (lookDirectionDistance * xScale * 2);

                var parentRadius = this.Parent.SphereNode.Radius;

                isBodyVisible = orbitRadius > (parentRadius + sphereRadius) * 1.1 &&            // If orbit is too close to parent, then hide the planet or moon
                                orbitRadiusScreenSize > VisualizationEngine.MinOrbitScreenSize; // If moon or planet's orbit is smaller than 20 pixels, then hide the planet or moon 

                bool isOrbitVisible = isBodyVisible && (orbitRadius > parentRadius * 1.1);

                SphereNode.Visibility = isBodyVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

                if (OrbitNode != null)
                    OrbitNode.Visibility = (isOrbitVisible && _visualizationEngine.ShowOrbits) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

                if (TrajectoryNode != null)
                    TrajectoryNode.Visibility = (isOrbitVisible && _visualizationEngine.ShowTrajectories) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            }
            else
            {
                isBodyVisible = true;
            }

            if (NameSceneNode != null)
                NameSceneNode.Visibility = (isBodyVisible && _visualizationEngine.ShowNames) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        }


        UpdateNameSceneNode();
    }

    private MatrixTransform ComputeTiltAndRotationTransform()
    {
        var center = SphereNode.CenterPosition;
        var matrix = (
            Matrix4x4.CreateTranslation(-center) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, MathUtils.DegreesToRadians((float)CelestialBody.Rotation)) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, MathUtils.DegreesToRadians((float)CelestialBody.AxialTilt)) *
            Matrix4x4.CreateTranslation(center)
        );

        return new MatrixTransform(matrix);
    }

    private double ScaleDistance(double distance)
    {
        // Scale so 1 unit in 3D view space is 1 million km = 1e9 m
        return distance * VisualizationEngine.ViewUnitScale;
    }

    private Vector3 ScalePosition(Vector3d realPosition)
    {
        var length = realPosition.Length();
        var scaledLength = ScaleDistance(length);

        var scaledPosition = scaledLength > 0 ? scaledLength * Vector3d.Normalize(realPosition) : Vector3d.Zero;
        return new Vector3((float)scaledPosition.X, (float)scaledPosition.Y, (float)scaledPosition.Z);
    }

    private float ScaleSize(double realSize)
    {
        // Scale so 1 unit in 3D view space is 1 million km = 1e9 m
        var scaledSize = (float)(realSize * VisualizationEngine.ViewUnitScale); 

        // Scale by global scale factor
        scaledSize *= _visualizationEngine.CelestialBodyScaleFactor;

        return scaledSize;
    }

    private Vector3[] GetTrajectoryTrail()
    {
        if (_trajectoryTracker == null)
            return [];

        var data = _trajectoryTracker.TrajectoryData;
        var trajectory = new Vector3[data.Count + 1]; // Always append current position

        var idx = 0;
        if (CelestialBody.Parent != null /* && !forceHeliocentricTrajectories */)
        {
            // Parent-centric trajectory
            var currentParentPosition = CelestialBody.Parent.Position;
            foreach (var entry in data)
            {
                var position = currentParentPosition + (entry.Position - entry.ParentPosition);
                trajectory[idx++] = ScalePosition(position);
            }
        }
        else
        {
            // Helio-centric trajectory
            foreach (var entry in data)
            {
                trajectory[idx++] = ScalePosition(entry.Position);
            }
        }

        // Add current position - this prevents "gaps" between the last tracked position and the current position
        // (current position might not be tracked due to its revolution angle being below the threshold), and makes
        // the trajectory update appear to be smooth.
        trajectory[idx++] = ScalePosition(CelestialBody.Position);

        return trajectory;
    }

    public void UpdateNameSceneNode()
    {
        if (NameSceneNode == null)
            return;

        var desiredScreenHeight = 30f;

        // If we want to specify the screen size in device independent units, then we need to scale by DPI scale.
        // If we want to set the size in pixels, the comment the following line.
        desiredScreenHeight *= _visualizationEngine.SceneView.DpiScaleX;

        var camera = _visualizationEngine.Camera;

        var viewWidth = (float)_visualizationEngine.SceneView.Width;
        var viewHeight = (float)_visualizationEngine.SceneView.Height;


        // Get lookDirectionDistance
        // If we look directly at the sphere, then we could use: lookDirectionDistance = textPosition - cameraPosition,
        // but when we look at some other direction, then we need to use the following code that
        // gets the distance to the text in the look direction:
        var textCenterPosition = NameSceneNode.WorldBoundingBox.GetCenterPosition();
        var cameraPosition = camera.GetCameraPosition();

        var distanceVector = textCenterPosition - cameraPosition;

        var lookDirection = Vector3.Normalize(camera.GetLookDirection());

        // To get look direction distance we project the distanceVector to the look direction vector
        var lookDirectionDistance = Vector3.Dot(distanceVector, lookDirection);

        float aspectRatio = viewWidth / viewHeight;
        float xScale = MathF.Tan(camera.FieldOfView * MathF.PI / 360);

        float scaleFactor = (2 * lookDirectionDistance * desiredScreenHeight * xScale) / (viewWidth * aspectRatio);


        // To align the text with camera, we first need to generate the text
        // so that its textDirection is set to (1, 0, 0) and upDirection is set to (0, 1, 0).
        // This will orient the text with the camera when Heading is 0 and Attitude is 0.
        // After that, we can align the text with the camera by simply negating the camera's 
        // rotation that is defined by view matrix.

        var invertedView = camera.GetInvertedViewMatrix();

        // Get the right direction vector from the inverted view matrix                
        var rightDirectionVector = new Vector3(invertedView.M11, invertedView.M12, invertedView.M13);

        // Move the text to the right of the body (for 8 pixels to the right)
        var textPosition = this.SphereNode.CenterPosition + rightDirectionVector * (this.SphereNode.Radius + ((2 * 8) / viewWidth * xScale * lookDirectionDistance));
        
        // and adjust the matrix
        invertedView.M41 = textPosition.X;
        invertedView.M42 = textPosition.Y;
        invertedView.M43 = textPosition.Z;

        // Make sure that NameSceneNode has MatrixTransform
        if (NameSceneNode.Transform is not MatrixTransform matrixTransform)
        {
            matrixTransform = new MatrixTransform();
            NameSceneNode.Transform = matrixTransform;
        }

        matrixTransform.SetMatrix(Matrix4x4.CreateScale(scaleFactor, scaleFactor, 1) * invertedView);
    }
}
