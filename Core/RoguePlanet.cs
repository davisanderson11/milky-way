using System;

/// <summary>
/// Represents a rogue planet - a planetary-mass object not bound to any star
/// </summary>
/// 
namespace MilkyWay.Core
{
    public class RoguePlanet
    {
        public long Seed { get; set; }
        public GalaxyGenerator.Vector3 Position { get; set; }
        public float Mass { get; set; } // In Jupiter masses (0.1 - 13)
        public float Temperature { get; set; } // Very cold, typically 50-200K
        public int MoonCount { get; set; }
        public UnifiedSystemGenerator.PlanetType Type { get; set; }
        public string Origin { get; set; } = "Unknown"; // "Ejected" or "Formed"
        public float Radius { get; set; } // In Earth radii

        // For chunk-based system
        public int ChunkR { get; set; }
        public int ChunkTheta { get; set; }
        public int ChunkZ { get; set; }
        public int Index { get; set; } // Negative value for rogue planets

        /// <summary>
        /// Generate properties for a rogue planet
        /// </summary>
        public static RoguePlanet Generate(long seed, GalaxyGenerator.Vector3 position, int chunkR, int chunkTheta, int chunkZ, int index)
        {
            var rng = new Random((int)(seed % int.MaxValue));
            var rogue = new RoguePlanet
            {
                Seed = seed,
                Position = position,
                ChunkR = chunkR,
                ChunkTheta = chunkTheta,
                ChunkZ = chunkZ,
                Index = -Math.Abs(index) // Ensure negative index
            };

            // Determine type and mass - rogue planets are likely ejected, so follow similar distribution
            // but with bias toward larger planets (easier to detect, more likely to survive ejection)
            var typeRoll = rng.NextDouble();

            if (typeRoll < 0.15) // 15% Jupiter-type
            {
                rogue.Type = UnifiedSystemGenerator.PlanetType.Jupiter;
                // Jupiter-type: 0.3-13 Jupiter masses
                rogue.Mass = 0.3f + (float)(rng.NextDouble() * 12.7);
                // Radius using mass-radius relation for gas giants
                rogue.Radius = 11.2f * (float)Math.Pow(rogue.Mass / 1.0, 0.5); // Earth radii
            }
            else if (typeRoll < 0.35) // 20% Neptune-type
            {
                rogue.Type = UnifiedSystemGenerator.PlanetType.Neptune;
                // Neptune-type: 0.01-0.3 Jupiter masses (10-300 Earth masses)
                rogue.Mass = 0.01f + (float)(rng.NextDouble() * 0.29);
                rogue.Radius = 3.9f * (float)Math.Pow(rogue.Mass / 0.0537, 0.35);
            }
            else if (typeRoll < 0.5) // 15% Terra-type
            {
                rogue.Type = UnifiedSystemGenerator.PlanetType.Terra;
                // Terra-type: 0.5-5 Earth masses
                float massEarth = 0.5f + (float)(rng.NextDouble() * 4.5);
                rogue.Mass = massEarth * 0.00315f; // Convert to Jupiter masses
                rogue.Radius = (float)Math.Pow(massEarth, 0.27); // Earth radii
            }
            else if (typeRoll < 0.65) // 15% Aquaria-type (frozen ocean worlds)
            {
                rogue.Type = UnifiedSystemGenerator.PlanetType.Aquaria;
                // Aquaria-type: 0.5-3 Earth masses
                float massEarth = 0.5f + (float)(rng.NextDouble() * 2.5);
                rogue.Mass = massEarth * 0.00315f;
                rogue.Radius = (float)Math.Pow(massEarth, 0.3); // Slightly larger due to ice
            }
            else if (typeRoll < 0.75) // 10% Selena-type
            {
                rogue.Type = UnifiedSystemGenerator.PlanetType.Selena;
                // Selena-type: 0.01-0.5 Earth masses
                float massEarth = 0.01f + (float)(rng.NextDouble() * 0.49);
                rogue.Mass = massEarth * 0.00315f;
                rogue.Radius = (float)Math.Pow(massEarth, 0.25);
            }
            else if (typeRoll < 0.85) // 10% Ferria-type
            {
                rogue.Type = UnifiedSystemGenerator.PlanetType.Ferria;
                // Ferria-type: 0.1-2 Earth masses
                float massEarth = 0.1f + (float)(rng.NextDouble() * 1.9);
                rogue.Mass = massEarth * 0.00315f;
                rogue.Radius = (float)Math.Pow(massEarth, 0.25); // Dense iron planets
            }
            else // 15% Carbonia-type
            {
                rogue.Type = UnifiedSystemGenerator.PlanetType.Carbonia;
                // Carbonia-type: 0.5-3 Earth masses
                float massEarth = 0.5f + (float)(rng.NextDouble() * 2.5);
                rogue.Mass = massEarth * 0.00315f;
                rogue.Radius = (float)Math.Pow(massEarth, 0.28); // Less dense than iron
            }

            // Temperature - very cold without a star
            // Base temperature from cosmic microwave background + internal heat
            var baseTemp = 2.7f; // CMB temperature
            float internalHeat = 0;

            // Gas giants and ice giants have more internal heat
            if (rogue.Type == UnifiedSystemGenerator.PlanetType.Jupiter)
            {
                internalHeat = 50f + (float)(rng.NextDouble() * 100); // 50-150K internal
            }
            else if (rogue.Type == UnifiedSystemGenerator.PlanetType.Neptune)
            {
                internalHeat = 30f + (float)(rng.NextDouble() * 50); // 30-80K internal
            }
            else
            {
                // Rocky planets have minimal internal heat
                internalHeat = 5f + (float)(rng.NextDouble() * 20); // 5-25K internal
            }

            rogue.Temperature = baseTemp + internalHeat;

            // Origin - more massive ones are likely ejected, smaller ones might have formed alone
            if (rogue.Mass > 0.1f && rng.NextDouble() < 0.8)
            {
                rogue.Origin = "Ejected";
            }
            else
            {
                rogue.Origin = "Formed";
            }

            // Moons - rare but possible, especially for larger rogue planets
            // Ejected planets may retain some moons
            if (rogue.Type == UnifiedSystemGenerator.PlanetType.Jupiter)
            {
                if (rng.NextDouble() < 0.4) // 40% chance
                    rogue.MoonCount = 1 + rng.Next(0, 4); // 1-4 moons
            }
            else if (rogue.Type == UnifiedSystemGenerator.PlanetType.Neptune)
            {
                if (rng.NextDouble() < 0.25) // 25% chance
                    rogue.MoonCount = 1 + rng.Next(0, 2); // 1-2 moons
            }
            else if (rogue.Mass > 0.001f) // Larger than ~0.3 Earth masses
            {
                if (rng.NextDouble() < 0.1) // 10% chance
                    rogue.MoonCount = 1; // Single moon
            }
            else
            {
                rogue.MoonCount = 0;
            }

            return rogue;
        }

        public override string ToString()
        {
            float massEarth = Mass / 0.00315f; // Convert to Earth masses for display
            string massStr = massEarth < 10 ? $"{massEarth:F2} M⊕" : $"{Mass:F3} MJ";
            return $"Rogue {Type} - Mass: {massStr}, Radius: {Radius:F1} R⊕, Temp: {Temperature:F0}K, Origin: {Origin}, Moons: {MoonCount}";
        }
    }
}