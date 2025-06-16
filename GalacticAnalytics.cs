using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Comprehensive galactic analysis and special objects management
/// Combines special object tracking with statistical analysis of the galaxy
/// </summary>
public static class GalacticAnalytics
{
    #region Special Galactic Objects
    
    /// <summary>
    /// Definition of a special galactic object
    /// </summary>
    public class SpecialObject
    {
        public long Seed { get; set; }
        public string Name { get; set; } = "";
        public ScientificMilkyWayGenerator.Vector3 Position { get; set; }
        public ScientificMilkyWayGenerator.StellarType Type { get; set; }
        public float Mass { get; set; }
        public float Temperature { get; set; }
        public float Luminosity { get; set; }
        public string Description { get; set; } = "";
    }
    
    /// <summary>
    /// List of all special objects in the galaxy
    /// </summary>
    private static readonly List<SpecialObject> SpecialObjects = new List<SpecialObject>
    {
        // Supermassive black hole at galactic center
        new SpecialObject
        {
            Seed = 0,
            Name = "Sagittarius A*",
            Position = new ScientificMilkyWayGenerator.Vector3(0, 0, 0),
            Type = ScientificMilkyWayGenerator.StellarType.SMBH,
            Mass = 4_310_000f,
            Temperature = 0f,
            Luminosity = 0f,
            Description = "Supermassive black hole at the center of the Milky Way"
        }
    };
    
    /// <summary>
    /// Get all special objects within a chunk's bounds
    /// </summary>
    public static List<ScientificMilkyWayGenerator.Star> GetSpecialObjectsInChunk(
        double rMin, double rMax, double thetaMin, double thetaMax, double zMin, double zMax)
    {
        var stars = new List<ScientificMilkyWayGenerator.Star>();
        
        foreach (var obj in SpecialObjects)
        {
            // Convert position to cylindrical coordinates
            double r = Math.Sqrt(obj.Position.X * obj.Position.X + obj.Position.Y * obj.Position.Y);
            double theta = Math.Atan2(obj.Position.Y, obj.Position.X);
            double z = obj.Position.Z;
            
            // Normalize theta to [0, 2π]
            if (theta < 0) theta += 2 * Math.PI;
            
            // Check if object is within chunk bounds
            if (r >= rMin && r < rMax &&
                theta >= thetaMin && theta < thetaMax &&
                z >= zMin && z < zMax)
            {
                // Convert to Star object
                var star = new ScientificMilkyWayGenerator.Star
                {
                    Seed = obj.Seed,
                    Position = obj.Position,
                    Type = obj.Type,
                    Mass = obj.Mass,
                    Temperature = obj.Temperature,
                    Luminosity = obj.Luminosity,
                    Color = CalculateStarColor(obj.Temperature),
                    Population = DeterminePopulation(obj.Position),
                    Region = obj.Name, // Use name as region for special objects
                    PlanetCount = 0, // Special objects don't have planets
                    IsMultiple = false,
                    SystemName = obj.Seed.ToString()
                };
                
                stars.Add(star);
            }
        }
        
        return stars;
    }
    
    #endregion
    
    #region Statistical Analysis
    
    /// <summary>
    /// Calculate stellar density at a given position
    /// </summary>
    public static double CalculateStellarDensity(double r, double z)
    {
        // Use the unified GalaxyGenerator for density calculations
        var position = new GalaxyGenerator.Vector3((float)r, 0, (float)z);
        float density = GalaxyGenerator.CalculateTotalDensity(position);
        
        // Scale to actual star count based on total stars in galaxy
        // The GalaxyGenerator returns normalized density [0,1]
        double totalStars = 100e9; // 100 billion stars
        double galaxyVolume = Math.PI * Math.Pow(60000, 2) * 2000; // Rough galaxy volume
        double averageDensity = totalStars / galaxyVolume;
        
        return density * averageDensity * 10; // Scale factor for realistic densities
    }
    
    /// <summary>
    /// Calculate average stellar separation from density
    /// </summary>
    private static double CalculateAverageStellarSeparation(double density)
    {
        if (density <= 0) return double.PositiveInfinity;
        // Average separation = (1/density)^(1/3)
        return Math.Pow(1.0 / density, 1.0 / 3.0);
    }
    
    
    private static double GetNormalizationFactor()
    {
        // Pre-calculated normalization to ensure total = 100 billion
        return 1.05; // Approximate value
    }
    
    #endregion
    
    #region Shared Utilities
    
    /// <summary>
    /// Calculate star color from temperature (simplified blackbody)
    /// </summary>
    private static ScientificMilkyWayGenerator.Vector3 CalculateStarColor(float temperature)
    {
        if (temperature <= 0) return new ScientificMilkyWayGenerator.Vector3(0, 0, 0);
        
        float r, g, b;
        
        if (temperature < 3500)
        {
            r = 1.0f;
            g = 0.3f;
            b = 0.0f;
        }
        else if (temperature < 5000)
        {
            r = 1.0f;
            g = 0.6f;
            b = 0.2f;
        }
        else if (temperature < 6000)
        {
            r = 1.0f;
            g = 0.9f;
            b = 0.7f;
        }
        else if (temperature < 7500)
        {
            r = 0.9f;
            g = 0.9f;
            b = 1.0f;
        }
        else if (temperature < 10000)
        {
            r = 0.7f;
            g = 0.8f;
            b = 1.0f;
        }
        else
        {
            r = 0.6f;
            g = 0.7f;
            b = 1.0f;
        }
        
        return new ScientificMilkyWayGenerator.Vector3(r, g, b);
    }
    
    /// <summary>
    /// Determine stellar population based on position
    /// </summary>
    private static ScientificMilkyWayGenerator.StellarPopulation DeterminePopulation(
        ScientificMilkyWayGenerator.Vector3 position)
    {
        var r = Math.Sqrt(position.X * position.X + position.Y * position.Y);
        var z = Math.Abs(position.Z);
        
        if (r < 1000 && z < 200)
            return ScientificMilkyWayGenerator.StellarPopulation.PopIII;
        else if (r < 5000 && z < 1000)
            return ScientificMilkyWayGenerator.StellarPopulation.PopII;
        else if (z > 5000)
            return ScientificMilkyWayGenerator.StellarPopulation.Halo;
        else
            return ScientificMilkyWayGenerator.StellarPopulation.ThinDisk;
    }
    
    #endregion
}