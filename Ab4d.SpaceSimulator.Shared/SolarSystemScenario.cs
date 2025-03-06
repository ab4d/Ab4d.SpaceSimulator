using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SpaceSimulator.PhysicsEngine;

namespace Ab4d.SpaceSimulator;

public class SolarSystemScenario
{
    enum EntityType
    {
        Star = 1,
        Planet = 2,
        Moon = 3,
    };

    private struct Entity
    {
        // ** Basic info **
        public required string Name;
        public required EntityType Type;

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

        public float MinimumVisualizationSize = 0.001f;

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
        Type = EntityType.Star,

        Mass = 1_988_550 * 1e24,
        Diameter = 1_392_700 * 1e3,
        DistanceFromParent = 0,
        OrbitalVelocity = 0,

        AxialTilt = 7.25, // https://en.wikipedia.org/wiki/Axial_tilt

        RotationPeriod = 27 * 24, // 24.47 days at equator, 30 days at poles - use 27 days

        TextureName = "sunmap.png",
    };

    // Mercury
    private readonly Entity Mercury = new()
    {
        Name = "Mercury",
        Type = EntityType.Planet,

        Mass = 0.330 * 1e24,
        Diameter = 4_879 * 1e3,
        DistanceFromParent = 57.9 * 1e9,
        OrbitalVelocity = 47.4 * 1e3,

        OrbitalInclination = 7.0,

        AxialTilt = 0.034,

        RotationPeriod = 1407.6, // hours

        TextureName = "mercurymap.png",
    };

    // Venus
    private readonly Entity Venus = new()
    {
        Name = "Venus",
        Type = EntityType.Planet,

        Mass = 4.87 * 1e24,
        Diameter = 12_104 * 1e3,
        DistanceFromParent = 108.2 * 1e9,
        OrbitalVelocity = 35.0 * 1e3,

        OrbitalInclination = 3.4,

        AxialTilt = 177.4,

        RotationPeriod = -5832.5, // hours; retrograde rotation

        TextureName = "venusmap.png",
    };

    // Earth and its Moon
    private readonly Entity Earth = new()
    {
        Name = "Earth",
        Type = EntityType.Planet,

        Mass = 5.97 * 1e24,
        Diameter = 12_756 * 1e3,
        DistanceFromParent = 149.6 * 1e9,
        OrbitalVelocity = 29.8 * 1e3,

        AxialTilt = 23.4,

        OrbitalInclination = 0.0, // Earth's revolution plane is the reference!

        RotationPeriod = 23.9, // hours

        TextureName = "earthmap1k.png",

        Moons = [
            new Entity {
                Name = "Moon",
                Type = EntityType.Moon,

                Mass = 0.073 * 1e24,
                Diameter = 3_475 * 1e3,
                DistanceFromParent = 0.384 * 1e9,
                OrbitalVelocity = 1.0 * 1e3,

                AxialTilt = 6.7,

                OrbitalInclination = 5.1,

                RotationPeriod = 655.7, // hours

                TextureName = "moonmap1k.png",

                MinimumVisualizationSize = 0.0001f,
            },
        ],
    };

    // Mars
    private readonly Entity Mars = new()
    {
        Name = "Mars",
        Type = EntityType.Planet,

        Mass = 0.642 * 1e24,
        Diameter = 6_792 * 1e3,
        DistanceFromParent = 228.0 * 1e9,
        OrbitalVelocity = 24.1 * 1e3,

        OrbitalInclination = 1.8,

        AxialTilt = 25.2,

        RotationPeriod = 24.6, // hours

        TextureName = "mars_1k_color.png",
    };

    // Jupiter
    private readonly Entity Jupiter = new()
    {
        Name = "Jupiter",
        Type = EntityType.Planet,

        Mass = 1_898 * 1e24,
        Diameter = 142_984 * 1e3,
        DistanceFromParent = 778.5 * 1e9,
        OrbitalVelocity = 13.1 * 1e3,

        OrbitalInclination = 1.3,

        AxialTilt = 3.1,

        RotationPeriod = 9.9, // hours

        TextureName = "jupitermap.png",
    };

    // Saturn
    private readonly Entity Saturn = new()
    {
        Name = "Saturn",
        Type = EntityType.Planet,

        Mass = 568 * 1e24,
        Diameter = 120_536 * 1e3,
        DistanceFromParent = 1_432.0 * 1e9,
        OrbitalVelocity = 9.7 * 1e3,

        OrbitalInclination = 2.5,

        AxialTilt = 26.7,

        RotationPeriod = 10.7, // hours

        TextureName = "saturnmap.png",
    };

    // Uranus
    private readonly Entity Uranus = new()
    {
        Name = "Uranus",
        Type = EntityType.Planet,

        Mass = 86.8 * 1e24,
        Diameter = 51_118 * 1e3,
        DistanceFromParent = 2_867.0 * 1e9,
        OrbitalVelocity = 6.8 * 1e3,

        OrbitalInclination = 0.8,

        AxialTilt = 97.8,

        RotationPeriod = -17.2, // hours; retrograde rotation

        TextureName = "uranusmap.png",
    };

    // Neptune
    private readonly Entity Neptune = new()
    {
        Name = "Nepute",
        Type = EntityType.Planet,

        Mass = 102 * 1e24,
        Diameter = 49_528 * 1e3,
        DistanceFromParent = 4_515.0 * 1e9,
        OrbitalVelocity = 5.4 * 1e3,

        OrbitalInclination = 1.8,

        AxialTilt = 28.3,

        RotationPeriod = 16.1, // hours

        TextureName = "neptunemap.png",
    };

    // Pluto
    private readonly Entity Pluto = new()
    {
        Name = "Pluto",
        Type = EntityType.Planet,

        Mass = 0.0130 * 1e24,
        Diameter = 2_376 * 1e3,
        DistanceFromParent = 5_906.4 * 1e9,
        OrbitalVelocity = 4.7 * 1e3,

        OrbitalInclination = 17.2,

        AxialTilt = 119.5,

        RotationPeriod = -153.3, // hours; retrograde rotation

        TextureName = "plutomap1k.png",
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

    public void SetupScenario(PhysicsEngine.PhysicsEngine physicsEngine, VisualizationEngine.VisualizationEngine visualizationEngine)
    {
        PhysicsEngine.CelestialBody? sunObject = null; // Used to set parent object for planets

        foreach (var entity in _entities)
        {
            // Mass body for the physics engine
            var massBody = new PhysicsEngine.CelestialBody()
            {
                Name = entity.Name,
                Position = new Vector3d(0, 0, entity.DistanceFromParent), // meters
                Mass = entity.Mass, // kg
                Radius = entity.Diameter / 2.0, // meters
                Velocity = TiltOrbitalVelocity(entity.OrbitalVelocity, entity.OrbitalInclination), // m/s
                RotationSpeed = (entity.RotationPeriod != 0) ? 360.0 / (entity.RotationPeriod * 3600) : 0, // rotation period (hours) -> angular speed (deg/s)
                AxialTilt = entity.AxialTilt, // degrees
                Parent = sunObject,
            };

            physicsEngine.AddBody(massBody);

            if (entity.Name == "Sun")
                sunObject = massBody;

            // Visualization
            Debug.Assert(entity.TextureName != null, $"Texture file not specified for {entity.Name}!");
            var textureFilename = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", entity.TextureName);
            Debug.Assert(System.IO.Path.Exists(textureFilename), $"Texture file {textureFilename} does not exist!");
            var material = new StandardMaterial(textureFilename, _imageReader, name: $"Texture-{entity.Name}");
            var visualization = new VisualizationEngine.CelestialBodyView(massBody, material, entity.MinimumVisualizationSize);

            visualizationEngine.AddCelestialBodyVisualization(visualization);

            // Create moon(s)
            foreach (var moonEntity in entity.Moons ?? [])
            {
                var moonMassBody = new PhysicsEngine.CelestialBody()
                {
                    Name = moonEntity.Name,
                    Position = new Vector3d(0, 0, moonEntity.DistanceFromParent) + massBody.Position, // meters
                    Mass = moonEntity.Mass, // kg
                    Radius = moonEntity.Diameter / 2.0, // meters
                    Velocity = TiltOrbitalVelocity(moonEntity.OrbitalVelocity,  moonEntity.OrbitalInclination) + massBody.Velocity, // m/s
                    Parent = massBody, // parent mass body
                };

                physicsEngine.AddBody(moonMassBody);

                Debug.Assert(moonEntity.TextureName != null, $"Texture file not specified for {moonEntity.Name}!");
                textureFilename = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", moonEntity.TextureName);
                Debug.Assert(System.IO.Path.Exists(textureFilename), $"Texture file {textureFilename} does not exist!");
                var moonMaterial = new StandardMaterial(textureFilename, _imageReader, name: $"Texture-{moonEntity.Name}");
                var moonVisualization = new VisualizationEngine.CelestialBodyView(moonMassBody, moonMaterial, moonEntity.MinimumVisualizationSize);

                visualizationEngine.AddCelestialBodyVisualization(moonVisualization);
            }
        }
    }
}
