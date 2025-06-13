using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Enhanced companion star system that supports planetary systems for each star
/// </summary>
public class CompanionStarSystem
{
    /// <summary>
    /// Represents a complete star with all properties needed for planetary system generation
    /// </summary>
    public class CompanionStar
    {
        public long PrimarySeed { get; set; }
        public string Designation { get; set; } = "";
        public double Mass { get; set; }
        public double SeparationAU { get; set; }
        public ScientificMilkyWayGenerator.StellarType Type { get; set; }
        public float Temperature { get; set; }
        public ScientificMilkyWayGenerator.Vector3 Color { get; set; }
        public float Luminosity { get; set; }
        public PlanetarySystemGenerator.PlanetarySystem? PlanetarySystem { get; set; }
        
        /// <summary>
        /// Get the full identifier for this companion star
        /// </summary>
        public string GetFullIdentifier()
        {
            return $"{PrimarySeed}-{Designation}";
        }
    }
    
    /// <summary>
    /// Represents a multiple star system with primary and companions
    /// </summary>
    public class MultipleStarSystem
    {
        public long PrimarySeed { get; set; }
        public ScientificMilkyWayGenerator.Star PrimaryStar { get; set; }
        public List<CompanionStar> Companions { get; set; } = new List<CompanionStar>();
        public PlanetarySystemGenerator.PlanetarySystem? PrimaryPlanetarySystem { get; set; }
        
        /// <summary>
        /// Get all planetary systems in this multiple star system
        /// </summary>
        public List<PlanetarySystemGenerator.PlanetarySystem> GetAllPlanetarySystems()
        {
            var systems = new List<PlanetarySystemGenerator.PlanetarySystem>();
            
            if (PrimaryPlanetarySystem != null)
                systems.Add(PrimaryPlanetarySystem);
                
            foreach (var companion in Companions)
            {
                if (companion.PlanetarySystem != null)
                    systems.Add(companion.PlanetarySystem);
            }
            
            return systems;
        }
    }
    
    /// <summary>
    /// Calculate stable orbital zones for planets in a multiple star system
    /// </summary>
    public static class StableOrbitCalculator
    {
        /// <summary>
        /// Calculate the inner and outer stable orbit limits for S-type orbits around a star
        /// S-type: planet orbits one star in a binary/multiple system
        /// </summary>
        public static (float innerLimit, float outerLimit) CalculateSTypeStableLimits(
            double starMass, 
            double nearestCompanionSeparation,
            double nearestCompanionMass)
        {
            // Inner limit is typically the Roche limit or minimum planetary formation distance
            float innerLimit = 0.01f * (float)starMass; // Simplified Roche limit
            innerLimit = Math.Max(innerLimit, 0.05f); // Minimum 0.05 AU
            
            // Outer limit for stable S-type orbits is approximately 0.3-0.5 times the binary separation
            // More conservative for higher mass ratios
            double massRatio = nearestCompanionMass / (starMass + nearestCompanionMass);
            double stabilityFactor = 0.3 - 0.1 * massRatio; // 0.2 to 0.3
            float outerLimit = (float)(nearestCompanionSeparation * stabilityFactor);
            
            return (innerLimit, outerLimit);
        }
        
        /// <summary>
        /// Calculate stable zones for P-type orbits (circumbinary planets)
        /// P-type: planet orbits both/all stars
        /// </summary>
        public static (float innerLimit, float outerLimit) CalculatePTypeStableLimits(
            double totalSystemMass,
            double maxSeparation)
        {
            // Inner limit for P-type orbits is typically 2-3 times the maximum stellar separation
            float innerLimit = (float)(maxSeparation * 2.5);
            
            // Outer limit depends on Hill sphere of the system
            float outerLimit = (float)(maxSeparation * 10); // Simplified
            
            return (innerLimit, outerLimit);
        }
        
        /// <summary>
        /// Check if a planetary orbit would be stable in this multiple star system
        /// </summary>
        public static bool IsOrbitStable(
            float planetOrbitalDistance,
            CompanionStar hostStar,
            List<CompanionStar> otherStars)
        {
            if (!otherStars.Any()) return true;
            
            // Find nearest companion
            var nearestCompanion = otherStars.OrderBy(s => Math.Abs(s.SeparationAU - hostStar.SeparationAU)).First();
            double separation = Math.Abs(nearestCompanion.SeparationAU - hostStar.SeparationAU);
            
            // For simplicity, if companion is very close, use tighter constraints
            if (separation < 1.0) separation = nearestCompanion.SeparationAU; // Use absolute separation
            
            var (innerLimit, outerLimit) = CalculateSTypeStableLimits(
                hostStar.Mass, 
                separation, 
                nearestCompanion.Mass);
                
            return planetOrbitalDistance >= innerLimit && planetOrbitalDistance <= outerLimit;
        }
    }
    
    /// <summary>
    /// Generate a complete multiple star system with planetary systems for all stars
    /// </summary>
    public static MultipleStarSystem GenerateMultipleStarSystem(ScientificMilkyWayGenerator.Star primaryStar)
    {
        var system = new MultipleStarSystem
        {
            PrimarySeed = primaryStar.Seed,
            PrimaryStar = primaryStar
        };
        
        // Check for companions
        var (isMultiple, companionCount, designations) = 
            CompanionStarDatabase.GetCompanionInfo(primaryStar.Seed, primaryStar.Type);
            
        if (!isMultiple) return system;
        
        // Generate companion stars with full properties
        foreach (var designation in designations)
        {
            var companion = GenerateCompanionStar(primaryStar, designation);
            system.Companions.Add(companion);
        }
        
        // Sort companions by separation for stable orbit calculations
        system.Companions = system.Companions.OrderBy(c => c.SeparationAU).ToList();
        
        // Generate planetary systems for primary star
        system.PrimaryPlanetarySystem = GeneratePlanetarySystemForStar(
            primaryStar.Seed, 
            primaryStar.Type, 
            primaryStar.Mass, 
            primaryStar.SystemName,
            GetNearestCompanionSeparation(null, system.Companions));
            
        // Generate planetary systems for each companion
        for (int i = 0; i < system.Companions.Count; i++)
        {
            var companion = system.Companions[i];
            var otherStars = system.Companions.Where((c, idx) => idx != i).ToList();
            
            companion.PlanetarySystem = GeneratePlanetarySystemForStar(
                primaryStar.Seed,
                companion.Type,
                (float)companion.Mass,
                companion.GetFullIdentifier(),
                GetNearestCompanionSeparation(companion, otherStars),
                companion,
                otherStars);
        }
        
        return system;
    }
    
    /// <summary>
    /// Generate a companion star with full stellar properties
    /// </summary>
    private static CompanionStar GenerateCompanionStar(ScientificMilkyWayGenerator.Star primaryStar, string designation)
    {
        var (mass, separation, _) = CompanionStarDatabase.GetCompanionProperties(
            primaryStar.Seed, 
            primaryStar.Mass, 
            designation);
            
        // Get stellar type based on mass
        var stellarTypeString = CompanionStarDatabase.GetCompanionStellarType(mass, primaryStar.Seed, designation);
        var stellarType = ParseStellarType(stellarTypeString);
        
        // Get stellar properties
        var properties = GetStellarProperties(stellarType);
        
        return new CompanionStar
        {
            PrimarySeed = primaryStar.Seed,
            Designation = designation,
            Mass = mass,
            SeparationAU = separation,
            Type = stellarType,
            Temperature = properties.temperature,
            Color = properties.color,
            Luminosity = properties.luminosity
        };
    }
    
    /// <summary>
    /// Generate planetary system considering multiple star constraints
    /// </summary>
    private static PlanetarySystemGenerator.PlanetarySystem? GeneratePlanetarySystemForStar(
        long seed,
        ScientificMilkyWayGenerator.StellarType stellarType,
        float stellarMass,
        string starName,
        double? nearestCompanionSeparation,
        CompanionStar? hostCompanion = null,
        List<CompanionStar>? otherCompanions = null)
    {
        var generator = new PlanetarySystemGenerator();
        var baseSystem = generator.GeneratePlanetarySystem(seed, stellarType, stellarMass, starName);
        
        // If no companions or very distant companions, return normal system
        if (!nearestCompanionSeparation.HasValue || nearestCompanionSeparation.Value > 100)
            return baseSystem;
            
        // Filter planets based on stable orbits
        var stablePlanets = new List<PlanetarySystemGenerator.Planet>();
        
        foreach (var planet in baseSystem.Planets)
        {
            bool isStable = true;
            
            if (hostCompanion != null && otherCompanions != null)
            {
                // For companion stars, check stability against other companions
                isStable = StableOrbitCalculator.IsOrbitStable(
                    planet.OrbitalDistance, 
                    hostCompanion, 
                    otherCompanions);
            }
            else
            {
                // For primary star, simple distance check
                var (_, outerLimit) = StableOrbitCalculator.CalculateSTypeStableLimits(
                    stellarMass, 
                    nearestCompanionSeparation.Value, 
                    stellarMass * 0.5); // Assume average companion mass
                    
                isStable = planet.OrbitalDistance <= outerLimit;
            }
            
            if (isStable)
            {
                stablePlanets.Add(planet);
            }
        }
        
        baseSystem.Planets = stablePlanets;
        
        // Re-index planets
        for (int i = 0; i < baseSystem.Planets.Count; i++)
        {
            baseSystem.Planets[i].Index = i + 1;
        }
        
        return baseSystem;
    }
    
    /// <summary>
    /// Get nearest companion separation for a star
    /// </summary>
    private static double? GetNearestCompanionSeparation(CompanionStar? currentStar, List<CompanionStar> companions)
    {
        if (!companions.Any()) return null;
        
        if (currentStar == null)
        {
            // For primary star, find closest companion
            return companions.Min(c => c.SeparationAU);
        }
        else
        {
            // For companion star, find closest other companion
            double minSeparation = double.MaxValue;
            foreach (var other in companions)
            {
                if (other.Designation != currentStar.Designation)
                {
                    double separation = Math.Abs(other.SeparationAU - currentStar.SeparationAU);
                    minSeparation = Math.Min(minSeparation, separation);
                }
            }
            return minSeparation < double.MaxValue ? minSeparation : (double?)null;
        }
    }
    
    /// <summary>
    /// Parse stellar type string to enum
    /// </summary>
    private static ScientificMilkyWayGenerator.StellarType ParseStellarType(string typeString)
    {
        // Map string stellar types to enum
        return typeString switch
        {
            "O5V" => ScientificMilkyWayGenerator.StellarType.O5V,
            "B0V" => ScientificMilkyWayGenerator.StellarType.B0V,
            "B5V" => ScientificMilkyWayGenerator.StellarType.B5V,
            "A0V" => ScientificMilkyWayGenerator.StellarType.A0V,
            "A5V" => ScientificMilkyWayGenerator.StellarType.A5V,
            "F0V" => ScientificMilkyWayGenerator.StellarType.F0V,
            "F5V" => ScientificMilkyWayGenerator.StellarType.F5V,
            "G0V" => ScientificMilkyWayGenerator.StellarType.G0V,
            "G2V" => ScientificMilkyWayGenerator.StellarType.G0V, // Close enough
            "G5V" => ScientificMilkyWayGenerator.StellarType.G5V,
            "K0V" => ScientificMilkyWayGenerator.StellarType.K0V,
            "K5V" => ScientificMilkyWayGenerator.StellarType.K5V,
            "M0V" => ScientificMilkyWayGenerator.StellarType.M0V,
            "M5V" => ScientificMilkyWayGenerator.StellarType.M5V,
            "M8V" => ScientificMilkyWayGenerator.StellarType.M8V,
            "K0III" => ScientificMilkyWayGenerator.StellarType.K0III,
            "K5III" => ScientificMilkyWayGenerator.StellarType.K5III,
            "M0III" => ScientificMilkyWayGenerator.StellarType.M0III,
            "B0III" => ScientificMilkyWayGenerator.StellarType.B0III,
            "M2I" => ScientificMilkyWayGenerator.StellarType.M2I,
            "B0I" => ScientificMilkyWayGenerator.StellarType.B0I,
            "L2" => ScientificMilkyWayGenerator.StellarType.M8V, // Brown dwarf -> very low mass star
            _ => ScientificMilkyWayGenerator.StellarType.G5V // Default
        };
    }
    
    /// <summary>
    /// Get stellar properties for a given type (simplified version)
    /// </summary>
    private static (float temperature, ScientificMilkyWayGenerator.Vector3 color, float luminosity) GetStellarProperties(
        ScientificMilkyWayGenerator.StellarType type)
    {
        // Simplified stellar properties
        return type switch
        {
            ScientificMilkyWayGenerator.StellarType.O5V => (40000f, new ScientificMilkyWayGenerator.Vector3(0.6f, 0.7f, 1.0f), 500000f),
            ScientificMilkyWayGenerator.StellarType.B0V => (30000f, new ScientificMilkyWayGenerator.Vector3(0.7f, 0.8f, 1.0f), 50000f),
            ScientificMilkyWayGenerator.StellarType.B5V => (15000f, new ScientificMilkyWayGenerator.Vector3(0.8f, 0.9f, 1.0f), 800f),
            ScientificMilkyWayGenerator.StellarType.A0V => (10000f, new ScientificMilkyWayGenerator.Vector3(0.9f, 0.95f, 1.0f), 50f),
            ScientificMilkyWayGenerator.StellarType.A5V => (8200f, new ScientificMilkyWayGenerator.Vector3(0.95f, 0.97f, 1.0f), 12f),
            ScientificMilkyWayGenerator.StellarType.F0V => (7200f, new ScientificMilkyWayGenerator.Vector3(1.0f, 1.0f, 0.98f), 5f),
            ScientificMilkyWayGenerator.StellarType.F5V => (6400f, new ScientificMilkyWayGenerator.Vector3(1.0f, 1.0f, 0.95f), 2.5f),
            ScientificMilkyWayGenerator.StellarType.G0V => (6000f, new ScientificMilkyWayGenerator.Vector3(1.0f, 1.0f, 0.9f), 1.5f),
            ScientificMilkyWayGenerator.StellarType.G5V => (5700f, new ScientificMilkyWayGenerator.Vector3(1.0f, 1.0f, 0.85f), 0.9f),
            ScientificMilkyWayGenerator.StellarType.K0V => (5200f, new ScientificMilkyWayGenerator.Vector3(1.0f, 0.95f, 0.8f), 0.5f),
            ScientificMilkyWayGenerator.StellarType.K5V => (4300f, new ScientificMilkyWayGenerator.Vector3(1.0f, 0.85f, 0.65f), 0.15f),
            ScientificMilkyWayGenerator.StellarType.M0V => (3800f, new ScientificMilkyWayGenerator.Vector3(1.0f, 0.75f, 0.5f), 0.08f),
            ScientificMilkyWayGenerator.StellarType.M5V => (3100f, new ScientificMilkyWayGenerator.Vector3(1.0f, 0.6f, 0.3f), 0.01f),
            ScientificMilkyWayGenerator.StellarType.M8V => (2500f, new ScientificMilkyWayGenerator.Vector3(1.0f, 0.5f, 0.2f), 0.001f),
            _ => (5700f, new ScientificMilkyWayGenerator.Vector3(1.0f, 1.0f, 0.85f), 1.0f)
        };
    }
}