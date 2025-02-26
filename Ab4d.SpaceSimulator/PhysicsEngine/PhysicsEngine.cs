using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Ab4d.SpaceSimulator.PhysicsEngine;

public class PhysicsEngine
{
    // Simulation time, in seconds since the start of simulation
    public double SimulationTime = 0;

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
        // TODO: allow discrete time step to be specified from the outside; this would allow application to operate
        // either with fixed discrete time step that is independent from simulation speed, or with fixed number of
        // discrete time steps per simulation cycle.
        //const double discreteTimeStep = 1; // 1 second
        const double discreteTimeStep = 3600; // 1 hour

        // Break the given time interval into multiple discrete time steps, if necessary
        while (timeDelta > 0)
        {
            var timeStep = Math.Min(timeDelta, discreteTimeStep);

            RunSimulationStep(timeStep);

            timeDelta -= timeStep;
        }
    }

    private void RunSimulationStep(double timeDelta)
    {
        // Compute pair-wise gravitational interactions between mass bodies in the system, and compute total gravitational
        // force on each of them.
        foreach (var body in _massBodies)
        {
            body.TotalGravitationalForce = Vector3d.Zero;
        }

        for (var i = 0; i < _massBodies.Count; i++)
        {
            var body1 = _massBodies[i];
            for (var j = i + 1; j < _massBodies.Count; j++)
            {
                var body2 = _massBodies[j];

                var vectorFromFirstToSecond = body2.Position - body1.Position;
                var distanceSquared = vectorFromFirstToSecond.LengthSquared();
                var direction = distanceSquared > 0 ? vectorFromFirstToSecond / Math.Sqrt(distanceSquared) : Vector3d.Zero;

                // force = gravitational constant * mass1 * mass2 / distance^2
                var force = Constants.GravitationalConstant * body1.Mass * body2.Mass / distanceSquared;
                Debug.Assert(double.IsFinite(force), $"Gravitational force between {body1.Name} and {body2.Name} is non-finite!");

                body1.TotalGravitationalForce += direction * force;
                body2.TotalGravitationalForce -= direction * force;
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
