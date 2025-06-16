using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Complete galaxy system using chunk-based seeding
/// Seeds encode: ChunkR_ChunkTheta_ChunkZ_StarIndex
/// </summary>
public class ChunkBasedGalaxySystem
{
    // Chunk dimensions
    private const int CHUNK_SIZE = 100; // 100 light years per chunk
    private const int MAX_RADIUS_CHUNKS = 1500; // 0-1499 (150,000 ly) - extended for halo
    private const int MAX_ANGULAR_CHUNKS = 360; // 0-359 degrees
    private const int MAX_Z_CHUNKS = 201; // -100 to 100 (-10000 to 10000 ly) - extended for halo
    
    // No longer need ScientificMilkyWayGenerator
    
    /// <summary>
    /// Chunk coordinate representation
    /// </summary>
    public class ChunkCoordinate
    {
        public int R { get; set; }      // 0-1499
        public int Theta { get; set; }  // 0-359
        public int Z { get; set; }      // -100 to 100
        
        public ChunkCoordinate(int r, int theta, int z)
        {
            if (r < 0 || r >= 1500)
                throw new ArgumentException($"ChunkCoordinate R must be 0-1499, got {r}");
            if (theta < 0 || theta >= 360)
                throw new ArgumentException($"ChunkCoordinate Theta must be 0-359, got {theta}");
            if (z < -100 || z > 100)
                throw new ArgumentException($"ChunkCoordinate Z must be -100 to 100, got {z}");
                
            R = r;
            Theta = theta;
            Z = z;
        }
        
        public ChunkCoordinate(string chunkId)
        {
            var parts = chunkId.Split('_');
            if (parts.Length != 3)
                throw new ArgumentException("Invalid chunk ID format. Use 'r_theta_z' format.");
                
            int r = int.Parse(parts[0]);
            int theta = int.Parse(parts[1]);
            int z = int.Parse(parts[2]);
            
            if (r < 0 || r >= 1500)
                throw new ArgumentException($"ChunkCoordinate R must be 0-1499, got {r}");
            if (theta < 0 || theta >= 360)
                throw new ArgumentException($"ChunkCoordinate Theta must be 0-359, got {theta}");
            if (z < -100 || z > 100)
                throw new ArgumentException($"ChunkCoordinate Z must be -100 to 100, got {z}");
                
            R = r;
            Theta = theta;
            Z = z;
        }
        
        public override string ToString() => $"{R}_{Theta}_{Z}";
        
        public (double rMin, double rMax, double thetaMin, double thetaMax, double zMin, double zMax) GetBounds()
        {
            double rMin = R * CHUNK_SIZE;
            double rMax = (R + 1) * CHUNK_SIZE;
            double thetaMin = Theta * Math.PI / 180.0;
            double thetaMax = (Theta + 1) * Math.PI / 180.0;
            double zMin = Z * CHUNK_SIZE;
            double zMax = (Z + 1) * CHUNK_SIZE;
            
            return (rMin, rMax, thetaMin, thetaMax, zMin, zMax);
        }
    }
    
    /// <summary>
    /// Encode chunk coordinates and star index into a seed
    /// </summary>
    public static long EncodeSeed(int chunkR, int chunkTheta, int chunkZ, long starIndex)
    {
        // Validate inputs
        if (chunkR < 0 || chunkR >= MAX_RADIUS_CHUNKS)
            throw new ArgumentException($"chunkR must be 0-{MAX_RADIUS_CHUNKS-1}");
        if (chunkTheta < 0 || chunkTheta >= MAX_ANGULAR_CHUNKS)
            throw new ArgumentException($"chunkTheta must be 0-{MAX_ANGULAR_CHUNKS-1}");
        if (chunkZ < -100 || chunkZ > 100)
            throw new ArgumentException("chunkZ must be -100 to 100");
        if (starIndex < 0)
            throw new ArgumentException($"starIndex must be >= 0, got {starIndex}");
            
        // Normalize chunkZ to 0-200 range
        int normalizedZ = chunkZ + 100;
        
        // Encode using bit shifting for efficient packing
        // New layout: R(11 bits) + Theta(9 bits) + Z(8 bits) + StarIndex(36 bits) = 64 bits total
        // This allows up to ~68 billion stars per chunk
        long seed = ((long)chunkR << 53) | ((long)chunkTheta << 44) | ((long)normalizedZ << 36) | (long)starIndex;
        
        return seed;
    }
    
    /// <summary>
    /// Decode a seed back to chunk coordinates and star index
    /// </summary>
    public static (int chunkR, int chunkTheta, int chunkZ, long starIndex) DecodeSeed(long seed)
    {
        // New layout: R(11 bits) + Theta(9 bits) + Z(8 bits) + StarIndex(36 bits) = 64 bits total
        int chunkR = (int)((seed >> 53) & 0x7FF); // 11 bits
        int chunkTheta = (int)((seed >> 44) & 0x1FF); // 9 bits
        int normalizedZ = (int)((seed >> 36) & 0xFF); // 8 bits
        long starIndex = (long)(seed & 0xFFFFFFFFF); // 36 bits
        
        int chunkZ = normalizedZ - 100; // Convert back to -100 to 100 range
        
        return (chunkR, chunkTheta, chunkZ, starIndex);
    }
    
    /// <summary>
    /// Generate rogue planets for a chunk (deterministic)
    /// </summary>
    private List<RoguePlanet> GenerateRoguePlanetsForChunk(ChunkCoordinate chunk)
    {
        var roguePlanets = new List<RoguePlanet>();
        var bounds = chunk.GetBounds();
        
        // Use chunk center for density calculation
        // Use chunk center for density calculation
        // Note: bounds are in cylindrical coordinates (r, theta, z)
        var centerR = (float)((bounds.rMin + bounds.rMax) / 2);
        var centerTheta = (float)((bounds.thetaMin + bounds.thetaMax) / 2);
        var centerZ = (float)((bounds.zMin + bounds.zMax) / 2);
        
        // Convert to Cartesian for density calculation
        float x = centerR * (float)Math.Cos(centerTheta);
        float y = centerR * (float)Math.Sin(centerTheta);
        var cartesianPos = new GalaxyGenerator.Vector3(x, y, centerZ);
        
        // Calculate rogue planet density
        float rogueDensity = GalaxyGenerator.CalculateRoguePlanetDensity(cartesianPos);
        
        // Expected number of rogue planets (using chunk volume)
        float chunkVolume = (float)((bounds.rMax - bounds.rMin) * 
                           (bounds.rMax * (bounds.thetaMax - bounds.thetaMin)) * 
                           (bounds.zMax - bounds.zMin));
        float expectedRogues = rogueDensity * chunkVolume; // No scale factor needed now
        
        // Use Poisson-like distribution
        var baseRng = new Random(chunk.GetHashCode() ^ unchecked((int)0x524F475545)); // "ROGUE" in hex
        int rogueCount = 0;
        
        if (expectedRogues < 1)
        {
            if (baseRng.NextDouble() < expectedRogues)
                rogueCount = 1;
        }
        else
        {
            rogueCount = (int)expectedRogues;
            if (baseRng.NextDouble() < (expectedRogues - rogueCount))
                rogueCount++;
        }
        
        // Generate each rogue planet
        for (int i = 0; i < rogueCount; i++)
        {
            // Generate position within chunk
            var rng = new Random(chunk.GetHashCode() ^ i ^ unchecked((int)0x524F475545));
            
            float r = (float)(bounds.rMin + rng.NextDouble() * (bounds.rMax - bounds.rMin));
            float theta = (float)(bounds.thetaMin + rng.NextDouble() * (bounds.thetaMax - bounds.thetaMin));
            float z = (float)(bounds.zMin + rng.NextDouble() * (bounds.zMax - bounds.zMin));
            
            // Convert to Cartesian
            float px = r * (float)Math.Cos(theta);
            float py = r * (float)Math.Sin(theta);
            var position = new GalaxyGenerator.Vector3(px, py, z);
            
            // Create seed with negative index
            long rogueSeed = EncodeSeed(chunk.R, chunk.Theta, chunk.Z, (long)i | 0x800000000L); // Set high bit for negative
            
            var rogue = RoguePlanet.Generate(rogueSeed, position, chunk.R, chunk.Theta, chunk.Z, -(i + 1));
            roguePlanets.Add(rogue);
        }
        
        return roguePlanets;
    }
    
    /// <summary>
    /// Check if a seed refers to a rogue planet
    /// </summary>
    public static bool IsRoguePlanetSeed(long seed)
    {
        var (_, _, _, index) = DecodeSeed(seed);
        return (index & 0x800000000) != 0;
    }
    
    /// <summary>
    /// Get a star by its seed
    /// </summary>
    public Star GetStarBySeed(long seed)
    {
        // Special case for Sagittarius A*
        if (seed == 0)
        {
            var sgrA = new Star
            {
                Seed = 0,
                Position = GalaxyGenerator.Vector3.Zero,
                Type = StellarTypeGenerator.StellarType.SMBH,
                Mass = 4310000f,
                Temperature = 0f,
                Luminosity = 0f,
                Color = GalaxyGenerator.Vector3.Zero,
                Population = "Bulge",
                Region = "Galactic Center",
                PlanetCount = 0,
                IsMultiple = false,
                SystemName = "Sagittarius A*"
            };
            return sgrA;
        }
        
        // Decode the seed
        var (chunkR, chunkTheta, chunkZ, starIndex) = DecodeSeed(seed);
        
        // Check if this is a rogue planet (negative index)
        if ((starIndex & 0x800000000) != 0)
        {
            throw new ArgumentException($"Seed {seed} refers to a rogue planet, not a star. Use GetRoguePlanetBySeed instead.");
        }
        
        // Get chunk bounds
        var chunk = new ChunkCoordinate(chunkR, chunkTheta, chunkZ);
        var bounds = chunk.GetBounds();
        
        // Calculate expected stars in this chunk
        int expectedStars = CalculateExpectedStars(chunk);
        
        // If star index exceeds expected stars, it doesn't exist
        if (starIndex >= expectedStars)
        {
            throw new ArgumentException($"Star index {starIndex} exceeds expected stars ({expectedStars}) in chunk {chunk}");
        }
        
        // Generate deterministic position within chunk
        var rng = new Random((int)(seed & 0x7FFFFFFF));
        
        // Use stratified sampling for even distribution
        int strata = (int)Math.Ceiling(Math.Pow(expectedStars, 1.0/3.0));
        long strataIndex = starIndex % (strata * strata * strata);
        
        int rStrata = (int)(strataIndex / (strata * strata));
        int thetaStrata = (int)((strataIndex / strata) % strata);
        int zStrata = (int)(strataIndex % strata);
        
        // Generate position within stratum
        double r = bounds.rMin + (bounds.rMax - bounds.rMin) * ((rStrata + rng.NextDouble()) / strata);
        double theta = bounds.thetaMin + (bounds.thetaMax - bounds.thetaMin) * ((thetaStrata + rng.NextDouble()) / strata);
        double z = bounds.zMin + (bounds.zMax - bounds.zMin) * ((zStrata + rng.NextDouble()) / strata);
        
        // Convert to Cartesian
        float x = (float)(r * Math.Cos(theta));
        float y = (float)(r * Math.Sin(theta));
        var position = new GalaxyGenerator.Vector3(x, y, (float)z);
        
        // Generate star using unified system
        var star = Star.GenerateAtPosition(position, seed);
        
        return star;
    }
    
    /// <summary>
    /// Get a rogue planet by its seed
    /// </summary>
    public RoguePlanet GetRoguePlanetBySeed(long seed)
    {
        // Decode the seed
        var (chunkR, chunkTheta, chunkZ, index) = DecodeSeed(seed);
        
        // Check if this is actually a star
        if ((index & 0x800000000) == 0)
        {
            throw new ArgumentException($"Seed {seed} refers to a star, not a rogue planet. Use GetStarBySeed instead.");
        }
        
        // Get the actual rogue planet index
        long rogueIndex = index & 0x7FFFFFFFF;
        
        // Get chunk
        var chunk = new ChunkCoordinate(chunkR, chunkTheta, chunkZ);
        var bounds = chunk.GetBounds();
        
        // Generate position deterministically
        var rng = new Random(chunk.GetHashCode() ^ (int)rogueIndex ^ unchecked((int)0x524F475545));
        
        float r = (float)(bounds.rMin + rng.NextDouble() * (bounds.rMax - bounds.rMin));
        float theta = (float)(bounds.thetaMin + rng.NextDouble() * (bounds.thetaMax - bounds.thetaMin));
        float z = (float)(bounds.zMin + rng.NextDouble() * (bounds.zMax - bounds.zMin));
        
        // Convert to Cartesian
        float px = r * (float)Math.Cos(theta);
        float py = r * (float)Math.Sin(theta);
        var position = new GalaxyGenerator.Vector3(px, py, z);
        
        // Generate the rogue planet
        var rogue = RoguePlanet.Generate(seed, position, chunkR, chunkTheta, chunkZ, -(int)(rogueIndex + 1));
        
        return rogue;
    }
    
    /// <summary>
    /// Calculate expected number of stars in a chunk based on density
    /// </summary>
    private int CalculateExpectedStars(ChunkCoordinate chunk)
    {
        var bounds = chunk.GetBounds();
        
        // Calculate chunk volume (cylindrical wedge)
        double deltaTheta = bounds.thetaMax - bounds.thetaMin;
        double avgRadius = (bounds.rMin + bounds.rMax) / 2;
        double deltaR = bounds.rMax - bounds.rMin;
        double deltaZ = bounds.zMax - bounds.zMin;
        double volume = avgRadius * deltaR * deltaTheta * deltaZ;
        
        // Sample density at multiple points for better accuracy
        double totalStarCount = 0;
        int samples = 0;
        
        for (int i = 0; i < 3; i++)
        {
            double r = bounds.rMin + (bounds.rMax - bounds.rMin) * (i + 0.5) / 3;
            for (int j = 0; j < 3; j++)
            {
                double theta = bounds.thetaMin + (bounds.thetaMax - bounds.thetaMin) * (j + 0.5) / 3;
                for (int k = 0; k < 3; k++)
                {
                    double z = bounds.zMin + (bounds.zMax - bounds.zMin) * (k + 0.5) / 3;
                    
                    // Convert to Cartesian for density calculation
                    float x = (float)(r * Math.Cos(theta));
                    float y = (float)(r * Math.Sin(theta));
                    
                    // Use unified GalaxyGenerator for stellar density
                    var galaxyPos = new GalaxyGenerator.Vector3(x, y, (float)z);
                    var stellarDensity = GalaxyGenerator.GetExpectedStarDensity(galaxyPos);
                    
                    // Calculate small volume around sample point
                    // This gives us stars per sample volume
                    var sampleVolume = volume / 27.0; // 3x3x3 = 27 samples
                    var starsInSample = stellarDensity * sampleVolume;
                    
                    totalStarCount += starsInSample;
                    samples++;
                }
            }
        }
        
        // Total expected stars is sum of all samples
        int expectedStars = Math.Max(0, (int)Math.Round(totalStarCount));
        
        // No cap - let density determine star count
        return expectedStars;
    }
    
    /// <summary>
    /// Generate all stars in a chunk - SUPER FAST!
    /// </summary>
    public List<Star> GenerateChunkStars(string chunkId)
    {
        var chunk = new ChunkCoordinate(chunkId);
        var stars = new List<Star>();
        
        // First add special objects (like Sgr A*)
        var bounds = chunk.GetBounds();
        var specialObjects = GalacticAnalytics.GetSpecialObjectsInChunk(
            bounds.rMin, bounds.rMax, bounds.thetaMin, bounds.thetaMax, bounds.zMin, bounds.zMax);
        stars.AddRange(specialObjects);
        
        // Calculate expected stars
        int expectedStars = CalculateExpectedStars(chunk);
        
        // Generate all stars in chunk - just iterate through indices!
        for (int i = 0; i < expectedStars; i++)
        {
            long seed = EncodeSeed(chunk.R, chunk.Theta, chunk.Z, i);
            var star = GetStarBySeed(seed);
            stars.Add(star);
        }
        
        return stars;
    }
    
    /// <summary>
    /// Generate all objects in a chunk (stars and optionally rogue planets)
    /// </summary>
    public (List<Star> stars, List<RoguePlanet>? roguePlanets) GenerateChunkObjects(string chunkId, bool includeRoguePlanets = false)
    {
        var stars = GenerateChunkStars(chunkId);
        
        List<RoguePlanet>? roguePlanets = null;
        if (includeRoguePlanets)
        {
            var chunk = new ChunkCoordinate(chunkId);
            roguePlanets = GenerateRoguePlanetsForChunk(chunk);
        }
        
        return (stars, roguePlanets);
    }
    
    /// <summary>
    /// Find which chunk a position belongs to
    /// </summary>
    public ChunkCoordinate GetChunkForPosition(float x, float y, float z)
    {
        double r = Math.Sqrt(x * x + y * y);
        double theta = Math.Atan2(y, x) * 180 / Math.PI;
        if (theta < 0) theta += 360;
        
        int rIndex = Math.Min((int)(r / CHUNK_SIZE), MAX_RADIUS_CHUNKS - 1);
        int thetaIndex = (int)(theta);
        int zIndex = Math.Max(-50, Math.Min(50, (int)(z / CHUNK_SIZE)));
        
        return new ChunkCoordinate(rIndex, thetaIndex, zIndex);
    }
    
    /// <summary>
    /// Investigate a chunk and export to CSV
    /// </summary>
    public void InvestigateChunk(string chunkId, string? outputPath = null, bool includeRoguePlanets = false)
    {
        var chunk = new ChunkCoordinate(chunkId);
        var bounds = chunk.GetBounds();
        
        Console.WriteLine($"\n=== Investigating Chunk {chunkId} ===");
        Console.WriteLine($"Radial range: {bounds.rMin:F0} - {bounds.rMax:F0} ly");
        Console.WriteLine($"Angular range: {bounds.thetaMin * 180 / Math.PI:F0}° - {bounds.thetaMax * 180 / Math.PI:F0}°");
        Console.WriteLine($"Vertical range: {bounds.zMin:F0} - {bounds.zMax:F0} ly");
        
        // Calculate chunk volume
        var deltaTheta = bounds.thetaMax - bounds.thetaMin;
        var avgRadius = (bounds.rMin + bounds.rMax) / 2;
        var deltaR = bounds.rMax - bounds.rMin;
        var deltaZ = bounds.zMax - bounds.zMin;
        var volume = avgRadius * deltaR * deltaTheta * deltaZ;
        Console.WriteLine($"Chunk volume: {volume:E2} ly³");
        
        // Calculate expected density at chunk center
        var centerR = (bounds.rMin + bounds.rMax) / 2;
        var centerTheta = (bounds.thetaMin + bounds.thetaMax) / 2;
        var centerZ = (bounds.zMin + bounds.zMax) / 2;
        var centerX = (float)(centerR * Math.Cos(centerTheta));
        var centerY = (float)(centerR * Math.Sin(centerTheta));
        var centerPos = new GalaxyGenerator.Vector3(centerX, centerY, (float)centerZ);
        var expectedDensity = GalaxyGenerator.GetExpectedStarDensity(centerPos);
        Console.WriteLine($"Expected density (from formula): {expectedDensity:E3} stars/ly³");
        
        // Generate stars - THIS IS FAST NOW!
        var startTime = DateTime.Now;
        var stars = GenerateChunkStars(chunkId);
        var elapsed = (DateTime.Now - startTime).TotalSeconds;
        
        Console.WriteLine($"Stars in chunk: {stars.Count} (generated in {elapsed:F2}s)");
        
        // Calculate actual density
        var actualDensity = stars.Count / volume;
        var densityRatio = actualDensity / expectedDensity;
        Console.WriteLine($"Actual density: {actualDensity:E3} stars/ly³");
        Console.WriteLine($"Density ratio (actual/expected): {densityRatio:F2}");
        Console.WriteLine($"Region: {GalaxyGenerator.DetermineRegion(centerPos)}");
        
        // Generate rogue planets if requested
        List<RoguePlanet>? roguePlanets = null;
        if (includeRoguePlanets)
        {
            roguePlanets = GenerateRoguePlanetsForChunk(chunk);
            Console.WriteLine($"\nRogue planets in chunk: {roguePlanets.Count}");
            
            if (roguePlanets.Count > 0)
            {
                var rogueTypes = roguePlanets.GroupBy(r => r.Type).OrderByDescending(g => g.Count());
                Console.WriteLine("Rogue planet types:");
                foreach (var group in rogueTypes)
                {
                    Console.WriteLine($"  {group.Key}: {group.Count()} ({group.Count() * 100.0 / roguePlanets.Count:F1}%)");
                }
                
                // Rogue-to-star ratio
                Console.WriteLine($"Rogue-to-star ratio: 1:{(stars.Count > 0 ? stars.Count / (double)Math.Max(1, roguePlanets.Count) : 0):F1}");
            }
        }
        
        // Statistics
        if (stars.Count > 0)
        {
            var typeGroups = stars.GroupBy(s => s.Type).OrderByDescending(g => g.Count());
            Console.WriteLine("\nStellar types:");
            foreach (var group in typeGroups)
            {
                string typeDisplay = group.Key.ToString();
                Console.WriteLine($"  {typeDisplay}: {group.Count()} ({group.Count() * 100.0 / stars.Count:F1}%)");
            }
        }
        
        // Export to CSV
        if (outputPath == null)
        {
            outputPath = includeRoguePlanets ? $"chunk_{chunkId}_with_rogues_data.csv" : $"chunk_{chunkId}_data.csv";
        }
        
        using (var writer = new StreamWriter(outputPath))
        {
            if (includeRoguePlanets && roguePlanets != null && roguePlanets.Count > 0)
            {
                // Include rogue planets - different CSV format
                writer.WriteLine("ChunkID,ObjectType,Index,Seed,X,Y,Z,R,Theta,Type,Mass,Radius,Temperature,Luminosity,ColorR,ColorG,ColorB,Population,Region,Planets,IsMultiple,SystemName,MoonCount,Origin");
                
                // Write stars
                foreach (var star in stars)
                {
                    var r = Math.Sqrt(star.Position.X * star.Position.X + star.Position.Y * star.Position.Y);
                    var theta = Math.Atan2(star.Position.Y, star.Position.X) * 180 / Math.PI;
                    if (theta < 0) theta += 360;
                    
                    // Decode to get star index
                    var (_, _, _, starIndex) = DecodeSeed(star.Seed);
                    
                    writer.WriteLine($"{chunk},Star,{starIndex},{star.Seed},{star.Position.X:F2},{star.Position.Y:F2},{star.Position.Z:F2}," +
                        $"{r:F2},{theta:F2},{star.Type},{star.Mass:F3},0,{star.Temperature:F0},{star.Luminosity:F4}," +
                        $"{star.Color.X:F3},{star.Color.Y:F3},{star.Color.Z:F3},{star.Population},{star.Region},{star.PlanetCount}," +
                        $"{star.IsMultiple},{star.SystemName},,");
                }
                
                // Write rogue planets
                foreach (var rogue in roguePlanets)
                {
                    var r = Math.Sqrt(rogue.Position.X * rogue.Position.X + rogue.Position.Y * rogue.Position.Y);
                    var theta = Math.Atan2(rogue.Position.Y, rogue.Position.X) * 180 / Math.PI;
                    if (theta < 0) theta += 360;
                    
                    writer.WriteLine($"{chunk},RoguePlanet,{rogue.Index},{rogue.Seed},{rogue.Position.X:F2},{rogue.Position.Y:F2},{rogue.Position.Z:F2}," +
                        $"{r:F2},{theta:F2},{rogue.Type},{rogue.Mass:F3},{rogue.Radius:F1},{rogue.Temperature:F0},0," +
                        $"0,0,0,,,0," +
                        $"false,Rogue,{rogue.MoonCount},{rogue.Origin}");
                }
            }
            else
            {
                // Standard star-only format
                writer.WriteLine("ChunkID,Seed,X,Y,Z,R,Theta,Type,Mass,Temperature,Luminosity,ColorR,ColorG,ColorB,Population,Region,Planets,IsMultiple,SystemName");
                
                foreach (var star in stars)
                {
                    var r = Math.Sqrt(star.Position.X * star.Position.X + star.Position.Y * star.Position.Y);
                    var theta = Math.Atan2(star.Position.Y, star.Position.X) * 180 / Math.PI;
                    if (theta < 0) theta += 360;
                    
                    writer.WriteLine($"{chunk},{star.Seed},{star.Position.X:F2},{star.Position.Y:F2},{star.Position.Z:F2}," +
                        $"{r:F2},{theta:F2},{star.Type},{star.Mass:F3},{star.Temperature:F0},{star.Luminosity:F4}," +
                        $"{star.Color.X:F3},{star.Color.Y:F3},{star.Color.Z:F3},{star.Population},{star.Region},{star.PlanetCount}," +
                        $"{star.IsMultiple},{star.SystemName}");
                }
            }
        }
        
        Console.WriteLine($"Data exported to: {outputPath}");
    }
    
    
    /// <summary>
    /// Estimate total star count using density formula integration
    /// </summary>
    public void EstimateTotalStarCount()
    {
        Console.WriteLine("\n=== Estimating Total Galaxy Star Count ===");
        Console.WriteLine("Using density formula integration...\n");
        
        // First check some key density values
        Console.WriteLine("Sample density values:");
        var testPoints = new[] {
            (0.0f, 0.0f, 0.0f, "Galactic center"),
            (100.0f, 0.0f, 0.0f, "100 ly from center"),
            (500.0f, 0.0f, 0.0f, "500 ly from center"),
            (1000.0f, 0.0f, 0.0f, "1000 ly from center"),
            (2500.0f, 0.0f, 0.0f, "2500 ly from center"),
            (5000.0f, 0.0f, 0.0f, "5000 ly from center"),
            (8000.0f, 0.0f, 0.0f, "Inner disk"),
            (26000.0f, 0.0f, 0.0f, "Solar neighborhood (actual Sun position)"),
            (50000.0f, 0.0f, 0.0f, "Outer disk")
        };
        
        foreach (var (x, y, z, desc) in testPoints)
        {
            var pos = new GalaxyGenerator.Vector3(x, y, z);
            var density = GalaxyGenerator.GetExpectedStarDensity(pos);
            var rogueDensity = GalaxyGenerator.CalculateRoguePlanetDensity(pos);
            Console.WriteLine($"  {desc}: {density:E3} stars/ly³, {rogueDensity:E3} rogues/ly³");
        }
        Console.WriteLine();
        
        var startTime = DateTime.Now;
        double totalStars = 0;
        double totalRogues = 0;
        
        // Integration parameters - sample points for numerical integration
        // Use logarithmic sampling in radius to better capture high density regions
        int rSamples = 500;     // Radial samples
        int thetaSamples = 50;  // Angular samples (full circle)
        int zSamples = 100;     // Vertical samples
        
        double minRadius = 10;      // Start at 10 ly to avoid singularity
        double maxRadius = 150000;  // Maximum radius in light years
        double logMin = Math.Log10(minRadius);
        double logMax = Math.Log10(maxRadius);
        double dlogr = (logMax - logMin) / rSamples;
        double dtheta = 2 * Math.PI / thetaSamples;
        double maxZ = 10000.0;
        double dz = 2 * maxZ / zSamples;
        
        // Perform numerical integration
        for (int i = 0; i < rSamples; i++)
        {
            // Use logarithmic sampling
            double logr = logMin + (i + 0.5) * dlogr;
            double r = Math.Pow(10, logr);
            double r_next = Math.Pow(10, logr + dlogr);
            double dr = r_next - r; // Width of this radial shell
            
            double ringDensity = 0;
            double ringRogues = 0;
            
            for (int j = 0; j < thetaSamples; j++)
            {
                double theta = (j + 0.5) * dtheta;
                
                for (int k = 0; k < zSamples; k++)
                {
                    double z = -maxZ + (k + 0.5) * dz;
                    
                    // Convert to cartesian
                    float x = (float)(r * Math.Cos(theta));
                    float y = (float)(r * Math.Sin(theta));
                    var pos = new GalaxyGenerator.Vector3(x, y, (float)z);
                    
                    // Get density at this point
                    double density = GalaxyGenerator.GetExpectedStarDensity(pos);
                    double rogueDensity = GalaxyGenerator.CalculateRoguePlanetDensity(pos); // No scale factor needed
                    
                    // Volume element in cylindrical coordinates
                    double dV = r * dr * dtheta * dz;
                    
                    ringDensity += density * dV;
                    ringRogues += rogueDensity * dV;
                }
            }
            
            totalStars += ringDensity;
            totalRogues += ringRogues;
            
            // Progress update
            if (i % 50 == 0 && i > 0)
            {
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                Console.WriteLine($"Progress: {i}/{rSamples} ({i * 100.0 / rSamples:F1}%), " +
                    $"r={r:F0} ly, Running total: {totalStars:E2} stars, {totalRogues:E2} rogues");
            }
        }
        
        var totalTime = (DateTime.Now - startTime).TotalSeconds;
        
        Console.WriteLine($"\n=== Integration Complete ===");
        Console.WriteLine($"Integration grid: {rSamples} x {thetaSamples} x {zSamples} = {rSamples * thetaSamples * zSamples:N0} samples");
        Console.WriteLine($"Radial sampling: logarithmic from {minRadius} to {maxRadius} ly");
        Console.WriteLine($"Integration time: {totalTime:F1} seconds");
        Console.WriteLine($"\nEstimated total stars in galaxy: {totalStars:E2} ({totalStars:N0})");
        Console.WriteLine($"Estimated total rogue planets: {totalRogues:E2} ({totalRogues:N0})");
        
        // Compare to expected 225 billion
        double ratio = totalStars / 225e9;
        Console.WriteLine($"\nRatio to 225 billion target: {ratio:F2}x");
        Console.WriteLine($"Rogue planet to star ratio: {totalRogues / totalStars:F2}:1");
    }
}