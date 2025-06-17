using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Unified system generator - Improved hierarchical stellar and planetary systems
/// Clearer relationships between binaries, satellites, planets, and moons
/// </summary>
public class UnifiedSystemGenerator
{
    #region Enums and Classes
    
    public enum ObjectType
    {
        Star,
        Planet,
        Moon
    }
    
    public enum PlanetType
    {
        Ferria,    // Iron-rich rocky planets (Mercury-like)
        Carbonia,  // Carbon-rich rocky planets
        Aquaria,   // Ocean worlds with substantial water
        Terra,     // Earth-like rocky planets with moderate volatiles
        Selena,    // Airless rocky worlds (Moon-like)
        Neptune,   // Ice giants (Neptune/Uranus-like)
        Jupiter    // Gas giants (Jupiter/Saturn-like)
    }
    
    public enum StarRelationship
    {
        Primary,      // The main star
        Binary,       // Close binary companion
        Satellite     // Distant companion star
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
        
        public abstract string GetTreeDisplay(int indent = 0, bool isLast = false);
    }
    
    /// <summary>
    /// Star object - can have companion stars or planets
    /// </summary>
    public class Star : SystemObject
    {
        public StellarTypeGenerator.StellarType StellarType { get; set; }
        public double Temperature { get; set; }
        public double Luminosity { get; set; }
        public double Radius { get; set; }
        public StarRelationship Relationship { get; set; }
        public Star? BinaryCompanion { get; set; } // Direct binary companion
        
        public Star()
        {
            Type = ObjectType.Star;
        }
        
        public override string GetTreeDisplay(int indent = 0, bool isLast = false)
        {
            var sb = new StringBuilder();
            var prefix = new string(' ', indent);
            var connector = isLast ? "└─" : "├─";
            var typeStr = StellarType.ToString();
            
            // Display star info
            var relStr = Relationship == StarRelationship.Binary ? " (binary)" : 
                        Relationship == StarRelationship.Satellite ? " (satellite)" : "";
            sb.AppendLine($"{prefix}{connector} {Name} [{typeStr}, {Mass:F3} M☉, {Temperature:F0}K]{relStr}");
            
            // Display orbital info for non-primary stars
            if (Relationship != StarRelationship.Primary && OrbitalDistance > 0)
            {
                var childPrefix = new string(' ', indent + 3);
                sb.AppendLine($"{childPrefix}Separation: {OrbitalDistance:F2} AU");
            }
            
            // Show planets
            var planets = Children.Where(c => c is Planet).ToList();
            if (planets.Any())
            {
                var childPrefix = new string(' ', indent + 3);
                sb.AppendLine($"{childPrefix}Planets ({planets.Count}):");
                
                for (int i = 0; i < planets.Count; i++)
                {
                    sb.Append(planets[i].GetTreeDisplay(indent + 6, i == planets.Count - 1));
                }
            }
            
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Planet object - can have moons
    /// </summary>
    public class Planet : SystemObject
    {
        public PlanetType PlanetType { get; set; }
        public double Radius { get; set; } // Earth radii
        public double Temperature { get; set; } // Kelvin
        public bool HasRings { get; set; }
        public Planet? BinaryCompanion { get; set; } // For binary planets
        
        public Planet()
        {
            Type = ObjectType.Planet;
        }
        
        public override string GetTreeDisplay(int indent = 0, bool isLast = false)
        {
            var sb = new StringBuilder();
            var prefix = new string(' ', indent);
            var connector = isLast ? "└─" : "├─";
            
            var binaryStr = BinaryCompanion != null ? " (binary)" : "";
            sb.AppendLine($"{prefix}{connector} Planet {Name} [{PlanetType}, {Mass:F2} M⊕, {OrbitalDistance:F2} AU]{binaryStr}");
            
            // Show moons
            var moons = Children.Where(c => c is Moon).ToList();
            if (moons.Any())
            {
                var childPrefix = new string(' ', indent + 3);
                sb.AppendLine($"{childPrefix}Moons ({moons.Count}):");
                
                for (int i = 0; i < moons.Count; i++)
                {
                    sb.Append(moons[i].GetTreeDisplay(indent + 6, i == moons.Count - 1));
                }
            }
            
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Moon object
    /// </summary>
    public class Moon : SystemObject
    {
        public PlanetType Composition { get; set; }
        public double Radius { get; set; } // Moon radii (relative to our Moon)
        public Moon? BinaryCompanion { get; set; } // For binary moons
        
        public Moon()
        {
            Type = ObjectType.Moon;
        }
        
        public override string GetTreeDisplay(int indent = 0, bool isLast = false)
        {
            var sb = new StringBuilder();
            var prefix = new string(' ', indent);
            var connector = isLast ? "└─" : "├─";
            
            var binaryStr = BinaryCompanion != null ? " (binary)" : "";
            sb.AppendLine($"{prefix}{connector} Moon {Name} [{Composition}, {Mass:F4} M☾]{binaryStr}");
            
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Complete star system containing all stars and their planets/moons
    /// </summary>
    public class StarSystem
    {
        public long Seed { get; set; }
        public Star PrimaryStar { get; set; } = null!;
        public List<Star> AllStars { get; set; } = new List<Star>();
        
        public string GetFullTreeDisplay()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\n═══ Star System {Seed} ═══");
            
            // First show primary star
            sb.AppendLine("\nStars in System:");
            sb.Append(PrimaryStar.GetTreeDisplay(0, false));
            
            // Then show binary companion if exists
            if (PrimaryStar.BinaryCompanion != null)
            {
                sb.Append(PrimaryStar.BinaryCompanion.GetTreeDisplay(0, false));
            }
            
            // Then show satellite stars
            var satellites = AllStars.Where(s => s.Relationship == StarRelationship.Satellite)
                                   .OrderBy(s => s.Name).ToList();
            for (int i = 0; i < satellites.Count; i++)
            {
                sb.Append(satellites[i].GetTreeDisplay(0, i == satellites.Count - 1));
                
                // Show binary companion of satellite if exists
                if (satellites[i].BinaryCompanion != null)
                {
                    sb.Append(satellites[i].BinaryCompanion!.GetTreeDisplay(3, true));
                }
            }
            
            sb.AppendLine($"\nTotal Stars: {AllStars.Count}");
            sb.AppendLine($"Total Planets: {AllStars.Sum(s => s.Children.Count(c => c is Planet))}");
            sb.AppendLine($"Total Moons: {AllStars.SelectMany(s => s.Children.OfType<Planet>()).Sum(p => p.Children.Count)}");
            
            return sb.ToString();
        }
    }
    
    #endregion
    
    #region System Generation
    
    /// <summary>
    /// Generate a complete hierarchical system for a star
    /// </summary>
    public StarSystem GenerateSystem(long seed, StellarTypeGenerator.StellarType stellarType, 
        double stellarMass, double temperature, double luminosity)
    {
        var rng = new Random((int)(seed % int.MaxValue));
        
        var system = new StarSystem { Seed = seed };
        
        // Create primary star
        var primaryStar = new Star
        {
            Id = seed.ToString(),
            Name = "A",
            StellarType = stellarType,
            Mass = stellarMass,
            Temperature = temperature,
            Luminosity = luminosity,
            Radius = CalculateStellarRadius(stellarMass, temperature),
            Relationship = StarRelationship.Primary
        };
        
        system.PrimaryStar = primaryStar;
        system.AllStars.Add(primaryStar);
        
        // Generate stellar companions
        GenerateStellarCompanions(system, seed, rng);
        
        // Generate planetary systems for all stars
        foreach (var star in system.AllStars)
        {
            GeneratePlanetarySystem(star, seed, rng);
        }
        
        return system;
    }
    
    /// <summary>
    /// Generate stellar companions with clear binary/satellite relationships
    /// </summary>
    private void GenerateStellarCompanions(StarSystem system, long seed, Random rng)
    {
        var roll = rng.NextDouble();
        
        if (roll < 0.001) // 0.1% quadruple system (2 binary pairs)
        {
            // Primary A gets binary companion B
            var companionB = GenerateCompanionStar(system.PrimaryStar, seed, rng, "B", StarRelationship.Binary);
            companionB.OrbitalDistance = 0.05 + rng.NextDouble() * 0.5; // 0.05-0.55 AU
            system.PrimaryStar.BinaryCompanion = companionB;
            companionB.BinaryCompanion = system.PrimaryStar;
            system.AllStars.Add(companionB);
            
            // Distant binary pair C and D
            var companionC = GenerateCompanionStar(system.PrimaryStar, seed, rng, "C", StarRelationship.Satellite);
            companionC.OrbitalDistance = 100 + rng.NextDouble() * 400; // 100-500 AU from A-B
            system.AllStars.Add(companionC);
            
            var companionD = GenerateCompanionStar(companionC, seed, rng, "D", StarRelationship.Binary);
            companionD.OrbitalDistance = 0.5 + rng.NextDouble() * 2; // 0.5-2.5 AU from C
            companionC.BinaryCompanion = companionD;
            companionD.BinaryCompanion = companionC;
            system.AllStars.Add(companionD);
        }
        else if (roll < 0.01) // 0.9% triple system
        {
            var config = rng.NextDouble();
            
            if (config < 0.5) // A-B close binary + distant C
            {
                // Binary companion B
                var companionB = GenerateCompanionStar(system.PrimaryStar, seed, rng, "B", StarRelationship.Binary);
                companionB.OrbitalDistance = 0.05 + rng.NextDouble() * 0.5; // 0.05-0.55 AU
                system.PrimaryStar.BinaryCompanion = companionB;
                companionB.BinaryCompanion = system.PrimaryStar;
                system.AllStars.Add(companionB);
                
                // Distant companion C
                var companionC = GenerateCompanionStar(system.PrimaryStar, seed, rng, "C", StarRelationship.Satellite);
                companionC.OrbitalDistance = 50 + rng.NextDouble() * 450; // 50-500 AU
                system.AllStars.Add(companionC);
            }
            else // A alone + distant B-C binary
            {
                // Satellite B with binary companion C
                var companionB = GenerateCompanionStar(system.PrimaryStar, seed, rng, "B", StarRelationship.Satellite);
                companionB.OrbitalDistance = 50 + rng.NextDouble() * 450; // 50-500 AU from A
                system.AllStars.Add(companionB);
                
                var companionC = GenerateCompanionStar(companionB, seed, rng, "C", StarRelationship.Binary);
                companionC.OrbitalDistance = 0.1 + rng.NextDouble() * 1; // 0.1-1.1 AU from B
                companionB.BinaryCompanion = companionC;
                companionC.BinaryCompanion = companionB;
                system.AllStars.Add(companionC);
            }
        }
        else if (roll < 0.07) // 6% binary system
        {
            var companionB = GenerateCompanionStar(system.PrimaryStar, seed, rng, "B", StarRelationship.Binary);
            companionB.OrbitalDistance = 0.1 + rng.NextDouble() * 10; // 0.1-10.1 AU
            system.PrimaryStar.BinaryCompanion = companionB;
            companionB.BinaryCompanion = system.PrimaryStar;
            system.AllStars.Add(companionB);
        }
        // else single star system
    }
    
    /// <summary>
    /// Generate a companion star
    /// </summary>
    private Star GenerateCompanionStar(Star primary, long seed, Random rng, string designation, StarRelationship relationship)
    {
        // Mass ratio - allow for brown dwarf companions
        double mass;
        if (rng.NextDouble() < 0.15) // 15% chance of brown dwarf companion
        {
            // Brown dwarf mass range: 0.013-0.08 solar masses
            mass = 0.013 + rng.NextDouble() * 0.067;
        }
        else
        {
            // Normal stellar companion
            var massRatio = 0.1 + rng.NextDouble() * 0.8;
            mass = primary.Mass * massRatio;
        }
        
        // Determine stellar type based on mass
        var stellarType = DetermineStellarTypeFromMass(mass, rng);
        var temperature = EstimateTemperatureFromType(stellarType);
        var luminosity = Math.Pow(mass, 3.5); // Rough mass-luminosity relation
        
        var companion = new Star
        {
            Id = $"{seed}-{designation}",
            Name = designation,
            StellarType = stellarType,
            Mass = mass,
            Temperature = temperature,
            Luminosity = luminosity,
            Radius = CalculateStellarRadius(mass, temperature),
            Relationship = relationship
        };
        
        return companion;
    }
    
    /// <summary>
    /// Generate planetary system for a star
    /// </summary>
    private void GeneratePlanetarySystem(Star star, long seed, Random rng)
    {
        // Check if this star can have planets
        if (!CanHavePlanets(star.StellarType)) return;
        
        // Binary stars have reduced planet formation
        if (star.BinaryCompanion != null && rng.NextDouble() > 0.3) return;
        
        // Generate planets
        var planetCount = DeterminePlanetCount(star, rng);
        var currentDistance = 0.1 + rng.NextDouble() * 0.3; // 0.1-0.4 AU start
        
        var planetIndex = 1;
        for (int i = 0; i < planetCount; i++)
        {
            var planet = GeneratePlanet(star, seed, rng, planetIndex, currentDistance);
            star.Children.Add(planet);
            planet.Parent = star;
            planetIndex++;
            
            // Check for binary planet (rare)
            if (rng.NextDouble() < 0.02 && i < planetCount - 1) // 2% chance
            {
                var binaryPlanet = GeneratePlanet(star, seed, rng, planetIndex, currentDistance);
                binaryPlanet.Mass = planet.Mass * (0.3 + rng.NextDouble() * 0.6); // 30-90% of primary
                binaryPlanet.Radius = CalculatePlanetRadius(binaryPlanet.Mass, binaryPlanet.PlanetType);
                
                planet.BinaryCompanion = binaryPlanet;
                binaryPlanet.BinaryCompanion = planet;
                star.Children.Add(binaryPlanet);
                binaryPlanet.Parent = star;
                planetIndex++;
            }
            
            // Generate moons for the planet(s)
            GenerateMoons(planet, seed, rng);
            if (planet.BinaryCompanion != null)
            {
                GenerateMoons(planet.BinaryCompanion, seed, rng);
            }
            
            // Next planet distance
            currentDistance *= 1.4 + rng.NextDouble() * 0.4; // 1.4-1.8x spacing
        }
    }
    
    /// <summary>
    /// Generate a planet with scientifically accurate mass distributions and types
    /// </summary>
    private Planet GeneratePlanet(Star star, long seed, Random rng, int index, double distance)
    {
        var planet = new Planet
        {
            Id = $"{star.Id}-{index}",
            Name = index.ToString(),
            OrbitalDistance = distance
        };
        
        // Calculate key boundaries
        var frostLine = Math.Sqrt(star.Luminosity) * 2.7; // Snow line in AU
        var sootLine = Math.Sqrt(star.Luminosity) * 0.2;  // Carbon condensation line
        var rockLine = Math.Sqrt(star.Luminosity) * 0.7;  // Silicate condensation line
        
        // Temperature at this distance (rough estimate)
        var temp = EstimatePlanetTemperature(star, distance);
        
        // Determine planet mass using realistic occurrence rates
        double mass = GeneratePlanetMass(rng, distance, frostLine);
        planet.Mass = mass;
        
        // Determine planet type based on mass, temperature, and composition
        if (mass < 0.5) // Small rocky worlds
        {
            if (distance < sootLine)
            {
                planet.PlanetType = PlanetType.Ferria; // Iron-rich like Mercury
            }
            else if (distance < rockLine)
            {
                planet.PlanetType = PlanetType.Selena; // Airless rocky
            }
            else if (temp > 350)
            {
                planet.PlanetType = PlanetType.Carbonia; // Carbon-rich in hot zones
            }
            else
            {
                planet.PlanetType = PlanetType.Selena; // Default small rocky
            }
        }
        else if (mass < 2.0) // Earth-mass range
        {
            if (distance < rockLine * 0.5)
            {
                planet.PlanetType = PlanetType.Ferria; // Very hot, atmosphere lost
            }
            else if (distance < frostLine && temp > 250 && temp < 350)
            {
                planet.PlanetType = PlanetType.Terra; // Potentially habitable
            }
            else if (distance > frostLine && mass > 1.0)
            {
                planet.PlanetType = PlanetType.Aquaria; // Water-rich
            }
            else if (rng.NextDouble() < 0.1)
            {
                planet.PlanetType = PlanetType.Carbonia; // 10% chance of carbon world
            }
            else
            {
                planet.PlanetType = PlanetType.Terra; // Generic terrestrial
            }
        }
        else if (mass < 10.0) // Super-Earth range
        {
            if (distance < frostLine * 0.5)
            {
                // Hot super-Earths are often rocky
                planet.PlanetType = rng.NextDouble() < 0.7 ? PlanetType.Terra : PlanetType.Neptune;
            }
            else if (distance > frostLine * 1.5)
            {
                // Cold super-Earths are usually mini-Neptunes
                planet.PlanetType = PlanetType.Neptune;
            }
            else
            {
                // Transition zone - could be either
                planet.PlanetType = rng.NextDouble() < 0.4 ? PlanetType.Aquaria : PlanetType.Neptune;
            }
        }
        else if (mass < 50.0) // Neptune-mass range
        {
            planet.PlanetType = PlanetType.Neptune;
        }
        else // Jupiter-mass range
        {
            planet.PlanetType = PlanetType.Jupiter;
        }
        
        // Special case: very hot Jupiters can lose their envelopes
        if (planet.PlanetType == PlanetType.Jupiter && distance < 0.1)
        {
            if (rng.NextDouble() < 0.2) // 20% chance
            {
                planet.PlanetType = PlanetType.Neptune; // Envelope stripped
                planet.Mass *= 0.3; // Reduce mass
            }
        }
        
        planet.Radius = CalculatePlanetRadius(planet.Mass, planet.PlanetType);
        planet.Temperature = temp;
        planet.OrbitalPeriod = Math.Sqrt(Math.Pow(distance, 3) / star.Mass); // Kepler's third law
        
        return planet;
    }
    
    /// <summary>
    /// Generate planet mass based on occurrence rates from exoplanet surveys
    /// </summary>
    private double GeneratePlanetMass(Random rng, double distance, double frostLine)
    {
        var roll = rng.NextDouble();
        
        if (distance < 0.1) // Very hot zone
        {
            // Hot Jupiters are rare but do exist
            if (roll < 0.01) return 100 + rng.NextDouble() * 900; // 100-1000 Earth masses
            else if (roll < 0.05) return 10 + rng.NextDouble() * 40; // Hot Neptunes
            else if (roll < 0.15) return 2 + rng.NextDouble() * 8; // Hot super-Earths
            else return 0.1 + rng.NextDouble() * 2; // Hot rocky planets
        }
        else if (distance < frostLine) // Inner system
        {
            // Based on Kepler occurrence rates
            if (roll < 0.02) return 100 + rng.NextDouble() * 200; // Rare warm Jupiters
            else if (roll < 0.10) return 10 + rng.NextDouble() * 40; // Mini-Neptunes
            else if (roll < 0.35) return 1.5 + rng.NextDouble() * 8.5; // Super-Earths (common!)
            else if (roll < 0.60) return 0.5 + rng.NextDouble() * 1.5; // Earth-like
            else return 0.05 + rng.NextDouble() * 0.5; // Small rocky
        }
        else if (distance < frostLine * 3) // Outer system
        {
            // Gas giant zone
            if (roll < 0.15) return 50 + rng.NextDouble() * 350; // Jupiter-like
            else if (roll < 0.35) return 10 + rng.NextDouble() * 40; // Neptune-like
            else if (roll < 0.50) return 2 + rng.NextDouble() * 8; // Ice-rich super-Earths
            else return 0.1 + rng.NextDouble() * 2; // Small icy bodies
        }
        else // Far outer system
        {
            // Distant ice giants and dwarfs
            if (roll < 0.05) return 50 + rng.NextDouble() * 150; // Distant giants (rare)
            else if (roll < 0.20) return 10 + rng.NextDouble() * 40; // Ice giants
            else if (roll < 0.40) return 1 + rng.NextDouble() * 9; // Large icy bodies
            else return 0.01 + rng.NextDouble() * 1; // Small icy bodies
        }
    }
    
    /// <summary>
    /// Generate moons for a planet
    /// </summary>
    private void GenerateMoons(Planet planet, long seed, Random rng)
    {
        // Determine max moons based on planet type and mass
        int maxMoons = planet.PlanetType switch
        {
            PlanetType.Jupiter => (int)(5 + Math.Sqrt(planet.Mass) * 0.5), // Many moons
            PlanetType.Neptune => (int)(2 + Math.Sqrt(planet.Mass) * 0.3), // Moderate moons
            PlanetType.Terra => planet.Mass > 0.8 ? 2 : 1,    // Earth can have 1-2
            PlanetType.Aquaria => planet.Mass > 2 ? 3 : 1,    // Ocean worlds might capture more
            PlanetType.Ferria => 0,                           // Too close to star usually
            PlanetType.Carbonia => 1,                         // Rare to have moons
            PlanetType.Selena => 0,                           // Small bodies rarely have moons
            _ => 1
        };
        
        var moonCount = rng.Next(0, maxMoons + 1);
        var letters = "abcdefghijklmnopqrstuvwxyz";
        
        var moonIndex = 0;
        for (int i = 0; i < moonCount; i++)
        {
            var moon = new Moon
            {
                Id = $"{planet.Id}-{letters[moonIndex]}",
                Name = letters[moonIndex].ToString(),
                OrbitalDistance = 0.001 + rng.NextDouble() * 0.01 // Very close, in AU
            };
            
            // Moon properties based on planet type and location
            if (planet.PlanetType == PlanetType.Jupiter || planet.PlanetType == PlanetType.Neptune)
            {
                // Gas/ice giant moons
                if (rng.NextDouble() < 0.3)
                {
                    // Large moon (like Galilean satellites)
                    moon.Mass = 0.001 + rng.NextDouble() * 0.025; // Up to Ganymede-sized
                    moon.Composition = planet.OrbitalDistance > 5 ? PlanetType.Aquaria : 
                                      rng.NextDouble() < 0.5 ? PlanetType.Selena : PlanetType.Aquaria;
                }
                else
                {
                    // Small moon
                    moon.Mass = 0.00001 + rng.NextDouble() * 0.001;
                    moon.Composition = PlanetType.Selena; // Small moons are usually just rock/ice
                }
            }
            else
            {
                // Terrestrial planet moons (rare and small)
                moon.Composition = PlanetType.Selena; // Airless bodies
                moon.Mass = 0.000001 + rng.NextDouble() * 0.02; // Up to Moon-sized (rare)
            }
            
            moon.Radius = Math.Pow(moon.Mass / 0.0123, 0.33); // Relative to our Moon
            planet.Children.Add(moon);
            moon.Parent = planet;
            moonIndex++;
            
            // Binary moon (very rare)
            if (rng.NextDouble() < 0.01 && i < moonCount - 1) // 1% chance
            {
                var binaryMoon = new Moon
                {
                    Id = $"{planet.Id}-{letters[moonIndex]}",
                    Name = letters[moonIndex].ToString(),
                    Composition = moon.Composition,
                    Mass = moon.Mass * (0.4 + rng.NextDouble() * 0.5), // 40-90% of primary
                    OrbitalDistance = moon.OrbitalDistance
                };
                
                binaryMoon.Radius = Math.Pow(binaryMoon.Mass / 0.0123, 0.33);
                moon.BinaryCompanion = binaryMoon;
                binaryMoon.BinaryCompanion = moon;
                planet.Children.Add(binaryMoon);
                binaryMoon.Parent = planet;
                moonIndex++;
                i++; // Skip next iteration
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private bool CanHavePlanets(StellarTypeGenerator.StellarType type)
    {
        return type != StellarTypeGenerator.StellarType.K0III &&
               type != StellarTypeGenerator.StellarType.K5III &&
               type != StellarTypeGenerator.StellarType.M0III &&
               type != StellarTypeGenerator.StellarType.B0III &&
               type != StellarTypeGenerator.StellarType.M2I &&
               type != StellarTypeGenerator.StellarType.B0I &&
               type != StellarTypeGenerator.StellarType.L0 &&     // Brown dwarfs don't have planets
               type != StellarTypeGenerator.StellarType.L5 &&
               type != StellarTypeGenerator.StellarType.T0 &&
               type != StellarTypeGenerator.StellarType.T5 &&
               type != StellarTypeGenerator.StellarType.Y0 &&
               type != StellarTypeGenerator.StellarType.DA &&
               type != StellarTypeGenerator.StellarType.NS &&
               type != StellarTypeGenerator.StellarType.BH &&
               type != StellarTypeGenerator.StellarType.SMBH;
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
        // Based on empirical mass-radius relationships from exoplanet data
        return type switch
        {
            PlanetType.Ferria => Math.Pow(mass, 0.26),        // Dense iron-rich (like Mercury)
            PlanetType.Selena => Math.Pow(mass, 0.28),        // Airless rocky (like Moon)
            PlanetType.Terra => mass < 2 ? Math.Pow(mass, 0.28) : Math.Pow(mass, 0.59), // Earth-like with transition
            PlanetType.Carbonia => Math.Pow(mass, 0.30),      // Less dense carbon-rich
            PlanetType.Aquaria => Math.Pow(mass, 0.35),       // Lower density due to water/ice
            PlanetType.Neptune => 2.0 + 0.6 * Math.Log10(mass), // Ice giant scaling
            PlanetType.Jupiter => mass < 100 ? 3.0 + 0.5 * Math.Log10(mass) : 
                                 9.0 + 0.1 * Math.Log10(mass/100), // Gas giant with saturation
            _ => 1.0
        };
    }
    
    private double EstimatePlanetTemperature(Star star, double distance)
    {
        // Simple equilibrium temperature
        return 278 * Math.Pow(star.Luminosity / Math.Pow(distance, 2), 0.25);
    }
    
    private StellarTypeGenerator.StellarType DetermineStellarTypeFromMass(double mass, Random rng)
    {
        if (mass > 2.0) return StellarTypeGenerator.StellarType.B5V;
        if (mass > 1.5) return StellarTypeGenerator.StellarType.A0V;
        if (mass > 1.2) return StellarTypeGenerator.StellarType.F0V;
        if (mass > 0.9) return StellarTypeGenerator.StellarType.G0V;
        if (mass > 0.7) return StellarTypeGenerator.StellarType.K0V;
        if (mass > 0.3) return StellarTypeGenerator.StellarType.M0V;
        if (mass > 0.08) return StellarTypeGenerator.StellarType.M5V;
        
        // Brown dwarf range (0.013 - 0.08 solar masses)
        if (mass > 0.075) return StellarTypeGenerator.StellarType.L0;
        if (mass > 0.065) return StellarTypeGenerator.StellarType.L5;
        if (mass > 0.050) return StellarTypeGenerator.StellarType.T0;
        if (mass > 0.030) return StellarTypeGenerator.StellarType.T5;
        return StellarTypeGenerator.StellarType.Y0;
    }
    
    private double EstimateTemperatureFromType(StellarTypeGenerator.StellarType type)
    {
        return type switch
        {
            StellarTypeGenerator.StellarType.O5V => 42000,
            StellarTypeGenerator.StellarType.B0V => 30000,
            StellarTypeGenerator.StellarType.B5V => 15400,
            StellarTypeGenerator.StellarType.A0V => 9520,
            StellarTypeGenerator.StellarType.F0V => 7200,
            StellarTypeGenerator.StellarType.G0V => 6000,
            StellarTypeGenerator.StellarType.K0V => 5250,
            StellarTypeGenerator.StellarType.M0V => 3850,
            StellarTypeGenerator.StellarType.M5V => 3170,
            StellarTypeGenerator.StellarType.L0 => 2200,
            StellarTypeGenerator.StellarType.L5 => 1700,
            StellarTypeGenerator.StellarType.T0 => 1400,
            StellarTypeGenerator.StellarType.T5 => 1000,
            StellarTypeGenerator.StellarType.Y0 => 500,
            _ => 5778
        };
    }
    
    #endregion
    
    #region Investigation Support
    
    /// <summary>
    /// Find an object in the system by its ID suffix (e.g., "A", "B", "1", "2-a")
    /// </summary>
    public SystemObject? FindObjectBySuffix(StarSystem system, string suffix)
    {
        // Check if it's a star (single letter)
        if (suffix.Length == 1 && char.IsLetter(suffix[0]) && char.IsUpper(suffix[0]))
        {
            return system.AllStars.FirstOrDefault(s => s.Name == suffix);
        }
        
        // Parse planet/moon identifiers
        var parts = suffix.Split('-');
        
        // Find the parent star (if specified)
        Star? parentStar = system.PrimaryStar;
        int partIndex = 0;
        
        // Check if first part is a star designation
        if (parts.Length > 0 && parts[0].Length == 1 && char.IsLetter(parts[0][0]) && char.IsUpper(parts[0][0]))
        {
            parentStar = system.AllStars.FirstOrDefault(s => s.Name == parts[0]);
            if (parentStar == null) return null;
            partIndex = 1;
        }
        
        // Look for planet
        if (partIndex < parts.Length && int.TryParse(parts[partIndex], out int planetNum))
        {
            var planet = parentStar.Children.OfType<Planet>()
                .FirstOrDefault(p => p.Name == planetNum.ToString());
            
            if (planet == null) return null;
            
            // Check for moon
            if (partIndex + 1 < parts.Length)
            {
                var moonLetter = parts[partIndex + 1];
                return planet.Children.OfType<Moon>()
                    .FirstOrDefault(m => m.Name == moonLetter);
            }
            
            return planet;
        }
        
        return null;
    }
    
    #endregion
}