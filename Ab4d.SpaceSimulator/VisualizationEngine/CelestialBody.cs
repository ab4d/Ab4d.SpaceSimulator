using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SpaceSimulator.PhysicsEngine;

namespace Ab4d.SpaceSimulator.VisualizationEngine;

public class CelestialBody
{
    private PhysicsEngine.CelestialBody? _celestialBody;

    public readonly SphereModelNode SceneNode;
    public float MinimumSize;

    public const int TrajectoryLength = 100;
    private Queue<Vector3> _trajectoryPositions = new();
    public readonly MultiLineNode TrajectoryNode;

    public CelestialBody(PhysicsEngine.CelestialBody physicsObject, StandardMaterial material, float minimumSize)
    {
        MinimumSize = minimumSize;

        // Store reference to object from physics engine
        _celestialBody = physicsObject;

        // Create sphere node
        SceneNode = new SphereModelNode(name: $"{_celestialBody.Name}-Sphere")
        {
            CenterPosition = ScalePosition(physicsObject.Position),
            Radius = ScaleSize(_celestialBody.Radius),
            Material = material,
        };

        // Create trajectory multi-line node
        var trajectoryColor = new Color4(Colors.White, .25f);
        _trajectoryPositions.Enqueue(SceneNode.CenterPosition);
        TrajectoryNode =
            new MultiLineNode(_trajectoryPositions.ToArray(), true, trajectoryColor, 2, name: $"{_celestialBody.Name}-Trajectory");
    }

    public void Update()
    {
        if (_celestialBody is null)
            return;

        // Update position from the underlying physical object
        SceneNode.CenterPosition = ScalePosition(_celestialBody.Position);
        SceneNode.Radius = ScaleSize(_celestialBody.Radius);

        // Rotate around body's axis
        SceneNode.Transform = new AxisAngleRotateTransform(
            Vector3.UnitY,
            (float)_celestialBody.Rotation * 180.0f / MathF.PI,
            SceneNode.CenterPosition);

        // Update trail
        // TODO: once multiple position-scaling methods are implemented, we will need to keep track of the original
        // positions and re-scale them as necessary. For now, it suffices to store the last scaled position.
        // TODO: the length of the trajectory should likely be defined by the age of its points instead of just
        // maximum number of elements.
        _trajectoryPositions.Enqueue(SceneNode.CenterPosition);
        while (_trajectoryPositions.Count > TrajectoryLength)
        {
            _trajectoryPositions.Dequeue();
        }
        TrajectoryNode.Positions = _trajectoryPositions.ToArray();
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
