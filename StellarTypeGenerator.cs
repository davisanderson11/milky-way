using System;

/// <summary>
/// Handles stellar type determination using GalaxyGenerator's density and population systems
/// </summary>
public static class StellarTypeGenerator
{
    /// <summary>
    /// Stellar types including main sequence, giants, and compact objects
    /// </summary>
    public enum StellarType
    {
        // Main sequence stars (Luminosity class V)
        O5V, B0V, B5V, A0V, A5V, F0V, F5V, G0V, G5V, K0V, K5V, M0V, M5V, M8V,
        
        // Giants (Luminosity class III)
        G5III, K0III, K5III, M0III, // Red giants
        B0III, // Blue giant
        
        // Supergiants (Luminosity class I)
        M2I, // Red supergiant
        B0I, // Blue supergiant
        
        // Brown dwarfs (substellar objects)
        L0, // Early L dwarf (~2200-1400K, 75-80 Jupiter masses)
        L5, // Mid L dwarf (~1700-1400K, 65-75 Jupiter masses)
        T0, // Early T dwarf (~1400-1200K, 50-65 Jupiter masses)
        T5, // Mid T dwarf (~1200-900K, 30-50 Jupiter masses)
        Y0, // Y dwarf (~600-300K, 13-30 Jupiter masses)
        
        // Compact objects and stellar remnants
        DA, // White dwarf (DA = hydrogen atmosphere)
        NS, // Neutron star/Pulsar
        BH, // Stellar-mass black hole
        QS, // Quark star
        SMBH // Supermassive black hole (special case for Sgr A*)
    }
    
    /// <summary>
    /// Determine stellar type based on position using GalaxyGenerator's population system
    /// </summary>
    public static StellarType DetermineStellarType(GalaxyGenerator.Vector3 position, long seed)
    {
        var rng = new Random((int)((seed * 31) % int.MaxValue));
        var roll = rng.NextDouble();
        
        // Get population from GalaxyGenerator
        var population = GalaxyGenerator.DeterminePopulation(position);
        
        // Calculate galactocentric distance
        var galacticRadius = position.Length2D();
        
        // Check for special objects in central region
        if (population == GalaxyGenerator.StellarPopulation.Bulge && position.Length() < 100)
        {
            if (roll < 0.001) return StellarType.BH;   // 0.1% in central region
            if (roll < 0.005) if (rng.Next(1000) == 0)      // 1 in 1000 chance
        return StellarType.QS;
    else
        return StellarType.NS;   // 0.4% in central region
        }
        
        // Convert GalaxyGenerator population to internal enum for switch
        switch (GetPopulationType(population))
        {
            case PopulationType.Halo:
                // Old, metal-poor population  
                if (roll < 0.0001) return StellarType.B0V;
                if (roll < 0.001) return StellarType.A0V;
                if (roll < 0.005) return StellarType.F0V;
                if (roll < 0.025) return StellarType.G5V;
                if (roll < 0.12) return StellarType.K5V;
                if (roll < 0.73) return StellarType.M5V;    // Reduced for brown dwarfs
                if (roll < 0.75) // 2% brown dwarfs in halo (less common)
                {
                    var bdRoll = (roll - 0.73) / 0.02;
                    if (bdRoll < 0.5) return StellarType.L0;
                    if (bdRoll < 0.8) return StellarType.L5;
                    return StellarType.T0; // Cooler types rare in old halo
                }
                if (roll < 0.83) return StellarType.DA;     // 8% white dwarfs
                if (roll < 0.93) return StellarType.K0III;  // 10% red giants
                if (roll < 0.9995) return StellarType.M5V;  // More M dwarfs
                if (roll < 0.9999) if (rng.Next(1000) == 0)      // 1 in 1000 chance
                                return StellarType.QS;
                                else
                                return StellarType.NS;   // 0.04%
                return StellarType.BH;  // 0.01%
                
            case PopulationType.ThickDisk:
                // Intermediate age population
                // Brown dwarfs less common in thick disk (3%)
                if (roll < 0.0002) return StellarType.B0V;
                if (roll < 0.002) return StellarType.A0V;
                if (roll < 0.01) return StellarType.F0V;
                if (roll < 0.04) return StellarType.G5V;
                if (roll < 0.15) return StellarType.K5V;
                if (roll < 0.74) return StellarType.M5V;    // Reduced for brown dwarfs
                if (roll < 0.77) // 3% brown dwarfs
                {
                    var bdRoll = (roll - 0.74) / 0.03;
                    if (bdRoll < 0.4) return StellarType.L0;
                    if (bdRoll < 0.7) return StellarType.L5;
                    if (bdRoll < 0.9) return StellarType.T0;
                    return StellarType.T5;
                }
                if (roll < 0.83) return StellarType.DA;     // 6% white dwarfs
                if (roll < 0.90) return StellarType.K0III;  // 7% red giants
                if (roll < 0.9995) return StellarType.M5V;  // More M dwarfs
                if (roll < 0.99995) if (rng.Next(1000) == 0)      // 1 in 1000 chance
        return StellarType.QS;
    else
        return StellarType.NS;  // 0.05%
                return StellarType.BH;  // 0.005%
                
            case PopulationType.ThinDisk:
                // Check if in spiral arm for star formation
                var spiralMultiplier = GalaxyGenerator.CalculateSpiralArmMultiplier(position);
                var spiralBoost = spiralMultiplier - 1.0f;
                
                // Brown dwarfs are less common in dense regions
                var densityFactor = galacticRadius < 3000 ? 0.3f : 1.0f; // 70% reduction in central regions
                var brownDwarfRate = 0.06f * densityFactor; // 6% base rate in disc
                
                // Young population in spiral arms
                if (spiralBoost > 0.2 && roll < 0.001 * (1 + spiralBoost))
                    return StellarType.O5V;
                if (roll < 0.0003 + 0.001 * spiralBoost) return StellarType.O5V;
                if (roll < 0.0013 + 0.003 * spiralBoost) return StellarType.B0V;
                if (roll < 0.006 + 0.006 * spiralBoost) return StellarType.A0V;
                if (roll < 0.03 + 0.01 * spiralBoost) return StellarType.F0V;
                if (roll < 0.076) return StellarType.G5V;
                if (roll < 0.121) return StellarType.K5V;
                if (roll < 0.885 - brownDwarfRate) return StellarType.M5V; // Reduced to make room for brown dwarfs
                if (roll < 0.885) // Brown dwarfs
                {
                    var bdRoll = (roll - (0.885 - brownDwarfRate)) / brownDwarfRate;
                    if (bdRoll < 0.3) return StellarType.L0;     // 30% of brown dwarfs
                    if (bdRoll < 0.5) return StellarType.L5;     // 20% of brown dwarfs
                    if (bdRoll < 0.7) return StellarType.T0;     // 20% of brown dwarfs
                    if (bdRoll < 0.9) return StellarType.T5;     // 20% of brown dwarfs
                    return StellarType.Y0;                       // 10% of brown dwarfs
                }
                if (roll < 0.915) return StellarType.DA;    // 3% white dwarfs
                if (roll < 0.945) return StellarType.K0III; // 3% red giants
                if (roll < 0.9997) return StellarType.M5V;  // More M dwarfs
                if (roll < 0.99995) if (rng.Next(1000) == 0)      // 1 in 1000 chance
        return StellarType.QS;
    else
        return StellarType.NS;  // 0.025%
                return StellarType.BH;  // 0.005%
                
            case PopulationType.Bulge:
                // Old, metal-rich population
                if (roll < 0.0001) return StellarType.B0V;
                if (roll < 0.001) return StellarType.A0V;
                if (roll < 0.008) return StellarType.F0V;
                if (roll < 0.045) return StellarType.G5V;
                if (roll < 0.16) return StellarType.K5V;
                if (roll < 0.65) return StellarType.M5V;     // Reduced for brown dwarfs
                if (roll < 0.70) // 5% brown dwarfs in bulge
                {
                    var bdRoll = (roll - 0.65) / 0.05;
                    if (bdRoll < 0.4) return StellarType.L0;
                    if (bdRoll < 0.7) return StellarType.L5;
                    if (bdRoll < 0.9) return StellarType.T0;
                    if (bdRoll < 0.98) return StellarType.T5;
                    return StellarType.Y0;
                }
                if (roll < 0.78) return StellarType.DA;     // 8% white dwarfs
                if (roll < 0.88) return StellarType.K0III;  // 10% red giants
                if (roll < 0.9995) return StellarType.M5V;  // More M dwarfs
                if (roll < 0.99995) if (rng.Next(1000) == 0)      // 1 in 1000 chance
        return StellarType.QS;
    else
        return StellarType.NS;  // 0.05%
                return StellarType.BH;  // 0.005%
                
            default:
                // Default to M dwarf
                return StellarType.M5V;
        }
    }
    
    /// <summary>
    /// Get stellar properties for a given type
    /// </summary>
    public static (float mass, float temperature, GalaxyGenerator.Vector3 color, float luminosity) GetStellarProperties(StellarType type)
    {
        switch (type)
        {
            case StellarType.O5V:
                return (40f, 35000f, new GalaxyGenerator.Vector3(0.6f, 0.7f, 1.0f), 500000f);
            case StellarType.B0V:
                return (15f, 20000f, new GalaxyGenerator.Vector3(0.7f, 0.8f, 1.0f), 25000f);
            case StellarType.A0V:
                return (2.2f, 8500f, new GalaxyGenerator.Vector3(0.9f, 0.9f, 1.0f), 40f);
            case StellarType.F0V:
                return (1.4f, 6500f, new GalaxyGenerator.Vector3(1.0f, 1.0f, 0.95f), 3.5f);
            case StellarType.G5V:
                return (1.0f, 5500f, new GalaxyGenerator.Vector3(1.0f, 1.0f, 0.8f), 1f);
            case StellarType.K5V:
                return (0.7f, 4200f, new GalaxyGenerator.Vector3(1.0f, 0.85f, 0.65f), 0.4f);
            case StellarType.M5V:
                return (0.3f, 3200f, new GalaxyGenerator.Vector3(1.0f, 0.6f, 0.4f), 0.04f);
            
            // Brown dwarfs (substellar objects)
            case StellarType.L0:
                return (0.078f, 2200f, new GalaxyGenerator.Vector3(0.7f, 0.3f, 0.2f), 0.00016f); // Dark red-brown
            case StellarType.L5:
                return (0.070f, 1700f, new GalaxyGenerator.Vector3(0.6f, 0.2f, 0.15f), 0.00008f);
            case StellarType.T0:
                return (0.055f, 1400f, new GalaxyGenerator.Vector3(0.5f, 0.15f, 0.1f), 0.00004f); // Very dark brown
            case StellarType.T5:
                return (0.040f, 1000f, new GalaxyGenerator.Vector3(0.4f, 0.1f, 0.08f), 0.00001f);
            case StellarType.Y0:
                return (0.020f, 500f, new GalaxyGenerator.Vector3(0.3f, 0.05f, 0.05f), 0.000002f); // Almost invisible
            
            case StellarType.K0III:
                return (1.2f, 3500f, new GalaxyGenerator.Vector3(1.0f, 0.4f, 0.2f), 200f);
            case StellarType.B0III:
                return (20f, 25000f, new GalaxyGenerator.Vector3(0.6f, 0.75f, 1.0f), 80000f);
            case StellarType.M2I:
                return (25f, 4000f, new GalaxyGenerator.Vector3(1.0f, 0.3f, 0.1f), 300000f);
            case StellarType.DA:
                return (0.6f, 10000f, new GalaxyGenerator.Vector3(0.95f, 0.95f, 1.0f), 0.01f);
            case StellarType.NS:
                return (1.4f, 1000000f, new GalaxyGenerator.Vector3(0.7f, 0.7f, 1.0f), 0.001f);
            case StellarType.QS:
                return (2.0f, 1000000f, new GalaxyGenerator.Vector3(0.7f, 0.8f, 1.0f), 0.003f);
            case StellarType.BH:
                return (10f, 0f, GalaxyGenerator.Vector3.Zero, 0f);
            case StellarType.SMBH:
                return (4310000f, 0f, GalaxyGenerator.Vector3.Zero, 0f);
            default:
                return (1f, 5500f, new GalaxyGenerator.Vector3(1f, 1f, 1f), 1f);
        }
    }
    
    // Helper enum to simplify population mapping
    private enum PopulationType
    {
        Halo,
        ThickDisk,
        ThinDisk,
        Bulge
    }
    
    // Convert GalaxyGenerator population to internal type
    private static PopulationType GetPopulationType(GalaxyGenerator.StellarPopulation population)
    {
        switch (population)
        {
            case GalaxyGenerator.StellarPopulation.Halo:
                return PopulationType.Halo;
            case GalaxyGenerator.StellarPopulation.ThickDisk:
                return PopulationType.ThickDisk;
            case GalaxyGenerator.StellarPopulation.ThinDisk:
                return PopulationType.ThinDisk;
            case GalaxyGenerator.StellarPopulation.Bulge:
                return PopulationType.Bulge;
            default:
                return PopulationType.ThinDisk;
        }
    }
}