using System;

namespace Ab4d.SpaceSimulator.PhysicsEngine;

public class PhysicsEngine
{
    // Simulation time, in seconds since the start of simulation
    public double SimulationTime = 0;

    public void Simulate(double timeDelta)
    {
        // TODO: allow discrete time step to be specified from the outside; this would allow application to operate
        // either with fixed discrete time step that is independent from simulation speed, or with fixed number of
        // discrete time steps per simulation cycle
        const double discreteTimeStep = 1; // 1 second

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
        // Compute pair-wise gravitational interactions and update total gravitation force

        // Update each object's state - compute acceleration due to gravity (and other forces), update velocities,
        // update positions.

        // Update current simulation time
        SimulationTime += timeDelta;
    }
}
