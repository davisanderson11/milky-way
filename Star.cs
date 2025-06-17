using System;

/// <summary>
/// Represents a star in the galaxy using the unified system
/// </summary>
public class Star
{
    public long Seed { get; set; }
    public GalaxyGenerator.Vector3 Position { get; set; }
    public StellarTypeGenerator.StellarType Type { get; set; }
    public float Mass { get; set; }  // Solar masses
    public float Temperature { get; set; }  // Kelvin
    public float Luminosity { get; set; }  // Solar luminosities
    public GalaxyGenerator.Vector3 Color { get; set; }  // RGB normalized
    public string Population { get; set; } = "";
    public string Region { get; set; } = "";
    public int PlanetCount { get; set; }
    public bool IsMultiple { get; set; }
    public string SystemName { get; set; } = "";
    
    /// <summary>
    /// Generate a star at a specific position
    /// </summary>
    public static Star GenerateAtPosition(GalaxyGenerator.Vector3 position, long seed)
    {
        var star = new Star
        {
            Seed = seed,
            Position = position
        };
        
        // Determine stellar type using the unified generator
        star.Type = StellarTypeGenerator.DetermineStellarType(position, seed);
        
        // Get properties for this type
        var (mass, temperature, color, luminosity) = StellarTypeGenerator.GetStellarProperties(star.Type);
        star.Mass = mass;
        star.Temperature = temperature;
        star.Color = color;
        star.Luminosity = luminosity;
        
        // Determine population and region
        var population = GalaxyGenerator.DeterminePopulation(position);
        star.Population = population.ToString();
        star.Region = GalaxyGenerator.DetermineRegion(position);
        
        // Generate system with companions and planets
        var unifiedGen = new UnifiedSystemGenerator();
        var system = unifiedGen.GenerateSystem(seed, 
            ConvertToScientificType(star.Type), 
            star.Mass, 
            star.Temperature, 
            star.Luminosity);
        
        // Count total planets from all stars in the system
        int totalPlanets = 0;
        foreach (var systemStar in system.AllStars)
        {
            totalPlanets += systemStar.Children.Count(c => c is UnifiedSystemGenerator.Planet);
        }
        
        star.PlanetCount = totalPlanets;
        
        // Check for multiple star system
        bool hasCompanions = system.AllStars.Count > 1;
        star.IsMultiple = hasCompanions;
        
        // Generate system name
        if (hasCompanions)
        {
            int starCount = system.AllStars.Count;
            
            if (starCount == 2)
                star.SystemName = "Binary";
            else if (starCount == 3)
                star.SystemName = "Triple";
            else if (starCount == 4)
                star.SystemName = "Quadruple";
            else
                star.SystemName = $"Multiple ({starCount} stars)";
        }
        else
        {
            star.SystemName = "Single";
        }
        
        return star;
    }
    
    /// <summary>
    /// Convert StellarTypeGenerator type to ScientificMilkyWayGenerator type for UnifiedSystemGenerator
    /// </summary>
    private static StellarTypeGenerator.StellarType ConvertToScientificType(StellarTypeGenerator.StellarType type)
    {
        return type switch
        {
            StellarTypeGenerator.StellarType.O5V => StellarTypeGenerator.StellarType.O5V,
            StellarTypeGenerator.StellarType.B0V => StellarTypeGenerator.StellarType.B0V,
            StellarTypeGenerator.StellarType.B5V => StellarTypeGenerator.StellarType.B5V,
            StellarTypeGenerator.StellarType.A0V => StellarTypeGenerator.StellarType.A0V,
            StellarTypeGenerator.StellarType.A5V => StellarTypeGenerator.StellarType.A5V,
            StellarTypeGenerator.StellarType.F0V => StellarTypeGenerator.StellarType.F0V,
            StellarTypeGenerator.StellarType.F5V => StellarTypeGenerator.StellarType.F5V,
            StellarTypeGenerator.StellarType.G0V => StellarTypeGenerator.StellarType.G0V,
            StellarTypeGenerator.StellarType.G5V => StellarTypeGenerator.StellarType.G5V,
            StellarTypeGenerator.StellarType.K0V => StellarTypeGenerator.StellarType.K0V,
            StellarTypeGenerator.StellarType.K5V => StellarTypeGenerator.StellarType.K5V,
            StellarTypeGenerator.StellarType.M0V => StellarTypeGenerator.StellarType.M0V,
            StellarTypeGenerator.StellarType.M5V => StellarTypeGenerator.StellarType.M5V,
            StellarTypeGenerator.StellarType.M8V => StellarTypeGenerator.StellarType.M8V,
            
            // Brown dwarfs
            StellarTypeGenerator.StellarType.L0 => StellarTypeGenerator.StellarType.L0,
            StellarTypeGenerator.StellarType.L5 => StellarTypeGenerator.StellarType.L5,
            StellarTypeGenerator.StellarType.T0 => StellarTypeGenerator.StellarType.T0,
            StellarTypeGenerator.StellarType.T5 => StellarTypeGenerator.StellarType.T5,
            StellarTypeGenerator.StellarType.Y0 => StellarTypeGenerator.StellarType.Y0,
            
            // Giants
            StellarTypeGenerator.StellarType.G5III => StellarTypeGenerator.StellarType.G5III,
            StellarTypeGenerator.StellarType.K0III => StellarTypeGenerator.StellarType.K0III,
            StellarTypeGenerator.StellarType.K5III => StellarTypeGenerator.StellarType.K5III,
            StellarTypeGenerator.StellarType.M0III => StellarTypeGenerator.StellarType.M0III,
            StellarTypeGenerator.StellarType.B0III => StellarTypeGenerator.StellarType.B0III,
            
            // Supergiants
            StellarTypeGenerator.StellarType.M2I => StellarTypeGenerator.StellarType.M2I,
            StellarTypeGenerator.StellarType.B0I => StellarTypeGenerator.StellarType.B0I,
            
            // Compact objects
            StellarTypeGenerator.StellarType.DA => StellarTypeGenerator.StellarType.DA,
            StellarTypeGenerator.StellarType.NS => StellarTypeGenerator.StellarType.NS,
            StellarTypeGenerator.StellarType.BH => StellarTypeGenerator.StellarType.BH,
            StellarTypeGenerator.StellarType.QS => StellarTypeGenerator.StellarType.NS, // Treat as neutron star
            StellarTypeGenerator.StellarType.SMBH => StellarTypeGenerator.StellarType.SMBH,
            
            _ => StellarTypeGenerator.StellarType.G5V
        };
    }
}