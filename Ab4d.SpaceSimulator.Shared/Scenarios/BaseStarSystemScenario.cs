using System;
using System.Collections.Generic;
using System.Numerics;
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
    protected class Entity
    {
        // ** Basic info **
        public required string Name;
        public required CelestialBodyType Type;

        // ** Basic dimension properties required for the mass/gravity interaction model **
        public required double Mass; // kg
        public required double Diameter; // meters
        public required double DistanceFromParent; // meters

        // Optional initial position and velocity vector (in ecliptic coordinates). If either is missing, the entity
        // is placed into periapsis of its orbit, and initial velocity is automatically estimated.
        public Vector3d? InitialPosition = null;
        public Vector3d? InitialVelocity = null;

        // Eccentricity
        public double OrbitalEccentricity = 0;

        // Inclination of orbit with respect to ecliptic plane; in the case of Solar system, the ecliptic plane is
        // Earth's rotation plane (i.e., Earth's orbital inclination is 0).
        public double OrbitalInclination = 0; // degrees

        // Longitude of ascending node and argument of periapsis
        public double LongitudeOfAscendingNode = 0; // degrees
        public double ArgumentOfPeriapsis = 0; // degrees
        // NOTE: in-lieu of "arugment of periapsis", "longitude of periapsis" might be specified, which is a sum of
        // "longitude of ascending node" and "argument of periapsis".

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
            var initialPosition = Vector3d.Zero;
            var initialVelocity = Vector3d.Zero;

            if (hostStarObject != null)
            {
                if (entity is { InitialPosition: not null, InitialVelocity: not null })
                {
                    initialPosition = entity.InitialPosition.Value;
                    initialVelocity = entity.InitialVelocity.Value;
                }
                else
                {
                    (initialPosition, initialVelocity) = PlaceIntoOrbitAtPeriapsis(entity, hostStarObject);
                }
            }

            // Mass body for the physics engine
            var celestialBody = new CelestialBody()
            {
                Name = entity.Name,
                Type = entity.Type,
                Position = initialPosition, // meters
                Mass = entity.Mass, // kg
                Radius = entity.Diameter / 2.0, // meters
                HasOrbit = true,
                OrbitRadius = entity.DistanceFromParent, // meters
                OrbitalEccentricity = entity.OrbitalEccentricity,
                OrbitalInclination = entity.OrbitalInclination, // deg
                LongitudeOfAscendingNode = entity.LongitudeOfAscendingNode, // deg
                ArgumentOfPeriapsis = entity.ArgumentOfPeriapsis, // deg
                Velocity = initialVelocity, // m/s
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
                Vector3d moonInitialPosition;
                Vector3d moonInitialVelocity;

                if (moonEntity is { InitialPosition: not null, InitialVelocity: not null })
                {
                    moonInitialPosition = moonEntity.InitialPosition.Value;
                    moonInitialVelocity = moonEntity.InitialVelocity.Value;
                }
                else
                {
                    (moonInitialPosition, moonInitialVelocity) = PlaceIntoOrbitAtPeriapsis(moonEntity, celestialBody);
                }

                var moonMassBody = new CelestialBody()
                {
                    Name = moonEntity.Name,
                    Type = moonEntity.Type,
                    Position = moonInitialPosition, // meters
                    Mass = moonEntity.Mass, // kg
                    Radius = moonEntity.Diameter / 2.0, // meters
                    HasOrbit = true,
                    OrbitRadius = moonEntity.DistanceFromParent, // meters
                    OrbitalEccentricity = moonEntity.OrbitalEccentricity,
                    OrbitalInclination = moonEntity.OrbitalInclination, // deg
                    LongitudeOfAscendingNode = moonEntity.LongitudeOfAscendingNode, // deg
                    ArgumentOfPeriapsis = moonEntity.ArgumentOfPeriapsis, // deg
                    Velocity = moonInitialVelocity, // m/s
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

    private static Tuple<Vector3d, Vector3d> PlaceIntoOrbitAtPeriapsis(Entity entity, CelestialBody parent)
    {
        // Orientation of the ellipse is determined from Kepler elements - longitude of ascending node (Omega),
        // inclination (i), and argument of periapsis (omega), which can be interpreted as 3-1-3 Euler angles,
        // and used to rotate the base axis vectors.
        var dcm =
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, (float)entity.ArgumentOfPeriapsis * MathF.PI / 180) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, (float)entity.OrbitalInclination * MathF.PI / 180) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, (float)entity.LongitudeOfAscendingNode * MathF.PI / 180);

        // Place the entity at the periapsis. The nice thing about periapsis (and apoapsis) is that the direction of
        // velocity is the same as that of semi-minor axis.
        var majorSemiAxisDir = Vector3.Transform(Vector3.UnitX, dcm);
        var minorSemiAxisDir = Vector3.Transform(Vector3.UnitY, dcm);

        var distPeriapsis = (1 - entity.OrbitalEccentricity) * entity.DistanceFromParent;
        var orbitalVelocity = Math.Sqrt(Constants.GravitationalConstant  * parent.Mass * (2.0 / distPeriapsis - 1 / entity.DistanceFromParent));

        var initialPosition = parent.Position + new Vector3d(majorSemiAxisDir.X, majorSemiAxisDir.Y, majorSemiAxisDir.Z) * distPeriapsis;
        var initialVelocity = new Vector3d(minorSemiAxisDir.X, minorSemiAxisDir.Y, minorSemiAxisDir.Z) * orbitalVelocity;

        return new Tuple<Vector3d, Vector3d>(initialPosition, initialVelocity + parent.Velocity);
    }
}
