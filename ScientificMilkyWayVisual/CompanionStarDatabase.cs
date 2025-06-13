using System;
using System.Collections.Generic;

/// <summary>
/// Global database for determining which stars have companions based on their seed
/// Uses deterministic random generation to ensure consistency
/// </summary>
public static class CompanionStarDatabase
{
    /// <summary>
    /// Determine if a star has companions and how many based on its seed and type
    /// </summary>
    public static (bool isMultiple, int companionCount, List<string> companionDesignations) GetCompanionInfo(
        long seed, 
        ScientificMilkyWayGenerator.StellarType stellarType)
    {
        // Use seed for deterministic randomness
        var rng = new Random((int)((seed * 73) % int.MaxValue));
        var roll = rng.NextDouble();
        
        int companionCount = 0;
        
        // Check if this is a compact object (white dwarf, neutron star, black hole)
        bool isCompactObject = stellarType == ScientificMilkyWayGenerator.StellarType.DA ||
                               stellarType == ScientificMilkyWayGenerator.StellarType.NS ||
                               stellarType == ScientificMilkyWayGenerator.StellarType.BH;
        
        if (isCompactObject)
        {
            // Compact objects: high companion probability
            if (roll < 1.0 / 400.0)
            {
                companionCount = 3; // Quaternary
            }
            else if (roll < 1.0 / 50.0)
            {
                companionCount = 2; // Ternary
            }
            else if (roll < 1.0 / 3.0)
            {
                companionCount = 1; // Binary
            }
        }
        else
        {
            // Normal stars: lower companion probability
            if (roll < 1.0 / 1000.0)
            {
                companionCount = 3; // Quaternary
            }
            else if (roll < 1.0 / 100.0)
            {
                companionCount = 2; // Ternary
            }
            else if (roll < 1.0 / 15.0)
            {
                companionCount = 1; // Binary
            }
        }
        
        // Generate companion designations
        var designations = new List<string>();
        var letters = new[] { "A", "B", "C" };
        for (int i = 0; i < companionCount; i++)
        {
            designations.Add(letters[i]);
        }
        
        return (companionCount > 0, companionCount, designations);
    }
    
    /// <summary>
    /// Get companion properties for a specific companion
    /// </summary>
    public static (double mass, double separation, string designation) GetCompanionProperties(
        long primarySeed,
        double primaryMass,
        string companionDesignation)
    {
        // Use combined seed for consistent companion properties
        var companionIndex = companionDesignation switch
        {
            "A" => 1,
            "B" => 2,
            "C" => 3,
            _ => 1
        };
        
        var rng = new Random((int)((primarySeed * 137 + companionIndex * 31) % int.MaxValue));
        
        // Mass ratio: 10-90% of primary
        var massRatio = 0.1 + rng.NextDouble() * 0.8;
        var mass = primaryMass * massRatio;
        
        // Separation in AU (log-normal distribution)
        var separationAU = companionIndex switch
        {
            1 => Math.Exp(rng.NextDouble() * 4.6 - 2.3), // ~0.1 to 100 AU
            2 => Math.Exp(rng.NextDouble() * 5.3 - 1.6), // ~0.2 to 200 AU
            3 => Math.Exp(rng.NextDouble() * 5.8 - 1.1), // ~0.3 to 300 AU
            _ => 50.0
        };
        
        // Clamp to reasonable range
        separationAU = Math.Max(0.1, Math.Min(1000.0, separationAU));
        
        return (mass, separationAU, companionDesignation);
    }
    
    /// <summary>
    /// Determines the stellar type of a companion star based on its mass
    /// </summary>
    public static string GetCompanionStellarType(double companionMass, long primarySeed, string companionDesignation)
    {
        var rng = new Random((int)(primarySeed % int.MaxValue) + companionDesignation.GetHashCode() * 2);
        
        // Determine stellar type based on mass with some evolution
        if (companionMass > 16.0)
        {
            // Very massive stars - can be O or B type, or evolved
            var evolution = rng.NextDouble();
            if (evolution < 0.7) return "O5V";
            else if (evolution < 0.85) return "B0V";
            else return "B0I"; // Blue supergiant
        }
        else if (companionMass > 8.0)
        {
            // Massive stars - B type or evolved
            var evolution = rng.NextDouble();
            if (evolution < 0.8) return "B5V";
            else return "B0III"; // Blue giant
        }
        else if (companionMass > 2.1)
        {
            // A type stars
            var subtype = rng.NextDouble();
            if (subtype < 0.5) return "A0V";
            else return "A5V";
        }
        else if (companionMass > 1.4)
        {
            // F type stars
            var subtype = rng.NextDouble();
            if (subtype < 0.5) return "F0V";
            else return "F5V";
        }
        else if (companionMass > 1.04)
        {
            // G type stars (like our Sun)
            var subtype = rng.NextDouble();
            if (subtype < 0.33) return "G0V";
            else if (subtype < 0.66) return "G2V";
            else return "G5V";
        }
        else if (companionMass > 0.8)
        {
            // K type stars
            var subtype = rng.NextDouble();
            if (subtype < 0.5) return "K0V";
            else return "K5V";
        }
        else if (companionMass > 0.45)
        {
            // M type red dwarfs
            var subtype = rng.NextDouble();
            if (subtype < 0.5) return "M0V";
            else return "M5V";
        }
        else if (companionMass > 0.08)
        {
            // Very low mass red dwarfs
            return "M8V";
        }
        else
        {
            // Brown dwarf (not a true star)
            return "L2"; // Brown dwarf spectral class
        }
    }
    
    /// <summary>
    /// Get the system name for a star (primary seed or companion designation)
    /// </summary>
    public static string GetSystemName(long seed, ScientificMilkyWayGenerator.StellarType stellarType)
    {
        var (isMultiple, _, _) = GetCompanionInfo(seed, stellarType);
        return seed.ToString(); // Primary always keeps its seed as name
    }
}