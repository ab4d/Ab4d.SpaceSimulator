using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SpaceSimulator.PhysicsEngine;

namespace Ab4d.SpaceSimulator.VisualizationEngine;

public class TrajectoryTracker
{
    public class TrajectoryEntry
    {
        //public double Timestamp;
        public Vector3d Position;
        public Vector3d ParentPosition;
    };

    public double MinimumAngleIncrement = 1; // minimum angle increment w.r.t. previous location, in degrees
    public double MaxAngle = 270; // maximum angle w.r.t. last entry, in degrees

    // Axis of the celestial body's revolution. Inferred on the very first update from the initial position.
    public Vector3d? RevolutionAxis = null;

    public readonly Queue<TrajectoryEntry> TrajectoryData = new();
    private TrajectoryEntry? _lastTrajectoryEntry = null; // Keep track of last inserted item, since Queue supports look-up of only first element.

    public void UpdatePosition(CelestialBody celestialBody)
    {
        var newEntry = new TrajectoryEntry()
        {
            Position = celestialBody.Position,
            ParentPosition = celestialBody.Parent?.Position ?? Vector3d.Zero,
        };

        // Determine axis of revolution, if necessary. This serves as a normal vector in computation of signed revolution
        // angle.
        if (RevolutionAxis == null)
        {
            if (celestialBody.Parent != null)
            {
                var vector1 = Vector3d.Normalize(celestialBody.Position - celestialBody.Parent.Position);
                var vector2 = Vector3d.Normalize(celestialBody.Velocity);
                RevolutionAxis = Vector3d.Cross(vector1, vector2);
            }
            else
            {
                RevolutionAxis = new Vector3d(0, 1, 0); // Point upwards
            }
        }

        // If we have information about the last inserted entry, compute angle between last and this entry.
        if (_lastTrajectoryEntry != null)
        {
            var angle = ComputeAngle(_lastTrajectoryEntry, newEntry, RevolutionAxis.Value);
            if (angle < MinimumAngleIncrement)
            {
                return; // Skip the update
            }
        }

        // Store new entry.
        TrajectoryData.Enqueue(newEntry);
        _lastTrajectoryEntry = newEntry;

        // Prune old entries - i.e., the ones that exceed the maximum angle w.r.t. the latest entry.
        while (TrajectoryData.Count > 1)
        {
            // Peek at first entry
            var entry = TrajectoryData.Peek();
            var angle = ComputeAngle(entry, _lastTrajectoryEntry, RevolutionAxis.Value);
            if (angle > MaxAngle)
            {
                TrajectoryData.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    private static double ComputeAngle(TrajectoryEntry entry1, TrajectoryEntry entry2, Vector3d normalAxis)
    {
        // TODO: if we wanted to track helio-centric trajectories of moons, the vectors would need to be computed
        // w.r.t. sun position, not parent position!
        var vector1 = entry1.Position - entry1.ParentPosition;
        var vector2 = entry2.Position - entry2.ParentPosition;

        var length1 = vector1.Length();
        var length2 = vector2.Length();
        if (length1 == 0 || length2 == 0)
        {
            return 0;
        }

        vector1 /= length1;
        vector2 /= length2;

        var dot = Vector3d.Dot(vector1, vector2);
        var angle = Math.Acos(dot) * 180 / Math.PI;

        var cross = Vector3d.Normalize(Vector3d.Cross(vector1, vector2));
        var dir = Vector3d.Dot(cross, normalAxis);
        if (dir < 0)
            angle = 360 - angle;

        return angle;
    }
}

public class CelestialBodyVisualization
{
    private readonly CelestialBody _celestialBody;

    // Celestial body sphere
    public readonly SphereModelNode SpehereNode;
    public float MinimumSize;

    // Fixed trajectory / orbit
    public readonly EllipseLineNode? OrbitNode;

    // Dynamic trajectory / trail
    private readonly TrajectoryTracker? _trajectoryTracker;
    public readonly MultiLineNode? TrajectoryNode;

    public CelestialBodyVisualization(CelestialBody physicsObject, StandardMaterial material, float minimumSize)
    {
        MinimumSize = minimumSize;

        // Store reference to object from physics engine
        _celestialBody = physicsObject;

        // Create sphere node
        SpehereNode = new SphereModelNode(name: $"{_celestialBody.Name}-Sphere")
        {
            Material = material,
        };

        // Orbit ellipse
        if (_celestialBody.HasOrbit && _celestialBody.Parent != null)
        {
            var orbitColor = new Color3(0.1f, 0.1f, 0.1f);

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
        Update();
    }

    public void RegisterNodes(GroupNode RootNode)
    {
        RootNode.Add(SpehereNode);
        if (OrbitNode != null)
        {
            RootNode.Add(OrbitNode);
        }
        if (TrajectoryNode != null)
        {
            RootNode.Add(TrajectoryNode);
        }
    }

    public void Update()
    {
        // Update position from the underlying physical object
        SpehereNode.CenterPosition = ScalePosition(_celestialBody.Position);
        SpehereNode.Radius = ScaleSize(_celestialBody.Radius);

        // Rotate around body's axis
        SpehereNode.Transform = ComputeTiltAndRotationTransform();

        // Update orbit ellipse - its position
        if (OrbitNode != null && _celestialBody.Parent != null)
        {
            // NOTE: strictly speaking, we should scale using parent's ScalePosition(), in case it uses different
            // parameters..
            OrbitNode.CenterPosition = ScalePosition(_celestialBody.Parent.Position);
        }

        // Update trajectory tracker to obtain celestial body's trail
        if (_trajectoryTracker != null && TrajectoryNode != null)
        {
            _trajectoryTracker.UpdatePosition(_celestialBody);
            TrajectoryNode.Positions = GetTrajectoryTrail();
        }
    }

    private MatrixTransform ComputeTiltAndRotationTransform()
    {
        var center = SpehereNode.CenterPosition;
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
