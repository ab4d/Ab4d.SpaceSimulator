using Ab4d.SharpEngine.Common;
using Ab4d.SpaceSimulator.Physics;

namespace Ab4d.SpaceSimulator.Scenarios;

/// <summary>
/// TRAPPIST-1 star system.
///
/// Sources:
///  - https://en.wikipedia.org/wiki/TRAPPIST-1
///  - https://www.spitzer.caltech.edu/image/ssc2017-01f-trappist-1-statistics-table
///
/// For textures, see:
///  - https://celestiaproject.space/forum/viewtopic.php?f=5&t=20423
///  - https://caltech.app.box.com/s/a9kd2sxhirx59vds2dlx95f5rlmnhsec/folder/46298041428
/// </summary>
public class Trappist1System : BaseStarSystemScenario
{
    public Trappist1System()
        : base([
            Trappist1,
            Trappist1B,
            Trappist1C,
            Trappist1D,
            Trappist1E,
            Trappist1F,
            Trappist1G,
            Trappist1H,
        ])
    {
    }

    #region Celestial bodies

    // TRAPPIST-1
    // https://en.wikipedia.org/wiki/TRAPPIST-1
    private static readonly Entity Trappist1 = new()
    {
        Name = "TRAPPIST-1",
        Type = CelestialBodyType.Star,

        Mass = 0.0898 * Constants.MassOfSun,
        Diameter = 0.1192 * Constants.DiameterOfSun,
        DistanceFromParent = 0,
        OrbitalVelocity = 0,

        AxialTilt = 0,

        RotationPeriod = 3.295,

        BaseColor = Colors.OrangeRed,
    };

    // TRAPPIST-1b
    // https://en.wikipedia.org/wiki/TRAPPIST-1b
    // https://science.nasa.gov/exoplanet-catalog/trappist-1-b/
    private static readonly Entity Trappist1B = new()
    {
        Name = "TRAPPIST-1b",
        Type = CelestialBodyType.Planet,

        Mass = 1.374 * Constants.MassOfEarth,
        Diameter = 1.116 * Constants.DiameterOfEarth,
        DistanceFromParent = 0.01154 * Constants.AstronomicalUnit,
        OrbitalVelocity = 0, // Estimate

        TextureName = "T1b-color+clouds-1k.png",
        BaseColor = Colors.LightSkyBlue,
    };

    // TRAPPIST-1c
    // https://en.wikipedia.org/wiki/TRAPPIST-1c
    // https://science.nasa.gov/exoplanet-catalog/trappist-1-c/
    private static readonly Entity Trappist1C = new()
    {
        Name = "TRAPPIST-1c",
        Type = CelestialBodyType.Planet,

        Mass = 1.308 * Constants.MassOfEarth,
        Diameter = 1.097 * Constants.DiameterOfEarth,
        DistanceFromParent = 0.01580 * Constants.AstronomicalUnit,
        OrbitalVelocity = 0, // Estimate

        TextureName = "T1c-color+clouds-1k.png",
        BaseColor = Colors.Beige,
    };

    // TRAPPIST-1d
    // https://en.wikipedia.org/wiki/TRAPPIST-1d
    // https://science.nasa.gov/exoplanet-catalog/trappist-1-d/
    private static readonly Entity Trappist1D = new()
    {
        Name = "TRAPPIST-1d",
        Type = CelestialBodyType.Planet,

        Mass = 0.388 * Constants.MassOfEarth,
        Diameter = 0.788 * Constants.DiameterOfEarth,
        DistanceFromParent = 0.02227 * Constants.AstronomicalUnit,
        OrbitalVelocity = 0, // Estimate

        TextureName = "T1d-color+clouds-1k.png",
        BaseColor = Colors.DarkBlue,
    };

    // TRAPPIST-1e
    // https://en.wikipedia.org/wiki/TRAPPIST-1e
    // https://science.nasa.gov/exoplanet-catalog/trappist-1-e/
    private static readonly Entity Trappist1E = new()
    {
        Name = "TRAPPIST-1e",
        Type = CelestialBodyType.Planet,

        Mass = 0.692 * Constants.MassOfEarth,
        Diameter = 0.920 * Constants.DiameterOfEarth,
        DistanceFromParent = 0.02925 * Constants.AstronomicalUnit,
        OrbitalVelocity = 0, // Estimate

        TextureName = "T1e-color+clouds-1k.png",
        BaseColor = Colors.DimGray,
    };

    // TRAPPIST-1f
    // https://en.wikipedia.org/wiki/TRAPPIST-1f
    // https://science.nasa.gov/exoplanet-catalog/trappist-1-f/
    private static readonly Entity Trappist1F = new()
    {
        Name = "TRAPPIST-1f",
        Type = CelestialBodyType.Planet,

        Mass = 1.039 * Constants.MassOfEarth,
        Diameter = 1.045 * Constants.DiameterOfEarth,
        DistanceFromParent = 0.03849 * Constants.AstronomicalUnit,
        OrbitalVelocity = 0, // Estimate

        TextureName = "T1f-color+clouds-1k.png",
        BaseColor = Colors.WhiteSmoke,
    };

    // TRAPPIST-1g
    // https://en.wikipedia.org/wiki/TRAPPIST-1g
    // https://science.nasa.gov/exoplanet-catalog/trappist-1-g/
    private static readonly Entity Trappist1G = new()
    {
        Name = "TRAPPIST-1g",
        Type = CelestialBodyType.Planet,

        Mass = 1.321 * Constants.MassOfEarth,
        Diameter = 1.129 * Constants.DiameterOfEarth,
        DistanceFromParent = 0.04683 * Constants.AstronomicalUnit,
        OrbitalVelocity = 0, // Estimate

        TextureName = "T1g-color+clouds-1k.png",
        BaseColor = Colors.DimGray,
    };

    // TRAPPIST-1h
    // https://en.wikipedia.org/wiki/TRAPPIST-1h
    // https://science.nasa.gov/exoplanet-catalog/trappist-1-h/
    private static readonly Entity Trappist1H = new()
    {
        Name = "TRAPPIST-1h",
        Type = CelestialBodyType.Planet,

        Mass = 0.326 * Constants.MassOfEarth,
        Diameter = 0.755 * Constants.DiameterOfEarth,
        DistanceFromParent = 0.06189 * Constants.AstronomicalUnit,
        OrbitalVelocity = 0, // Estimate

        TextureName = "T1h-color-1k.png",
        BaseColor = Colors.SandyBrown,
    };

    #endregion
}
