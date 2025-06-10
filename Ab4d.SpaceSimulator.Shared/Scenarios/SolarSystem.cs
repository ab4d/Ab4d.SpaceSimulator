using System;
using Ab4d.SharpEngine.Common;
using Ab4d.SpaceSimulator.Physics;

namespace Ab4d.SpaceSimulator.Scenarios;

/// <summary>
/// Our solar system.
///
/// Unless otherwise indicated, data is taken from the NASA planetary fact sheet:
/// https://nssdc.gsfc.nasa.gov/planetary/factsheet/index.html
///
/// For textures, see the following possible sources:
///  - https://nasa3d.arc.nasa.gov/images
///  - https://planetpixelemporium.com
/// </summary>
public class SolarSystem : BaseStarSystemScenario
{
    public SolarSystem()
        : base("Solar system", [
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
        ])
    {
    }

    public override void SetupScenario(PhysicsEngine physicsEngine, Visualization.VisualizationEngine visualizationEngine, Visualization.PlanetTextureLoader planetTextureLoader)
    {
        const bool initializeFromAlmanac = true; // Initialize planet positions from almanac.
        const bool showAlmanacPositions = false; // Show planet positions computed by almanac.

        // Instantiate almanac
        var almanac = new SolarSystemAlmanac();

        var dateTime = new DateTime(year: 1990, month: 4, day: 19, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc); // Start time

        var entries = new[]
        {
            (Mercury, almanac.Mercury),
            (Venus, almanac.Venus),
            (Earth, almanac.Earth),
            //(Moon, almanac.Moon), // TODO
            (Mars, almanac.Mars),
            (Jupiter, almanac.Jupiter),
            (Saturn, almanac.Saturn),
            (Uranus, almanac.Uranus),
            (Neptune, almanac.Neptune),
            (Pluto, almanac.Pluto),
        };

        // Initialize planet positions from almanac
        if (initializeFromAlmanac)
        {
            almanac.Update(dateTime);
            foreach (var (entity, almanacBody) in entries)
            {
                entity.InitialPosition = almanacBody.Position;

                // TODO: we could also update the orbital parameters with the ones from almanac.
            }

            // For now, estimate initial velocities from 1-minute deltas.
            const double dt = 60;
            almanac.Update(dateTime.AddSeconds(dt));
            foreach (var (entity, almanacBody) in entries)
            {
                entity.InitialVelocity = (almanacBody.Position - entity.InitialPosition) / dt;
            }
        }

        // Setup scenario
        base.SetupScenario(physicsEngine, visualizationEngine, planetTextureLoader);

        // Show planet positions computed by almanac.
        if (!showAlmanacPositions)
            return;

        foreach (var (entity, almanacBody) in entries)
        {
            SharpEngine.Materials.StandardMaterialBase material;

            // Material / texture
            if (entity.Type == CelestialBodyType.Star)
                material = new SharpEngine.Materials.SolidColorMaterial(entity.BaseColor, name: $"{entity.Name}Material");
            else
                material = new SharpEngine.Materials.StandardMaterial(entity.BaseColor, name: $"{entity.Name}Material");
            if (entity.TextureName != null)
                planetTextureLoader.LoadPlanetTextureAsync(entity.TextureName, material);
            material.Opacity = 0.5f;

            const double mu = Constants.GravitationalConstant * Constants.MassOfSun;
            var orbitalPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(entity.DistanceFromParent, 3) / mu); // seconds
            var timeIncrement = orbitalPeriod / 360 * 10; // 10 deg
            for (var i = 0; i < (360 / 10); i++)
            {
                almanac.Update(dateTime.AddSeconds(timeIncrement * i));

                var physicalObject = new Physics.CelestialBody()
                {
                    Name = $"{entity.Name}-{i}",
                    Position = almanacBody.Position,
                    Radius = 0.5 * entity.Diameter / 4, // Quarter of original planet size
                };

                var visualization = new Visualization.CelestialBodyView(visualizationEngine, physicalObject, material)
                {
                    ShowName = false
                };
                visualizationEngine.AddCelestialBodyVisualization(visualization);
            }
        }
    }

    #region Celestial bodies

    // Sun / Sol
    private static readonly Entity Sun = new()
    {
        Name = "Sun",
        Type = CelestialBodyType.Star,

        Mass = 1_988_550 * 1e24,
        Diameter = 1_392_700 * 1e3,
        DistanceFromParent = 0,

        AxialTilt = 7.25, // https://en.wikipedia.org/wiki/Axial_tilt

        RotationPeriod = 27 * 24, // 24.47 days at Equator, 30 days at poles - use 27 days.

        TextureName = "sunmap.png",
        BaseColor = Colors.Yellow,
    };

    // Mercury
    private static Entity Mercury = new()
    {
        Name = "Mercury",
        Type = CelestialBodyType.Planet,

        Mass = 0.330 * 1e24,
        Diameter = 4_879 * 1e3,
        DistanceFromParent = 57.9 * 1e9,

        OrbitalEccentricity = 0.206,

        OrbitalInclination = 7.00487,
        LongitudeOfAscendingNode = 48.33167,
        ArgumentOfPeriapsis = 77.45645 - 48.33167,

        AxialTilt = 0.034,

        RotationPeriod = 1407.6, // hours

        TextureName = "mercurymap.png",
        BaseColor = new Color3(0.55f, 0.55f, 0.55f), // grey
    };

    // Venus
    private static Entity Venus = new()
    {
        Name = "Venus",
        Type = CelestialBodyType.Planet,

        Mass = 4.87 * 1e24,
        Diameter = 12_104 * 1e3,
        DistanceFromParent = 108.2 * 1e9,

        OrbitalEccentricity = 0.007,

        OrbitalInclination = 3.39471,
        LongitudeOfAscendingNode = 76.68069,
        ArgumentOfPeriapsis = 131.53298 - 76.68069,

        AxialTilt = 177.4,

        RotationPeriod = -5832.5, // hours; retrograde rotation

        TextureName = "venusmap.png",
        BaseColor = new Color3(0.85f, 0.74f, 0.55f), // pale yellow

    };

    // Earth and its Moon
    private static Entity Earth = new()
    {
        Name = "Earth",
        Type = CelestialBodyType.Planet,

        Mass = 5.97 * 1e24,
        Diameter = 12_756 * 1e3,
        DistanceFromParent = 149.6 * 1e9,

        AxialTilt = 23.4,

        OrbitalEccentricity = 0.017,

        OrbitalInclination = 0.00005, // Earth's revolution plane is the reference!
        LongitudeOfAscendingNode = -11.26064,
        ArgumentOfPeriapsis = 102.94719 - (-11.26064),

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

                AxialTilt = 6.7,

                OrbitalEccentricity = 0.055,
                OrbitalInclination = 5.1,

                RotationPeriod = 655.7, // hours

                TextureName = "moonmap1k.png",
                BaseColor = Colors.Gray,
            },
        ],
    };

    // Mars
    private static Entity Mars = new()
    {
        Name = "Mars",
        Type = CelestialBodyType.Planet,

        Mass = 0.642 * 1e24,
        Diameter = 6_792 * 1e3,
        DistanceFromParent = 228.0 * 1e9,

        OrbitalEccentricity = 0.094,

        OrbitalInclination = 1.85061,
        LongitudeOfAscendingNode = 49.57854,
        ArgumentOfPeriapsis =336.04084 - 49.57854,

        AxialTilt = 25.2,

        RotationPeriod = 24.6, // hours

        TextureName = "mars_1k_color.png",
        BaseColor = new Color3(0.69f, 0.19f, 0.13f), // reddish-brown
    };

    // Jupiter
    private static Entity Jupiter = new()
    {
        Name = "Jupiter",
        Type = CelestialBodyType.Planet,

        Mass = 1_898 * 1e24,
        Diameter = 142_984 * 1e3,
        DistanceFromParent = 778.5 * 1e9,

        OrbitalEccentricity = 0.049,

        OrbitalInclination = 1.30530,
        LongitudeOfAscendingNode = 100.55615,
        ArgumentOfPeriapsis = 14.75385 - 100.55615,

        AxialTilt = 3.1,

        RotationPeriod = 9.9, // hours

        TextureName = "jupitermap.png",
        BaseColor = new Color3(0.80f, 0.62f, 0.45f),  // brown and white with bands
    };

    // Saturn
    private static Entity Saturn = new()
    {
        Name = "Saturn",
        Type = CelestialBodyType.Planet,

        Mass = 568 * 1e24,
        Diameter = 120_536 * 1e3,
        DistanceFromParent = 1_432.0 * 1e9,

        OrbitalEccentricity = 0.052,

        OrbitalInclination = 2.48446,
        LongitudeOfAscendingNode = 113.71504,
        ArgumentOfPeriapsis = 92.43194 - 113.71504,

        AxialTilt = 26.7,

        RotationPeriod = 10.7, // hours

        TextureName = "saturnmap.png",
        BaseColor = new Color3(0.85f, 0.77f, 0.63f),  // Pale gold
    };

    // Uranus
    private static Entity Uranus = new()
    {
        Name = "Uranus",
        Type = CelestialBodyType.Planet,

        Mass = 86.8 * 1e24,
        Diameter = 51_118 * 1e3,
        DistanceFromParent = 2_867.0 * 1e9,

        OrbitalEccentricity = 0.047,

        OrbitalInclination = 0.76986,
        LongitudeOfAscendingNode = 74.22988,
        ArgumentOfPeriapsis = 170.96424 - 74.22988,

        AxialTilt = 97.8,

        RotationPeriod = -17.2, // hours; retrograde rotation

        TextureName = "uranusmap.png",
        BaseColor = new Color3(0.56f, 0.77f, 0.89f), // pale blue
    };

    // Neptune
    private static Entity Neptune = new()
    {
        Name = "Nepute",
        Type = CelestialBodyType.Planet,

        Mass = 102 * 1e24,
        Diameter = 49_528 * 1e3,
        DistanceFromParent = 4_515.0 * 1e9,

        OrbitalEccentricity = 0.010,

        OrbitalInclination = 1.76917,
        LongitudeOfAscendingNode = 131.72169,
        ArgumentOfPeriapsis = 44.97135 - 131.72169,

        AxialTilt = 28.3,

        RotationPeriod = 16.1, // hours

        TextureName = "neptunemap.png",
        BaseColor = new Color3(0.25f, 0.41f, 0.88f),  // deep blue
    };

    // Pluto
    private static Entity Pluto = new()
    {
        Name = "Pluto",
        Type = CelestialBodyType.Planet,

        Mass = 0.0130 * 1e24,
        Diameter = 2_376 * 1e3,
        DistanceFromParent = 5_906.4 * 1e9,

        OrbitalEccentricity = 0.244,

        OrbitalInclination = 17.14175,
        LongitudeOfAscendingNode = 110.30347,
        ArgumentOfPeriapsis = 224.06676 - 110.30347,

        AxialTilt = 119.5,

        RotationPeriod = -153.3, // hours; retrograde rotation

        TextureName = "plutomap1k.png",
        BaseColor = new Color3(0.75f, 0.78f, 0.80f), // grey
    };

    #endregion
}
