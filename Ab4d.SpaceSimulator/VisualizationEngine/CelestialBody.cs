using System;
using System.Numerics;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SpaceSimulator.PhysicsEngine;

namespace Ab4d.SpaceSimulator.VisualizationEngine;

public class CelestialBody
{
    private PhysicsEngine.CelestialBody? _celestialBody;

    public readonly SphereModelNode SceneNode;
    public float MinimumSize;

    public CelestialBody(PhysicsEngine.CelestialBody physicsObject, StandardMaterial material, float minimumSize)
    {
        MinimumSize = minimumSize;

        // Store reference
        _celestialBody = physicsObject;

        SceneNode = new SphereModelNode(_celestialBody.Name)
        {
            CenterPosition = ScalePosition(physicsObject.Position),
            Radius = ScaleSize(_celestialBody.Radius),
            Material = material
        };

        Console.Out.WriteLine($"{physicsObject.Name}: pos={SceneNode.CenterPosition}, radius={SceneNode.Radius}");
    }

    public void Update()
    {
        if (_celestialBody is null)
            return;

        // Update position from the underlying physical object
        SceneNode.CenterPosition = ScalePosition(_celestialBody.Position);
        SceneNode.Radius = ScaleSize(_celestialBody.Radius);
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
}
