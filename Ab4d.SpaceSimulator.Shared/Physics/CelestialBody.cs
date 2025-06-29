using System.Collections.Generic;
using System.Diagnostics;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SpaceSimulator.Physics;

[DebuggerDisplay("CelestialBody({Name})")]
public class CelestialBody : MassBody
{
    public CelestialBodyType Type;

    public double Radius; // meters

    // Angular velocity of rotation around the body's axis.
    public double RotationSpeed = 0; // deg/sec
    public double Rotation = 0; // degrees

    // Axial tilt
    public double AxialTilt = 0; // degrees

    // Fixed orbit
    public bool HasOrbit;

    public double OrbitRadius; // meters
    public double OrbitalEccentricity;

    public double OrbitalInclination; // deg
    public double LongitudeOfAscendingNode; // deg
    public double ArgumentOfPeriapsis; // deg

    // Parent in the star -> planet -> moon hierarchy
    public CelestialBody? Parent;

    // Trajectory tracker
    public ITrajectoryTracker? TrajectoryTracker;

    // Ring(s).
    public struct RingInfo
    {
        // Name of the ring
        public required string Name;

        // Inner and outer radius of the ring, measured from parent planet's center (i.e., including the parent
        // planet's radius).
        public required double InnerRadius; // meters
        public required double OuterRadius; // meters

        // Base color and texture.
        public required Color3 BaseColor;
        public string? TextureName;
    };

    public List<RingInfo>? Rings;

    public override void Initialize()
    {
        // Create trajectory trail tracker, if applicable (i.e., if the celestial body has stable orbit around a parent).
        if (Parent != null && HasOrbit)
        {
            TrajectoryTracker = new AngularTrajectoryTracker();
        }
        else
        {
            TrajectoryTracker = new LinearTrajectoryTracker();
        }
        TrajectoryTracker.UpdatePosition(this); // Initialize with current position.
    }

    public override void UpdateTrajectory()
    {
        // Update our instance of trajectory tracker
        TrajectoryTracker?.UpdatePosition(this);
    }

    public override void UpdateState(double timeDelta)
    {
        // Chain up to parent
        base.UpdateState(timeDelta);

        // Apply rotation speed, and clamp the rotation into 0 ~ 360 range.
        Rotation = (Rotation + RotationSpeed * timeDelta) % 360;
        Debug.Assert(double.IsFinite(Rotation), $"Rotation angle of {Name} is non-finite!");
    }
}
