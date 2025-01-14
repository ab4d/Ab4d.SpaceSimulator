using System;
using System.Collections.Generic;
using System.Linq;

namespace Ab4d.SpaceSimulator;

public class SolarSystemScenario
{
    private struct Entity
    {
        public string Name;

        public string Parent;

        public string Type;

        public string MaterialName;

        public double Mass;
        public double Radius;
        public double Density;
        public double Gravity;
        public double EscapeVelocity;
        public double RotationPeriod;
        public double LengthOfDay;
        public double DistanceFromParent;
        public double Perihelion;
        public double Aphelion;
        public double OrbitalPeriod;
        public double OrbitalVelocity;
        public double OrbitalInclination;
        public double OrbitalEccentricity;
        public double ObliquityToOrbit;
        public double MeanTemperature;
        public double SurfacePressure;
        public double NumberOfMoons;
    };


    private readonly List<Entity> _entities = [];

    public SolarSystemScenario(string? dataFilename = null)
    {
        dataFilename ??= System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/SolarSystemData.txt");

        var dataLines = System.IO.File.ReadAllLines(dataFilename);

        // First line is the data source

        // Second line are the keys.
        var tokens = dataLines[1].Split('\t');
        var keyMap = new Dictionary<string, int>();
        for (var i = 0; i < tokens.Length; i++)
        {
            keyMap.Add(tokens[i].ToLower(), i);
        }

        // Third line are the units

        // Fourth line are the scale factors for conversion to basic SI units.
        tokens = dataLines[3].Split('\t');
        tokens[0] = "1.0";  // First column is "Scale:" - replace it with a number
        var basicUnitScaleFactors = tokens.Select(t => double.Parse(t, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        // The rest of lines are entries
        for (var i = 4; i < dataLines.Length; i++)
        {
            tokens = dataLines[i].Split('\t');

            var entity = new Entity
            {
                Name = tokens[0], // First column
                Parent = GetStringField("Parent", tokens),
                Type = GetStringField("Type", tokens),
                MaterialName = GetStringField("Material", tokens),
                Mass = GetNumericField("Mass", tokens),
                Radius = GetNumericField("Radius", tokens),
                Density = GetNumericField("Density", tokens),
                Gravity = GetNumericField("Gravity", tokens),
                EscapeVelocity = GetNumericField("Escape Velocity", tokens),
                RotationPeriod = GetNumericField("Rotation Period", tokens),
                LengthOfDay = GetNumericField("Length of Day", tokens),
                DistanceFromParent = GetNumericField("Distance from parent", tokens),
                Perihelion = GetNumericField("Perihelion", tokens),
                Aphelion = GetNumericField("Aphelion", tokens),
                OrbitalPeriod = GetNumericField("Orbital Period", tokens),
                OrbitalVelocity = GetNumericField("Orbital Velocity", tokens),
                OrbitalInclination = GetNumericField("Orbital Inclination", tokens),
                OrbitalEccentricity = GetNumericField("Orbital Eccentricity", tokens),
                ObliquityToOrbit = GetNumericField("Obliquity to Orbit", tokens),
                MeanTemperature = GetNumericField("Mean Temperature", tokens),
                SurfacePressure = GetNumericField("Surface Pressure", tokens),
                NumberOfMoons = GetNumericField("Number of Moons", tokens),
            };
            _entities.Add(entity);
        }

        return;

        // Local helper functions
        double GetNumericField(string key, string[] fieldTokens)
        {
            var keyIdx = keyMap[key.ToLower()];
            if (keyIdx >= fieldTokens.Length)
            {
                return double.NaN;
            }
            var rawValue = double.Parse(fieldTokens[keyIdx], System.Globalization.CultureInfo.InvariantCulture);
            var scaleFactor = basicUnitScaleFactors[keyIdx];
            return scaleFactor * rawValue;
        }

        string GetStringField(string key, string[] fieldTokens)
        {
            var keyIdx = keyMap[key.ToLower()];
            return fieldTokens[keyIdx];
        }
    }

    public void SetupScenario(PhysicsEngine.PhysicsEngine physicsEngine, VisualizationEngine.VisualizationEngine visualizationEngine)
    {
        foreach (var entity in _entities)
        {
            Console.Out.WriteLine($"Adding entity: {entity.Name}, of type {entity.Type} with parent {entity.Parent}.");
        }
    }
}
