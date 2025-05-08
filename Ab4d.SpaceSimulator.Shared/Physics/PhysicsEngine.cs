using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ab4d.SpaceSimulator.Physics;

public class PhysicsEngine
{
    // Simulation time, in seconds since the start of simulation
    public double SimulationTime { get; set; }

    public double MaxSimulationTimeStep { get; set; } = 3600; // in seconds: 3600 = 1 hour by default (can be changed when simulation speed is changed)

    // Mass bodies in the system
    private readonly List<MassBody> _massBodies = [];

    public void AddBody(MassBody body)
    {
        Debug.Assert(Vector3d.IsFinite(body.Position), "Position is non-finite!");
        Debug.Assert(Vector3d.IsFinite(body.Velocity), "Velocity is non-finite!");
        Debug.Assert(Vector3d.IsFinite(body.Acceleration), "Acceleration is non-finite!");

        _massBodies.Add(body);
    }

    public void Simulate(double timeDelta)
    {
        var timeStep = Math.Min(timeDelta, MaxSimulationTimeStep);

        // Break the given time interval into multiple discrete time steps, if necessary
        while (timeDelta > 0)
        {
            RunSimulationStep(timeStep);

            // Once the simulation step is complete, update trajectory trackers. This needs to be done after we have
            // updated all objects' state (position), in case we need to keep track of both object's and its parent's
            // position, independently of the said parent's trajectory tracker.
            foreach (var body in _massBodies)
            {
                body.UpdateTrajectory();
            }

            timeDelta -= timeStep;
        }
    }

    private void RunSimulationStep(double timeDelta)
    {
        // Compute pair-wise gravitational interactions between mass bodies in the system, and compute total gravitational
        // force on each of them.
        var massBodiesCount = _massBodies.Count;

        for (var i = 0; i < _massBodies.Count; i++)
            _massBodies[i].TotalGravitationalForce = Vector3d.Zero;

        for (var i = 0; i < massBodiesCount; i++)
        {
            var body1 = _massBodies[i];
            var position1 = body1.Position;
            var gravitationalConstantByMass1 = Constants.GravitationalConstant * body1.Mass;

            for (var j = i + 1; j < massBodiesCount; j++)
            {
                var body2 = _massBodies[j];

                var vectorFromFirstToSecond = body2.Position - position1;

                // Normalize (prevent dividing by 0 in case pos1 == pos2)
                var distanceSquared = vectorFromFirstToSecond.LengthSquared();
                var direction = distanceSquared > 0 ? vectorFromFirstToSecond / Math.Sqrt(distanceSquared) : Vector3d.Zero;

                // force = gravitational constant * mass1 * mass2 / distance^2
                var force = gravitationalConstantByMass1 * body2.Mass / distanceSquared;
                Debug.Assert(double.IsFinite(force), $"Gravitational force between {body1.Name} and {body2.Name} is non-finite!");

                var forceVector = direction * force;

                body1.TotalGravitationalForce += forceVector;
                body2.TotalGravitationalForce -= forceVector;
            }
        }

        // Update each object's state - compute acceleration due to gravity (and other forces), update velocities,
        // update positions.
        foreach (var body in _massBodies)
        {
            // Update object's state; might be necessary to obtain the additional (non-gravity) force.
            body.UpdateState(timeDelta);

            // total force = mass * acceleration
            var totalForce = body.TotalGravitationalForce + body.GetAdditionalForce();
            body.Acceleration = totalForce / body.Mass;

            Debug.Assert(Vector3d.IsFinite(body.Acceleration), $"Acceleration of {body.Name} is non-finite!");

            // Update velocity and position
            body.Velocity += body.Acceleration * timeDelta;
            Debug.Assert(Vector3d.IsFinite(body.Velocity), $"Velocity of {body.Name} is non-finite!");

            body.Position += body.Velocity * timeDelta;
            Debug.Assert(Vector3d.IsFinite(body.Position), $"Position of {body.Name} is non-finite!");
        }

        // Update current simulation time
        SimulationTime += timeDelta;
    }
}
