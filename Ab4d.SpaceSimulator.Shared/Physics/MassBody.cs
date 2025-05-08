namespace Ab4d.SpaceSimulator.Physics;

public class MassBody
{
    public required string Name;

    public Vector3d Position; // meters
    public Vector3d Velocity; // m/s
    public Vector3d Acceleration; // m/s^2

    public double Mass; // kg

    public Vector3d TotalGravitationalForce; // N

    public virtual void Initialize()
    {

    }

    public virtual void UpdateTrajectory()
    {

    }

    public virtual void UpdateState(double timeDelta)
    {

    }

    public virtual Vector3d GetAdditionalForce()
    {
        return Vector3d.Zero;
    }
}
