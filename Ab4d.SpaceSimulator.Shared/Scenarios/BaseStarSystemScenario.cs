using System;
using System.Collections.Generic;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SpaceSimulator.Physics;
using Ab4d.SpaceSimulator.Visualization;

namespace Ab4d.SpaceSimulator.Scenarios;

/// <summary>
/// Helper class for implementing scenarios with a single star and planets orbiting around it.
/// </summary>
public abstract class BaseStarSystemScenario : IScenario
{
    protected struct Entity
    {
        // ** Basic info **
        public required string Name;
        public required CelestialBodyType Type;

        // ** Basic dimension properties required for the mass/gravity interaction model **
        public required double Mass; // kg
        public required double Diameter; // meters
        public required double DistanceFromParent; // meters
        public required double OrbitalVelocity; // m/s

        // Inclination of orbit with respect to Earth's rotation plane (i.e., Earth's orbital inclination is 0).
        public double OrbitalInclination = 0; // degrees

        // The tilt of planet's axis; called "obliquity to orbit" in NASA planetary fact sheet.
        public double AxialTilt = 0; // degrees

        // Rotation
        public double RotationPeriod = 0; // hours

        // ** Visualization info **

        // Texture, if applicable.
        public string? TextureName;

        // Base color name.
        public required Color3 BaseColor;

        // Moons.
        public List<Entity>? Moons;

        public Entity()
        {
        }
    };

    private readonly IBitmapIO _imageReader = new PngBitmapIO();
    private readonly List<Entity> _entities;
    private readonly string _name;

    protected BaseStarSystemScenario(string name, List<Entity> entities)
    {
        _name = name;

        // Validate the entities list; by convention, we require the first entity to be the host star.
        if (entities.Count > 0)
        {
            if (entities[0].Type != CelestialBodyType.Star)
            {
                throw new ArgumentException(
                    "The first entity in the list needs to be the host star (must be of type CelestialBodyType.Star)!");
            }
        }

        _entities = entities;
    }

    public string GetName()
    {
        return _name;
    }

    public virtual void SetupScenario(PhysicsEngine physicsEngine, VisualizationEngine visualizationEngine,
        PlanetTextureLoader planetTextureLoader)
    {
        CelestialBody? hostStarObject = null; // Used to set parent object for planets
        CelestialBodyView? hostStarView = null;

        foreach (var entity in _entities)
        {
            // If orbital velocity is not given for a planet/moon, estimate it.
            var orbitalVelocity = entity.OrbitalVelocity;
            if (orbitalVelocity == 0 && entity.Type != CelestialBodyType.Star && hostStarObject != null)
            {
                orbitalVelocity = ComputeOrbitalVelocity(hostStarObject.Mass, entity.DistanceFromParent);
            }

            // Mass body for the physics engine
            var celestialBody = new CelestialBody()
            {
                Name = entity.Name,
                Type = entity.Type,
                Position = new Vector3d(0, 0, entity.DistanceFromParent), // meters
                Mass = entity.Mass, // kg
                Radius = entity.Diameter / 2.0, // meters
                HasOrbit = true,
                OrbitRadius = entity.DistanceFromParent, // meters
                OrbitalInclination = entity.OrbitalInclination, // deg
                Velocity = TiltOrbitalVelocity(orbitalVelocity, entity.OrbitalInclination), // m/s
                RotationSpeed = (entity.RotationPeriod != 0) ? 360.0 / (entity.RotationPeriod * 3600) : 0, // rotation period (hours) -> angular speed (deg/s)
                AxialTilt = entity.AxialTilt, // degrees
                Parent = hostStarObject,
            };
            celestialBody.Initialize(); // Set up trajectory tracker, etc.
            physicsEngine.AddBody(celestialBody);

            // Visualization
            StandardMaterialBase material;

            if (entity.Type == CelestialBodyType.Star)
                material = new SolidColorMaterial(entity.BaseColor, name: $"{entity.Name}Material");
            else
                material = new StandardMaterial(entity.BaseColor, name: $"{entity.Name}Material");

            if (entity.TextureName != null)
                planetTextureLoader.LoadPlanetTextureAsync(entity.TextureName, material);

            var celestialBodyView = new CelestialBodyView(visualizationEngine, celestialBody, material);

            celestialBodyView.OrbitColor = entity.BaseColor;

            if (entity.Type == CelestialBodyType.Star)
            {
                hostStarObject = celestialBody;
                hostStarView = celestialBodyView;
            }
            else
            {
                celestialBodyView.Parent = hostStarView;
            }

            if (entity.Type == CelestialBodyType.Star)
                visualizationEngine.Lights.Add(new PointLight(celestialBodyView.SphereNode.CenterPosition));

            visualizationEngine.AddCelestialBodyVisualization(celestialBodyView);

            // Create moon(s)
            foreach (var moonEntity in entity.Moons ?? [])
            {
                var moonOrbitalVelocity = moonEntity.OrbitalVelocity;
                if (moonOrbitalVelocity == 0)
                {
                    moonOrbitalVelocity = ComputeOrbitalVelocity(celestialBody.Mass, moonEntity.DistanceFromParent);
                }

                var moonMassBody = new CelestialBody()
                {
                    Name = moonEntity.Name,
                    Type = moonEntity.Type,
                    Position = new Vector3d(0, 0, moonEntity.DistanceFromParent) + celestialBody.Position, // meters
                    Mass = moonEntity.Mass, // kg
                    Radius = moonEntity.Diameter / 2.0, // meters
                    HasOrbit = true,
                    OrbitRadius = moonEntity.DistanceFromParent, // meters
                    OrbitalInclination = moonEntity.OrbitalInclination, // deg
                    Velocity = TiltOrbitalVelocity(moonOrbitalVelocity,  moonEntity.OrbitalInclination) + celestialBody.Velocity, // m/s
                    Parent = celestialBody, // parent mass body
                };
                moonMassBody.Initialize(); // Set up trajectory tracker, etc.
                physicsEngine.AddBody(moonMassBody);

                var moonMaterial = new StandardMaterial(Colors.Gray, name: $"{entity.Name}Material");

                if (moonEntity.TextureName != null)
                    planetTextureLoader.LoadPlanetTextureAsync(moonEntity.TextureName, moonMaterial);

                var moonVisualization = new CelestialBodyView(
                    visualizationEngine,
                    moonMassBody,
                    moonMaterial)
                {
                    Parent = celestialBodyView, // parent visualization
                };

                visualizationEngine.AddCelestialBodyVisualization(moonVisualization);
                celestialBodyView.Children.Add(moonVisualization); // register as child visualization
            }
        }
    }

    public virtual string? GetDefaultView()
    {
        // Return the name of the host star; by convention, we require it to be the first defined entity.
        return _entities.Count > 0 ? _entities[0].Name : null;
    }

    public virtual float? GetDefaultCameraDistance()
    {
        return null; // Default view is given via GetDefaultView().
    }

    public virtual int[]? GetSimulationSpeedIntervals()
    {
        return null; // No custom intervals; might be overridden by child implementation.
    }

    public virtual (double, int)? GetSimulationStepSettings()
    {
        return null; // No custom settings; might be overridden by child implementation.
    }

    private double ComputeOrbitalVelocity(double parentMass, double orbitRadius)
    {
        return Math.Sqrt(parentMass * Constants.GravitationalConstant / orbitRadius);
    }

    private static Vector3d TiltOrbitalVelocity(double orbitalVelocity, double orbitalInclination)
    {
        // In initial state, the celestial body is placed at position (0, 0, R), in coordinate system where X axis
        // points to the right of the monitor, Y axis points upwards, and Z axis points out of the monitor (towards
        // user). The orbital velocity is tangential, so if it were not for orbital inclination, it would point in
        // direction of the X unit vector. To account for inclination, we need to tilt the vector in the X-Y plane.
        var phi = orbitalInclination * Math.PI / 180.0; // deg -> rad
        var directionVector = new Vector3d(Math.Cos(phi), Math.Sin(phi), 0); // becomes (1, 0, 0) when phi=0
        return orbitalVelocity * directionVector;
    }
}
