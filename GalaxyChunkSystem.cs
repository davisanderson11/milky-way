using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Chunk-based system for organizing and investigating the galaxy
/// </summary>
public class GalaxyChunkSystem
{
    private readonly ScientificMilkyWayGenerator generator;
    private const int CHUNK_SIZE = 100; // 100 light years per chunk
    private const int MAX_RADIUS_CHUNKS = 600; // Up to 60,000 ly radius
    private const int DEGREES_PER_CHUNK = 1; // 1 degree per angular chunk
    private const int MAX_HEIGHT_CHUNKS = 100; // -5000 to +5000 ly in height
    
    public GalaxyChunkSystem(ScientificMilkyWayGenerator generator)
    {
        this.generator = generator;
    }
    
    /// <summary>
    /// Represents a chunk in cylindrical coordinates
    /// </summary>
    public class ChunkCoordinate
    {
        public int RadialIndex { get; set; }      // 0-599 (0-59,900 ly in 100 ly steps)
        public int AngularIndex { get; set; }     // 0-359 (degrees)
        public int VerticalIndex { get; set; }    // -50 to +50 (-5000 to +5000 ly)
        
        public ChunkCoordinate(int r, int theta, int z)
        {
            RadialIndex = r;
            AngularIndex = theta;
            VerticalIndex = z;
        }
        
        public ChunkCoordinate(string chunkId)
        {
            var parts = chunkId.Split('_');
            if (parts.Length != 3)
                throw new ArgumentException("Invalid chunk ID format. Use 'r_theta_z' format.");
                
            RadialIndex = int.Parse(parts[0]);
            AngularIndex = int.Parse(parts[1]);
            VerticalIndex = int.Parse(parts[2]);
        }
        
        public override string ToString()
        {
            return $"{RadialIndex}_{AngularIndex}_{VerticalIndex}";
        }
        
        // Get the cylindrical bounds of this chunk
        public (double rMin, double rMax, double thetaMin, double thetaMax, double zMin, double zMax) GetBounds()
        {
            double rMin = RadialIndex * CHUNK_SIZE;
            double rMax = (RadialIndex + 1) * CHUNK_SIZE;
            double thetaMin = AngularIndex * Math.PI / 180.0;
            double thetaMax = (AngularIndex + 1) * Math.PI / 180.0;
            double zMin = VerticalIndex * CHUNK_SIZE;
            double zMax = (VerticalIndex + 1) * CHUNK_SIZE;
            
            return (rMin, rMax, thetaMin, thetaMax, zMin, zMax);
        }
    }
    
    /// <summary>
    /// Generate all stars in a specific chunk
    /// </summary>
    public List<ScientificMilkyWayGenerator.Star> GenerateChunkStars(ChunkCoordinate chunk)
    {
        var bounds = chunk.GetBounds();
        var stars = new List<ScientificMilkyWayGenerator.Star>();
        
        // First, add any special objects in this chunk
        var specialObjects = SpecialGalacticObjects.GetSpecialObjectsInChunk(
            bounds.rMin, bounds.rMax, bounds.thetaMin, bounds.thetaMax, bounds.zMin, bounds.zMax);
        stars.AddRange(specialObjects);
        
        // Calculate expected number of regular stars based on density
        double avgDensity = CalculateAverageChunkDensity(chunk);
        double volume = CalculateChunkVolume(chunk);
        int expectedStars = (int)(avgDensity * volume);
        
        // Ensure we have at least some samples for very sparse regions
        expectedStars = Math.Max(expectedStars, 10);
        
        // Generate regular stars within chunk bounds
        var random = new Random(GetChunkSeed(chunk));
        long attemptsWithSeed = 0;
        int generated = 0;
        const long totalStars = 100_000_000_000L;
        
        // Try random seeds and check if they fall within this chunk
        while (generated < expectedStars && attemptsWithSeed < expectedStars * 100)
        {
            // Generate a random seed
            long seed = (long)(random.NextDouble() * totalStars);
            
            // Generate the star
            var star = generator.GetStarBySeed(seed);
            
            // Convert position to cylindrical coordinates
            double r = Math.Sqrt(star.Position.X * star.Position.X + star.Position.Y * star.Position.Y);
            double theta = Math.Atan2(star.Position.Y, star.Position.X);
            double z = star.Position.Z;
            
            // Normalize theta to [0, 2π]
            if (theta < 0) theta += 2 * Math.PI;
            
            // Check if star is within chunk bounds
            if (r >= bounds.rMin && r < bounds.rMax &&
                theta >= bounds.thetaMin && theta < bounds.thetaMax &&
                z >= bounds.zMin && z < bounds.zMax)
            {
                stars.Add(star);
                generated++;
            }
            
            attemptsWithSeed++;
        }
        
        return stars;
    }
    
    /// <summary>
    /// Investigate a chunk and export data to CSV
    /// </summary>
    public void InvestigateChunk(string chunkId, string? outputPath = null)
    {
        var chunk = new ChunkCoordinate(chunkId);
        var bounds = chunk.GetBounds();
        
        Console.WriteLine($"\n=== Investigating Chunk {chunkId} ===");
        Console.WriteLine($"Radial range: {bounds.rMin:F0} - {bounds.rMax:F0} ly");
        Console.WriteLine($"Angular range: {bounds.thetaMin * 180 / Math.PI:F0}° - {bounds.thetaMax * 180 / Math.PI:F0}°");
        Console.WriteLine($"Vertical range: {bounds.zMin:F0} - {bounds.zMax:F0} ly");
        
        // Generate stars in chunk
        var stars = GenerateChunkStars(chunk);
        Console.WriteLine($"Stars in chunk: {stars.Count}");
        
        // Count multiple star systems
        var multipleSystemCount = stars.Count(s => s.IsMultiple);
        if (multipleSystemCount > 0)
        {
            Console.WriteLine($"Multiple star systems: {multipleSystemCount}");
        }
        
        // Calculate statistics
        if (stars.Count > 0)
        {
            var typeGroups = stars.GroupBy(s => s.Type).OrderByDescending(g => g.Count());
            Console.WriteLine("\nStellar types:");
            foreach (var group in typeGroups)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()} ({group.Count() * 100.0 / stars.Count:F1}%)");
            }
            
            var avgMass = stars.Average(s => s.Mass);
            var avgTemp = stars.Average(s => s.Temperature);
            var avgLum = stars.Average(s => s.Luminosity);
            
            Console.WriteLine($"\nAverage properties:");
            Console.WriteLine($"  Mass: {avgMass:F2} solar masses");
            Console.WriteLine($"  Temperature: {avgTemp:F0} K");
            Console.WriteLine($"  Luminosity: {avgLum:F3} solar luminosities");
        }
        
        // Export to CSV
        if (outputPath == null)
        {
            outputPath = $"chunk_{chunkId}_data.csv";
        }
        
        ExportChunkToCSV(chunk, stars, outputPath);
        Console.WriteLine($"\nData exported to: {outputPath}");
    }
    
    /// <summary>
    /// Export chunk data to CSV file
    /// </summary>
    private void ExportChunkToCSV(ChunkCoordinate chunk, List<ScientificMilkyWayGenerator.Star> stars, string filename)
    {
        using (var writer = new StreamWriter(filename))
        {
            // Write header
            writer.WriteLine("ChunkID,Seed,X,Y,Z,R,Theta,Type,Mass,Temperature,Luminosity,ColorR,ColorG,ColorB,Population,Region,Planets,IsMultiple,SystemName");
            
            // Write star data
            foreach (var star in stars)
            {
                var r = Math.Sqrt(star.Position.X * star.Position.X + star.Position.Y * star.Position.Y);
                var theta = Math.Atan2(star.Position.Y, star.Position.X) * 180 / Math.PI;
                if (theta < 0) theta += 360; // Normalize to 0-360
                
                writer.WriteLine($"{chunk},{star.Seed},{star.Position.X:F2},{star.Position.Y:F2},{star.Position.Z:F2}," +
                    $"{r:F2},{theta:F2},{star.Type},{star.Mass:F3},{star.Temperature:F0},{star.Luminosity:F4}," +
                    $"{star.Color.X:F3},{star.Color.Y:F3},{star.Color.Z:F3},{star.Population},{star.Region},{star.PlanetCount}," +
                    $"{star.IsMultiple},{star.SystemName}");
            }
        }
    }
    
    /// <summary>
    /// Calculate average density for a chunk
    /// </summary>
    private double CalculateAverageChunkDensity(ChunkCoordinate chunk)
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
                    totalDensity += AdvancedGalaxyStatistics.CalculateStellarDensity(r, z, theta);
                    samples++;
                }
            }
        }
        
        return totalDensity / samples;
    }
    
    /// <summary>
    /// Calculate volume of a chunk (accounting for cylindrical geometry)
    /// </summary>
    private double CalculateChunkVolume(ChunkCoordinate chunk)
    {
        var bounds = chunk.GetBounds();
        
        // Volume of cylindrical wedge
        double deltaTheta = bounds.thetaMax - bounds.thetaMin;
        double avgRadius = (bounds.rMin + bounds.rMax) / 2;
        double deltaR = bounds.rMax - bounds.rMin;
        double deltaZ = bounds.zMax - bounds.zMin;
        
        // Approximate volume (more accurate for small chunks)
        return avgRadius * deltaR * deltaTheta * deltaZ;
    }
    
    /// <summary>
    /// Get deterministic seed for a chunk
    /// </summary>
    private int GetChunkSeed(ChunkCoordinate chunk)
    {
        // Create unique seed based on chunk coordinates
        return chunk.RadialIndex * 1000000 + chunk.AngularIndex * 1000 + (chunk.VerticalIndex + 50);
    }
    
    /// <summary>
    /// Find which chunk contains a specific position
    /// </summary>
    public ChunkCoordinate GetChunkForPosition(float x, float y, float z)
    {
        double r = Math.Sqrt(x * x + y * y);
        double theta = Math.Atan2(y, x) * 180 / Math.PI;
        if (theta < 0) theta += 360;
        
        int rIndex = (int)(r / CHUNK_SIZE);
        int thetaIndex = (int)(theta / DEGREES_PER_CHUNK);
        int zIndex = (int)(z / CHUNK_SIZE);
        
        return new ChunkCoordinate(rIndex, thetaIndex, zIndex);
    }
    
    /// <summary>
    /// List neighboring chunks
    /// </summary>
    public List<ChunkCoordinate> GetNeighboringChunks(ChunkCoordinate chunk)
    {
        var neighbors = new List<ChunkCoordinate>();
        
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dtheta = -1; dtheta <= 1; dtheta++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dr == 0 && dtheta == 0 && dz == 0) continue;
                    
                    int r = chunk.RadialIndex + dr;
                    int theta = (chunk.AngularIndex + dtheta + 360) % 360;
                    int z = chunk.VerticalIndex + dz;
                    
                    if (r >= 0 && r < MAX_RADIUS_CHUNKS && z >= -50 && z <= 50)
                    {
                        neighbors.Add(new ChunkCoordinate(r, theta, z));
                    }
                }
            }
        }
        
        return neighbors;
    }
}