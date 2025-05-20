using System.Collections.Generic;

namespace Ab4d.SpaceSimulator.Physics;

public interface ITrajectoryTracker
{
    public class TrajectoryEntry
    {
        //public double Timestamp;
        public Vector3d Position;
        public Vector3d ParentPosition;
    };

    public Queue<TrajectoryEntry> GetTrajectoryData();

    public void UpdatePosition(CelestialBody celestialBody);
}
