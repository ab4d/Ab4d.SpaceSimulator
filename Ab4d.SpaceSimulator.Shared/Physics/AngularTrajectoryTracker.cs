using System;
using System.Collections.Generic;

namespace Ab4d.SpaceSimulator.Physics;

public class AngularTrajectoryTracker : ITrajectoryTracker
{
    public double MinimumAngleIncrement = 1; // minimum angle increment w.r.t. previous location, in degrees
    public double MaxAngle = 90;             // maximum angle w.r.t. last entry, in degrees

    // Axis of the celestial body's revolution. Inferred on the very first update from the initial position.
    public Vector3d? RevolutionAxis = null;

    private readonly Queue<ITrajectoryTracker.TrajectoryEntry> _trajectoryData = new();
    private ITrajectoryTracker.TrajectoryEntry? _lastTrajectoryEntry = null; // Keep track of last inserted item, since Queue supports look-up of only first element.

    public Queue<ITrajectoryTracker.TrajectoryEntry> GetTrajectoryData()
    {
        return _trajectoryData;
    }

    public void UpdatePosition(CelestialBody celestialBody)
    {
        var newEntry = new ITrajectoryTracker.TrajectoryEntry()
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
        _trajectoryData.Enqueue(newEntry);
        _lastTrajectoryEntry = newEntry;

        // Prune old entries - i.e., the ones that exceed the maximum angle w.r.t. the latest entry.
        while (_trajectoryData.Count > 1)
        {
            // Peek at first entry
            var entry = _trajectoryData.Peek();
            var angle = ComputeAngle(entry, _lastTrajectoryEntry, RevolutionAxis.Value);
            if (angle > MaxAngle)
            {
                _trajectoryData.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    private static double ComputeAngle(ITrajectoryTracker.TrajectoryEntry entry1, ITrajectoryTracker.TrajectoryEntry entry2, Vector3d normalAxis)
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
