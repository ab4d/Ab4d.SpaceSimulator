using System;
using System.Diagnostics;

namespace Ab4d.SpaceSimulator.Physics;

public class CelestialBody : MassBody
{
    public double Radius; // meters

    // Angular velocity of rotation around the body's axis.
    public double RotationSpeed = 0; // deg/sec
    public double Rotation = 0; // degrees

    // Axial tilt
    public double AxialTilt = 0; // degrees

    // Parent in the star -> planet -> moon hierarchy
    public CelestialBody? Parent;

    public override void UpdateState(double timeDelta)
    {
        // Chain up to parent
        base.UpdateState(timeDelta);

        // Apply rotation speed, and clamp the rotation into 0 ~ 360 range.
        Rotation = (Rotation + RotationSpeed * timeDelta) % 360;
        Debug.Assert(double.IsFinite(Rotation), $"Rotation angle of {Name} is non-finite!");
    }
}
