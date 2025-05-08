using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SpaceSimulator.Physics;
using Ab4d.SpaceSimulator.Visualization;

namespace Ab4d.SpaceSimulator;

public class SolarSystemScenario
{
    private struct Entity
    {
        // ** Basic info **
        public required string Name;
        public required CelestialBodyType Type;

        // ** Basic dimension properties required for the mass/gravity interaction model **
        // Available from NASA planetary fact sheet:
        // https://nssdc.gsfc.nasa.gov/planetary/factsheet/index.html
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

        // Texture - see the following possible sources:
        //  https://nasa3d.arc.nasa.gov/images
        //  https://planetpixelemporium.com
        public string? TextureName;

        // Base color name
        public required Color3 BaseColor;

        // Moons
        public List<Entity>? Moons;

        public Entity()
        {
        }
    };

    // Sun / Sol
    private readonly Entity Sun = new()
    {
        Name = "Sun",
        Type = CelestialBodyType.Star,

        Mass = 1_988_550 * 1e24,
        Diameter = 1_392_700 * 1e3,
        DistanceFromParent = 0,
        OrbitalVelocity = 0,

        AxialTilt = 7.25, // https://en.wikipedia.org/wiki/Axial_tilt

        RotationPeriod = 27 * 24, // 24.47 days at equator, 30 days at poles - use 27 days

        TextureName = "sunmap.png",
        BaseColor = Colors.Yellow,
    };

    // Mercury
    private readonly Entity Mercury = new()
    {
        Name = "Mercury",
        Type = CelestialBodyType.Planet,

        Mass = 0.330 * 1e24,
        Diameter = 4_879 * 1e3,
        DistanceFromParent = 57.9 * 1e9,
        OrbitalVelocity = 47.4 * 1e3,

        OrbitalInclination = 7.0,

        AxialTilt = 0.034,

        RotationPeriod = 1407.6, // hours

        TextureName = "mercurymap.png",
        BaseColor = new Color3(0.55f, 0.55f, 0.55f), // grey
    };

    // Venus
    private readonly Entity Venus = new()
    {
        Name = "Venus",
        Type = CelestialBodyType.Planet,

        Mass = 4.87 * 1e24,
        Diameter = 12_104 * 1e3,
        DistanceFromParent = 108.2 * 1e9,
        OrbitalVelocity = 35.0 * 1e3,

        OrbitalInclination = 3.4,

        AxialTilt = 177.4,

        RotationPeriod = -5832.5, // hours; retrograde rotation

        TextureName = "venusmap.png",
        BaseColor = new Color3(0.85f, 0.74f, 0.55f), // pale yellow

    };

    // Earth and its Moon
    private readonly Entity Earth = new()
    {
        Name = "Earth",
        Type = CelestialBodyType.Planet,

        Mass = 5.97 * 1e24,
        Diameter = 12_756 * 1e3,
        DistanceFromParent = 149.6 * 1e9,
        OrbitalVelocity = 29.8 * 1e3,

        AxialTilt = 23.4,

        OrbitalInclination = 0.0, // Earth's revolution plane is the reference!

        RotationPeriod = 23.9, // hours

        TextureName = "earthmap1k.png",
        BaseColor = new Color3(0.27f, 0.50f, 0.70f), // blue with patches of green and brown (oceans and landmasses)

        Moons = [
            new Entity {
                Name = "Moon",
                Type = CelestialBodyType.Moon,

                Mass = 0.073 * 1e24,
                Diameter = 3_475 * 1e3,
                DistanceFromParent = 0.384 * 1e9,
                OrbitalVelocity = 1.0 * 1e3,

                AxialTilt = 6.7,

                OrbitalInclination = 5.1,

                RotationPeriod = 655.7, // hours

                TextureName = "moonmap1k.png",
                BaseColor = Colors.Gray,
            },
        ],
    };

    // Mars
    private readonly Entity Mars = new()
    {
        Name = "Mars",
        Type = CelestialBodyType.Planet,

        Mass = 0.642 * 1e24,
        Diameter = 6_792 * 1e3,
        DistanceFromParent = 228.0 * 1e9,
        OrbitalVelocity = 24.1 * 1e3,

        OrbitalInclination = 1.8,

        AxialTilt = 25.2,

        RotationPeriod = 24.6, // hours

        TextureName = "mars_1k_color.png",
        BaseColor = new Color3(0.69f, 0.19f, 0.13f), // reddish-brown
    };

    // Jupiter
    private readonly Entity Jupiter = new()
    {
        Name = "Jupiter",
        Type = CelestialBodyType.Planet,

        Mass = 1_898 * 1e24,
        Diameter = 142_984 * 1e3,
        DistanceFromParent = 778.5 * 1e9,
        OrbitalVelocity = 13.1 * 1e3,

        OrbitalInclination = 1.3,

        AxialTilt = 3.1,

        RotationPeriod = 9.9, // hours

        TextureName = "jupitermap.png",
        BaseColor = new Color3(0.80f, 0.62f, 0.45f),  // brown and white with bands
    };

    // Saturn
    private readonly Entity Saturn = new()
    {
        Name = "Saturn",
        Type = CelestialBodyType.Planet,

        Mass = 568 * 1e24,
        Diameter = 120_536 * 1e3,
        DistanceFromParent = 1_432.0 * 1e9,
        OrbitalVelocity = 9.7 * 1e3,

        OrbitalInclination = 2.5,

        AxialTilt = 26.7,

        RotationPeriod = 10.7, // hours

        TextureName = "saturnmap.png",
        BaseColor = new Color3(0.85f, 0.77f, 0.63f),  // Pale gold
    };

    // Uranus
    private readonly Entity Uranus = new()
    {
        Name = "Uranus",
        Type = CelestialBodyType.Planet,

        Mass = 86.8 * 1e24,
        Diameter = 51_118 * 1e3,
        DistanceFromParent = 2_867.0 * 1e9,
        OrbitalVelocity = 6.8 * 1e3,

        OrbitalInclination = 0.8,

        AxialTilt = 97.8,

        RotationPeriod = -17.2, // hours; retrograde rotation

        TextureName = "uranusmap.png",
        BaseColor = new Color3(0.56f, 0.77f, 0.89f), // pale blue
    };

    // Neptune
    private readonly Entity Neptune = new()
    {
        Name = "Nepute",
        Type = CelestialBodyType.Planet,

        Mass = 102 * 1e24,
        Diameter = 49_528 * 1e3,
        DistanceFromParent = 4_515.0 * 1e9,
        OrbitalVelocity = 5.4 * 1e3,

        OrbitalInclination = 1.8,

        AxialTilt = 28.3,

        RotationPeriod = 16.1, // hours

        TextureName = "neptunemap.png",
        BaseColor = new Color3(0.25f, 0.41f, 0.88f),  // deep blue
    };

    // Pluto
    private readonly Entity Pluto = new()
    {
        Name = "Pluto",
        Type = CelestialBodyType.Planet,

        Mass = 0.0130 * 1e24,
        Diameter = 2_376 * 1e3,
        DistanceFromParent = 5_906.4 * 1e9,
        OrbitalVelocity = 4.7 * 1e3,

        OrbitalInclination = 17.2,

        AxialTilt = 119.5,

        RotationPeriod = -153.3, // hours; retrograde rotation

        TextureName = "plutomap1k.png",
        BaseColor = new Color3(0.75f, 0.78f, 0.80f), // grey
    };

    private readonly IBitmapIO _imageReader = new PngBitmapIO();
    private readonly List<Entity> _entities;

    public SolarSystemScenario()
    {
        _entities =
        [
            Sun,
            Mercury,
            Venus,
            Earth,
            Mars,
            Jupiter,
            Saturn,
            Uranus,
            Neptune,
            Pluto,
        ];
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

    public void SetupScenario(PhysicsEngine physicsEngine, VisualizationEngine visualizationEngine, PlanetTextureLoader planetTextureLoader)
    {
        CelestialBody? sunObject = null; // Used to set parent object for planets
        CelestialBodyView? sunView = null;

        foreach (var entity in _entities)
        {
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
                Velocity = TiltOrbitalVelocity(entity.OrbitalVelocity, entity.OrbitalInclination), // m/s
                RotationSpeed = (entity.RotationPeriod != 0) ? 360.0 / (entity.RotationPeriod * 3600) : 0, // rotation period (hours) -> angular speed (deg/s)
                AxialTilt = entity.AxialTilt, // degrees
                Parent = sunObject,
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

            if (entity.Name == "Sun")
            {
                sunObject = celestialBody;
                sunView = celestialBodyView;
            }
            else
            {
                celestialBodyView.Parent = sunView;
            }

            if (entity.Type == CelestialBodyType.Star)
                visualizationEngine.Lights.Add(new PointLight(celestialBodyView.SphereNode.CenterPosition));

            visualizationEngine.AddCelestialBodyVisualization(celestialBodyView);

            // Create moon(s)
            foreach (var moonEntity in entity.Moons ?? [])
            {
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
                    Velocity = TiltOrbitalVelocity(moonEntity.OrbitalVelocity,  moonEntity.OrbitalInclination) + celestialBody.Velocity, // m/s
                    Parent = celestialBody, // parent mass body
                };
                moonMassBody.Initialize(); // Set up trajectory tracker, etc.
                physicsEngine.AddBody(moonMassBody);


                StandardMaterial moonMaterial = new StandardMaterial(Colors.Gray, name: $"{entity.Name}Material");

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
}
