using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generates planetary systems for stars based on leftover mass from star formation
/// </summary>
public class PlanetarySystemGenerator
{
    public enum PlanetType
    {
        Rocky,
        Gas,
        Ice
    }
    
    public class Planet
    {
        public int Index { get; set; }
        public PlanetType Type { get; set; }
        public float Mass { get; set; } // Earth masses
        public float OrbitalDistance { get; set; } // AU
        public List<Moon> Moons { get; set; } = new List<Moon>();
    }
    
    public class Moon
    {
        public string Letter { get; set; } = "";
        public PlanetType Type { get; set; }
        public float Mass { get; set; } // Earth masses
        public int ParentPlanetIndex { get; set; }
    }
    
    public class PlanetarySystem
    {
        public long StarSeed { get; set; }
        public string StarName { get; set; } = "";
        public ScientificMilkyWayGenerator.StellarType StarType { get; set; }
        public float StarMass { get; set; }
        public List<Planet> Planets { get; set; } = new List<Planet>();
    }
    
    /// <summary>
    /// Generate a planetary system based on star properties
    /// </summary>
    public PlanetarySystem GeneratePlanetarySystem(long starSeed, ScientificMilkyWayGenerator.StellarType stellarType, float stellarMass, string starName)
    {
        var system = new PlanetarySystem
        {
            StarSeed = starSeed,
            StarName = starName,
            StarType = stellarType,
            StarMass = stellarMass
        };
        
        // No planets for certain stellar types
        if (stellarType == ScientificMilkyWayGenerator.StellarType.M2I ||
            stellarType == ScientificMilkyWayGenerator.StellarType.B0I ||
            stellarType == ScientificMilkyWayGenerator.StellarType.SMBH)
        {
            return system;
        }
        
        var rng = new Random((int)(starSeed % int.MaxValue));
        
        // Calculate leftover mass fraction based on stellar type and mass
        float leftoverMassFraction = CalculateLeftoverMassFraction(stellarType, stellarMass);
        float totalPlanetMass = stellarMass * leftoverMassFraction * 333000; // Convert to Earth masses (1 solar mass = 333,000 Earth masses)
        
        // Determine number of planets
        int planetCount = DeterminePlanetCount(stellarType, rng);
        if (planetCount == 0) return system;
        
        // Calculate frost line distance
        float frostLine = CalculateFrostLine(stellarMass);
        
        // Generate planets
        float remainingMass = totalPlanetMass;
        float currentDistance = 0.1f + 0.3f * (float)rng.NextDouble(); // Start between 0.1-0.4 AU
        
        for (int i = 0; i < planetCount && remainingMass > 0.1f; i++)
        {
            var planet = new Planet { Index = i + 1 };
            
            // Orbital distance using modified Titius-Bode law
            if (i == 0)
            {
                planet.OrbitalDistance = currentDistance;
            }
            else
            {
                currentDistance *= 1.4f + 0.4f * (float)rng.NextDouble(); // 1.4-1.8x spacing
                planet.OrbitalDistance = currentDistance;
            }
            
            // Determine planet type based on position relative to frost line
            if (planet.OrbitalDistance < frostLine * 0.8f)
            {
                planet.Type = PlanetType.Rocky;
                planet.Mass = 0.1f + 2f * (float)rng.NextDouble(); // 0.1-2.1 Earth masses
            }
            else if (planet.OrbitalDistance < frostLine * 2f)
            {
                // Transition zone - can be any type
                var typeRoll = rng.NextDouble();
                if (typeRoll < 0.3)
                {
                    planet.Type = PlanetType.Rocky;
                    planet.Mass = 0.5f + 1.5f * (float)rng.NextDouble(); // 0.5-2 Earth masses
                }
                else if (typeRoll < 0.6)
                {
                    planet.Type = PlanetType.Ice;
                    planet.Mass = 5f + 20f * (float)rng.NextDouble(); // 5-25 Earth masses
                }
                else
                {
                    planet.Type = PlanetType.Gas;
                    planet.Mass = 10f + 300f * (float)rng.NextDouble(); // 10-310 Earth masses
                }
            }
            else
            {
                // Beyond frost line - gas or ice giants
                if (rng.NextDouble() < 0.6)
                {
                    planet.Type = PlanetType.Gas;
                    planet.Mass = 20f + 280f * (float)rng.NextDouble(); // 20-300 Earth masses
                }
                else
                {
                    planet.Type = PlanetType.Ice;
                    planet.Mass = 10f + 15f * (float)rng.NextDouble(); // 10-25 Earth masses
                }
            }
            
            // Scale mass by available leftover mass
            float massFraction = Math.Min(planet.Mass / remainingMass, 0.5f); // No single planet takes more than 50%
            planet.Mass *= massFraction;
            remainingMass -= planet.Mass;
            
            // Generate moons for larger planets
            GenerateMoons(planet, rng);
            
            system.Planets.Add(planet);
        }
        
        return system;
    }
    
    /// <summary>
    /// Calculate leftover mass fraction based on stellar properties
    /// </summary>
    private float CalculateLeftoverMassFraction(ScientificMilkyWayGenerator.StellarType type, float mass)
    {
        // Base fractions by type
        float baseFraction = type switch
        {
            ScientificMilkyWayGenerator.StellarType.M0V or ScientificMilkyWayGenerator.StellarType.M5V or ScientificMilkyWayGenerator.StellarType.M8V => 0.05f,  // 5% for M dwarfs
            ScientificMilkyWayGenerator.StellarType.K0V or ScientificMilkyWayGenerator.StellarType.K5V => 0.04f,  // 4% for K dwarfs
            ScientificMilkyWayGenerator.StellarType.G0V or ScientificMilkyWayGenerator.StellarType.G5V => 0.03f,  // 3% for G dwarfs
            ScientificMilkyWayGenerator.StellarType.F0V or ScientificMilkyWayGenerator.StellarType.F5V => 0.02f,  // 2% for F stars
            ScientificMilkyWayGenerator.StellarType.A0V or ScientificMilkyWayGenerator.StellarType.A5V => 0.01f,  // 1% for A stars
            ScientificMilkyWayGenerator.StellarType.B0V or ScientificMilkyWayGenerator.StellarType.B5V => 0.005f, // 0.5% for B stars
            ScientificMilkyWayGenerator.StellarType.O5V => 0.001f, // 0.1% for O stars
            _ => 0.0f
        };
        
        // Scale inversely with mass (more massive stars have stronger winds)
        float massScale = 1.0f / (1.0f + mass);
        
        return baseFraction * massScale;
    }
    
    /// <summary>
    /// Determine number of planets based on stellar type
    /// </summary>
    private int DeterminePlanetCount(ScientificMilkyWayGenerator.StellarType type, Random rng)
    {
        return type switch
        {
            ScientificMilkyWayGenerator.StellarType.M0V or ScientificMilkyWayGenerator.StellarType.M5V or ScientificMilkyWayGenerator.StellarType.M8V => rng.Next(0, 4),    // 0-3 planets
            ScientificMilkyWayGenerator.StellarType.K0V or ScientificMilkyWayGenerator.StellarType.K5V => rng.Next(2, 7),    // 2-6 planets
            ScientificMilkyWayGenerator.StellarType.G0V or ScientificMilkyWayGenerator.StellarType.G5V => rng.Next(3, 9),    // 3-8 planets
            ScientificMilkyWayGenerator.StellarType.F0V or ScientificMilkyWayGenerator.StellarType.F5V => rng.Next(2, 6),    // 2-5 planets
            ScientificMilkyWayGenerator.StellarType.A0V or ScientificMilkyWayGenerator.StellarType.A5V => rng.Next(0, 3),    // 0-2 planets
            ScientificMilkyWayGenerator.StellarType.B0V or ScientificMilkyWayGenerator.StellarType.B5V => rng.Next(0, 2),    // 0-1 planets
            ScientificMilkyWayGenerator.StellarType.O5V => rng.NextDouble() < 0.1 ? 1 : 0, // 10% chance of 1 planet
            _ => 0
        };
    }
    
    /// <summary>
    /// Calculate frost line distance based on stellar luminosity
    /// </summary>
    private float CalculateFrostLine(float stellarMass)
    {
        // Approximate luminosity from mass (L ∝ M^3.5)
        float luminosity = (float)Math.Pow(stellarMass, 3.5);
        
        // Frost line ∝ sqrt(L)
        return 2.7f * (float)Math.Sqrt(luminosity); // ~2.7 AU for Sun
    }
    
    /// <summary>
    /// Generate moons for a planet
    /// </summary>
    private void GenerateMoons(Planet planet, Random rng)
    {
        int moonCount = 0;
        
        // Determine moon count based on planet type and mass
        if (planet.Type == PlanetType.Gas && planet.Mass > 50)
        {
            moonCount = rng.Next(10, 30); // Many moons for large gas giants
        }
        else if (planet.Type == PlanetType.Gas)
        {
            moonCount = rng.Next(3, 10);
        }
        else if (planet.Type == PlanetType.Ice)
        {
            moonCount = rng.Next(2, 8);
        }
        else if (planet.Type == PlanetType.Rocky && planet.Mass > 0.8f)
        {
            moonCount = rng.Next(0, 3); // 0-2 moons for Earth-sized rocky planets
        }
        else
        {
            moonCount = rng.NextDouble() < 0.3 ? 1 : 0; // 30% chance of 1 moon for small rocky planets
        }
        
        for (int i = 0; i < moonCount; i++)
        {
            var moon = new Moon
            {
                Letter = ((char)('a' + i)).ToString(),
                ParentPlanetIndex = planet.Index
            };
            
            // Moon type and mass based on planet
            if (planet.Type == PlanetType.Gas)
            {
                moon.Type = rng.NextDouble() < 0.7 ? PlanetType.Ice : PlanetType.Rocky;
                moon.Mass = 0.01f + 0.1f * (float)rng.NextDouble() * planet.Mass / 100f;
            }
            else
            {
                moon.Type = PlanetType.Rocky;
                moon.Mass = 0.001f + 0.02f * (float)rng.NextDouble() * planet.Mass;
            }
            
            planet.Moons.Add(moon);
        }
    }
    
    /// <summary>
    /// Get planet details by index (1-based)
    /// </summary>
    public Planet? GetPlanetDetails(PlanetarySystem system, int planetIndex)
    {
        if (planetIndex < 1 || planetIndex > system.Planets.Count)
            return null;
            
        return system.Planets[planetIndex - 1];
    }
    
    /// <summary>
    /// Get moon details by identifier (e.g., "3-a" for first moon of third planet)
    /// </summary>
    public Moon? GetMoonDetails(PlanetarySystem system, string moonId)
    {
        var parts = moonId.Split('-');
        if (parts.Length != 2)
            return null;
            
        if (!int.TryParse(parts[0], out int planetIndex))
            return null;
            
        var planet = GetPlanetDetails(system, planetIndex);
        if (planet == null)
            return null;
            
        return planet.Moons.FirstOrDefault(m => m.Letter == parts[1]);
    }
}