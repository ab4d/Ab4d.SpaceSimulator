using System;
using System.Collections.Generic;
using System.Numerics;
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

    public CelestialBodyView(VisualizationEngine engine, CelestialBody physicsObject, Material material)
    {
        _visualizationEngine = engine;

        // Store reference to object from physics engine
        CelestialBody = physicsObject;

        // Create sphere node
        SphereNode = new SphereModelNode(name: $"{CelestialBody.Name}-Sphere")
        {
            Material = material,
        };

        // Orbit ellipse
        if (CelestialBody.HasOrbit && CelestialBody.Parent != null)
        {
            var orbitColor = new Color3(0.2f, 0.2f, 0.2f);

            var majorSemiAxis = (float)ScaleDistance(CelestialBody.OrbitRadius);
            var majorSemiAxisDir = Vector3.UnitZ;

            var minorSemiAxis = majorSemiAxis; // Approximate circular orbit
            var phi = (float)CelestialBody.OrbitalInclination * MathF.PI / 180.0f; // deg -> rad
            var minorSemiAxisDir = new Vector3(MathF.Cos(phi), MathF.Sin(phi), 0); // becomes (1, 0, 0) when phi=0

            OrbitNode = new EllipseLineNode(
                orbitColor,
                1,
                name: $"{CelestialBody.Name}-OrbitEllipse")
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
            var trajectoryColor = new Color3(0.25f, 0.25f, 0.25f);
            var initialTrajectory = GetTrajectoryTrail();
            TrajectoryNode = new MultiLineNode(
                initialTrajectory,
                true,
                trajectoryColor,
                2,
                name: $"{CelestialBody.Name}-Trajectory");
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

    public void SetVisible(bool visible)
    {
        SphereNode.Visibility = visible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        if (OrbitNode != null)
        {
            OrbitNode.Visibility = visible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        }
        if (TrajectoryNode != null)
        {
            TrajectoryNode.Visibility = visible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
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

        // Dynamic size change - can be triggered by other changes, such as viewport
        SphereNode.Radius = ScaleSize(CelestialBody.Radius);

        var camera = _visualizationEngine.Camera;
        var showChildren = true;
        if (_visualizationEngine.EnableMinimumPixelSize && camera.SceneView is { Width: > 0 })
        {
            // Adapted from CameraUtils.GetPerspectiveScreenSize()
            var distanceVector = SphereNode.CenterPosition - camera.GetCameraPosition();
            var lookDirection = Vector3.Normalize(camera.GetLookDirection());
            var lookDirectionDistance = Vector3.Dot(distanceVector, lookDirection);

            var xScale = MathF.Tan(camera.FieldOfView * MathF.PI / 360);
            var viewSizeX = camera.SceneView.Width;
            var displayedSize = viewSizeX * SphereNode.Radius / (lookDirectionDistance * xScale * 2f);

            var minSize = _visualizationEngine.MinimumPixelSize;
            if (displayedSize < minSize)
            {
                var correctedSize = (lookDirectionDistance * xScale * 2f) * minSize / viewSizeX; // Inverted eq. for displayedSize
                SphereNode.Radius = correctedSize;
                showChildren = false;
            }
        }

        // Modify the child visibility
        foreach (var child in Children)
        {
            child.SetVisible(showChildren);
        }
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
        return distance / Constants.AstronomicalUnit;
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
        // Scale with one astronomical unit by default - same as with distances
        var scaledSize = (float)(realSize / Constants.AstronomicalUnit);

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
