using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine;
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

        if (this.Parent != null && this.Parent.SphereNode != null)
        {
            float orbitRadius = (float)this.CelestialBody.OrbitRadius * VisualizationEngine.ViewUnitScale;

            var parentRadius = this.Parent.SphereNode.Radius;
            bool isBodyVisible = orbitRadius > (parentRadius + this.SphereNode.Radius);
            bool isOrbitVisible = orbitRadius > parentRadius;

            SphereNode.Visibility = isBodyVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            if (OrbitNode != null)
                OrbitNode.Visibility = isOrbitVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            
            if (TrajectoryNode != null)
                TrajectoryNode.Visibility = isOrbitVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        }

        var camera = _visualizationEngine.Camera;
        var viewWidth = _visualizationEngine.SceneView.Width;

        // Dynamic size change - can be triggered by other changes, such as viewport
        float sphereRadius = ScaleSize(CelestialBody.Radius);
        
        if (_visualizationEngine.IsMinSizeLimited && viewWidth > 0)
        {
            // Adapted from CameraUtils.GetPerspectiveScreenSize()
            var distanceVector = SphereNode.CenterPosition - camera.GetCameraPosition();
            var lookDirection = Vector3.Normalize(camera.GetLookDirection());
            var lookDirectionDistance = Vector3.Dot(distanceVector, lookDirection);

            var xScale = MathF.Tan(camera.FieldOfView * MathF.PI / 360);
            var displayedSize = viewWidth * sphereRadius / lookDirectionDistance * xScale;

            var minSize = _visualizationEngine.MinScreenSize;
            if (displayedSize < minSize)
            {
                var correctedSize = lookDirectionDistance * xScale * minSize / viewWidth; // Inverted eq. for displayedSize
                
                if (correctedSize > 0)
                    sphereRadius = correctedSize;
            }
        }

        SphereNode.Radius = sphereRadius;
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
}
