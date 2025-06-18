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
        public GalaxyGenerator.Vector3 Position { get; set; }
        public StellarTypeGenerator.StellarType Type { get; set; }
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
            Position = new GalaxyGenerator.Vector3(0, 0, 0),
            Type = StellarTypeGenerator.StellarType.SMBH,
            Mass = 4_310_000f,
            Temperature = 0f,
            Luminosity = 0f,
            Description = "Supermassive black hole at the center of the Milky Way"
        }
    };
    
    /// <summary>
    /// Get all special objects within a chunk's bounds
    /// </summary>
    public static List<Star> GetSpecialObjectsInChunk(
        double rMin, double rMax, double thetaMin, double thetaMax, double zMin, double zMax)
    {
        var stars = new List<Star>();
        
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
                var star = new Star
                {
                    Seed = obj.Seed,
                    Position = new GalaxyGenerator.Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z),
                    Type = obj.Type,
                    Mass = obj.Mass,
                    Temperature = obj.Temperature,
                    Luminosity = obj.Luminosity,
                    Color = CalculateStarColorUnified(obj.Temperature),
                    Population = DeterminePopulationUnified(obj.Position),
                    Region = obj.Name, // Use name as region for special objects
                    PlanetCount = 0, // Special objects don't have planets
                    IsMultiple = false,
                    SystemName = obj.Name // Use name instead of seed
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
    // No longer needed - using StellarTypeGenerator.StellarType directly
    
    private static GalaxyGenerator.Vector3 CalculateStarColorUnified(float temperature)
    {
        if (temperature <= 0) return new GalaxyGenerator.Vector3(0, 0, 0);
        
        float r, g, b;
        
        if (temperature < 3500)
        {
            // Red stars
            r = 1.0f;
            g = temperature / 3500f * 0.5f;
            b = 0.0f;
        }
        else if (temperature < 5000)
        {
            // Orange stars
            r = 1.0f;
            g = 0.5f + (temperature - 3500f) / 1500f * 0.3f;
            b = (temperature - 3500f) / 1500f * 0.2f;
        }
        else if (temperature < 6000)
        {
            // Yellow stars
            r = 1.0f;
            g = 0.8f + (temperature - 5000f) / 1000f * 0.2f;
            b = 0.2f + (temperature - 5000f) / 1000f * 0.3f;
        }
        else if (temperature < 10000)
        {
            // White stars
            r = 1.0f;
            g = 1.0f;
            b = 0.5f + (temperature - 6000f) / 4000f * 0.5f;
        }
        else
        {
            // Blue stars
            r = 0.7f + (10000f / temperature) * 0.3f;
            g = 0.8f + (10000f / temperature) * 0.2f;
            b = 1.0f;
        }
        
        return new GalaxyGenerator.Vector3(r, g, b);
    }
    
    private static string DeterminePopulationUnified(GalaxyGenerator.Vector3 position)
    {
        return GalaxyGenerator.DeterminePopulation(position).ToString();
    }
    
    
    #endregion
}