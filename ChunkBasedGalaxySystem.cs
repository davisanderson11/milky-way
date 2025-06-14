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
    private const int MAX_RADIUS_CHUNKS = 600; // 0-599 (60,000 ly)
    private const int MAX_ANGULAR_CHUNKS = 360; // 0-359 degrees
    private const int MAX_Z_CHUNKS = 101; // -50 to 50 (-5000 to 5000 ly)
    private const int MAX_STARS_PER_CHUNK = 100000; // Maximum stars in any chunk
    
    private readonly ScientificMilkyWayGenerator baseGenerator;
    
    public ChunkBasedGalaxySystem()
    {
        baseGenerator = new ScientificMilkyWayGenerator();
    }
    
    /// <summary>
    /// Chunk coordinate representation
    /// </summary>
    public class ChunkCoordinate
    {
        public int R { get; set; }      // 0-599
        public int Theta { get; set; }  // 0-359
        public int Z { get; set; }      // -50 to 50
        
        public ChunkCoordinate(int r, int theta, int z)
        {
            R = r;
            Theta = theta;
            Z = z;
        }
        
        public ChunkCoordinate(string chunkId)
        {
            var parts = chunkId.Split('_');
            if (parts.Length != 3)
                throw new ArgumentException("Invalid chunk ID format. Use 'r_theta_z' format.");
                
            R = int.Parse(parts[0]);
            Theta = int.Parse(parts[1]);
            Z = int.Parse(parts[2]);
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
    public static long EncodeSeed(int chunkR, int chunkTheta, int chunkZ, int starIndex)
    {
        // Validate inputs
        if (chunkR < 0 || chunkR >= MAX_RADIUS_CHUNKS)
            throw new ArgumentException($"chunkR must be 0-{MAX_RADIUS_CHUNKS-1}");
        if (chunkTheta < 0 || chunkTheta >= MAX_ANGULAR_CHUNKS)
            throw new ArgumentException($"chunkTheta must be 0-{MAX_ANGULAR_CHUNKS-1}");
        if (chunkZ < -50 || chunkZ > 50)
            throw new ArgumentException("chunkZ must be -50 to 50");
        if (starIndex < 0 || starIndex >= MAX_STARS_PER_CHUNK)
            throw new ArgumentException($"starIndex must be 0-{MAX_STARS_PER_CHUNK-1}");
            
        // Normalize chunkZ to 0-100 range
        int normalizedZ = chunkZ + 50;
        
        // Encode using bit shifting for efficient packing
        // 10 bits for R (0-599), 9 bits for Theta (0-359), 7 bits for Z (0-100), 17 bits for star index
        long seed = ((long)chunkR << 33) | ((long)chunkTheta << 24) | ((long)normalizedZ << 17) | (long)starIndex;
        
        return seed;
    }
    
    /// <summary>
    /// Decode a seed back to chunk coordinates and star index
    /// </summary>
    public static (int chunkR, int chunkTheta, int chunkZ, int starIndex) DecodeSeed(long seed)
    {
        int chunkR = (int)((seed >> 33) & 0x3FF); // 10 bits
        int chunkTheta = (int)((seed >> 24) & 0x1FF); // 9 bits
        int normalizedZ = (int)((seed >> 17) & 0x7F); // 7 bits
        int starIndex = (int)(seed & 0x1FFFF); // 17 bits
        
        int chunkZ = normalizedZ - 50; // Convert back to -50 to 50 range
        
        return (chunkR, chunkTheta, chunkZ, starIndex);
    }
    
    /// <summary>
    /// Get a star by its seed
    /// </summary>
    public ScientificMilkyWayGenerator.Star GetStarBySeed(long seed)
    {
        // Special case for Sagittarius A*
        if (seed == 0)
        {
            return baseGenerator.GetStarBySeed(0);
        }
        
        // Decode the seed
        var (chunkR, chunkTheta, chunkZ, starIndex) = DecodeSeed(seed);
        
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
        int strataIndex = starIndex % (strata * strata * strata);
        
        int rStrata = strataIndex / (strata * strata);
        int thetaStrata = (strataIndex / strata) % strata;
        int zStrata = strataIndex % strata;
        
        // Generate position within stratum
        double r = bounds.rMin + (bounds.rMax - bounds.rMin) * ((rStrata + rng.NextDouble()) / strata);
        double theta = bounds.thetaMin + (bounds.thetaMax - bounds.thetaMin) * ((thetaStrata + rng.NextDouble()) / strata);
        double z = bounds.zMin + (bounds.zMax - bounds.zMin) * ((zStrata + rng.NextDouble()) / strata);
        
        // Convert to Cartesian
        float x = (float)(r * Math.Cos(theta));
        float y = (float)(r * Math.Sin(theta));
        var position = new ScientificMilkyWayGenerator.Vector3(x, y, (float)z);
        
        // Generate star properties based on position
        var population = baseGenerator.DeterminePopulation(position);
        var type = baseGenerator.DetermineStellarType(position, population, seed);
        var properties = baseGenerator.GetStellarProperties(type);
        var mass = properties.mass * (0.8f + 0.4f * (float)rng.NextDouble());
        
        // Generate planetary system
        var planetarySystemGen = new PlanetarySystemGenerator();
        var planetarySystem = planetarySystemGen.GeneratePlanetarySystem(seed, type, mass, $"Star-{seed}");
        
        // Check for companions
        var (isMultiple, companionCount, companions) = MultipleStarSystems.GetCompanionInfo(seed, type);
        
        return new ScientificMilkyWayGenerator.Star
        {
            Position = position,
            Type = type,
            Mass = mass,
            Temperature = properties.temperature,
            Color = properties.color,
            Luminosity = properties.luminosity,
            Seed = seed,
            Population = population,
            Region = baseGenerator.DetermineRegion(position),
            PlanetCount = planetarySystem.Planets.Count,
            IsMultiple = isMultiple,
            SystemName = MultipleStarSystems.GetSystemName(seed, type)
        };
    }
    
    /// <summary>
    /// Calculate expected number of stars in a chunk based on density
    /// </summary>
    private int CalculateExpectedStars(ChunkCoordinate chunk)
    {
        var bounds = chunk.GetBounds();
        
        // Sample density at multiple points
        double totalDensity = 0;
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
                    totalDensity += GalacticAnalytics.CalculateStellarDensity(r, z);
                    samples++;
                }
            }
        }
        
        double avgDensity = totalDensity / samples;
        
        // Calculate chunk volume
        double deltaTheta = bounds.thetaMax - bounds.thetaMin;
        double avgRadius = (bounds.rMin + bounds.rMax) / 2;
        double deltaR = bounds.rMax - bounds.rMin;
        double deltaZ = bounds.zMax - bounds.zMin;
        double volume = avgRadius * deltaR * deltaTheta * deltaZ;
        
        int expectedStars = Math.Max(1, (int)(avgDensity * volume));
        
        // Cap at maximum
        return Math.Min(expectedStars, MAX_STARS_PER_CHUNK);
    }
    
    /// <summary>
    /// Generate all stars in a chunk - SUPER FAST!
    /// </summary>
    public List<ScientificMilkyWayGenerator.Star> GenerateChunkStars(string chunkId)
    {
        var chunk = new ChunkCoordinate(chunkId);
        var stars = new List<ScientificMilkyWayGenerator.Star>();
        
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
    public void InvestigateChunk(string chunkId, string? outputPath = null)
    {
        var chunk = new ChunkCoordinate(chunkId);
        var bounds = chunk.GetBounds();
        
        Console.WriteLine($"\n=== Investigating Chunk {chunkId} ===");
        Console.WriteLine($"Radial range: {bounds.rMin:F0} - {bounds.rMax:F0} ly");
        Console.WriteLine($"Angular range: {bounds.thetaMin * 180 / Math.PI:F0}° - {bounds.thetaMax * 180 / Math.PI:F0}°");
        Console.WriteLine($"Vertical range: {bounds.zMin:F0} - {bounds.zMax:F0} ly");
        
        // Generate stars - THIS IS FAST NOW!
        var startTime = DateTime.Now;
        var stars = GenerateChunkStars(chunkId);
        var elapsed = (DateTime.Now - startTime).TotalSeconds;
        
        Console.WriteLine($"Stars in chunk: {stars.Count} (generated in {elapsed:F2}s)");
        
        // Statistics
        if (stars.Count > 0)
        {
            var typeGroups = stars.GroupBy(s => s.Type).OrderByDescending(g => g.Count());
            Console.WriteLine("\nStellar types:");
            foreach (var group in typeGroups)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()} ({group.Count() * 100.0 / stars.Count:F1}%)");
            }
        }
        
        // Export to CSV
        if (outputPath == null)
        {
            outputPath = $"chunk_{chunkId}_data.csv";
        }
        
        using (var writer = new StreamWriter(outputPath))
        {
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
        
        Console.WriteLine($"Data exported to: {outputPath}");
    }
}