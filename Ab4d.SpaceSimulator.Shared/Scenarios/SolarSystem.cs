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

    #region Celestial bodies

    // Sun / Sol
    private static readonly Entity Sun = new()
    {
        Name = "Sun",
        Type = CelestialBodyType.Star,

        Mass = 1_988_550 * 1e24,
        Diameter = 1_392_700 * 1e3,
        DistanceFromParent = 0,
        OrbitalVelocity = 0,

        AxialTilt = 7.25, // https://en.wikipedia.org/wiki/Axial_tilt

        RotationPeriod = 27 * 24, // 24.47 days at Equator, 30 days at poles - use 27 days.

        TextureName = "sunmap.png",
        BaseColor = Colors.Yellow,
    };

    // Mercury
    private static readonly Entity Mercury = new()
    {
        Name = "Mercury",
        Type = CelestialBodyType.Planet,

        Mass = 0.330 * 1e24,
        Diameter = 4_879 * 1e3,
        DistanceFromParent = 57.9 * 1e9,
        OrbitalVelocity = 47.4 * 1e3,

        OrbitalEccentricity = 0.206,
        OrbitalInclination = 7.0,

        AxialTilt = 0.034,

        RotationPeriod = 1407.6, // hours

        TextureName = "mercurymap.png",
        BaseColor = new Color3(0.55f, 0.55f, 0.55f), // grey
    };

    // Venus
    private static readonly Entity Venus = new()
    {
        Name = "Venus",
        Type = CelestialBodyType.Planet,

        Mass = 4.87 * 1e24,
        Diameter = 12_104 * 1e3,
        DistanceFromParent = 108.2 * 1e9,
        OrbitalVelocity = 35.0 * 1e3,

        OrbitalEccentricity = 0.007,
        OrbitalInclination = 3.4,

        AxialTilt = 177.4,

        RotationPeriod = -5832.5, // hours; retrograde rotation

        TextureName = "venusmap.png",
        BaseColor = new Color3(0.85f, 0.74f, 0.55f), // pale yellow

    };

    // Earth and its Moon
    private static readonly Entity Earth = new()
    {
        Name = "Earth",
        Type = CelestialBodyType.Planet,

        Mass = 5.97 * 1e24,
        Diameter = 12_756 * 1e3,
        DistanceFromParent = 149.6 * 1e9,
        OrbitalVelocity = 29.8 * 1e3,

        AxialTilt = 23.4,

        OrbitalEccentricity = 0.017,
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

                OrbitalEccentricity = 0.055,
                OrbitalInclination = 5.1,

                RotationPeriod = 655.7, // hours

                TextureName = "moonmap1k.png",
                BaseColor = Colors.Gray,
            },
        ],
    };

    // Mars
    private static readonly Entity Mars = new()
    {
        Name = "Mars",
        Type = CelestialBodyType.Planet,

        Mass = 0.642 * 1e24,
        Diameter = 6_792 * 1e3,
        DistanceFromParent = 228.0 * 1e9,
        OrbitalVelocity = 24.1 * 1e3,

        OrbitalEccentricity = 0.094,
        OrbitalInclination = 1.8,

        AxialTilt = 25.2,

        RotationPeriod = 24.6, // hours

        TextureName = "mars_1k_color.png",
        BaseColor = new Color3(0.69f, 0.19f, 0.13f), // reddish-brown
    };

    // Jupiter
    private static readonly Entity Jupiter = new()
    {
        Name = "Jupiter",
        Type = CelestialBodyType.Planet,

        Mass = 1_898 * 1e24,
        Diameter = 142_984 * 1e3,
        DistanceFromParent = 778.5 * 1e9,
        OrbitalVelocity = 13.1 * 1e3,

        OrbitalEccentricity = 0.049,
        OrbitalInclination = 1.3,

        AxialTilt = 3.1,

        RotationPeriod = 9.9, // hours

        TextureName = "jupitermap.png",
        BaseColor = new Color3(0.80f, 0.62f, 0.45f),  // brown and white with bands
    };

    // Saturn
    private static readonly Entity Saturn = new()
    {
        Name = "Saturn",
        Type = CelestialBodyType.Planet,

        Mass = 568 * 1e24,
        Diameter = 120_536 * 1e3,
        DistanceFromParent = 1_432.0 * 1e9,
        OrbitalVelocity = 9.7 * 1e3,

        OrbitalEccentricity = 0.052,
        OrbitalInclination = 2.5,

        AxialTilt = 26.7,

        RotationPeriod = 10.7, // hours

        TextureName = "saturnmap.png",
        BaseColor = new Color3(0.85f, 0.77f, 0.63f),  // Pale gold
    };

    // Uranus
    private static readonly Entity Uranus = new()
    {
        Name = "Uranus",
        Type = CelestialBodyType.Planet,

        Mass = 86.8 * 1e24,
        Diameter = 51_118 * 1e3,
        DistanceFromParent = 2_867.0 * 1e9,
        OrbitalVelocity = 6.8 * 1e3,

        OrbitalEccentricity = 0.047,
        OrbitalInclination = 0.8,

        AxialTilt = 97.8,

        RotationPeriod = -17.2, // hours; retrograde rotation

        TextureName = "uranusmap.png",
        BaseColor = new Color3(0.56f, 0.77f, 0.89f), // pale blue
    };

    // Neptune
    private static readonly Entity Neptune = new()
    {
        Name = "Nepute",
        Type = CelestialBodyType.Planet,

        Mass = 102 * 1e24,
        Diameter = 49_528 * 1e3,
        DistanceFromParent = 4_515.0 * 1e9,
        OrbitalVelocity = 5.4 * 1e3,

        OrbitalEccentricity = 0.010,
        OrbitalInclination = 1.8,

        AxialTilt = 28.3,

        RotationPeriod = 16.1, // hours

        TextureName = "neptunemap.png",
        BaseColor = new Color3(0.25f, 0.41f, 0.88f),  // deep blue
    };

    // Pluto
    private static readonly Entity Pluto = new()
    {
        Name = "Pluto",
        Type = CelestialBodyType.Planet,

        Mass = 0.0130 * 1e24,
        Diameter = 2_376 * 1e3,
        DistanceFromParent = 5_906.4 * 1e9,
        OrbitalVelocity = 4.7 * 1e3,

        OrbitalEccentricity = 0.244,
        OrbitalInclination = 17.2,

        AxialTilt = 119.5,

        RotationPeriod = -153.3, // hours; retrograde rotation

        TextureName = "plutomap1k.png",
        BaseColor = new Color3(0.75f, 0.78f, 0.80f), // grey
    };

    #endregion
}
