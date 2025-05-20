using System.Collections.Generic;

namespace Ab4d.SpaceSimulator.Physics;

public class LinearTrajectoryTracker : ITrajectoryTracker
{
    public double MinimumDistanceIncrement = 1_000; // minimum distance increment w.r.t. previous location, in meters
    public double MaxEntries = 500; // maximum angle number of entries

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

        // If we have information about the last inserted entry, compute distance between last and this entry.
        if (_lastTrajectoryEntry != null)
        {
            var distance = (_lastTrajectoryEntry.Position - newEntry.Position).Length();
            if (distance < MinimumDistanceIncrement) {
                return; // Skip the update
            }
        }

        // Store new entry.
        _trajectoryData.Enqueue(newEntry);
        _lastTrajectoryEntry = newEntry;

        // Prune old entries.
        while (_trajectoryData.Count > MaxEntries)
        {
            _trajectoryData.Dequeue();
        }
    }
}
