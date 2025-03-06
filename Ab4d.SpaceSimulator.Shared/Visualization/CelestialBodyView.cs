using System;
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
    private readonly CelestialBody _celestialBody;

    // Celestial body sphere
    public readonly SphereModelNode SphereNode;
    public float MinimumSize;

    // Fixed trajectory / orbit
    public readonly EllipseLineNode? OrbitNode;

    // Dynamic trajectory / trail
    private readonly TrajectoryTracker? _trajectoryTracker;
    public readonly MultiLineNode? TrajectoryNode;

    public CelestialBodyView(VisualizationEngine engine, CelestialBody physicsObject, StandardMaterial material, float minimumSize)
    {
        _visualizationEngine = engine;

        MinimumSize = minimumSize;

        // Store reference to object from physics engine
        _celestialBody = physicsObject;

        // Create sphere node
        SphereNode = new SphereModelNode(name: $"{_celestialBody.Name}-Sphere")
        {
            Material = material,
        };

        // Orbit ellipse
        if (_celestialBody.HasOrbit && _celestialBody.Parent != null)
        {
            var orbitColor = new Color3(0.2f, 0.2f, 0.2f);

            var majorSemiAxis = (float)ScaleDistance(_celestialBody.OrbitRadius);
            var majorSemiAxisDir = Vector3.UnitZ;

            var minorSemiAxis = majorSemiAxis; // Approximate circular orbit
            var phi = (float)_celestialBody.OrbitalInclination * MathF.PI / 180.0f; // deg -> rad
            var minorSemiAxisDir = new Vector3(MathF.Cos(phi), MathF.Sin(phi), 0); // becomes (1, 0, 0) when phi=0

            OrbitNode = new EllipseLineNode(
                orbitColor,
                1,
                name: $"{_celestialBody.Name}-OrbitEllipse")
            {
                CenterPosition = ScalePosition(_celestialBody.Parent.Position),
                WidthDirection = majorSemiAxisDir,
                Width = majorSemiAxis * 2,
                HeightDirection = minorSemiAxisDir,
                Height = majorSemiAxis * 2,
                Segments = 359, // 1-degree resolution
            };
        }

        // Trail / trajectory for objects with parent (planets and moons)
        if (_celestialBody.Parent != null)
        {
            // Create trajectory tracker
            _trajectoryTracker = new TrajectoryTracker();
            _trajectoryTracker.UpdatePosition(_celestialBody);

            // Create trajectory multi-line node
            var trajectoryColor = new Color3(0.25f, 0.25f, 0.25f);
            var initialTrajectory = GetTrajectoryTrail();
            TrajectoryNode = new MultiLineNode(
                initialTrajectory,
                true,
                trajectoryColor,
                2,
                name: $"{_celestialBody.Name}-Trajectory");
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
            SphereNode.CenterPosition = ScalePosition(_celestialBody.Position);

            // Rotate around body's axis
            SphereNode.Transform = ComputeTiltAndRotationTransform();

            // Update orbit ellipse - its position
            if (OrbitNode != null && _celestialBody.Parent != null)
            {
                // NOTE: strictly speaking, we should scale using parent's ScalePosition(), in case it uses different
                // parameters...
                OrbitNode.CenterPosition = ScalePosition(_celestialBody.Parent.Position);
            }

            // Update trajectory tracker to obtain celestial body's trail
            if (_trajectoryTracker != null && TrajectoryNode != null)
            {
                _trajectoryTracker.UpdatePosition(_celestialBody);
                TrajectoryNode.Positions = GetTrajectoryTrail();
            }
        }

        // Dynamic size change - can be triggered by other changes, such as viewport
        SphereNode.Radius = ScaleSize(_celestialBody.Radius);

        var camera = _visualizationEngine.Camera;
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
            }
        }
    }

    private MatrixTransform ComputeTiltAndRotationTransform()
    {
        var center = SphereNode.CenterPosition;
        var matrix = (
            Matrix4x4.CreateTranslation(-center) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, MathUtils.DegreesToRadians((float)_celestialBody.Rotation)) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, MathUtils.DegreesToRadians((float)_celestialBody.AxialTilt)) *
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
        if (_celestialBody.Parent != null /* && !forceHeliocentricTrajectories */)
        {
            // Parent-centric trajectory
            var currentParentPosition = _celestialBody.Parent.Position;
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
        trajectory[idx++] = ScalePosition(_celestialBody.Position);

        return trajectory;
    }
}
