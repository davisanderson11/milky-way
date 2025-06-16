using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Galaxy chunk system with fixed 100 ly chunks
/// Uses unified GalaxyGenerator for all density and position calculations
/// </summary>
public class GalaxyChunkSystem
{
    // Chunk dimensions
    private const int CHUNK_SIZE = 100; // 100 light years per chunk
    private const int MAX_RADIUS_CHUNKS = 10000; // 0-9999 (1,000,000 ly)
    private const int MAX_ANGULAR_CHUNKS = 360; // 0-359 degrees
    private const int MAX_Z_CHUNKS = 1001; // -500 to 500 (-50000 to 50000 ly)
    
    private readonly ScientificMilkyWayGenerator baseGenerator;
    
    public GalaxyChunkSystem()
    {
        baseGenerator = new ScientificMilkyWayGenerator();
    }
    
    /// <summary>
    /// Chunk coordinate representation
    /// </summary>
    public class ChunkCoordinate
    {
        public int R { get; set; }      // 0-9999
        public int Theta { get; set; }  // 0-359
        public int Z { get; set; }      // -500 to 500
        
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
        
        /// <summary>
        /// Get the center position of this chunk in Cartesian coordinates
        /// </summary>
        public GalaxyGenerator.Vector3 GetCenterPosition()
        {
            var bounds = GetBounds();
            double centerR = (bounds.rMin + bounds.rMax) / 2;
            double centerTheta = (bounds.thetaMin + bounds.thetaMax) / 2;
            double centerZ = (bounds.zMin + bounds.zMax) / 2;
            
            float x = (float)(centerR * Math.Cos(centerTheta));
            float y = (float)(centerR * Math.Sin(centerTheta));
            float z = (float)centerZ;
            
            return new GalaxyGenerator.Vector3(x, y, z);
        }
        
        /// <summary>
        /// Calculate volume of this chunk
        /// </summary>
        public double GetVolume()
        {
            var bounds = GetBounds();
            double deltaTheta = bounds.thetaMax - bounds.thetaMin;
            double deltaR = bounds.rMax - bounds.rMin;
            double deltaZ = bounds.zMax - bounds.zMin;
            
            if (R == 0)
            {
                // Volume of a wedge from r=0 to r=deltaR
                return 0.5 * deltaTheta * deltaR * deltaR * deltaZ;
            }
            else
            {
                // Standard cylindrical wedge volume
                double avgRadius = (bounds.rMin + bounds.rMax) / 2;
                return avgRadius * deltaR * deltaTheta * deltaZ;
            }
        }
    }
    
    /// <summary>
    /// Get a star by its seed (position-based)
    /// </summary>
    public ScientificMilkyWayGenerator.Star GetStarBySeed(long seed)
    {
        // Just pass through to base generator which uses position-based seeds
        return baseGenerator.GetStarBySeed(seed);
    }
    
    /// <summary>
    /// Check if a seed is chunk-encoded
    /// </summary>
    private bool IsChunkEncodedSeed(long seed)
    {
        // Chunk-encoded seeds have specific bit patterns
        // Check if the upper bits match our encoding pattern
        int chunkR = (int)((seed >> 39) & 0x3FFF);
        int chunkTheta = (int)((seed >> 30) & 0x1FF);
        int normalizedZ = (int)((seed >> 20) & 0x3FF);
        
        return chunkR < MAX_RADIUS_CHUNKS && 
               chunkTheta < MAX_ANGULAR_CHUNKS && 
               normalizedZ < MAX_Z_CHUNKS;
    }
    
    /// <summary>
    /// Get a star using chunk encoding
    /// </summary>
    private ScientificMilkyWayGenerator.Star GetStarByChunkEncoding(long encodedSeed)
    {
        // Decode the seed
        var (chunkR, chunkTheta, chunkZ, starIndex) = DecodeSeed(encodedSeed);
        
        // Get chunk and calculate position
        var chunk = new ChunkCoordinate(chunkR, chunkTheta, chunkZ);
        var bounds = chunk.GetBounds();
        
        // Generate stars in this chunk
        var starsInChunk = GenerateChunkStars(chunk.ToString());
        
        // Return the requested star
        if (starIndex >= 0 && starIndex < starsInChunk.Count)
        {
            return starsInChunk[starIndex];
        }
        
        throw new ArgumentException($"Star index {starIndex} not found in chunk {chunk}");
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
        if (chunkZ < -500 || chunkZ > 500)
            throw new ArgumentException("chunkZ must be -500 to 500");
        if (starIndex < 0)
            throw new ArgumentException("starIndex must be non-negative");
            
        // Normalize chunkZ to 0-1000 range
        int normalizedZ = chunkZ + 500;
        
        // Encode using bit shifting for efficient packing
        // 14 bits for R (0-9999), 9 bits for Theta (0-359), 10 bits for Z (0-1000), 20 bits for star index
        long seed = ((long)chunkR << 39) | ((long)chunkTheta << 30) | ((long)normalizedZ << 20) | (long)starIndex;
        
        return seed;
    }
    
    /// <summary>
    /// Decode a seed back to chunk coordinates and star index
    /// </summary>
    public static (int chunkR, int chunkTheta, int chunkZ, int starIndex) DecodeSeed(long seed)
    {
        int chunkR = (int)((seed >> 39) & 0x3FFF); // 14 bits
        int chunkTheta = (int)((seed >> 30) & 0x1FF); // 9 bits
        int normalizedZ = (int)((seed >> 20) & 0x3FF); // 10 bits
        int starIndex = (int)(seed & 0xFFFFF); // 20 bits
        
        int chunkZ = normalizedZ - 500; // Convert back to -500 to 500 range
        
        return (chunkR, chunkTheta, chunkZ, starIndex);
    }
    
    /// <summary>
    /// Generate all stars in a chunk using the unified density-based algorithm
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
        
        // Calculate expected star count based on density
        var centerPos = chunk.GetCenterPosition();
        var volume = chunk.GetVolume();
        var expectedCount = GalaxyGenerator.GetExpectedStarCount(centerPos, (float)volume);
        
        // Use the chunk's deterministic seed
        int chunkSeed = chunkId.GetHashCode();
        
        // Generate star positions using unified generator
        var minBounds = new GalaxyGenerator.Vector3(
            (float)(bounds.rMin * Math.Cos(bounds.thetaMin) - 50),
            (float)(bounds.rMin * Math.Sin(bounds.thetaMin) - 50),
            (float)bounds.zMin
        );
        
        var maxBounds = new GalaxyGenerator.Vector3(
            (float)(bounds.rMax * Math.Cos(bounds.thetaMax) + 50),
            (float)(bounds.rMax * Math.Sin(bounds.thetaMax) + 50),
            (float)bounds.zMax
        );
        
        // Generate positions using density-based rejection sampling
        var positions = GalaxyGenerator.GenerateStarPositionsInRegion(
            minBounds, maxBounds, (int)expectedCount, chunkSeed);
        
        // Filter positions to only those actually in the chunk
        int starIndex = 0;
        foreach (var pos in positions)
        {
            // Check if position is actually in this chunk
            var r = pos.Length2D();
            var theta = Math.Atan2(pos.Y, pos.X);
            if (theta < 0) theta += 2 * Math.PI;
            
            if (r >= bounds.rMin && r < bounds.rMax &&
                theta >= bounds.thetaMin && theta < bounds.thetaMax &&
                pos.Z >= bounds.zMin && pos.Z < bounds.zMax)
            {
                // Convert GalaxyGenerator.Vector3 to ScientificMilkyWayGenerator.Vector3
                var starPos = new ScientificMilkyWayGenerator.Vector3(pos.X, pos.Y, pos.Z);
                
                // Generate star at this position (it will use unified seed internally)
                var star = baseGenerator.GenerateStarAtPosition(starPos);
                
                // Star already has the correct position-based seed from GenerateStarAtPosition
                stars.Add(star);
            }
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
        int zIndex = Math.Max(-500, Math.Min(500, (int)(z / CHUNK_SIZE)));
        
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
        
        // Calculate chunk volume
        var volume = chunk.GetVolume();
        Console.WriteLine($"Chunk volume: {volume:F1} ly³");
        
        // Sample density at center using unified generator
        var centerPos = chunk.GetCenterPosition();
        var theoreticalDensity = GalaxyGenerator.CalculateTotalDensity(centerPos) * 0.14f * 2.0f; // Match scaling
        Console.WriteLine($"Theoretical density at center: {theoreticalDensity:E2} stars/ly³");
        
        // Generate stars
        var startTime = DateTime.Now;
        var stars = GenerateChunkStars(chunkId);
        var elapsed = (DateTime.Now - startTime).TotalSeconds;
        
        Console.WriteLine($"\nStars in chunk: {stars.Count:N0} (generated in {elapsed:F2}s)");
        
        // Calculate actual density
        double actualDensity = (double)stars.Count / volume;
        Console.WriteLine($"Actual density: {actualDensity:F2} stars/ly³");
        Console.WriteLine($"Density ratio: {actualDensity/theoreticalDensity:F2}");
        
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