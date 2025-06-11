using System;
using Ab4d.SpaceSimulator.Physics;

namespace Ab4d.SpaceSimulator.Scenarios;

// Based on:
// [1] E. Myles Standish and James G. Williams, Orbital Ephemerides of the Sun, Moon, and Planets, January 2006,
//     https://www.researchgate.net/publication/232203657
// [2] planet-positions: https://github.com/mgvez/planet-positions

// Additional resources:
// - https://www.stjarnhimlen.se/comp/tutorial.html
// - https://www2.arnes.si/~gljsentvid10/tutorial_.html

public class SolarSystemAlmanac
{
    public double DayNumber;

    public readonly CelestialBody Mercury;
    public readonly CelestialBody Venus;
    public readonly CelestialBody Earth;
    public readonly CelestialBody Mars;
    public readonly CelestialBody Jupiter;
    public readonly CelestialBody Saturn;
    public readonly CelestialBody Uranus;
    public readonly CelestialBody Neptune;
    public readonly CelestialBody Pluto;

    private readonly CelestialBody[] _planets;

    public SolarSystemAlmanac()
    {
        // Values taken from Table 8.10.2 in [1]. The same values are used by jsOrrery, which allows us to directly
        // validate our implementation.
        Mercury = new CelestialBody()
        {
            Name = "Mercury",
            OrbitalParametersBase = new CelestialBody.OrbitalParametersStruct
            {
                a = 0.38709927,
                e = 0.20563593,
                i = 7.00497902,
                l = 252.25032350,
                lp = 77.45779628,
                o = 48.33076593,
            },
            OrbitalParametersDelta = new CelestialBody.OrbitalParametersStruct
            {
                a = 0.00000037,
                e = 0.00001906,
                i = -0.00594749,
                l = 149472.67411175,
                lp = 0.16047689,
                o = -0.12534081,
            }
        };

        Venus = new CelestialBody()
        {
            Name = "Venus",
            OrbitalParametersBase = new CelestialBody.OrbitalParametersStruct
            {
                a = 0.72333566,
                e = 0.00677672,
                i = 3.39467605,
                l = 181.97909950,
                lp = 131.60246718,
                o = 76.67984255,
            },
            OrbitalParametersDelta = new CelestialBody.OrbitalParametersStruct
            {
                a = 0.00000390,
                e = -0.00004107,
                i = -0.00078890,
                l = 58517.81538729,
                lp = 0.00268329,
                o = -0.27769418,
            }
        };

        Earth = new CelestialBody()
        {
            Name = "Earth",
            OrbitalParametersBase = new CelestialBody.OrbitalParametersStruct
            {
                a = 1.00000261,
                e = 0.01671123,
                i = -0.00001531,
                l = 100.46457166,
                lp = 102.93768193,
                o = 0.0,
            },
            OrbitalParametersDelta = new CelestialBody.OrbitalParametersStruct
            {
                a = 0.00000562,
                e = -0.00004392,
                i = -0.01294668,
                l = 35999.37244981,
                lp = 0.32327364,
                o = 0.0,
            }
        };

        Mars = new CelestialBody()
        {
            Name = "Mars",
            OrbitalParametersBase = new CelestialBody.OrbitalParametersStruct
            {
                a = 1.52371034,
                e = 0.09339410,
                i = 1.84969142,
                l = -4.55343205,
                lp = -23.94362959,
                o = 49.55953891,
            },
            OrbitalParametersDelta = new CelestialBody.OrbitalParametersStruct
            {
                a = 0.00001847,
                e = 0.00007882,
                i = -0.00813131,
                l = 19140.30268499,
                lp = 0.44441088,
                o = -0.29257343,
            }
        };

        Jupiter = new CelestialBody()
        {
            Name = "Jupiter",
            OrbitalParametersBase = new CelestialBody.OrbitalParametersStruct
            {
                a = 5.20288700,
                e = 0.04838624,
                i = 1.30439695,
                l = 34.39644051,
                lp = 14.72847983,
                o = 100.47390909,
            },
            OrbitalParametersDelta = new CelestialBody.OrbitalParametersStruct
            {
                a = -0.00011607,
                e = -0.00013253,
                i = -0.00183714,
                l = 3034.74612775,
                lp = 0.21252668,
                o = 0.20469106,
            }
        };

        Saturn = new CelestialBody()
        {
            Name = "Saturn",
            OrbitalParametersBase = new CelestialBody.OrbitalParametersStruct
            {
                a = 9.53667594,
                e = 0.05386179,
                i = 2.48599187,
                l = 49.95424423,
                lp = 92.59887831,
                o = 113.66242448,
            },
            OrbitalParametersDelta = new CelestialBody.OrbitalParametersStruct
            {
                a = -0.00125060,
                e = -0.00050991,
                i = 0.00193609,
                l = 1222.49362201,
                lp = -0.41897216,
                o = -0.28867794,
            }
        };

        Uranus = new CelestialBody()
        {
            Name = "Uranus",
            OrbitalParametersBase = new CelestialBody.OrbitalParametersStruct
            {
                a = 19.18916464,
                e = 0.04725744,
                i = 0.77263783,
                l = 313.23810451,
                lp = 170.95427630,
                o = 74.01692503,
            },
            OrbitalParametersDelta = new CelestialBody.OrbitalParametersStruct
            {
                a = -0.00196176,
                e = -0.00004397,
                i = -0.00242939,
                l = 428.48202785,
                lp = 0.40805281,
                o = 0.04240589,
            }
        };

        Neptune = new CelestialBody()
        {
            Name = "Neptune",
            OrbitalParametersBase = new CelestialBody.OrbitalParametersStruct
            {
                a = 30.06992276,
                e = 0.00859048,
                i = 1.77004347,
                l = -55.12002969,
                lp = 44.96476227,
                o = 131.78422574,
            },
            OrbitalParametersDelta = new CelestialBody.OrbitalParametersStruct
            {
                a = 0.00026291,
                e = 0.00005105,
                i = 0.00035372,
                l = 218.45945325,
                lp = -0.32241464,
                o = -0.00508664,
            }
        };

        Pluto = new CelestialBody()
        {
            Name = "Pluto",
            OrbitalParametersBase = new CelestialBody.OrbitalParametersStruct
            {
                a = 39.48211675,
                e = 0.24882730,
                i = 17.14001206,
                l = 238.92903833,
                lp = 224.06891629,
                o = 110.30393684,
            },
            OrbitalParametersDelta = new CelestialBody.OrbitalParametersStruct
            {
                a = -0.00031596,
                e = 0.00005170,
                i = 0.00004818,
                l = 145.20780515,
                lp = -0.04062942,
                o = -0.01183482,
            }
        };

        _planets =
        [
            Mercury,
            Venus,
            Earth,
            Mars,
            Jupiter,
            Saturn,
            Uranus,
            Neptune,
            Pluto
        ];
    }

    public void Update(DateTime dateTime)
    {
        Update(DateTimeToDayNumber(dateTime));
    }

    public void Update(double dayNumber)
    {
        DayNumber = dayNumber;

        foreach (var planet in _planets)
        {
            planet.Update(dayNumber);
        }
    }

    // Used by [2]; keep in sync for easier debugging
    private static double DateTimeToDayNumber(DateTime dateTime)
    {
        dateTime = dateTime.ToUniversalTime(); // Ensure input timestamp is in UTC
        var epoch = new DateTime(year: 2000, month: 1, day: 1, hour: 12, minute: 0, second: 0, kind: DateTimeKind.Utc); // J2000 epoch
        return (dateTime - epoch).TotalDays;
    }

    public class CelestialBody()
    {
        public required string Name;

        // Orbital parameters and their rates of change; see Table 8.10.2.
        public struct OrbitalParametersStruct
        {
            public required double a; // major semi-axis, [au]
            public required double e; // eccentricity [rad]
            public required double i; // inclination [deg]
            public required double l; // mean longitude [deg]
            public required double lp; // longitude of perihelion [deg]
            public required double o; // longitude of the ascending node [deg]
        }

        public required OrbitalParametersStruct OrbitalParametersBase; // Base values
        public required OrbitalParametersStruct OrbitalParametersDelta; // Rates of change per century

        // Current values of orbital parameters
        public double SemiMajorAxis = 0;
        public double Eccentricity = 0;
        public double Inclination = 0;
        public double MeanLongitude = 0;
        public double LongitudeOfPerihelion = 0;
        public double LongitudeOfAscendingNode = 0;

        public double ArgumentOfPerihelion = 0;
        public double MeanAnomaly = 0;

        public double EccentricAnomaly = 0;

        public Vector3d Position = Vector3d.Zero;

        public void Update(double d)
        {
            var t = d / 36525.0; // Number of days -> number of centuries

            // Compute elements - see Section 8.10.1
            SemiMajorAxis = OrbitalParametersBase.a + OrbitalParametersDelta.a * t;
            Eccentricity = OrbitalParametersBase.e + OrbitalParametersDelta.e * t;
            Inclination = OrbitalParametersBase.i + OrbitalParametersDelta.i * t;
            MeanLongitude = OrbitalParametersBase.l + OrbitalParametersDelta.l * t;
            LongitudeOfPerihelion = OrbitalParametersBase.lp + OrbitalParametersDelta.lp * t;
            LongitudeOfAscendingNode = OrbitalParametersBase.o + OrbitalParametersDelta.o * t;

            SemiMajorAxis *= Constants.AstronomicalUnit;
            //SemiMajorAxis *= 149_597_870_000; // Used by [2]

            // Compute argument of perihelion, omega
            ArgumentOfPerihelion = LongitudeOfPerihelion - LongitudeOfAscendingNode;

            // Compute mean anomaly, M
            MeanAnomaly = MeanLongitude - LongitudeOfPerihelion;

            // Convert degree-based angular elements to radians to simplify subsequent computations.
            Inclination *= Math.PI / 180.0;
            MeanLongitude *= Math.PI / 180.0;
            LongitudeOfPerihelion *= Math.PI / 180.0;
            LongitudeOfAscendingNode *= Math.PI / 180.0;
            ArgumentOfPerihelion *= Math.PI / 180.0;
            MeanAnomaly *= Math.PI / 180.0;

            // Compute eccentric anomaly, E, by solving Kepler's equation: M = E - e * sin(E)
            EccentricAnomaly = EstimateEccentricAnomaly(MeanAnomaly, Eccentricity);

            // Modulo the angular elements
            EccentricAnomaly %= 2 * Math.PI;
            Inclination %= 2 * Math.PI;
            MeanLongitude %= 2 * Math.PI;
            LongitudeOfPerihelion %= 2 * Math.PI;
            LongitudeOfAscendingNode %= 2 * Math.PI;
            ArgumentOfPerihelion %= 2 * Math.PI;
            MeanAnomaly %= 2 * Math.PI;

            // Compute the (planet's) heliocentric coordinates in its orbital plane, r0, with the x0 -axis aligned from
            // the focus to the perihelion:
            // x' = a * (cos(E) - e)
            // y' = a * sqrt(1 - e^2) * sin(E)
            // z' = 0
            var x = SemiMajorAxis * (Math.Cos(EccentricAnomaly) - Eccentricity);
            var y = SemiMajorAxis * Math.Sqrt(1 - Eccentricity * Eccentricity) * Math.Sin(EccentricAnomaly);

            // Distance and true anomaly
            var distance = Math.Sqrt(x * x + y * y);
            var trueAnomaly = Math.Atan2(y, x);

            // Ecliptic coordinates
            var xh = distance * (Math.Cos(LongitudeOfAscendingNode) * Math.Cos(trueAnomaly + ArgumentOfPerihelion) - Math.Sin(LongitudeOfAscendingNode) * Math.Sin(trueAnomaly + ArgumentOfPerihelion) * Math.Cos(Inclination));
            var yh = distance * (Math.Sin(LongitudeOfAscendingNode) * Math.Cos(trueAnomaly + ArgumentOfPerihelion) + Math.Cos(LongitudeOfAscendingNode) * Math.Sin(trueAnomaly + ArgumentOfPerihelion) * Math.Cos(Inclination));
            var zh = distance * (Math.Sin(trueAnomaly + ArgumentOfPerihelion) * Math.Sin(Inclination));

            Position = new Vector3d(xh, yh, zh);
        }

        private static double EstimateEccentricAnomaly(double M, double e)
        {
            // NOTE: in contrast to Section 8.10.2 in [1], both mean anomaly (M) and eccentricity (e) are in radians,
            // and the estimated eccentric anomaly is in radians as well.

            const double tol = 1e-6 * Math.PI / 180; // 10^-6 of a degree
            var diff = double.PositiveInfinity;

            var E = M; // Initial guess.
            var En = E;
            while (diff > tol)
            {
                var sinE = Math.Sin(E);
                var cosE = Math.Cos(E);
                En = (M - e * (E * cosE - sinE)) / (1 - e * cosE);
                diff = Math.Abs(En - E);
                E = En;
            }
            return En;
        }
    }
}
