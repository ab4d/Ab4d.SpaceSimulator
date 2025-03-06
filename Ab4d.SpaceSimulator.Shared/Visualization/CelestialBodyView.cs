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
    private readonly CelestialBody _celestialBody;

    public readonly SphereModelNode SceneNode;
    public float MinimumSize;

    private readonly TrajectoryTracker? _trajectoryTracker = null;
    public readonly MultiLineNode? TrajectoryNode = null;

    public CelestialBodyView(CelestialBody physicsObject, StandardMaterial material, float minimumSize)
    {
        MinimumSize = minimumSize;

        // Store reference to object from physics engine
        _celestialBody = physicsObject;

        // Create sphere node
        SceneNode = new SphereModelNode(name: $"{_celestialBody.Name}-Sphere")
        {
            Material = material,
        };

        // Trail / trajectory for objects with parent (planets and moons)
        if (_celestialBody.Parent != null)
        {
            // Create trajectory tracker
            _trajectoryTracker = new TrajectoryTracker();
            _trajectoryTracker.UpdatePosition(_celestialBody);

            // Create trajectory multi-line node
            var trajectoryColor = new Color4(Colors.White, .25f);
            var initialTrajectory = GetTrajectoryTrail();
            TrajectoryNode = new MultiLineNode(
                initialTrajectory,
                true,
                trajectoryColor,
                2,
                name: $"{_celestialBody.Name}-Trajectory");
        }

        // Perform initial update
        Update();
    }

    public void Update()
    {
        // Update position from the underlying physical object
        SceneNode.CenterPosition = ScalePosition(_celestialBody.Position);
        SceneNode.Radius = ScaleSize(_celestialBody.Radius);

        // Rotate around body's axis
        SceneNode.Transform = ComputeTiltAndRotationTransform();

        // Update trajectory tracker to obtain celestial body's trail
        if (_trajectoryTracker != null && TrajectoryNode != null)
        {
            _trajectoryTracker.UpdatePosition(_celestialBody);
            TrajectoryNode.Positions = GetTrajectoryTrail();
        }
    }

    private MatrixTransform ComputeTiltAndRotationTransform()
    {
        var center = SceneNode.CenterPosition;
        var matrix = (
            Matrix4x4.CreateTranslation(-center) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, MathUtils.DegreesToRadians((float)_celestialBody.Rotation)) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, MathUtils.DegreesToRadians((float)_celestialBody.AxialTilt)) *
            Matrix4x4.CreateTranslation(center)
        );

        return new MatrixTransform(matrix);
    }

    private Vector3 ScalePosition(Vector3d realPosition)
    {
        var length = realPosition.Length();
        var scaledLength = length / Constants.AstronomicalUnit;

        var scaledPosition = scaledLength > 0 ? scaledLength * Vector3d.Normalize(realPosition) : Vector3d.Zero;
        return new Vector3((float)scaledPosition.X, (float)scaledPosition.Y, (float)scaledPosition.Z);
    }

    private float ScaleSize(double realSize)
    {
        // TODO: we can accurately show distances OR sizes, but not both at the same time...
        return MathF.Max(MinimumSize, (float)(realSize / Constants.AstronomicalUnit));
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