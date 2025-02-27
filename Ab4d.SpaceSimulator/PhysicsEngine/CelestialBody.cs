using System;
using System.Diagnostics;

namespace Ab4d.SpaceSimulator.PhysicsEngine;

public class CelestialBody : MassBody
{
    public double Radius; // meters

    // Angular velocity of rotation around the body's axis.
    public double RotationSpeed = 0; // rad/sec
    public double Rotation = 0; // rad

    public override void UpdateState(double timeDelta)
    {
        // Chain up to parent
        base.UpdateState(timeDelta);

        // Apply rotation speed, and clamp the rotation into 0 ~ 2*PI range.
        Rotation = (Rotation + RotationSpeed * timeDelta) % (2 * Math.PI);
        Debug.Assert(double.IsFinite(Rotation), $"Rotation angle of {Name} is non-finite!");
    }
}
