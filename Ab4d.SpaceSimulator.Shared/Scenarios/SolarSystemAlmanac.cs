using System;

namespace Ab4d.SpaceSimulator.Scenarios;

// https://www.stjarnhimlen.se/comp/tutorial.html
// https://www2.arnes.si/~gljsentvid10/tutorial_.html

public class SolarSystemAlmanac
{
    public double DayNumber;

    public readonly CelestialBody Mercury = new _Mercury();
    public readonly CelestialBody Venus = new _Venus();
    public readonly CelestialBody Mars = new _Mars();
    public readonly CelestialBody Jupiter = new _Jupiter();
    public readonly CelestialBody Saturn = new _Saturn();
    public readonly CelestialBody Uranus = new _Uranus();
    public readonly CelestialBody Neptune = new _Neptune();

    public void Update(DateTime dateTime)
    {
        Update(DateTimeToDayNumber(dateTime));
    }

    public void Update(double dayNumber, bool applyPerturbations = true)
    {
        DayNumber = dayNumber;

        // Update orbital parameters and ecliptic coordinates.
        var planets = new[]
        {
            Mercury,
            Venus,
            Mars,
            Jupiter,
            Saturn,
            Uranus,
            Neptune
        };
        foreach (var planet in planets)
        {
            planet.UpdateOrbitalParameters(dayNumber);
            planet.UpdateEclipticCoordinates();
        }

        // Apply perturbations - these require orbital parameters from other planets, so compute and apply them post-hoc.
        if (applyPerturbations)
        {
            ((_Jupiter)Jupiter).ApplyPerturbations(Saturn.MeanAnomaly);
            ((_Saturn)Saturn).ApplyPerturbations(Jupiter.MeanAnomaly);
            ((_Uranus)Uranus).ApplyPerturbations(Jupiter.MeanAnomaly, Saturn.MeanAnomaly);
        }
    }

    // Helpers
    private static double SinDeg(double x)
    {
        return Math.Sin(x * Math.PI / 180);
    }

    private static double CosDeg(double x)
    {
        return Math.Cos(x * Math.PI / 180);
    }

    private static double Atan2Deg(double y, double x)
    {
        return Math.Atan2(y, x) * 180 / Math.PI;
    }

    // https://stjarnhimlen.se/comp/ppcomp.html#3
    private static double DateTimeToDayNumber(DateTime dateTime)
    {
        dateTime = dateTime.ToUniversalTime(); // Ensure input timestamp is in UTC

        var year = dateTime.Year;
        var month = dateTime.Month;
        var day = dateTime.Day;

        var fractionalDay = dateTime.TimeOfDay.TotalDays;

        var d = (
            367 * year
            - 7 * (year + (month + 9) / 12) / 4
            - 3 * ((year + (month - 9) / 7) / 100 + 1) / 4
            + 275 * month / 9
            + day
            - 730515);

        return d + fractionalDay;
    }

    // Using original variable names results in naming warnings, so suppress them:
    // ReSharper disable InconsistentNaming

    private static double EstimateEccentricAnomaly(double M, double e, double tol = 0.005)
    {
        var diff = double.PositiveInfinity;
        var E0 = M + (180 / Math.PI) * e * SinDeg(M) * (1 + e * CosDeg(M)); // Initial value
        while (diff > tol)
        {
            var E1 = E0 - (E0 - (180 / Math.PI) * e * SinDeg(E0) - M) / (1 - e * CosDeg(E0));
            diff = E0 - E1;
            E0 = E1;
        }
        return E0;
    }

    public abstract class CelestialBody(string name)
    {
        public string Name = name;

        // Orbital parameters used in computations - must be initialized by child implementations!
        public double MeanDistance = Double.NaN; // a [relative units]
        public double Eccentricity = Double.NaN; // e
        public double LongitudeOfAscendingNode = Double.NaN; // Omega or N [deg]
        public double Inclination = Double.NaN; // i [deg]
        public double ArgumentOfPerihelion = Double.NaN; // omega or w [deg]
        public double MeanAnomaly = Double.NaN; // M [deg]

        // Ecliptic coordinates (rectangular and spheric)
        public double EclipticX;
        public double EclipticY;
        public double EclipticZ;

        public double EclipticLon;
        public double EclipticLat;
        public double EclipticDistance;

        public abstract void UpdateOrbitalParameters(double d);

        // Initialize orbital parameters for the given day number
        public void UpdateEclipticCoordinates()
        {
            // Use same variable names as the source text
            var a = MeanDistance;
            var e = Eccentricity;
            var N = LongitudeOfAscendingNode;
            var i = Inclination;
            var w = ArgumentOfPerihelion;
            var M = MeanAnomaly;

            // Compute eccentric anomaly
            var E = EstimateEccentricAnomaly(M, e);

            // Distance and true anomaly
            var x = a * (CosDeg(E) - e);
            var y = a * Math.Sqrt(1 - e * e) * SinDeg(E);

            var r = Math.Sqrt(x * x + y * y);
            var v = Atan2Deg(y, x);

            // Heliocentric ecliptic coordinates
            var xeclip = r * (CosDeg(N) * CosDeg(v + w) - SinDeg(N) * SinDeg(v + w) * CosDeg(i));
            var yeclip = r * (SinDeg(N) * CosDeg(v + w) + CosDeg(N) * SinDeg(v + w) * CosDeg(i));
            var zeclip = r * SinDeg(v + w) * SinDeg(i);

            // Ecliptic longitude, latitude, and distance.
            var lon = Atan2Deg(yeclip, xeclip);
            var lat = Atan2Deg(zeclip, Math.Sqrt(xeclip * xeclip + yeclip * yeclip));
            r = Math.Sqrt(xeclip * xeclip + yeclip * yeclip + zeclip * zeclip);

            if (lon < 0)
            {
                lon += 360;
            }

            // Store
            EclipticX = xeclip;
            EclipticY = yeclip;
            EclipticZ = zeclip;

            EclipticLon = lon;
            EclipticLat = lat;
            EclipticDistance = r;
        }
    }

    private class _Mercury() : CelestialBody("Mercury")
    {
        public override void UpdateOrbitalParameters(double d)
        {
            LongitudeOfAscendingNode = (48.3313 + 3.24587E-5 * d) % 360;
            Inclination = (7.0047 + 5.00E-8 * d) % 360;
            ArgumentOfPerihelion = (29.1241 + 1.01444E-5 * d) % 360;
            MeanDistance = 0.387098;
            Eccentricity = 0.205635 + 5.59E-10 * d;
            MeanAnomaly = (168.6562 + 4.0923344368 * d) % 360;
        }
    }

    private class _Venus() : CelestialBody("Venus")
    {
        public override void UpdateOrbitalParameters(double d)
        {
            LongitudeOfAscendingNode = (76.6799 + 2.46590E-5 * d) % 360;
            Inclination = (3.3946 + 2.75E-8 * d) % 360;
            ArgumentOfPerihelion = (54.8910 + 1.38374E-5 * d) % 360;
            MeanDistance = 0.723330;
            Eccentricity = 0.006773 - 1.302E-9 * d;
            MeanAnomaly = (48.0052 + 1.6021302244 * d) % 360;
        }
    }

    private class _Mars() : CelestialBody("Mars")
    {
        public override void UpdateOrbitalParameters(double d)
        {
            LongitudeOfAscendingNode = (49.5574 + 2.11081E-5 * d) % 360;
            Inclination = (1.8497 - 1.78E-8 * d) % 360;
            ArgumentOfPerihelion = (286.5016 + 2.92961E-5 * d) % 360;
            MeanDistance = 1.523688;
            Eccentricity = 0.093405 + 2.516E-9 * d;
            MeanAnomaly =  (18.6021 + 0.5240207766 * d) % 360;
        }
    }

    private class _Jupiter() : CelestialBody("Jupiter")
    {
        public override void UpdateOrbitalParameters(double d)
        {
            LongitudeOfAscendingNode = (100.4542 + 2.76854E-5 * d) % 360;
            Inclination = (1.3030 - 1.557E-7 * d) % 360;
            ArgumentOfPerihelion = (273.8777 + 1.64505E-5 * d) % 360;
            MeanDistance = 5.20256;
            Eccentricity = 0.048498 + 4.469E-9 * d;
            MeanAnomaly = (19.8950 + 0.0830853001 * d) % 360;
        }

        public void ApplyPerturbations(double Ms)
        {
            // Ms: Saturn's mean anomaly
            // Mj: Jupiter's mean anomaly
            var Mj = MeanAnomaly;

            var perturbationLon =
                -0.332 * SinDeg(2 * Mj - 5 * Ms - 67.6)
                - 0.056 * SinDeg(2 * Mj - 2 * Ms + 21)
                + 0.042 * SinDeg(3 * Mj - 5 * Ms + 21)
                - 0.036 * SinDeg(Mj - 2 * Ms)
                + 0.022 * CosDeg(Mj - Ms)
                + 0.023 * SinDeg(2 * Mj - 3 * Ms + 52)
                - 0.016 * SinDeg(Mj - 5 * Ms - 69);

            EclipticLon += perturbationLon;

            // FIXME: re-compute X, Y, Z
        }
    }

    private class _Saturn() : CelestialBody("Saturn")
    {
        public override void UpdateOrbitalParameters(double d)
        {
            LongitudeOfAscendingNode = (113.6634 + 2.38980E-5 * d) % 360;
            Inclination = (2.4886 - 1.081E-7 * d) % 360;
            ArgumentOfPerihelion = (339.3939 + 2.97661E-5 * d) % 360;
            MeanDistance = 9.55475;
            Eccentricity = 0.055546 - 9.499E-9 * d;
            MeanAnomaly = (316.9670 + 0.0334442282 * d) % 360;
        }

        public void ApplyPerturbations(double Mj)
        {
            var Ms = MeanAnomaly;

            var perturbationLon =
                + 0.812 * SinDeg(2 * Mj - 5 * Ms - 67.6)
                - 0.229 * CosDeg(2 * Mj - 4 * Ms - 2)
                + 0.119 * SinDeg(Mj - 2 * Ms - 3)
                + 0.046 * SinDeg(2 * Mj - 6 * Ms - 69)
                + 0.014 * SinDeg(Mj - 3 * Ms + 32);

            var perturbationLat =
                - 0.020 * CosDeg(2 * Mj - 4 * Ms - 2)
                + 0.018 * SinDeg(2 * Mj - 6 * Ms - 49);

            EclipticLon += perturbationLon;
            EclipticLat += perturbationLat;

            // FIXME: re-compute X, Y, Z
        }
    }

    private class _Uranus() : CelestialBody("Uranus")
    {
        public override void UpdateOrbitalParameters(double d)
        {
            LongitudeOfAscendingNode = (74.0005 + 1.3978E-5 * d) % 360;
            Inclination = (0.7733 + 1.9E-8 * d) % 360;
            ArgumentOfPerihelion = (96.6612 + 3.0565E-5 * d) % 360;
            MeanDistance = 19.18171 - 1.55E-8 * d;
            Eccentricity = 0.047318 + 7.45E-9 * d;
            MeanAnomaly = (142.5905 + 0.011725806 * d) % 360;
        }

        public void ApplyPerturbations(double Mj, double Ms)
        {
            var Mu = MeanAnomaly;

            var perturbationLon =
                + 0.040 * SinDeg(Ms - 2 * Mu + 6)
                + 0.035 * SinDeg(Ms - 3 * Mu + 33)
                - 0.015 * SinDeg(Mj - Mu + 20);

            EclipticLon += perturbationLon;

            // FIXME: re-compute X, Y, Z
        }
    }

    private class _Neptune() : CelestialBody("Neptune")
    {
        public override void UpdateOrbitalParameters(double d)
        {
            LongitudeOfAscendingNode = 131.7806 + 3.0173E-5 * d;
            Inclination = 1.7700 - 2.55E-7 * d;
            ArgumentOfPerihelion = 272.8461 - 6.027E-6 * d;
            MeanDistance = 30.05826 + 3.313E-8 * d;
            Eccentricity = 0.008606 + 2.15E-9 * d;
            MeanAnomaly = 260.2471 + 0.005995147 * d;
        }
    }
}
