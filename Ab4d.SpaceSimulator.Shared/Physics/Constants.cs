namespace Ab4d.SpaceSimulator.Physics;

public static class Constants
{
    // Astronomical unit (AU);
    public const double AstronomicalUnit = 149_597_870_700; // [m]

    // Gravitational constant - recommended value from year 2022.
    // https://en.wikipedia.org/wiki/Gravitational_constant
    public const double GravitationalConstant = 6.6743015E-11; // [m^3 kg^-1 s^-2]

    // Number of seconds in a day
    public const int SecondsInDay = 24 * 60 * 60;

    // Sometimes, mass and size of other celestial bodies are expressed in terms of corresponding dimension of Earth
    // or Sun.
    public const double MassOfSun = 1_988_550e24; // [kg]
    public const double DiameterOfSun = 1_392_700_000; // [m]

    public const double MassOfEarth = 5.97e24; // [kg]
    public const double DiameterOfEarth = 12_756_000; // [m]
}
