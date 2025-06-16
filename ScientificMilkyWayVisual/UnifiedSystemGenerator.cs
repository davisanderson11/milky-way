using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Unified system generator that creates hierarchical stellar and planetary systems
/// Supports sub-binaries, binary planets, binary moons, and complex orbital arrangements
/// </summary>
public class UnifiedSystemGenerator
{
    #region Enums and Classes
    
    public enum ObjectType
    {
        Star,
        Planet,
        Moon,
        Asteroid,
        Comet
    }
    
    public enum PlanetType
    {
        Rocky,
        Gas,
        Ice
    }
    
    /// <summary>
    /// Base class for all system objects
    /// </summary>
    public abstract class SystemObject
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public ObjectType Type { get; set; }
        public double Mass { get; set; } // Solar masses for stars, Earth masses for planets/moons
        public SystemObject? Parent { get; set; }
        public List<SystemObject> Children { get; set; } = new List<SystemObject>();
        public double OrbitalDistance { get; set; } // AU from parent
        public double OrbitalPeriod { get; set; } // Years
        public bool IsBinary { get; set; } // True if this object has a binary companion at same level
        public SystemObject? BinaryCompanion { get; set; }
        
        public abstract string GetTreeDisplay(int indent = 0);
    }
    
    /// <summary>
    /// Star object - can have companion stars or planets
    /// </summary>
    public class Star : SystemObject
    {
        public ScientificMilkyWayGenerator.StellarType StellarType { get; set; }
        public double Temperature { get; set; }
        public double Luminosity { get; set; }
        public double Radius { get; set; }
        
        public Star()
        {
            Type = ObjectType.Star;
        }
        
        public override string GetTreeDisplay(int indent = 0)
        {
            var sb = new StringBuilder();
            var prefix = new string(' ', indent);
            var typeStr = StellarType.ToString();
            
            sb.AppendLine($"{prefix}├─ {Name} [{typeStr}, {Mass:F3} M☉, {Temperature:F0}K]");
            
            // For binary stars, show them as a pair
            if (BinaryCompanion != null && BinaryCompanion is Star binaryStar && 
                string.Compare(Name, binaryStar.Name) < 0) // Only show once, for the alphabetically first star
            {
                sb.AppendLine($"{prefix}│  └─ {binaryStar.Name} [{binaryStar.StellarType}, {binaryStar.Mass:F3} M☉] (binary with {Name.Split(' ').Last()} @ {binaryStar.OrbitalDistance:F2} AU)");
            }
            
            // Show children (skip binary companions as they're shown inline)
            foreach (var child in Children)
            {
                if (child != BinaryCompanion)
                {
                    sb.Append(child.GetTreeDisplay(indent + 3));
                }
            }
            
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Planet object - can have moons or binary planet companions
    /// </summary>
    public class Planet : SystemObject
    {
        public PlanetType PlanetType { get; set; }
        public double Radius { get; set; } // Earth radii
        public double Temperature { get; set; } // Kelvin
        public bool HasRings { get; set; }
        
        public Planet()
        {
            Type = ObjectType.Planet;
        }
        
        public override string GetTreeDisplay(int indent = 0)
        {
            var sb = new StringBuilder();
            var prefix = new string(' ', indent);
            
            var distanceStr = $", {OrbitalDistance:F2} AU";
            sb.AppendLine($"{prefix}├─ {Name} [{PlanetType}, {Mass:F2} M⊕{distanceStr}]");
            
            // Show binary companion as sub-item if this planet comes first
            if (BinaryCompanion != null && BinaryCompanion is Planet binaryPlanet && 
                Parent != null && Parent.Children.IndexOf(this) < Parent.Children.IndexOf(BinaryCompanion))
            {
                sb.AppendLine($"{prefix}│  └─ {binaryPlanet.Name} [{binaryPlanet.PlanetType}, {binaryPlanet.Mass:F2} M⊕] (binary with {Name.Split(' ').Last()})");
            }
            
            // Show moons (skip binary companions as they're shown inline)
            foreach (var child in Children)
            {
                if (child != BinaryCompanion)
                {
                    sb.Append(child.GetTreeDisplay(indent + 3));
                }
            }
            
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Moon object - can have binary moon companions
    /// </summary>
    public class Moon : SystemObject
    {
        public PlanetType Composition { get; set; }
        public double Radius { get; set; } // Moon radii (relative to our Moon)
        
        public Moon()
        {
            Type = ObjectType.Moon;
        }
        
        public override string GetTreeDisplay(int indent = 0)
        {
            var sb = new StringBuilder();
            var prefix = new string(' ', indent);
            
            sb.AppendLine($"{prefix}├─ {Name} [{Composition}, {Mass:F4} M☾]");
            
            // Show binary companion as sub-item if this moon comes first
            if (BinaryCompanion != null && BinaryCompanion is Moon binaryMoon && 
                Parent != null && Parent.Children.IndexOf(this) < Parent.Children.IndexOf(BinaryCompanion))
            {
                sb.AppendLine($"{prefix}│  └─ {binaryMoon.Name} [{binaryMoon.Composition}, {binaryMoon.Mass:F4} M☾] (binary with {Name.Last()})");
            }
            
            return sb.ToString();
        }
    }
    
    #endregion
    
    #region System Generation
    
    /// <summary>
    /// Generate a complete hierarchical system for a star
    /// </summary>
    public Star GenerateSystem(long seed, ScientificMilkyWayGenerator.StellarType stellarType, 
        double stellarMass, double temperature, double luminosity, string baseName)
    {
        var rng = new Random((int)(seed % int.MaxValue));
        
        // Create primary star
        var primaryStar = new Star
        {
            Id = seed.ToString(),
            Name = seed.ToString(),  // Just use seed, no prefix
            StellarType = stellarType,
            Mass = stellarMass,
            Temperature = temperature,
            Luminosity = luminosity,
            Radius = CalculateStellarRadius(stellarMass, temperature)
        };
        
        // Check for stellar companions
        GenerateStellarCompanions(primaryStar, seed, rng);
        
        // Generate planetary systems for all stars
        GeneratePlanetarySystems(primaryStar, seed, rng);
        
        return primaryStar;
    }
    
    /// <summary>
    /// Generate stellar companions (including sub-binaries)
    /// </summary>
    private void GenerateStellarCompanions(Star primaryStar, long seed, Random rng)
    {
        var roll = rng.NextDouble();
        
        // Determine companion configuration
        if (roll < 0.005) // 0.5% chance of complex system
        {
            // Hierarchical triple or quadruple system
            GenerateHierarchicalSystem(primaryStar, seed, rng);
        }
        else if (roll < 0.07) // 6.5% chance of simple binary
        {
            // Simple binary system
            var companion = GenerateCompanionStar(primaryStar, seed, rng, "B");
            companion.OrbitalDistance = Math.Pow(10, rng.NextDouble() * 3 - 1); // 0.1-100 AU
            primaryStar.Children.Add(companion);
            companion.Parent = primaryStar;
        }
    }
    
    /// <summary>
    /// Generate hierarchical stellar system (sub-binaries)
    /// </summary>
    private void GenerateHierarchicalSystem(Star primaryStar, long seed, Random rng)
    {
        var config = rng.NextDouble();
        
        if (config < 0.4) // Close binary + distant companion
        {
            // Primary becomes A, gets a close binary companion B
            primaryStar.Name = primaryStar.Id + " A";
            var closeCompanion = GenerateCompanionStar(primaryStar, seed, rng, "B");
            closeCompanion.OrbitalDistance = 0.01 + rng.NextDouble() * 0.5; // 0.01-0.5 AU
            primaryStar.BinaryCompanion = closeCompanion;
            closeCompanion.BinaryCompanion = primaryStar;
            primaryStar.IsBinary = true;
            closeCompanion.IsBinary = true;
            
            // Don't add B to A's children since it's a binary companion
            
            // Distant third star C
            var distantCompanion = GenerateCompanionStar(primaryStar, seed, rng, "C");
            distantCompanion.OrbitalDistance = 50 + rng.NextDouble() * 450; // 50-500 AU
            primaryStar.Children.Add(distantCompanion);
            distantCompanion.Parent = primaryStar;
        }
        else if (config < 0.7) // Two separate binaries
        {
            // Primary becomes A with companion B
            primaryStar.Name = primaryStar.Id + " A";
            var companion1 = GenerateCompanionStar(primaryStar, seed, rng, "B");
            companion1.OrbitalDistance = 0.1 + rng.NextDouble() * 1; // 0.1-1 AU
            primaryStar.BinaryCompanion = companion1;
            companion1.BinaryCompanion = primaryStar;
            primaryStar.IsBinary = true;
            companion1.IsBinary = true;
            
            // Don't add B to A's children since it's a binary companion
            
            // Secondary binary pair C and D at distance
            var secondary = GenerateCompanionStar(primaryStar, seed, rng, "C");
            var secondaryCompanion = GenerateCompanionStar(primaryStar, seed, rng, "D");
            secondary.OrbitalDistance = 100 + rng.NextDouble() * 400; // 100-500 AU
            secondaryCompanion.OrbitalDistance = 0.5 + rng.NextDouble() * 2; // 0.5-2.5 AU from C
            
            secondary.BinaryCompanion = secondaryCompanion;
            secondaryCompanion.BinaryCompanion = secondary;
            secondary.IsBinary = true;
            secondaryCompanion.IsBinary = true;
            
            primaryStar.Children.Add(secondary);
            secondary.Parent = primaryStar;
            secondary.Children.Add(secondaryCompanion);
            secondaryCompanion.Parent = secondary;
        }
        else // Triple with one distant
        {
            // Close binary A and B
            primaryStar.Name = primaryStar.Id + " A";
            var closeCompanion = GenerateCompanionStar(primaryStar, seed, rng, "B");
            closeCompanion.OrbitalDistance = 0.05 + rng.NextDouble() * 0.5; // 0.05-0.5 AU
            primaryStar.BinaryCompanion = closeCompanion;
            closeCompanion.BinaryCompanion = primaryStar;
            primaryStar.IsBinary = true;
            closeCompanion.IsBinary = true;
            
            // Don't add B to A's children since it's a binary companion
            
            // Two distant companions C and D
            var distant1 = GenerateCompanionStar(primaryStar, seed, rng, "C");
            var distant2 = GenerateCompanionStar(primaryStar, seed, rng, "D");
            distant1.OrbitalDistance = 50 + rng.NextDouble() * 200; // 50-250 AU
            distant2.OrbitalDistance = 300 + rng.NextDouble() * 700; // 300-1000 AU
            
            primaryStar.Children.Add(distant1);
            primaryStar.Children.Add(distant2);
            distant1.Parent = primaryStar;
            distant2.Parent = primaryStar;
        }
    }
    
    /// <summary>
    /// Generate a companion star
    /// </summary>
    private Star GenerateCompanionStar(Star primary, long seed, Random rng, string designation)
    {
        // Mass ratio typically 0.1-0.9 of primary
        var massRatio = 0.1 + rng.NextDouble() * 0.8;
        var mass = primary.Mass * massRatio;
        
        // Determine stellar type based on mass
        var stellarType = DetermineStellarTypeFromMass(mass, rng);
        var temperature = EstimateTemperatureFromType(stellarType);
        var luminosity = Math.Pow(mass, 3.5); // Rough mass-luminosity relation
        
        var companion = new Star
        {
            Id = $"{seed} {designation}",
            Name = $"{primary.Id.Split(' ')[0]} {designation}",
            StellarType = stellarType,
            Mass = mass,
            Temperature = temperature,
            Luminosity = luminosity,
            Radius = CalculateStellarRadius(mass, temperature)
        };
        
        return companion;
    }
    
    /// <summary>
    /// Generate planetary systems for all stars in the system
    /// </summary>
    private void GeneratePlanetarySystems(Star star, long seed, Random rng)
    {
        // Check if this star can have planets
        if (!CanHavePlanets(star.StellarType)) return;
        
        // Binary stars have reduced planet formation
        if (star.IsBinary && rng.NextDouble() > 0.3) return;
        
        // Generate planets
        var planetCount = DeterminePlanetCount(star, rng);
        var currentDistance = 0.1 + rng.NextDouble() * 0.3; // 0.1-0.4 AU start
        
        for (int i = 0; i < planetCount; i++)
        {
            var planet = GeneratePlanet(star, seed, rng, i + 1, currentDistance);
            star.Children.Add(planet);
            planet.Parent = star;
            
            // Check for binary planet (rare)
            if (rng.NextDouble() < 0.02 && i < planetCount - 1) // 2% chance, skip last planet
            {
                var binaryPlanet = GenerateBinaryPlanet(planet, seed, rng);
                planet.BinaryCompanion = binaryPlanet;
                binaryPlanet.BinaryCompanion = planet;
                planet.IsBinary = true;
                binaryPlanet.IsBinary = true;
                
                // Don't add to children - will be shown as binary companion
                // But still skip next planet number since we used it
                i++;
            }
            
            // Generate moons
            GenerateMoons(planet, seed, rng);
            
            // Next planet distance
            currentDistance *= 1.4 + rng.NextDouble() * 0.4; // 1.4-1.8x spacing
        }
        
        // Recursively generate for stellar companions
        foreach (var child in star.Children.Where(c => c is Star))
        {
            GeneratePlanetarySystems((Star)child, seed + 1000, rng);
        }
    }
    
    /// <summary>
    /// Generate a planet
    /// </summary>
    private Planet GeneratePlanet(Star star, long seed, Random rng, int index, double distance)
    {
        var planet = new Planet
        {
            Id = $"{star.Id.Split(' ')[0]} {index}",
            Name = $"{star.Id.Split(' ')[0]} {index}",
            OrbitalDistance = distance
        };
        
        // Determine planet type based on distance and stellar properties
        var frostLine = Math.Sqrt(star.Luminosity) * 2.7; // AU
        
        if (distance < frostLine * 0.5)
        {
            // Hot rocky planet
            planet.PlanetType = PlanetType.Rocky;
            planet.Mass = 0.1 + rng.NextDouble() * 2; // 0.1-2.1 Earth masses
        }
        else if (distance < frostLine)
        {
            // Temperate rocky or small gas
            if (rng.NextDouble() < 0.7)
            {
                planet.PlanetType = PlanetType.Rocky;
                planet.Mass = 0.5 + rng.NextDouble() * 3; // 0.5-3.5 Earth masses
            }
            else
            {
                planet.PlanetType = PlanetType.Gas;
                planet.Mass = 5 + rng.NextDouble() * 20; // 5-25 Earth masses (Neptune-like)
            }
        }
        else if (distance < frostLine * 3)
        {
            // Gas giants
            planet.PlanetType = PlanetType.Gas;
            planet.Mass = 20 + rng.NextDouble() * 300; // 20-320 Earth masses
        }
        else
        {
            // Ice giants or ice worlds
            if (rng.NextDouble() < 0.6)
            {
                planet.PlanetType = PlanetType.Ice;
                planet.Mass = 10 + rng.NextDouble() * 40; // 10-50 Earth masses
            }
            else
            {
                planet.PlanetType = PlanetType.Ice;
                planet.Mass = 0.1 + rng.NextDouble() * 5; // 0.1-5.1 Earth masses (ice dwarfs)
            }
        }
        
        planet.Radius = CalculatePlanetRadius(planet.Mass, planet.PlanetType);
        planet.Temperature = EstimatePlanetTemperature(star, distance);
        planet.OrbitalPeriod = Math.Sqrt(Math.Pow(distance, 3) / star.Mass); // Kepler's third law
        
        return planet;
    }
    
    /// <summary>
    /// Generate a binary planet companion
    /// </summary>
    private Planet GenerateBinaryPlanet(Planet primary, long seed, Random rng)
    {
        // Get next planet number - binary companion gets next sequential number
        var parentStar = primary.Parent as Star;
        var primaryIndex = int.Parse(primary.Name.Split(' ').Last());
        var nextNumber = primaryIndex + 1;
        
        var companion = new Planet
        {
            Id = $"{primary.Id.Split(' ')[0]} {nextNumber}",
            Name = $"{primary.Id.Split(' ')[0]} {nextNumber}",
            PlanetType = primary.PlanetType,
            Mass = primary.Mass * (0.3 + rng.NextDouble() * 0.6), // 30-90% of primary mass
            OrbitalDistance = primary.OrbitalDistance, // Same distance from star
            Parent = primary.Parent
        };
        
        companion.Radius = CalculatePlanetRadius(companion.Mass, companion.PlanetType);
        companion.Temperature = primary.Temperature;
        companion.OrbitalPeriod = primary.OrbitalPeriod;
        
        return companion;
    }
    
    /// <summary>
    /// Generate moons for a planet
    /// </summary>
    private void GenerateMoons(Planet planet, long seed, Random rng)
    {
        // Rocky planets have fewer moons
        int maxMoons = planet.PlanetType == PlanetType.Rocky ? 2 : 
                      planet.Mass > 100 ? 20 : 8; // Gas giants can have many moons
        
        var moonCount = rng.Next(0, maxMoons + 1);
        var letters = "abcdefghijklmnopqrstuvwxyz";
        
        for (int i = 0; i < moonCount; i++)
        {
            var moon = new Moon
            {
                Id = $"{planet.Id}{letters[i]}",
                Name = $"{planet.Name}{letters[i]}",
                OrbitalDistance = 0.001 + rng.NextDouble() * 0.01 // Very close, in AU
            };
            
            // Moon properties based on planet type
            if (planet.PlanetType == PlanetType.Gas)
            {
                moon.Composition = rng.NextDouble() < 0.7 ? PlanetType.Ice : PlanetType.Rocky;
                moon.Mass = 0.00001 + rng.NextDouble() * 0.01; // Tiny to Ganymede-sized
            }
            else
            {
                moon.Composition = PlanetType.Rocky;
                moon.Mass = 0.000001 + rng.NextDouble() * 0.001; // Very small
            }
            
            moon.Radius = Math.Pow(moon.Mass / 0.0123, 0.33); // Relative to our Moon
            planet.Children.Add(moon);
            moon.Parent = planet;
            
            // Binary moon (very rare)
            if (rng.NextDouble() < 0.01 && i < moonCount - 1) // 1% chance
            {
                var binaryMoon = new Moon
                {
                    Id = $"{planet.Id}{letters[i + 1]}",
                    Name = $"{planet.Name}{letters[i + 1]}",
                    Composition = moon.Composition,
                    Mass = moon.Mass * (0.4 + rng.NextDouble() * 0.5), // 40-90% of primary
                    OrbitalDistance = moon.OrbitalDistance
                };
                
                binaryMoon.Radius = Math.Pow(binaryMoon.Mass / 0.0123, 0.33);
                moon.BinaryCompanion = binaryMoon;
                binaryMoon.BinaryCompanion = moon;
                moon.IsBinary = true;
                binaryMoon.IsBinary = true;
                binaryMoon.Parent = planet;
                
                // Add binary moon to children so it gets a place in the list
                planet.Children.Add(binaryMoon);
                
                // Skip next iteration since we used the next letter
                i++;
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private bool CanHavePlanets(ScientificMilkyWayGenerator.StellarType type)
    {
        return type != ScientificMilkyWayGenerator.StellarType.K0III &&
               type != ScientificMilkyWayGenerator.StellarType.K5III &&
               type != ScientificMilkyWayGenerator.StellarType.M0III &&
               type != ScientificMilkyWayGenerator.StellarType.B0III &&
               type != ScientificMilkyWayGenerator.StellarType.M2I &&
               type != ScientificMilkyWayGenerator.StellarType.B0I &&
               type != ScientificMilkyWayGenerator.StellarType.DA &&
               type != ScientificMilkyWayGenerator.StellarType.NS &&
               type != ScientificMilkyWayGenerator.StellarType.BH &&
               type != ScientificMilkyWayGenerator.StellarType.SMBH;
    }
    
    private int DeterminePlanetCount(Star star, Random rng)
    {
        // Fewer planets for very hot or very cool stars
        if (star.Temperature > 10000 || star.Temperature < 3000) 
            return rng.Next(0, 3);
        
        // Sun-like stars have more planets
        if (star.Temperature > 5000 && star.Temperature < 6500)
            return rng.Next(2, 9);
        
        return rng.Next(1, 6);
    }
    
    private double CalculateStellarRadius(double mass, double temperature)
    {
        // Stefan-Boltzmann law approximation
        var luminosity = Math.Pow(mass, 3.5);
        return Math.Sqrt(luminosity) * Math.Pow(5778 / temperature, 2);
    }
    
    private double CalculatePlanetRadius(double mass, PlanetType type)
    {
        return type switch
        {
            PlanetType.Rocky => Math.Pow(mass, 0.27), // Earth radii
            PlanetType.Gas => 4.0 + Math.Log10(mass), // Approximate for gas giants
            PlanetType.Ice => Math.Pow(mass, 0.33), // Between rocky and gas
            _ => 1.0
        };
    }
    
    private double EstimatePlanetTemperature(Star star, double distance)
    {
        // Simple equilibrium temperature
        return 278 * Math.Pow(star.Luminosity / Math.Pow(distance, 2), 0.25);
    }
    
    private ScientificMilkyWayGenerator.StellarType DetermineStellarTypeFromMass(double mass, Random rng)
    {
        if (mass > 2.0) return ScientificMilkyWayGenerator.StellarType.B5V;
        if (mass > 1.5) return ScientificMilkyWayGenerator.StellarType.A0V;
        if (mass > 1.2) return ScientificMilkyWayGenerator.StellarType.F0V;
        if (mass > 0.9) return ScientificMilkyWayGenerator.StellarType.G0V;
        if (mass > 0.7) return ScientificMilkyWayGenerator.StellarType.K0V;
        if (mass > 0.3) return ScientificMilkyWayGenerator.StellarType.M0V;
        return ScientificMilkyWayGenerator.StellarType.M5V;
    }
    
    private double EstimateTemperatureFromType(ScientificMilkyWayGenerator.StellarType type)
    {
        return type switch
        {
            ScientificMilkyWayGenerator.StellarType.O5V => 42000,
            ScientificMilkyWayGenerator.StellarType.B0V => 30000,
            ScientificMilkyWayGenerator.StellarType.B5V => 15400,
            ScientificMilkyWayGenerator.StellarType.A0V => 9520,
            ScientificMilkyWayGenerator.StellarType.F0V => 7200,
            ScientificMilkyWayGenerator.StellarType.G0V => 6000,
            ScientificMilkyWayGenerator.StellarType.K0V => 5250,
            ScientificMilkyWayGenerator.StellarType.M0V => 3850,
            ScientificMilkyWayGenerator.StellarType.M5V => 3170,
            _ => 5778
        };
    }
    
    #endregion
    
    /// <summary>
    /// Display the complete system tree
    /// </summary>
    public static string GetSystemTreeDisplay(Star rootStar)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\n═══ System Tree for {rootStar.Id} ═══");
        sb.Append(rootStar.GetTreeDisplay());
        return sb.ToString();
    }
}