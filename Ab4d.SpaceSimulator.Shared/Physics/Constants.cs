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
}
