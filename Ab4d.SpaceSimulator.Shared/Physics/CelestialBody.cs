using System;
using System.Diagnostics;

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
    public double OrbitalInclination; // deg

    public double OrbitalEccentricity;

    // Parent in the star -> planet -> moon hierarchy
    public CelestialBody? Parent;

    // Trajectory tracker
    public ITrajectoryTracker? TrajectoryTracker;

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
